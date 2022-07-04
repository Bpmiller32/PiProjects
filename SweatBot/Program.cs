using Serilog;
using Serilog.Events;
using Serilog.Templates;
using SweatBot;

try
{
    // Give the mutex a title
    using var mutex = new Mutex(false, "SweatBot");
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .WriteTo.Console(new ExpressionTemplate("[{@t:MM-dd-yyyy HH:mm:ss} {Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}] [{@l:u3}] {@m}\n{@x}"))
        .WriteTo.File(new ExpressionTemplate("[{@t:MM-dd-yyyy HH:mm:ss} {Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}] [{@l:u3}] {@m}\n{@x}"), @".\SweatbotLog.txt")
        .CreateLogger();

    // Crucially important for Windows Service, otherwise working directory runs out of Windows\System32
    System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);

    // Single instance of application check
    bool isAnotherInstanceOpen = !mutex.WaitOne(TimeSpan.Zero);
    if (isAnotherInstanceOpen)
    {
        throw new Exception("Only one instance of the application allowed");
    }

    IHost host = Host.CreateDefaultBuilder(args)
        .UseSerilog()
        .ConfigureServices(services =>
        {
            services.AddHostedService<MasterService>();
            services.AddSingleton<SensorReader>();
            services.AddSingleton<AlertHandler>();
        })
        .Build();

    await host.RunAsync();
}
catch (System.Exception e)
{
    Log.Fatal("There was a problem with a service");
    Log.Fatal(e.Message);
}
finally
{
    Log.CloseAndFlush();
}
