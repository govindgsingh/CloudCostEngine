namespace CloudCostEngine.Domain;

/// <summary>
/// A fully-validated cloud provider plan: a monthly base fee plus independent tiered
/// pricing schedules for storage and compute. This is the aggregate root of the domain —
/// once constructed, an instance is guaranteed internally consistent (see
/// <see cref="PricingSchedule"/>), so no other layer needs to re-validate it.
/// </summary>
public sealed class CloudPlan
{
    public string ProviderName { get; }
    public string PlanName { get; }
    public decimal BaseFeeCents { get; }
    public PricingSchedule StoragePrices { get; }
    public PricingSchedule ComputePrices { get; }

    public CloudPlan(
        string providerName,
        string planName,
        decimal baseFeeCents,
        PricingSchedule storagePrices,
        PricingSchedule computePrices)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            throw new ArgumentException("Provider name is required.", nameof(providerName));
        if (string.IsNullOrWhiteSpace(planName))
            throw new ArgumentException("Plan name is required.", nameof(planName));
        if (baseFeeCents < 0)
            throw new ArgumentException("Base fee cannot be negative.", nameof(baseFeeCents));

        ProviderName = providerName;
        PlanName = planName;
        BaseFeeCents = baseFeeCents;
        StoragePrices = storagePrices ?? throw new ArgumentNullException(nameof(storagePrices));
        ComputePrices = computePrices ?? throw new ArgumentNullException(nameof(computePrices));
    }

    public string DisplayName => $"{ProviderName}, {PlanName}";
}
