using System.CommandLine;
using ReceiptToolkit.Cli.Commands;

var root = new RootCommand("Receipt Toolkit CLI — validate, generate, and sample receipt artifacts.");
root.Add(ValidateCommand.Build());
root.Add(GenerateCommand.Build());
root.Add(SampleCommand.Build());

return root.Parse(args).Invoke();
