namespace CloudCostEngine.Application.Validation;

/// <summary>
/// Format-agnostic, "untrusted" representation of one plan entry — every field is nullable
/// because at this point we only know it came from *some* external source (today: JSON),
/// not that it's well-formed. <see cref="PlanFactory"/> is the only thing allowed to turn
/// this into a trusted <c>CloudPlan</c>. Keeping this type in Application (not
/// Infrastructure) means the validation rules don't depend on System.Text.Json at all.
/// </summary>
public sealed class RawPlanInput
{
    public string? ProviderName { get; init; }
    public string? PlanName { get; init; }
    public decimal? BaseFeeCents { get; init; }
    public IReadOnlyList<RawTierInput>? StoragePrices { get; init; }
    public IReadOnlyList<RawTierInput>? ComputePrices { get; init; }
}

public sealed class RawTierInput
{
    public decimal? Rate { get; init; }
    public decimal? Threshold { get; init; }
}
