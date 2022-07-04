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
        // Kick off SensorReader, polls temp every ~8s from BME280
        _ = Task.Run(() => sensorReader.ExecuteAsync(stoppingToken), stoppingToken);

        // Grab temp thresholds from appsettings.json
        double maxTemp = config.GetValue<double>("settings:maxTemp");

        // Start main loop
        logger.LogInformation("Starting service, getting average CPU temp over 10s.....");
        while (!stoppingToken.IsCancellationRequested)
        {
            if (sensorReader.CompTempF == 0)
            {
                // Warming up sensor....
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                continue;
            }

            if (sensorReader.CompTempF > maxTemp)
            {
                _ = alertHandler.RaiseAlert(nextNotification: NOTIFICATION_RATE);
            }

            _ = alertHandler.SendCanary();
            await Task.Delay(TimeSpan.FromSeconds(POLLING_RATE), stoppingToken);
        }
    }
}