// Phase 0 stub. Phase 6 wires the polling worker, ITelegramSender, and command handlers.

var builder = Host.CreateApplicationBuilder(args);
var host = builder.Build();
host.Run();
