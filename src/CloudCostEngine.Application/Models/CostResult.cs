namespace CloudCostEngine.Application.Models;

/// <summary>
/// The result of pricing one plan for one usage level. Cents are kept alongside the
/// rounded dollar figure so tests (and future features like sorting/diffing) can work with
/// exact values instead of re-parsing formatted strings.
/// </summary>
public sealed record CostResult(
    string ProviderName,
    string PlanName,
    decimal StorageGb,
    decimal ComputeHours,
    decimal StorageCostCents,
    decimal ComputeCostCents,
    decimal BaseFeeCents,
    decimal SubtotalCents,
    decimal TotalCentsInclTax,
    decimal TotalDollarsRounded);
