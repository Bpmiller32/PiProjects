using Python.Runtime;

namespace SweatBot;

public class SensorReader
{
    public double CompTempF { get; set; }
    public double Pressure { get; set; }
    public double Humidity { get; set; }
    public double Light { get; set; }

    private readonly ILogger<SensorReader> logger;
    private readonly Queue<double> cpuTemps = new();
    private readonly string pythonCode =
@"import time
from ltr559 import LTR559
from bme280 import BME280
from smbus import SMBus

bus = SMBus(1)
bme280 = BME280(i2c_dev=bus)
ltr559 = LTR559()

class Sensor:
    temperature = 0
    pressure = 0
    humidity = 0
    light = 0

sensor = Sensor()

for x in range(0, 5):
    sensor.temp = bme280.get_temperature()
    sensor.pressure = bme280.get_pressure()
    sensor.humidity = bme280.get_humidity()
    sensor.light = ltr559.get_lux()
    time.sleep(1.0)

output = str(sensor.temp) + ',' + str(sensor.pressure) + ',' + str(sensor.humidity) + ',' + str(sensor.light)";

    public SensorReader(ILogger<SensorReader> logger)
    {
        this.logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Collect initial CPU temperate values
        logger.LogDebug("Getting average CPU temp over 5s.....");
        for (int i = 0; i < 5; i++)
        {
            cpuTemps.Enqueue(GetCpuTemp());
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }

        // Start main loop
        logger.LogDebug("Starting sensor reading");
        while (!stoppingToken.IsCancellationRequested)
        {
            // Collect data from Enviro+
            string test = (string)RunPythonCode
            (
                pyCode: pythonCode,
                returnedVariableName: "output"
            );

            // Only able export primitive types from Python.Net so I sent back a delimited string to have only 1 Python execution, separate and assign values
            string[] sensorValues = test.Split(',');

            double rawTemp = double.Parse(sensorValues[0]);
            Pressure = double.Parse(sensorValues[1]);
            double rawHumidity = double.Parse(sensorValues[2]);
            Light = double.Parse(sensorValues[3]);

            // Average CPU temperature, collect new value
            double averageCpuTemp = 0;
            foreach (var temp in cpuTemps)
            {
                averageCpuTemp += temp;
            }
            averageCpuTemp /= 5;

            cpuTemps.Dequeue();
            cpuTemps.Enqueue(GetCpuTemp());

            // Calculate compensated temperature
            const double factor = 2.25;
            double compTempC = rawTemp - ((averageCpuTemp - rawTemp) / factor);
            CompTempF = (compTempC * 9 / 5) + 32;

            // Calculate corrected humidity
            double dewPoint = rawTemp - ((100 - rawHumidity) / 5);
            Humidity = 100 - (5 * (compTempC - dewPoint));

            // Debug output
            logger.LogDebug("Compensated temperature: {CompTempF} *F", CompTempF);
            logger.LogDebug("Pressure: {Pressure} hPa", Pressure);
            logger.LogDebug("Humidity: {Humidity} %", Humidity);
            logger.LogDebug("Light: {Light} lux", Light);
        }
    }

    private static double GetCpuTemp()
    {
        string cpuTempText = File.ReadAllText("/sys/class/thermal/thermal_zone0/temp");
        return double.Parse(cpuTempText) / 1000;
    }

    private static object RunPythonCode(string pyCode, string returnedVariableName)
    {
        Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", "/usr/lib/arm-linux-gnueabihf/libpython3.9.so.1.0");
        PythonEngine.Initialize();

        object returnedVariable = new();

        using (Py.GIL())
        using (PyModule scope = Py.CreateScope())
        {
            scope.Exec(pyCode);
            returnedVariable = scope.Get<object>(returnedVariableName);
        }

        return returnedVariable;
    }
}