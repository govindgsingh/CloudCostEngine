namespace CloudCostEngine.Cli.Commands;

/// <summary>Handles: exit</summary>
public sealed class ExitCommand : ICommand
{
    public string Name => "exit";

    public void Execute(string[] args, CliContext context)
    {
        context.IsRunning = false;
    }
}
