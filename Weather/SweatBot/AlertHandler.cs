using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

namespace SweatBot;

public class AlertHandler
{
    public double MaxTemp { get; set; }
    public string EmailListString { get; set; }
    public string SenderEmailAddress { get; set; }
    public string SenderEmailPassword { get; set; }

    private readonly ILogger<AlertHandler> logger;
    private readonly SensorReader sensorReader;

    private int alertCount = 0;
    private bool alertsEnabled = true;

    public AlertHandler(ILogger<AlertHandler> logger, SensorReader sensorReader)
    {
        this.logger = logger;
        this.sensorReader = sensorReader;
    }

    public async Task RaiseAlert(int nextNotification)
    {
        alertCount++;

        if (alertCount == 5 && alertsEnabled)
        {
            // Send email, wait 1 hour before sending additional notification
            logger.LogWarning("Temperature is above maximum threshold: {sensorReader:F2} (maxTemp: {maxTemp:F2} *F)", sensorReader.CompTempF, MaxTemp);
            logger.LogInformation("Sending alert email, waiting 1 hour before sending additional email");

            alertsEnabled = false;
            SendEmail();

            await Task.Delay(TimeSpan.FromSeconds(nextNotification));

            // Waiting is over, reset alert capability and count
            alertsEnabled = true;
            alertCount = 0;
        }
    }

    private void SendEmail()
    {
        try
        {
            // Save to local var so that value is consistent across email (possible that sensor could be read and this value changed during this email send)
            double compTempF = sensorReader.CompTempF;
            // Grab email addresses from appsettings.json
            string[] emailList = EmailListString.Split(',');

            // Create email
            MimeMessage email = new();
            email.From.Add(MailboxAddress.Parse(SenderEmailAddress));
            foreach (string emailAddress in emailList)
            {
                email.To.Add(MailboxAddress.Parse(emailAddress));
            }
            email.Subject = string.Format("Sever room temperature: {0:F2}*F 🌡️", compTempF);
            email.Body = new TextPart(TextFormat.Plain)
            {
                Text = string.Format("The current temperature in the server room is {0:F2}*F which is above the set temp threshold of {1:F2}*F\n\nSensor will continue polling every 10s. An additional alert email will be sent in 1 hour if the temperature is still above the temp threshold.\n\nOther data\nTemp: {0:F2} *F 🌡️\nHumidity: {2:F2}% 💦\nPressure: {3:F2} hPa 💨\nLight: {4:F2} lux 💡", compTempF, MaxTemp, sensorReader.Humidity, sensorReader.Pressure, sensorReader.Light)
            };

            // Send email using raf account
            using SmtpClient smtp = new();
            smtp.Connect("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
            smtp.Authenticate(SenderEmailAddress, SenderEmailPassword);
            smtp.Send(email);
            smtp.Disconnect(true);
        }
        catch (Exception e)
        {
            logger.LogError("{e}", e.Message);
        }
    }
}