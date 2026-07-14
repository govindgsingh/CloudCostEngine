namespace CloudCostEngine.Domain;

/// <summary>
/// An ordered, validated set of <see cref="PricingTier"/>s that together describe how one
/// resource (storage or compute) is priced. Building an instance guarantees the domain
/// invariants from spec §3.2 hold — callers never need to re-check them.
/// </summary>
public sealed class PricingSchedule
{
    public IReadOnlyList<PricingTier> Tiers { get; }

    private PricingSchedule(IReadOnlyList<PricingTier> tiers)
    {
        Tiers = tiers;
    }

    /// <summary>
    /// Creates a schedule, enforcing: at least one tier, every tier except the last has a
    /// (strictly increasing, positive) threshold, the last tier may be unlimited, and no
    /// rate is negative. Throws <see cref="ArgumentException"/> with a human-readable
    /// message on the first rule violation — the caller (PlanValidator) turns these into
    /// user-facing validation errors instead of crashing the process.
    /// </summary>
    public static PricingSchedule Create(IReadOnlyList<PricingTier> tiers, string structureName)
    {
        if (tiers is null || tiers.Count == 0)
            throw new ArgumentException($"{structureName} must contain at least one pricing tier.");

        decimal previousThreshold = 0;
        for (var i = 0; i < tiers.Count; i++)
        {
            var tier = tiers[i];
            var isLast = i == tiers.Count - 1;

            if (tier.Rate < 0)
                throw new ArgumentException($"{structureName} tier {i + 1} has a negative rate ({tier.Rate}).");

            if (!isLast)
            {
                if (tier.Threshold is null)
                    throw new ArgumentException(
                        $"{structureName} tier {i + 1} is missing a threshold — only the final tier may be unlimited.");

                if (tier.Threshold <= previousThreshold)
                    throw new ArgumentException(
                        $"{structureName} tier {i + 1} threshold ({tier.Threshold}) must be greater than the previous tier's threshold ({previousThreshold}).");

                previousThreshold = tier.Threshold.Value;
            }
            else if (tier.Threshold is not null && tier.Threshold <= previousThreshold)
            {
                throw new ArgumentException(
                    $"{structureName} final tier threshold ({tier.Threshold}) must be greater than the previous tier's threshold ({previousThreshold}).");
            }
        }

        return new PricingSchedule(tiers);
    }
}
