using CloudCostEngine.Domain;

namespace CloudCostEngine.Application.Services;

/// <summary>
/// Pure function that prices a quantity of a resource against a <see cref="PricingSchedule"/>,
/// filling tiers from the bottom up — exactly like a tax bracket. Storage and compute are
/// structurally identical pricing problems (spec §3.2), so both are priced through this one
/// class instead of two near-duplicate implementations. This is the single place that would
/// need to change if the marginal-tier rule itself ever changed.
/// </summary>
public static class TieredPricingCalculator
{
    /// <summary>
    /// Returns the total cost, in cents, of consuming <paramref name="quantity"/> units of a
    /// resource priced under <paramref name="schedule"/>.
    /// </summary>
    public static decimal CalculateCost(decimal quantity, PricingSchedule schedule)
    {
        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Consumption cannot be negative.");

        var remaining = quantity;
        var previousThreshold = 0m;
        var totalCost = 0m;

        foreach (var tier in schedule.Tiers)
        {
            if (remaining <= 0)
                break;

            // The final tier is always treated as unlimited, even if the input data
            // included a (spec-discouraged) threshold on it — see PricingSchedule for the
            // documented assumption.
            var tierCapacity = tier.Threshold is decimal threshold
                ? threshold - previousThreshold
                : remaining;

            var quantityInTier = Math.Min(remaining, tierCapacity);
            totalCost += quantityInTier * tier.Rate;
            remaining -= quantityInTier;

            if (tier.Threshold is decimal t)
                previousThreshold = t;
        }

        // Defensive guard: PricingSchedule.Create already guarantees the last tier absorbs
        // everything, so this should be unreachable. It exists so a future change to the
        // schedule invariants fails loudly instead of silently under-billing a customer.
        if (remaining > 0)
        {
            throw new InvalidOperationException(
                "Consumption exceeds every defined pricing tier and no unlimited final tier absorbed the remainder.");
        }

        return totalCost;
    }
}
