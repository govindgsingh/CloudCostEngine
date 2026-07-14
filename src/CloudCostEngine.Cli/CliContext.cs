using CloudCostEngine.Application.Interfaces;
using CloudCostEngine.Domain;

namespace CloudCostEngine.Cli;

/// <summary>
/// Session state shared across commands within one run of the interactive loop: which
/// plans are currently loaded, and whether the loop should keep running. Kept intentionally
/// tiny — this is a CLI session, not an application-wide state bag.
/// </summary>
public sealed class CliContext
{
    public IReadOnlyList<CloudPlan> LoadedPlans { get; set; } = Array.Empty<CloudPlan>();
    public bool HasLoadedPlans => LoadedPlans.Count > 0;
    public bool IsRunning { get; set; } = true;

    public IPlanRepository PlanRepository { get; }
    public ICostCalculator CostCalculator { get; }

    public CliContext(IPlanRepository planRepository, ICostCalculator costCalculator)
    {
        PlanRepository = planRepository;
        CostCalculator = costCalculator;
    }
}
