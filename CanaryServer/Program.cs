using CanaryServer;
using Serilog;
using Serilog.Events;
using Serilog.Templates;

Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .WriteTo.Console(new ExpressionTemplate("[{@t:MM-dd-yyyy HH:mm:ss} {Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}]  [{@l:u3}] {@m}\n{@x}"))
        .WriteTo.File(new ExpressionTemplate("[{@t:MM-dd-yyyy HH:mm:ss} {Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}]  [{@l:u3}] {@m}\n{@x}"), @".\CanaryServerLog.txt")
        .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

builder.WebHost.UseUrls("http://*:10021");

builder.Services.AddHostedService<CanaryWorker>();
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();