using Serilog;
using Serilog.Events;
using Serilog.Templates;
using SweatBot;

string applicationName = "SweatBot";
using var mutex = new Mutex(false, applicationName);

// Set exe directory to current directory, important when doing Windows services otherwise runs out of System32
Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

// Setup logger
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .WriteTo.Console(new ExpressionTemplate("[{@t:MM-dd-yyyy HH:mm:ss} {Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}] [{@l:u3}] {@m}\n{@x}"))
    .WriteTo.File(new ExpressionTemplate("[{@t:MM-dd-yyyy HH:mm:ss} {Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}] [{@l:u3}] {@m}\n{@x}"), string.Format("/home/billy/{0}.txt", applicationName))
    .CreateLogger();

try
{
    // Single instance of application check
    bool isAnotherInstanceOpen = !mutex.WaitOne(TimeSpan.Zero);
    if (isAnotherInstanceOpen)
    {
        throw new Exception("Only one instance of the application allowed");
    }

    IHost host = Host.CreateDefaultBuilder(args)
        .UseSystemd()
        .UseSerilog()
        .ConfigureServices(services =>
        {
            services.AddHostedService<MainService>();
            services.AddSingleton<SensorReader>();
            services.AddSingleton<AlertHandler>();
        })
        .Build();

    await host.RunAsync();
}
catch (Exception e)
{
    Log.Fatal("There was a problem with a service");
    Log.Fatal(e.Message);
}
finally
{
    Log.CloseAndFlush();
}
