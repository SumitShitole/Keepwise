using Keepwise.Application.Abstractions;
using Keepwise.Domain;
using Microsoft.EntityFrameworkCore;

namespace Keepwise.Application.Ingestion;

public sealed class DuplicateDetector(IKeepwiseDbContext db)
{
    public async Task<(bool IsDuplicate, Guid? OtherId)> FindAsync(
        Guid userId,
        string fingerprint,
        Guid? exceptCandidateId,
        CancellationToken cancellationToken)
    {
        var otherCandidate = await db.PurchaseCandidates.AsNoTracking()
            .Where(c => c.UserId == userId && c.Fingerprint == fingerprint && c.Status != CandidateStatus.Ignored)
            .Where(c => exceptCandidateId == null || c.Id != exceptCandidateId)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (otherCandidate is not null)
        {
            return (true, otherCandidate);
        }

        var purchase = await db.Purchases.AsNoTracking()
            .Where(p => p.UserId == userId && p.Fingerprint == fingerprint)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return purchase is null ? (false, null) : (true, purchase);
    }
}
