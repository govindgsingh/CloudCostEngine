using CloudCostEngine.Application.Services;
using CloudCostEngine.Domain;
using Xunit;

namespace CloudCostEngine.Tests;

public class MonthlyCostCalculatorTests
{
    private static CloudPlan NimbusEnterpriseHybrid() => new(
        providerName: "NimbusCloud",
        planName: "EnterpriseHybrid",
        baseFeeCents: 1000m,
        storagePrices: PricingSchedule.Create(new[]
        {
            new PricingTier(4.0m, 500m),
            new PricingTier(2.0m, null),
        }, "storage_prices"),
        computePrices: PricingSchedule.Create(new[]
        {
            new PricingTier(15.0m, 50m),
            new PricingTier(10.0m, null),
        }, "compute_prices"));

    [Fact]
    public void SpecWorkedExample_1200Gb_MatchesDocumentedTotal()
    {
        var calculator = new MonthlyCostCalculator();
        var plan = NimbusEnterpriseHybrid();

        var result = calculator.Calculate(plan, 1200m);

        Assert.Equal(120m, result.ComputeHours);
        Assert.Equal(3400m, result.StorageCostCents);   // 500*4.0 + 700*2.0
        Assert.Equal(1450m, result.ComputeCostCents);   // 50*15.0 + 70*10.0
        Assert.Equal(5850m, result.SubtotalCents);       // + 1000 base fee
        Assert.Equal(6435m, result.TotalCentsInclTax);   // * 1.10
        Assert.Equal(64.35m, result.TotalDollarsRounded);
    }

    [Fact]
    public void ComputeHours_AreDerivedFromStorageAtTenToOneRatio()
    {
        var calculator = new MonthlyCostCalculator();
        var plan = NimbusEnterpriseHybrid();

        var result = calculator.Calculate(plan, 1205m);

        Assert.Equal(120.5m, result.ComputeHours);
    }

    [Fact]
    public void ZeroStorage_OnlyChargesTheBaseFeePlusTax()
    {
        var calculator = new MonthlyCostCalculator();
        var plan = NimbusEnterpriseHybrid();

        var result = calculator.Calculate(plan, 0m);

        Assert.Equal(0m, result.StorageCostCents);
        Assert.Equal(0m, result.ComputeCostCents);
        Assert.Equal(1000m, result.SubtotalCents);
        Assert.Equal(11.00m, result.TotalDollarsRounded); // 1000c * 1.10 = 1100c = $11.00
    }

    [Fact]
    public void NegativeStorage_Throws()
    {
        var calculator = new MonthlyCostCalculator();
        var plan = NimbusEnterpriseHybrid();

        Assert.Throws<ArgumentOutOfRangeException>(() => calculator.Calculate(plan, -50m));
    }

    [Fact]
    public void SmallUsage_RoundsCorrectlyToTheNearestCent()
    {
        // 100 GB -> 10 compute hours.
        // Storage: 100 * 4.0 = 400c. Compute: 10 * 15.0 = 150c. Base fee: 1000c.
        // Subtotal: 1550c. Total incl. tax: 1550 * 1.10 = 1705c = $17.05 exactly.
        var calculator = new MonthlyCostCalculator();
        var plan = NimbusEnterpriseHybrid();

        var result = calculator.Calculate(plan, 100m);

        Assert.Equal(1705m, result.TotalCentsInclTax);
        Assert.Equal(17.05m, result.TotalDollarsRounded);
    }
}
