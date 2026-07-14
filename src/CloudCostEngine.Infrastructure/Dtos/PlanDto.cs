using System.Text.Json.Serialization;

namespace CloudCostEngine.Infrastructure.Dtos;

/// <summary>
/// Shape of one entry in the plans JSON array (spec §6.1), field-for-field. Kept separate
/// from <c>RawPlanInput</c> so a future switch to XML/YAML/a REST API only requires a new
/// mapper, never a change to validation rules.
/// </summary>
public sealed class PlanDto
{
    [JsonPropertyName("provider_name")]
    public string? ProviderName { get; set; }

    [JsonPropertyName("plan_name")]
    public string? PlanName { get; set; }

    [JsonPropertyName("base_fee")]
    public decimal? BaseFee { get; set; }

    [JsonPropertyName("storage_prices")]
    public List<PricingTierDto>? StoragePrices { get; set; }

    [JsonPropertyName("compute_prices")]
    public List<PricingTierDto>? ComputePrices { get; set; }
}

public sealed class PricingTierDto
{
    [JsonPropertyName("rate")]
    public decimal? Rate { get; set; }

    [JsonPropertyName("threshold")]
    public decimal? Threshold { get; set; }
}
