using CloudCostEngine.Application.Validation;
using Xunit;

namespace CloudCostEngine.Tests;

public class PlanFactoryTests
{
    private static RawPlanInput ValidPlan() => new()
    {
        ProviderName = "NimbusCloud",
        PlanName = "EnterpriseHybrid",
        BaseFeeCents = 1000m,
        StoragePrices = new[]
        {
            new RawTierInput { Rate = 4.0m, Threshold = 500m },
            new RawTierInput { Rate = 2.0m, Threshold = null },
        },
        ComputePrices = new[]
        {
            new RawTierInput { Rate = 15.0m, Threshold = 50m },
            new RawTierInput { Rate = 10.0m, Threshold = null },
        },
    };

    [Fact]
    public void ValidPlan_BuildsSuccessfully()
    {
        var (plan, errors) = PlanFactory.TryBuild(ValidPlan(), 0);

        Assert.NotNull(plan);
        Assert.Empty(errors);
        Assert.Equal("NimbusCloud", plan!.ProviderName);
    }

    [Fact]
    public void MissingProviderName_ProducesValidationError()
    {
        var raw = ValidPlan();
        raw = new RawPlanInput
        {
            ProviderName = null,
            PlanName = raw.PlanName,
            BaseFeeCents = raw.BaseFeeCents,
            StoragePrices = raw.StoragePrices,
            ComputePrices = raw.ComputePrices,
        };

        var (plan, errors) = PlanFactory.TryBuild(raw, 0);

        Assert.Null(plan);
        Assert.Contains(errors, e => e.Contains("provider_name"));
    }

    [Fact]
    public void NonFinalTierMissingThreshold_ProducesValidationError()
    {
        var raw = ValidPlan();
        raw = new RawPlanInput
        {
            ProviderName = raw.ProviderName,
            PlanName = raw.PlanName,
            BaseFeeCents = raw.BaseFeeCents,
            StoragePrices = new[]
            {
                new RawTierInput { Rate = 4.0m, Threshold = null }, // not last, but has no threshold
                new RawTierInput { Rate = 2.0m, Threshold = null },
            },
            ComputePrices = raw.ComputePrices,
        };

        var (plan, errors) = PlanFactory.TryBuild(raw, 0);

        Assert.Null(plan);
        Assert.Contains(errors, e => e.Contains("storage_prices") && e.Contains("threshold"));
    }

    [Fact]
    public void EmptyTierArray_ProducesValidationError()
    {
        var raw = ValidPlan();
        raw = new RawPlanInput
        {
            ProviderName = raw.ProviderName,
            PlanName = raw.PlanName,
            BaseFeeCents = raw.BaseFeeCents,
            StoragePrices = Array.Empty<RawTierInput>(),
            ComputePrices = raw.ComputePrices,
        };

        var (plan, errors) = PlanFactory.TryBuild(raw, 0);

        Assert.Null(plan);
        Assert.Contains(errors, e => e.Contains("storage_prices") && e.Contains("at least one tier"));
    }

    [Fact]
    public void NegativeBaseFee_ProducesValidationError()
    {
        var raw = ValidPlan();
        raw = new RawPlanInput
        {
            ProviderName = raw.ProviderName,
            PlanName = raw.PlanName,
            BaseFeeCents = -100m,
            StoragePrices = raw.StoragePrices,
            ComputePrices = raw.ComputePrices,
        };

        var (plan, errors) = PlanFactory.TryBuild(raw, 0);

        Assert.Null(plan);
        Assert.Contains(errors, e => e.Contains("base_fee"));
    }

    [Fact]
    public void NonIncreasingThresholds_ProducesValidationError()
    {
        var raw = ValidPlan();
        raw = new RawPlanInput
        {
            ProviderName = raw.ProviderName,
            PlanName = raw.PlanName,
            BaseFeeCents = raw.BaseFeeCents,
            StoragePrices = new[]
            {
                new RawTierInput { Rate = 4.0m, Threshold = 500m },
                new RawTierInput { Rate = 3.0m, Threshold = 400m }, // lower than previous — invalid
                new RawTierInput { Rate = 2.0m, Threshold = null },
            },
            ComputePrices = raw.ComputePrices,
        };

        var (plan, errors) = PlanFactory.TryBuild(raw, 0);

        Assert.Null(plan);
        Assert.Contains(errors, e => e.Contains("greater than the previous tier's threshold"));
    }

    [Fact]
    public void MultipleProblems_AreAllReportedTogether()
    {
        var raw = new RawPlanInput
        {
            ProviderName = null,
            PlanName = null,
            BaseFeeCents = null,
            StoragePrices = Array.Empty<RawTierInput>(),
            ComputePrices = Array.Empty<RawTierInput>(),
        };

        var (plan, errors) = PlanFactory.TryBuild(raw, 0);

        Assert.Null(plan);
        // provider_name, plan_name, base_fee, storage_prices, compute_prices — 5 independent problems.
        Assert.Equal(5, errors.Count);
    }
}
