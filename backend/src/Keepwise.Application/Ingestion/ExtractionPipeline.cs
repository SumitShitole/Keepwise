using System.Text;
using Keepwise.Application.Abstractions;
using Keepwise.Domain;
using Keepwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Keepwise.Application.Ingestion;

public sealed class ExtractionPipeline(
    IKeepwiseDbContext db,
    IHeuristicExtractor heuristic,
    IPdfTextExtractor pdf,
    IOcrProvider ocr,
    ILlmExtractor llm,
    DuplicateDetector duplicates,
    IFileStorage storage)
{
    public async Task ProcessJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await db.IngestionJobs.FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken)
            ?? throw new Common.NotFoundException("Ingestion job was not found.");

        if (job.Status == IngestionJobStatus.Succeeded && job.CandidateId is not null)
        {
            return;
        }

        job.Status = IngestionJobStatus.Running;
        job.AttemptCount++;
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var text = await LoadTextAsync(job, cancellationToken);
            if (string.IsNullOrWhiteSpace(text))
            {
                if (job.ContentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true)
                {
                    await CompleteCandidateAsync(
                        job,
                        EmptyExtraction(),
                        CandidateStatus.NeedsOcr,
                        cancellationToken);
                    return;
                }

                throw new Common.AppValidationException("Could not read any text from the upload.");
            }

            var extracted = heuristic.Extract(text);
            var settings = await db.UserIngestionSettings.FirstOrDefaultAsync(s => s.UserId == job.UserId, cancellationToken);
            if (settings?.AiProcessingEnabled == true && MissingCore(extracted))
            {
                var usedToday = await db.IngestionJobs.Where(j =>
                        j.UserId == job.UserId &&
                        j.CreatedAtUtc >= DateTimeOffset.UtcNow.Date &&
                        j.LlmRequests > 0)
                    .CountAsync(cancellationToken);
                if (usedToday < 20)
                {
                    job.LlmRequests++;
                    var ai = await llm.ExtractAsync(text, cancellationToken);
                    if (ai is not null)
                    {
                        extracted = Merge(extracted, ai);
                    }
                }
            }

            var status = extracted.IsPurchase ? CandidateStatus.PendingReview : CandidateStatus.Failed;
            await CompleteCandidateAsync(job, extracted, status, cancellationToken);
        }
        catch (Exception)
        {
            job.Status = IngestionJobStatus.Failed;
            job.ErrorCode = "processing_failed";
            await db.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private async Task<string?> LoadTextAsync(IngestionJob job, CancellationToken cancellationToken)
    {
        if (job.StorageKey is null)
        {
            return null;
        }

        await using var file = await storage.OpenReadAsync(job.StorageKey, cancellationToken);
        using var memory = new MemoryStream();
        await file.CopyToAsync(memory, cancellationToken);
        var bytes = memory.ToArray();

        if (job.ContentType == "text/plain")
        {
            return Encoding.UTF8.GetString(bytes);
        }

        if (string.Equals(job.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            var pdfText = pdf.Extract(bytes);
            if (!string.IsNullOrWhiteSpace(pdfText))
            {
                return pdfText;
            }
        }

        if (job.ContentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true)
        {
            job.OcrRequests++;
            memory.Position = 0;
            return await ocr.ExtractTextAsync(memory, job.ContentType, cancellationToken);
        }

        return Encoding.UTF8.GetString(bytes);
    }

    private static ExtractedPurchase EmptyExtraction() =>
        new(false, null, null, null, null, null, null, "INR", null, null, null, null, null, null, null, null,
            FieldProvenance.Estimated, 0, new Dictionary<string, double>());

    private static bool MissingCore(ExtractedPurchase extracted) =>
        extracted.ProductName is null || extracted.PurchaseDate is null || extracted.Amount is null;

    private static ExtractedPurchase Merge(ExtractedPurchase primary, ExtractedPurchase secondary) =>
        primary with
        {
            Vendor = primary.Vendor ?? secondary.Vendor,
            ProductName = primary.ProductName ?? secondary.ProductName,
            Brand = primary.Brand ?? secondary.Brand,
            PurchaseDate = primary.PurchaseDate ?? secondary.PurchaseDate,
            Amount = primary.Amount ?? secondary.Amount,
            OrderNumber = primary.OrderNumber ?? secondary.OrderNumber,
            WarrantyDurationMonths = primary.WarrantyDurationMonths ?? secondary.WarrantyDurationMonths,
            Gstin = primary.Gstin ?? secondary.Gstin,
            UpiReference = primary.UpiReference ?? secondary.UpiReference,
            OverallConfidence = Math.Max(primary.OverallConfidence, secondary.OverallConfidence)
        };

    private async Task CompleteCandidateAsync(
        IngestionJob job,
        ExtractedPurchase extracted,
        CandidateStatus status,
        CancellationToken cancellationToken)
    {
        var fingerprint = PurchaseFingerprint.Build(
            job.UserId,
            extracted.Vendor,
            extracted.OrderNumber,
            extracted.InvoiceNumber,
            extracted.Amount,
            extracted.PurchaseDate,
            extracted.ProductName);

        var (isDup, otherId) = await duplicates.FindAsync(job.UserId, fingerprint, job.CandidateId, cancellationToken);
        if (isDup)
        {
            status = CandidateStatus.Duplicate;
        }

        PurchaseCandidate candidate;
        if (job.CandidateId is Guid existingId)
        {
            candidate = await db.PurchaseCandidates.FirstAsync(c => c.Id == existingId, cancellationToken);
            candidate.Status = status;
            candidate.Fingerprint = fingerprint;
            candidate.DuplicateOfId = otherId;
            candidate.OverallConfidence = extracted.OverallConfidence;
            candidate.PayloadJson = CandidatePayload.From(extracted).ToJson();
            candidate.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }
        else
        {
            candidate = new PurchaseCandidate
            {
                UserId = job.UserId,
                JobId = job.Id,
                SourceType = job.SourceType,
                Status = status,
                StorageKey = job.StorageKey,
                Sha256 = job.Sha256,
                Fingerprint = fingerprint,
                DuplicateOfId = otherId,
                OverallConfidence = extracted.OverallConfidence,
                PayloadJson = CandidatePayload.From(extracted).ToJson()
            };
            db.PurchaseCandidates.Add(candidate);
            await db.SaveChangesAsync(cancellationToken);
            job.CandidateId = candidate.Id;
        }

        job.Status = IngestionJobStatus.Succeeded;
        job.ErrorCode = status == CandidateStatus.Failed ? "not_a_purchase" : null;
        await db.SaveChangesAsync(cancellationToken);
    }
}
