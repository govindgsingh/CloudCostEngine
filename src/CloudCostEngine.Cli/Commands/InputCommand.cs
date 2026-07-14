namespace CloudCostEngine.Cli.Commands;

/// <summary>Handles: input &lt;filename&gt;</summary>
public sealed class InputCommand : ICommand
{
    public string Name => "input";

    public void Execute(string[] args, CliContext context)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: input <filename>");
            return;
        }

        // File paths may contain spaces (the spec's own example does not, but Windows
        // paths commonly do), so re-join everything after the command word.
        var path = string.Join(' ', args);

        var result = context.PlanRepository.Load(path);

        if (!result.Success)
        {
            foreach (var error in result.FatalErrors)
                Console.WriteLine($"Error: {error}");
            return;
        }

        foreach (var warning in result.PlanWarnings)
            Console.WriteLine($"Warning: skipped invalid plan — {warning}");

        context.LoadedPlans = result.Plans;

        if (result.Plans.Count == 0)
        {
            Console.WriteLine("No valid plans were loaded.");
            return;
        }

        Console.WriteLine($"Loaded {result.Plans.Count} plan(s) from '{path}'.");
    }
}
