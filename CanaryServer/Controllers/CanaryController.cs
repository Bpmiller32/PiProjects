using Microsoft.AspNetCore.Mvc;

namespace CanaryServer.Controllers;

[ApiController]
[Route("[controller]")]
public class CanaryController : ControllerBase
{
    private readonly ILogger<CanaryController> logger;

    public CanaryController(ILogger<CanaryController> logger)
    {
        this.logger = logger;
    }

    [HttpPost]
    public ActionResult<string> Post(RecievedMessage message)
    {
        logger.LogDebug("Recieved message: {message}", message.Content);

        if (message.Content == "rafSweatBot")
        {
            CanaryWorker.MissedMessages = 0;
        }

        return "Canary message recieved";
    }
}
