using FluentAssertions;
using Keepwise.Application.Ingestion;
using Keepwise.Domain;

namespace Keepwise.Application.Tests;

public class CandidateWarrantyTests
{
    [Fact]
    public void Drops_extracted_expiry_when_it_is_before_purchase_date()
    {
        var payload = new CandidatePayload
        {
            Vendor = "Amazon",
            PurchaseDate = new DateOnly(2026, 8, 14),
            WarrantyDurationMonths = 12,
            WarrantyEndDate = new DateOnly(2026, 1, 1)
        };

        var coverage = CandidateService.WarrantyFromPayload(payload);

        coverage.Should().NotBeNull();
        coverage!.DurationValue.Should().Be(12);
        coverage.DurationUnit.Should().Be(DurationUnit.Months);
        coverage.ExplicitEndDate.Should().BeNull();
    }

    [Fact]
    public void Skips_warranty_when_only_expiry_is_before_purchase_date()
    {
        var payload = new CandidatePayload
        {
            PurchaseDate = new DateOnly(2026, 8, 14),
            WarrantyEndDate = new DateOnly(2026, 1, 1)
        };

        CandidateService.WarrantyFromPayload(payload).Should().BeNull();
    }
}
