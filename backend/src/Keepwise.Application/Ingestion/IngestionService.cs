using System.Security.Cryptography;
using Keepwise.Application.Abstractions;
using Keepwise.Application.Common;
using Keepwise.Domain;
using Keepwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Keepwise.Application.Ingestion;

public sealed class IngestionService(
    IKeepwiseDbContext db,
    ICurrentUser currentUser,
    IFileStorage storage,
    ExtractionPipeline pipeline,
    IEnumerable<IPurchaseSource> sources)
{
    public async Task<IngestAcceptedDto> IngestTextAsync(string text, IngestionSourceType sourceType, CancellationToken cancellationToken)
    {
        var settings = await EnsureSettingsAsync(cancellationToken);
        RequireEnabled(sourceType == IngestionSourceType.Document ? IngestionSourceType.Document : IngestionSourceType.SharedText, settings);

        if (string.IsNullOrWhiteSpace(text) || text.Length > 50_000)
        {
            throw new AppValidationException("Provide between 1 and 50,000 characters.");
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        await using var stream = new MemoryStream(bytes);
        var key = await storage.SaveAsync(stream, "message.txt", "text/plain", cancellationToken);
        return await QueueAsync(sourceType, "text/plain", key, Convert.ToHexString(SHA256.HashData(bytes)), cancellationToken);
    }

    public async Task<IngestAcceptedDto> IngestDocumentAsync(string fileName, string contentType, Stream content, long length, CancellationToken cancellationToken)
    {
        var settings = await EnsureSettingsAsync(cancellationToken);
        RequireEnabled(IngestionSourceType.Document, settings);

        if (length <= 0 || length > Documents.DocumentService.MaxFileBytes)
        {
            throw new AppValidationException("File must be between 1 byte and 10 MB.");
        }

        contentType = NormalizeContentType(fileName, contentType);
        if (!Documents.DocumentService.AllowedContentTypes.Contains(contentType) && contentType != "text/plain")
        {
            throw new AppValidationException("Allowed types: PDF, JPEG, PNG, WebP.");
        }

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();
        buffer.Position = 0;
        var key = await storage.SaveAsync(buffer, fileName, contentType, cancellationToken);
        return await QueueAsync(IngestionSourceType.Document, contentType, key, Convert.ToHexString(SHA256.HashData(bytes)), cancellationToken);
    }

    private async Task<IngestAcceptedDto> QueueAsync(
        IngestionSourceType sourceType,
        string contentType,
        string storageKey,
        string sha256,
        CancellationToken cancellationToken)
    {
        var existing = await db.IngestionJobs.FirstOrDefaultAsync(
            j => j.UserId == currentUser.UserId && j.Sha256 == sha256 && j.Status != IngestionJobStatus.Failed,
            cancellationToken);
        if (existing?.CandidateId is not null)
        {
            var existingCandidate = await db.PurchaseCandidates.FirstAsync(c => c.Id == existing.CandidateId, cancellationToken);
            return new IngestAcceptedDto(existing.Id, existing.CandidateId, existingCandidate.Status);
        }

        var job = new IngestionJob
        {
            UserId = currentUser.UserId,
            SourceType = sourceType,
            ContentType = contentType,
            StorageKey = storageKey,
            Sha256 = sha256,
            Status = IngestionJobStatus.Queued
        };
        db.IngestionJobs.Add(job);
        await db.SaveChangesAsync(cancellationToken);
        await pipeline.ProcessJobAsync(job.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        var processed = await db.IngestionJobs.FirstAsync(j => j.Id == job.Id, cancellationToken);
        var status = processed.CandidateId is null
            ? CandidateStatus.Failed
            : (await db.PurchaseCandidates.FirstAsync(c => c.Id == processed.CandidateId, cancellationToken)).Status;
        return new IngestAcceptedDto(processed.Id, processed.CandidateId, status);
    }

    private async Task<UserIngestionSettings> EnsureSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await db.UserIngestionSettings.FirstOrDefaultAsync(s => s.UserId == currentUser.UserId, cancellationToken);
        if (settings is not null)
        {
            return settings;
        }

        settings = new UserIngestionSettings { UserId = currentUser.UserId };
        db.UserIngestionSettings.Add(settings);
        await db.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private void RequireEnabled(IngestionSourceType sourceType, UserIngestionSettings settings)
    {
        var source = sources.FirstOrDefault(s => s.SourceType == sourceType)
            ?? throw new AppValidationException("This import source is not supported.");
        if (!source.IsEnabled(settings))
        {
            throw new AppValidationException("This import source is disabled in privacy settings.");
        }
    }

    private static string NormalizeContentType(string fileName, string? contentType)
    {
        if (!string.IsNullOrWhiteSpace(contentType) &&
            !contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase))
        {
            return contentType;
        }

        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".txt" => "text/plain",
            _ => contentType ?? string.Empty
        };
    }
}
