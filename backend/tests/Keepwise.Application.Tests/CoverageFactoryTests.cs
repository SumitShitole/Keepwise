using Keepwise.Application.Items;
using Keepwise.Domain;
using FluentAssertions;

namespace Keepwise.Application.Tests;

public class CoverageFactoryTests
{
    private sealed class FrozenClock(DateTimeOffset utc) : Keepwise.Application.Abstractions.IClock
    {
        public DateTimeOffset UtcNow { get; } = utc;
    }

    [Fact]
    public void Creates_warranty_from_duration_and_default_offsets()
    {
        var clock = new FrozenClock(new DateTimeOffset(2027, 3, 15, 12, 0, 0, TimeSpan.Zero));
        var factory = new CoverageFactory(clock);
        var item = new Keepwise.Domain.Entities.Item { Name = "Samsung Washing Machine", PurchaseDate = new DateOnly(2027, 3, 15) };
        var coverage = factory.Create(item, new CreateCoverageRequest(
            CoverageKind.Warranty, null, null, null, null, 2, DurationUnit.Years, null, null, null, null, null, null));

        coverage.EndDate.Should().Be(new DateOnly(2029, 3, 15));
        coverage.ReminderRules.Select(r => r.OffsetValue).Should().Equal(90, 30, 15, 7, 1, 0);
        coverage.Status.Should().Be(CoverageStatus.Active);
    }
}
