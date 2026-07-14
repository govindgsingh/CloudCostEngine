using CloudCostEngine.Application.Services;
using CloudCostEngine.Domain;
using Xunit;

namespace CloudCostEngine.Tests;

public class TieredPricingCalculatorTests
{
    [Fact]
    public void SingleTier_ChargesFlatRateForAllUnits()
    {
        var schedule = PricingSchedule.Create(new[] { new PricingTier(5.0m, null) }, "test");

        var cost = TieredPricingCalculator.CalculateCost(100m, schedule);

        Assert.Equal(500m, cost); // 100 * 5.0
    }

    [Fact]
    public void MultiTier_SpecWorkedIllustration_ChargesMarginally()
    {
        // From spec §3.2: 1,200 GB against [4.0/500, 3.0/1000, 2.0/unlimited] = 3,900c
        var schedule = PricingSchedule.Create(new[]
        {
            new PricingTier(4.0m, 500m),
            new PricingTier(3.0m, 1000m),
            new PricingTier(2.0m, null),
        }, "storage_prices");

        var cost = TieredPricingCalculator.CalculateCost(1200m, schedule);

        Assert.Equal(3900m, cost);
    }

    [Fact]
    public void MultiTier_ExactlyAtAThreshold_DoesNotSpillIntoNextTier()
    {
        var schedule = PricingSchedule.Create(new[]
        {
            new PricingTier(4.0m, 500m),
            new PricingTier(2.0m, null),
        }, "storage_prices");

        var cost = TieredPricingCalculator.CalculateCost(500m, schedule);

        Assert.Equal(2000m, cost); // 500 * 4.0, none at the 2.0 rate
    }

    [Fact]
    public void FractionalQuantity_IsPricedProportionally()
    {
        // 1,205 GB -> 120.5 compute hours is the spec's own example of fractional consumption.
        var schedule = PricingSchedule.Create(new[] { new PricingTier(10.0m, null) }, "compute_prices");

        var cost = TieredPricingCalculator.CalculateCost(120.5m, schedule);

        Assert.Equal(1205.0m, cost);
    }

    [Fact]
    public void ZeroQuantity_CostsNothing()
    {
        var schedule = PricingSchedule.Create(new[] { new PricingTier(4.0m, 500m), new PricingTier(2.0m, null) }, "storage_prices");

        var cost = TieredPricingCalculator.CalculateCost(0m, schedule);

        Assert.Equal(0m, cost);
    }

    [Fact]
    public void NegativeQuantity_Throws()
    {
        var schedule = PricingSchedule.Create(new[] { new PricingTier(4.0m, null) }, "storage_prices");

        Assert.Throws<ArgumentOutOfRangeException>(() => TieredPricingCalculator.CalculateCost(-1m, schedule));
    }
}
