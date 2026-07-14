using CloudCostEngine.Application.Interfaces;
using CloudCostEngine.Application.Models;
using CloudCostEngine.Domain;

namespace CloudCostEngine.Application.Services;

/// <summary>
/// Forward evaluation: given a plan and a monthly storage figure, derive compute hours
/// (spec §3.1), price both resources through <see cref="TieredPricingCalculator"/>, add the
/// base fee, apply the flat 10% Technology Service Tax (spec §3.3), and round to whole
/// cents/dollars using round-half-up — the specific rounding mode the spec calls for, which
/// is *not* the .NET default (banker's rounding).
/// </summary>
public sealed class MonthlyCostCalculator : ICostCalculator
{
    private const decimal GbPerComputeHour = 10m;
    private const decimal TaxRate = 0.10m;

    public CostResult Calculate(CloudPlan plan, decimal storageGb)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (storageGb < 0)
            throw new ArgumentOutOfRangeException(nameof(storageGb), "Storage consumption cannot be negative.");

        var computeHours = storageGb / GbPerComputeHour;

        var storageCostCents = TieredPricingCalculator.CalculateCost(storageGb, plan.StoragePrices);
        var computeCostCents = TieredPricingCalculator.CalculateCost(computeHours, plan.ComputePrices);

        var subtotalCents = plan.BaseFeeCents + storageCostCents + computeCostCents;
        var totalCentsInclTax = subtotalCents * (1 + TaxRate);

        // Round-half-up in dollars, per spec §6.2. MidpointRounding.AwayFromZero is
        // equivalent to round-half-up for the non-negative amounts this domain deals in.
        var totalDollars = totalCentsInclTax / 100m;
        var totalDollarsRounded = Math.Round(totalDollars, 2, MidpointRounding.AwayFromZero);

        return new CostResult(
            plan.ProviderName,
            plan.PlanName,
            storageGb,
            computeHours,
            storageCostCents,
            computeCostCents,
            plan.BaseFeeCents,
            subtotalCents,
            totalCentsInclTax,
            totalDollarsRounded);
    }
}
