using CloudCostEngine.Domain;

namespace CloudCostEngine.Application.Interfaces;

/// <summary>
/// Loads validated <see cref="CloudPlan"/>s from some source. The Application layer only
/// knows about this abstraction — it has no idea plans currently come from a JSON file.
/// That indirection is what lets Infrastructure be swapped out (e.g. for a database or an
/// HTTP API) without touching any business logic.
/// </summary>
public interface IPlanRepository
{
    /// <summary>
    /// Parses and validates every plan found at <paramref name="path"/>.
    /// Never throws for "expected" problems (missing file, bad JSON, rule violations);
    /// those are reported via <see cref="PlanLoadResult"/> instead so the CLI can print a
    /// clear message and keep running.
    /// </summary>
    PlanLoadResult Load(string path);
}

/// <summary>
/// Outcome of attempting to load plans from a file.
/// Two independent failure modes are modelled deliberately:
///   - FatalErrors: the file itself couldn't be read as a plan list (missing file, bad
///     JSON, wrong shape). Nothing is loaded.
///   - PlanWarnings: the file parsed fine, but one or more individual plan entries broke a
///     domain rule (e.g. a non-final tier missing its threshold). Those entries are
///     skipped; every other, valid plan is still loaded and usable.
/// This mirrors how a real ETL/import pipeline behaves — one bad record shouldn't sink an
/// otherwise-good batch.
/// </summary>
public sealed class PlanLoadResult
{
    public IReadOnlyList<CloudPlan> Plans { get; }
    public IReadOnlyList<string> FatalErrors { get; }
    public IReadOnlyList<string> PlanWarnings { get; }
    public bool Success => FatalErrors.Count == 0;

    private PlanLoadResult(IReadOnlyList<CloudPlan> plans, IReadOnlyList<string> fatalErrors, IReadOnlyList<string> planWarnings)
    {
        Plans = plans;
        FatalErrors = fatalErrors;
        PlanWarnings = planWarnings;
    }

    public static PlanLoadResult Ok(IReadOnlyList<CloudPlan> plans, IReadOnlyList<string> warnings) =>
        new(plans, Array.Empty<string>(), warnings);

    public static PlanLoadResult Failed(IReadOnlyList<string> errors) =>
        new(Array.Empty<CloudPlan>(), errors, Array.Empty<string>());
}
