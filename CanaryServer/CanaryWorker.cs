using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

namespace CanaryServer;

public class CanaryWorker : BackgroundService
{
    public static int MissedMessages { get; set; }

    private readonly ILogger<CanaryWorker> logger;

    // Change this to change how often server is pinged, should match polling rate of SweatBot
    private readonly int POLLING_RATE = 10;

    public CanaryWorker(ILogger<CanaryWorker> logger)
    {
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting Canary service");

        // Start main loop
        while (!stoppingToken.IsCancellationRequested)
        {
            // Number of missed canary check ins before altering. Check ins every 10s == 10 per minute. 10 * 3 minutes = 30. 
            if (MissedMessages > 30)
            {
                logger.LogWarning("Missed 3 minutes of canary messages, sending email and stopping monitoring");
                SendEmail();
                return;
            }

            MissedMessages++;
            await Task.Delay(TimeSpan.FromSeconds(POLLING_RATE), stoppingToken);
        }
    }

    private static void SendEmail()
    {
        // Create email
        MimeMessage email = new();
        email.From.Add(MailboxAddress.Parse("billy.miller@raf.com"));
        email.To.Add(MailboxAddress.Parse("bpmiller32@gmail.com"));
        email.Subject = string.Format("Potential power outage detected 🔌");
        email.Body = new TextPart(TextFormat.Plain)
        {
            Text = string.Format("Have not recieved a check in from the temperature sensor for over 3 consecutive minutes.\n\nPossible that this is caused by a loss of power to the device/building.")
        };

        // Send email using raf account
        using SmtpClient smtp = new();
        smtp.Connect("smtp.office365.com", 587, SecureSocketOptions.StartTls);
        smtp.Authenticate("billy.miller@raf.com", "wwnhkmhjctfttfjf");
        smtp.Send(email);
        smtp.Disconnect(true);
    }
}
