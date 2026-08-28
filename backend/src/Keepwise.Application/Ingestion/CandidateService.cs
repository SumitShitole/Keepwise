using Keepwise.Application.Abstractions;
using Keepwise.Application.Common;
using Keepwise.Application.Items;
using Keepwise.Domain;
using Keepwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Keepwise.Application.Ingestion;

public sealed class CandidateService(IKeepwiseDbContext db, ICurrentUser currentUser, ItemService items, CoverageService coverageService)
{
    public async Task<IReadOnlyList<PurchaseCandidateDto>> ListAsync(CandidateStatus? status, CancellationToken cancellationToken)
    {
        var query = db.PurchaseCandidates.AsNoTracking()
            .Where(c => c.UserId == currentUser.UserId);
        if (status is not null)
        {
            query = query.Where(c => c.Status == status);
        }

        var rows = await query.OrderByDescending(c => c.CreatedAtUtc).Take(50).ToListAsync(cancellationToken);
        return rows.Select(Map).ToList();
    }

    public async Task<PurchaseCandidateDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Map(await RequireAsync(id, cancellationToken));

    public async Task<PurchaseCandidateDto> EditAsync(Guid id, CandidatePayload payload, CancellationToken cancellationToken)
    {
        var candidate = await RequireAsync(id, cancellationToken);
        EnsureEditable(candidate);
        payload.WarrantyProvenance = FieldProvenance.UserProvided;
        candidate.PayloadJson = payload.ToJson();
        candidate.Fingerprint = PurchaseFingerprint.Build(
            currentUser.UserId,
            payload.Vendor,
            payload.OrderNumber,
            payload.InvoiceNumber,
            payload.Amount,
            payload.PurchaseDate,
            payload.ProductName);
        candidate.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Map(candidate);
    }

    public async Task IgnoreAsync(Guid id, CancellationToken cancellationToken)
    {
        var candidate = await RequireAsync(id, cancellationToken);
        EnsureEditable(candidate);
        candidate.Status = CandidateStatus.Ignored;
        candidate.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> ConfirmAsync(Guid id, CancellationToken cancellationToken)
    {
        var candidate = await RequireAsync(id, cancellationToken);
        EnsureEditable(candidate);
        if (candidate.Status == CandidateStatus.Duplicate)
        {
            throw new AppValidationException("This looks like a duplicate. Ignore it, or edit the order number first.");
        }

        var payload = CandidatePayload.Parse(candidate.PayloadJson);
        if (string.IsNullOrWhiteSpace(payload.ProductName))
        {
            throw new AppValidationException("A product name is required to confirm.");
        }

        var created = await items.CreateAsync(
            new CreateItemRequest(
                payload.ProductName,
                null,
                null,
                payload.Brand,
                payload.Model,
                payload.SerialNumber,
                payload.PurchaseDate,
                payload.Amount,
                payload.Currency,
                payload.Vendor,
                null,
                payload.OrderNumber is null ? null : $"Order {payload.OrderNumber}",
                WarrantyFromPayload(payload)),
            cancellationToken);

        if (payload.ReturnWindowDays is > 0)
        {
            await coverageService.AddAsync(created.Id, new CreateCoverageRequest(
                CoverageKind.ReturnWindow,
                "Return window",
                payload.Vendor,
                null,
                payload.PurchaseDate,
                payload.ReturnWindowDays,
                DurationUnit.Days,
                null,
                null,
                null,
                null,
                null,
                [Math.Min(payload.ReturnWindowDays.Value, 7), 1, 0]), cancellationToken);
        }

        var itemEntity = await db.Items.FirstAsync(i => i.Id == created.Id, cancellationToken);
        db.Purchases.Add(new Purchase
        {
            UserId = currentUser.UserId,
            ItemId = itemEntity.Id,
            CandidateId = candidate.Id,
            VendorName = payload.Vendor,
            OrderNumber = payload.OrderNumber,
            InvoiceNumber = payload.InvoiceNumber,
            PurchasedOn = payload.PurchaseDate,
            Amount = payload.Amount,
            Currency = payload.Currency,
            Gstin = payload.Gstin,
            UpiReference = payload.UpiReference,
            ReturnBy = payload.PurchaseDate is not null && payload.ReturnWindowDays is > 0
                ? payload.PurchaseDate.Value.AddDays(payload.ReturnWindowDays.Value)
                : null,
            Fingerprint = candidate.Fingerprint
        });

        candidate.Status = CandidateStatus.Confirmed;
        candidate.ConfirmedItemId = created.Id;
        candidate.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return created.Id;
    }

    private async Task<PurchaseCandidate> RequireAsync(Guid id, CancellationToken cancellationToken)
    {
        var candidate = await db.PurchaseCandidates.FirstOrDefaultAsync(
            c => c.Id == id && c.UserId == currentUser.UserId,
            cancellationToken);
        return candidate ?? throw new NotFoundException("Purchase candidate was not found.");
    }

    private static void EnsureEditable(PurchaseCandidate candidate)
    {
        if (candidate.Status is CandidateStatus.Confirmed or CandidateStatus.Ignored)
        {
            throw new AppValidationException("This candidate can no longer be changed.");
        }
    }

    private static PurchaseCandidateDto Map(PurchaseCandidate candidate) =>
        new(
            candidate.Id,
            candidate.Status,
            candidate.SourceType,
            candidate.OverallConfidence,
            candidate.DuplicateOfId,
            candidate.ConfirmedItemId,
            CandidatePayload.Parse(candidate.PayloadJson),
            candidate.CreatedAtUtc);

    public static CreateCoverageRequest? WarrantyFromPayload(CandidatePayload payload)
    {
        var duration = payload.WarrantyDurationMonths is > 0 ? payload.WarrantyDurationMonths : null;
        var explicitEnd = payload.WarrantyEndDate;
        if (explicitEnd is not null && payload.PurchaseDate is { } start && explicitEnd < start)
        {
            explicitEnd = null;
        }

        if (duration is null && explicitEnd is null)
        {
            return null;
        }

        return new CreateCoverageRequest(
            CoverageKind.Warranty,
            null,
            payload.Vendor,
            payload.OrderNumber,
            payload.PurchaseDate,
            duration,
            duration is null ? null : DurationUnit.Months,
            explicitEnd,
            null,
            null,
            null,
            payload.WarrantyProvenance == FieldProvenance.Estimated ? "Estimated warranty" : null,
            null);
    }
}
