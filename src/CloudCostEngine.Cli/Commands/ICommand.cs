namespace CloudCostEngine.Cli.Commands;

/// <summary>
/// One CLI verb (spec §7). Each command owns its own argument parsing and error messages,
/// so adding a new command later — e.g. a "budget" command for the interview extension —
/// means writing one new class and registering it, never touching the others.
/// </summary>
public interface ICommand
{
    /// <summary>The verb typed by the user, e.g. "input" or "monthly_cost".</summary>
    string Name { get; }

    void Execute(string[] args, CliContext context);
}
