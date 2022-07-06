using System.Diagnostics;

namespace SweatBot;

public class MasterService : BackgroundService
{
    private readonly ILogger<MasterService> logger;
    private readonly IConfiguration config;
    private readonly SensorReader sensorReader;
    private readonly AlertHandler alertHandler;

    // Change these to change how often temperature is polled, how long until follow up notification is sent 
    // Remember sensor takes ~8s to read
    private readonly int POLLING_RATE = 10;
    private readonly int NOTIFICATION_RATE = 3600;

    public MasterService(ILogger<MasterService> logger, IConfiguration config, SensorReader sensorReader, AlertHandler alertHandler)
    {
        this.logger = logger;
        this.config = config;
        this.sensorReader = sensorReader;
        this.alertHandler = alertHandler;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Validate appsettings
        if (!ValidateSettings())
        {
            return;
        }

        // Kick off SensorReader, time to pull all info from sensor is ~8s
        _ = Task.Run(() => sensorReader.ExecuteAsync(stoppingToken), stoppingToken);

        // Start main loop
        logger.LogInformation("✦✧✦✧✦✧✦✧✦✧✦✧✦✧✦✧✦✧✦✧");
        logger.LogInformation("Starting service, getting average CPU temp over 10s.....");
        logger.LogDebug("I am running from: {System}", System.AppDomain.CurrentDomain.BaseDirectory);
        while (!stoppingToken.IsCancellationRequested)
        {
            if (sensorReader.CompTempF == 0)
            {
                // Warming up sensor....
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                continue;
            }

            // Grab temp threshold from appsettings.json, check if sensor reading is above threshold
            double maxTemp = config.GetValue<double>("settings:maxTemp");
            if (sensorReader.CompTempF > maxTemp)
            {
                _ = alertHandler.RaiseAlert(nextNotification: NOTIFICATION_RATE);
            }

            // Check to see if canary service is enabled
            bool canaryEnabled = config.GetValue<bool>("settings:canaryEnabled");
            if (canaryEnabled)
            {
                _ = alertHandler.SendCanary();
            }

            await Task.Delay(TimeSpan.FromSeconds(POLLING_RATE), stoppingToken);
        }
    }

    private bool ValidateSettings()
    {
        try
        {
            // Check that appsettings.json exists at all
            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json")))
            {
                throw new Exception("appsettings.json is missing, make sure there is a valid appsettings.json file in the same directory as the application");
            }
            // Check emailList
            if (string.IsNullOrEmpty(config.GetValue<string>("settings:emailList")))
            {
                throw new Exception("emailList value is either missing or invalid in appsettings.json");
            }
            // Check maxTemp
            double maxTemp = config.GetValue<double>("settings:maxTemp");
            if (maxTemp == 0)
            {
                throw new Exception("maxTemp value is either missing or invalid in appsettings.json");
            }

            return true;
        }
        catch (System.Exception e)
        {
            logger.LogError("{e}", e.Message);
            return false;
        }
    }
}