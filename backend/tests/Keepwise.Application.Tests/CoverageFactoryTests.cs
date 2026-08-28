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

    [Fact]
    public void Creates_return_window_from_duration_in_days()
    {
        var clock = new FrozenClock(new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.Zero));
        var factory = new CoverageFactory(clock);
        var item = new Keepwise.Domain.Entities.Item { Name = "Mixer", PurchaseDate = new DateOnly(2026, 8, 14) };
        var coverage = factory.Create(item, new CreateCoverageRequest(
            CoverageKind.ReturnWindow, "Return window", "Amazon", null, new DateOnly(2026, 8, 14), 7, DurationUnit.Days, null, null, null, null, null, [7, 1, 0]));

        coverage.EndDate.Should().Be(new DateOnly(2026, 8, 21));
        coverage.Kind.Should().Be(CoverageKind.ReturnWindow);
    }
}
