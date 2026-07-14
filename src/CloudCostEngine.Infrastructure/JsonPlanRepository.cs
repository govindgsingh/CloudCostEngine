using System.Text.Json;
using CloudCostEngine.Application.Interfaces;
using CloudCostEngine.Application.Validation;
using CloudCostEngine.Domain;
using CloudCostEngine.Infrastructure.Dtos;

namespace CloudCostEngine.Infrastructure;

/// <summary>
/// Reads a plan list from a JSON file on disk. This is the only class in the whole
/// solution that knows plans currently live in JSON files — everything upstream
/// (Application, Cli) only ever sees <see cref="IPlanRepository"/>.
/// </summary>
public sealed class JsonPlanRepository : IPlanRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public PlanLoadResult Load(string path)
    {
        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (FileNotFoundException)
        {
            return PlanLoadResult.Failed(new[] { $"File not found: '{path}'." });
        }
        catch (DirectoryNotFoundException)
        {
            return PlanLoadResult.Failed(new[] { $"Directory not found for path: '{path}'." });
        }
        catch (UnauthorizedAccessException)
        {
            return PlanLoadResult.Failed(new[] { $"Access denied reading file: '{path}'." });
        }
        catch (IOException ex)
        {
            return PlanLoadResult.Failed(new[] { $"Could not read file '{path}': {ex.Message}" });
        }

        List<PlanDto>? dtos;
        try
        {
            dtos = JsonSerializer.Deserialize<List<PlanDto>>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            return PlanLoadResult.Failed(new[] { $"'{path}' is not valid JSON: {ex.Message}" });
        }

        if (dtos is null || dtos.Count == 0)
            return PlanLoadResult.Failed(new[] { $"'{path}' does not contain any plans." });

        var plans = new List<CloudPlan>();
        var warnings = new List<string>();

        for (var i = 0; i < dtos.Count; i++)
        {
            var raw = MapToRawInput(dtos[i]);
            var (plan, errors) = PlanFactory.TryBuild(raw, i);

            if (plan is not null)
                plans.Add(plan);
            else
                warnings.AddRange(errors);
        }

        return PlanLoadResult.Ok(plans, warnings);
    }

    private static RawPlanInput MapToRawInput(PlanDto dto) => new()
    {
        ProviderName = dto.ProviderName,
        PlanName = dto.PlanName,
        BaseFeeCents = dto.BaseFee,
        StoragePrices = dto.StoragePrices?
            .Select(t => new RawTierInput { Rate = t.Rate, Threshold = t.Threshold })
            .ToList(),
        ComputePrices = dto.ComputePrices?
            .Select(t => new RawTierInput { Rate = t.Rate, Threshold = t.Threshold })
            .ToList(),
    };
}
