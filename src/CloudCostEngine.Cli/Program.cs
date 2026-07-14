using CloudCostEngine.Application.Services;
using CloudCostEngine.Cli;
using CloudCostEngine.Cli.Commands;
using CloudCostEngine.Infrastructure;

// This is the solution's one and only composition root: the single place that knows about
// every concrete class and wires them together behind the interfaces the rest of the code
// depends on. No dependency-injection container is used deliberately — for a program this
// small, `new`-ing up four objects by hand is simpler to read than configuring a container,
// with zero loss of testability (see the README / Q&A guide for the trade-off discussion).

var repository = new JsonPlanRepository();
var calculator = new MonthlyCostCalculator();
var context = new CliContext(repository, calculator);

var commands = new ICommand[]
{
    new InputCommand(),
    new MonthlyCostCommand(),
    new ExitCommand(),
};

Console.WriteLine("Cloud Storage Cost Engine. Commands: input <filename> | monthly_cost <storage_gb> | exit");
new CommandLoop(context, commands).Run();
