namespace CloudCostEngine.Domain;

/// <summary>
/// A single marginal-rate band within a tiered pricing structure (storage or compute).
/// </summary>
/// <param name="Rate">Cost per unit, in cents, exclusive of tax.</param>
/// <param name="Threshold">
/// The cumulative amount of the resource — up to and including which — this rate applies.
/// <c>null</c> means "no ceiling": this tier absorbs all remaining consumption.
/// Only the last tier in a structure is allowed to have a null threshold.
/// </param>
public sealed record PricingTier(decimal Rate, decimal? Threshold)
{
    public bool IsUnlimited => Threshold is null;
}
