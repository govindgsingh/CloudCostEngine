using System.Globalization;

namespace CloudCostEngine.Cli.Commands;

/// <summary>Handles: monthly_cost &lt;storage_gb&gt;</summary>
public sealed class MonthlyCostCommand : ICommand
{
    public string Name => "monthly_cost";

    public void Execute(string[] args, CliContext context)
    {
        if (!context.HasLoadedPlans)
        {
            Console.WriteLine("No plans loaded. Use 'input <filename>' first.");
            return;
        }

        if (args.Length != 1)
        {
            Console.WriteLine("Usage: monthly_cost <storage_gb>");
            return;
        }

        if (!decimal.TryParse(args[0], NumberStyles.Number, CultureInfo.InvariantCulture, out var storageGb))
        {
            Console.WriteLine($"Invalid storage amount: '{args[0]}'. Expected a non-negative number of GB.");
            return;
        }

        if (storageGb < 0)
        {
            Console.WriteLine("Storage amount cannot be negative.");
            return;
        }

        var results = context.LoadedPlans
            .Select(plan => context.CostCalculator.Calculate(plan, storageGb))
            .OrderBy(r => r.TotalDollarsRounded)
            .ThenBy(r => r.ProviderName, StringComparer.Ordinal)
            .ThenBy(r => r.PlanName, StringComparer.Ordinal);

        foreach (var result in results)
        {
            var totalFormatted = result.TotalDollarsRounded.ToString("F2", CultureInfo.InvariantCulture);
            Console.WriteLine($"{result.ProviderName}, {result.PlanName}, ${totalFormatted}");
        }
    }
}
