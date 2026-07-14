using CloudCostEngine.Domain;

namespace CloudCostEngine.Application.Validation;

/// <summary>
/// Builds a validated <see cref="CloudPlan"/> from a <see cref="RawPlanInput"/>, or reports
/// every problem it can find in one pass (rather than stopping at the first). This is the
/// Factory pattern applied deliberately: object construction here has real branching logic
/// (missing fields, malformed tiers, cross-field rules) that doesn't belong inside the
/// CloudPlan constructor itself, and the caller needs a "did it work, and if not why"
/// answer rather than an exception.
/// </summary>
public static class PlanFactory
{
    public static (CloudPlan? Plan, IReadOnlyList<string> Errors) TryBuild(RawPlanInput raw, int index)
    {
        var errors = new List<string>();
        var label = DescribePlan(raw, index);

        if (string.IsNullOrWhiteSpace(raw.ProviderName))
            errors.Add($"{label}: 'provider_name' is required.");
        if (string.IsNullOrWhiteSpace(raw.PlanName))
            errors.Add($"{label}: 'plan_name' is required.");
        if (raw.BaseFeeCents is null)
            errors.Add($"{label}: 'base_fee' is required.");
        else if (raw.BaseFeeCents < 0)
            errors.Add($"{label}: 'base_fee' cannot be negative.");

        var storageSchedule = TryBuildSchedule(raw.StoragePrices, "storage_prices", label, errors);
        var computeSchedule = TryBuildSchedule(raw.ComputePrices, "compute_prices", label, errors);

        if (errors.Count > 0 || storageSchedule is null || computeSchedule is null)
            return (null, errors);

        var plan = new CloudPlan(
            raw.ProviderName!,
            raw.PlanName!,
            raw.BaseFeeCents!.Value,
            storageSchedule,
            computeSchedule);

        return (plan, errors);
    }

    private static PricingSchedule? TryBuildSchedule(
        IReadOnlyList<RawTierInput>? rawTiers, string fieldName, string label, List<string> errors)
    {
        if (rawTiers is null || rawTiers.Count == 0)
        {
            errors.Add($"{label}: '{fieldName}' must contain at least one tier.");
            return null;
        }

        var tiers = new List<PricingTier>(rawTiers.Count);
        for (var i = 0; i < rawTiers.Count; i++)
        {
            var raw = rawTiers[i];
            if (raw.Rate is null)
            {
                errors.Add($"{label}: '{fieldName}' tier {i + 1} is missing a 'rate'.");
                return null;
            }
            tiers.Add(new PricingTier(raw.Rate.Value, raw.Threshold));
        }

        try
        {
            return PricingSchedule.Create(tiers, $"{label}: '{fieldName}'");
        }
        catch (ArgumentException ex)
        {
            errors.Add(ex.Message);
            return null;
        }
    }

    private static string DescribePlan(RawPlanInput raw, int index)
    {
        var provider = string.IsNullOrWhiteSpace(raw.ProviderName) ? null : raw.ProviderName;
        var plan = string.IsNullOrWhiteSpace(raw.PlanName) ? null : raw.PlanName;
        return provider is not null && plan is not null
            ? $"Plan #{index + 1} ({provider}, {plan})"
            : $"Plan #{index + 1}";
    }
}
