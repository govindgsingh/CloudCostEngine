using CloudCostEngine.Cli.Commands;

namespace CloudCostEngine.Cli;

/// <summary>
/// The interactive read-parse-dispatch loop described in spec §7. Commands are looked up
/// by name in a dictionary built once at startup — adding a command means adding one entry
/// here, not adding another "if/else" branch.
/// </summary>
public sealed class CommandLoop
{
    private readonly Dictionary<string, ICommand> _commands;
    private readonly CliContext _context;
    private readonly TextReader _input;

    public CommandLoop(CliContext context, IEnumerable<ICommand> commands, TextReader? input = null)
    {
        _context = context;
        _input = input ?? Console.In;
        _commands = commands.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
    }

    public void Run()
    {
        while (_context.IsRunning)
        {
            Console.Write("> ");
            var line = _input.ReadLine();

            if (line is null)
                break; // stdin closed (e.g. piped input ran out) — exit gracefully.

            line = line.Trim();
            if (line.Length == 0)
                continue;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var commandName = parts[0];
            var args = parts.Skip(1).ToArray();

            if (!_commands.TryGetValue(commandName, out var command))
            {
                Console.WriteLine($"Unknown command: '{commandName}'. Available commands: {string.Join(", ", _commands.Keys)}");
                continue;
            }

            try
            {
                command.Execute(args, _context);
            }
            catch (Exception ex)
            {
                // Belt-and-braces: every *expected* failure is already handled inside each
                // command. This catch exists only so a truly unexpected error reports
                // cleanly and lets the user keep working, instead of crashing the process.
                Console.WriteLine($"Unexpected error while running '{commandName}': {ex.Message}");
            }
        }
    }
}
