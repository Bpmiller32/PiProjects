using System.Net.Http.Headers;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

namespace SweatBot;

public class AlertHandler
{
    private readonly ILogger<AlertHandler> logger;
    private readonly IConfiguration config;
    private readonly SensorReader sensorReader;

    private int missedPings = 0;
    private bool pingsEnabled = true;
    private int alertCount = 0;
    private bool alertsEnabled = true;
    private readonly double maxTemp;

    public AlertHandler(ILogger<AlertHandler> logger, IConfiguration config, SensorReader sensorReader)
    {
        this.logger = logger;
        this.config = config;
        this.sensorReader = sensorReader;

        maxTemp = config.GetValue<double>("settings:maxTemp");
    }

    public async Task RaiseAlert(int nextNotification)
    {
        alertCount++;

        if (alertCount == 5 && alertsEnabled)
        {
            // Send email, wait 1 hour before sending additional notification
            logger.LogWarning("Temperature is above maximum threshold: {sensorReader:F2} (maxTemp: {maxTemp:F2} *F)", sensorReader.CompTempF, maxTemp);
            logger.LogInformation("Sending alert email, waiting 1 hour before sending additional email");

            alertsEnabled = false;
            SendEmail();

            await Task.Delay(TimeSpan.FromSeconds(nextNotification));
            // Waiting is over, reset alert capability and count
            alertsEnabled = true;
            alertCount = 0;
        }
    }

    public async Task SendCanary()
    {
        try
        {
            if (!pingsEnabled)
            {
                return;
            }

            if (missedPings > 30)
            {
                logger.LogError("Was not able to reach canary server or service for over 3 minutes, stopping pings");
                pingsEnabled = false;
            }

            HttpRequestMessage request = new()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("http://192.168.50.184:10021/Canary"),
                Content = new StringContent("{\"Content\": \"rafSweatBot\"}")
                {
                    Headers = { ContentType = new MediaTypeHeaderValue("application/json") }
                }
            };

            using HttpClient client = new();
            await client.SendAsync(request);
            missedPings = 0;
        }
        catch (System.Threading.Tasks.TaskCanceledException e)
        {
            logger.LogDebug("Was not able to reach canary server or service: {e}", e.Message);
            missedPings++;
        }
        catch (System.Exception e)
        {
            logger.LogError("{e}", e.Message);
        }
    }

    private void SendEmail()
    {
        try
        {
            // Save to local var so that value is consistent across email
            double compTempF = sensorReader.CompTempF;
            // Grab email addresses from appsettings.json
            string emailListString = config.GetValue<string>("settings:emailList");
            string[] emailList = emailListString.Split(',');

            // Create email
            MimeMessage email = new();
            email.From.Add(MailboxAddress.Parse("billy.miller@raf.com"));
            foreach (string emailAddress in emailList)
            {
                email.To.Add(MailboxAddress.Parse(emailAddress));
            }
            email.Subject = string.Format("Sever room temperature: {0:F2}*F 🌡️", compTempF);
            email.Body = new TextPart(TextFormat.Plain)
            {
                Text = string.Format("The current temperature in the server room is {0:F2}*F which is above the max set threshold of {1:F2}*F\n\nSensor will continue polling every 10s. An additional alert email will be sent in 1 hour if the temperature is still above the max threshold.\n\nOther data\nTemp: {0:F2} *F 🌡️\nHumidity: {2:F2}% 💦\nPressure: {3:F2} hPa 💨\nLight: {4:F2} lux 💡", compTempF, maxTemp, sensorReader.Humidity, sensorReader.Pressure, sensorReader.Light)
            };

            // Send email using raf account
            using SmtpClient smtp = new();
            smtp.Connect("smtp.office365.com", 587, SecureSocketOptions.StartTls);
            smtp.Authenticate("billy.miller@raf.com", "wwnhkmhjctfttfjf");
            smtp.Send(email);
            smtp.Disconnect(true);
        }
        catch (System.Exception e)
        {
            logger.LogError("{e}", e.Message);
        }
    }
}