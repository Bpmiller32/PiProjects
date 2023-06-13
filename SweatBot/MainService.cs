namespace SweatBot;

public class MainService : BackgroundService
{
    private readonly ILogger<MainService> logger;
    private readonly IConfiguration config;
    private readonly SensorReader sensorReader;
    private readonly AlertHandler alertHandler;

    // Change these to change how often temperature is polled, how long until follow up notification is sent 
    // Remember sensor takes ~8s to read
    private readonly int POLLING_RATE = 10;
    private readonly int NOTIFICATION_RATE = 3600;

    public MainService(ILogger<MainService> logger, IConfiguration config, SensorReader sensorReader, AlertHandler alertHandler)
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

        // Grab valid values from appsettings.json, set for AlertHandler and SensorReader
        double maxTemp = config.GetValue<double>("settings:maxTemp");
        sensorReader.PythonDll = config.GetValue<string>("settings:pythonDll");
        alertHandler.MaxTemp = maxTemp;
        alertHandler.EmailListString = config.GetValue<string>("settings:destinationEmailList");
        alertHandler.SenderEmailAddress = config.GetValue<string>("settings:senderEmailAddress");
        alertHandler.SenderEmailPassword = config.GetValue<string>("settings:senderEmailPassword");

        // Kick off SensorReader asynchonously, time to pull all info from sensor is ~8s
        _ = Task.Run(() => sensorReader.ReadSensors(stoppingToken), stoppingToken);

        // Start main loop
        logger.LogInformation("✦✧✦✧✦✧✦✧✦✧✦✧✦✧✦✧✦✧✦✧");
        logger.LogInformation("Starting service, getting average CPU temp over 10s.....");
        logger.LogDebug("I am running from: {System}", AppDomain.CurrentDomain.BaseDirectory);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (sensorReader.CompTempF == 0)
            {
                // Warming up sensor....
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                continue;
            }

            // Check if sensor reading is above threshold
            if (sensorReader.CompTempF > maxTemp)
            {
                _ = alertHandler.RaiseAlert(nextNotification: NOTIFICATION_RATE);
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
            // Check maxTemp
            if (config.GetValue<double>("settings:maxTemp") == 0)
            {
                throw new Exception("maxTemp value is either missing or invalid in appsettings.json");
            }
            // Check other settings
            List<string> stringSettings = new() { "destinationEmailList", "pythonDll", "senderEmailAddress", "senderEmailPassword" };
            foreach (string stringSetting in stringSettings)
            {
                string setting = string.Format("settings:{0}", stringSetting);

                if (string.IsNullOrEmpty(config.GetValue<string>(setting)))
                {
                    throw new Exception(string.Format("{0} value is either missing or invalid in appsettings.json", setting));
                }
            }

            return true;
        }
        catch (Exception e)
        {
            logger.LogError("{e}", e.Message);
            return false;
        }
    }
}