using Keepwise.Application.Abstractions;
using Keepwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Keepwise.Application.Ingestion;

public sealed class PrivacyService(IKeepwiseDbContext db, ICurrentUser currentUser)
{
    public async Task<IngestionSettingsDto> GetSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await EnsureAsync(cancellationToken);
        return Map(settings);
    }

    public async Task<IngestionSettingsDto> UpdateSettingsAsync(IngestionSettingsDto request, CancellationToken cancellationToken)
    {
        var settings = await EnsureAsync(cancellationToken);
        settings.ReceiptScanningEnabled = request.ReceiptScanningEnabled;
        settings.SharedTextEnabled = request.SharedTextEnabled;
        settings.EmailScanningEnabled = request.EmailScanningEnabled;
        settings.SmsImportEnabled = request.SmsImportEnabled;
        settings.WhatsAppImportEnabled = request.WhatsAppImportEnabled;
        settings.AiProcessingEnabled = request.AiProcessingEnabled;
        settings.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Map(settings);
    }

    public async Task<PrivacySummaryDto> SummaryAsync(CancellationToken cancellationToken)
    {
        var settings = await EnsureAsync(cancellationToken);
        var pending = await db.PurchaseCandidates.CountAsync(
            c => c.UserId == currentUser.UserId && c.Status == CandidateStatus.PendingReview,
            cancellationToken);
        var docs = await db.IngestionJobs.CountAsync(j => j.UserId == currentUser.UserId, cancellationToken);
        return new PrivacySummaryDto(Map(settings), pending, docs, settings.AiProcessingEnabled);
    }

    private async Task<UserIngestionSettings> EnsureAsync(CancellationToken cancellationToken)
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

    private static IngestionSettingsDto Map(UserIngestionSettings settings) =>
        new(
            settings.ReceiptScanningEnabled,
            settings.SharedTextEnabled,
            settings.EmailScanningEnabled,
            settings.SmsImportEnabled,
            settings.WhatsAppImportEnabled,
            settings.AiProcessingEnabled);
}
