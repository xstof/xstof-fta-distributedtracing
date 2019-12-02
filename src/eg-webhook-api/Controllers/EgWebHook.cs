using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text;
using System.Text.Json;
using eg_webhook_api;

namespace eg_webhook_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EgWebHookController : ControllerBase
    {
        private readonly ILogger<EgWebHookController> _logger;

        private bool EventTypeSubcriptionValidation
            => HttpContext.Request.Headers["aeg-event-type"].FirstOrDefault() ==
            "SubscriptionValidation";

        private bool EventTypeNotification
            => HttpContext.Request.Headers["aeg-event-type"].FirstOrDefault() ==
            "Notification";

        public EgWebHookController(ILogger<EgWebHookController> logger)
        {
            _logger = logger;
        }

        [HttpOptions]
        public async Task<IActionResult> Options()
        {
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                var webhookRequestOrigin = HttpContext.Request.Headers["WebHook-Request-Origin"].FirstOrDefault();
                var webhookRequestCallback = HttpContext.Request.Headers["WebHook-Request-Callback"];
                var webhookRequestRate = HttpContext.Request.Headers["WebHook-Request-Rate"];
                HttpContext.Response.Headers.Add("WebHook-Allowed-Rate", "*");
                HttpContext.Response.Headers.Add("WebHook-Allowed-Origin", webhookRequestOrigin);

                _logger.LogInformation("Options called");
            }

            return Ok();
        }

        // send an event grid message?
        [HttpGet]
        public IActionResult Get()
        {
            _logger.LogInformation("GET called");
            return Ok("hello world");
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                var jsonContent = await reader.ReadToEndAsync();

                // Check the event type.
                // Return the validation code if it's 
                // a subscription validation request. 
                if (EventTypeSubcriptionValidation)
                {
                    return await HandleValidation(jsonContent);
                }
                else if (IsCloudEvent(jsonContent, out CloudEvent<dynamic> cloudEvent))
                {
                    return await HandleCloudEvent(cloudEvent);
                }

                return BadRequest();                
            }
        }

        private async Task<JsonResult> HandleValidation(string jsonContent)
        {
            var gridEvent =
                JsonSerializer.Deserialize<List<GridEvent<Dictionary<string, string>>>>(jsonContent)
                    .First();

            // Retrieve the validation code and echo back.
            var validationCode = gridEvent.Data["validationCode"];
            return new JsonResult(new
            {
                validationResponse = validationCode
            });
        }

        private async Task<IActionResult> HandleCloudEvent(CloudEvent<dynamic> details)
        {
            if (null == details){

                _logger.LogInformation("no cloud event | null");
                return BadRequest();
            }
            var sb = new StringBuilder();
            sb.AppendLine("cloud event received");
            sb.AppendLine(details.Id);
            sb.AppendLine(details.Type);
            sb.AppendLine(details.Subject);
            sb.AppendLine(details.Time);
 

            _logger.LogInformation(sb.ToString());

            return Ok();
        }

        private static bool IsCloudEvent(string jsonContent, out CloudEvent<dynamic> cloudEvent)
        {
            cloudEvent=null;

            try
            {
                // Attempt to read one JSON object. 
                var details = JsonSerializer.Deserialize<CloudEvent<dynamic>>(jsonContent);

                // Check for the spec version property.
                var version = details.SpecVersion;
                if (!string.IsNullOrEmpty(version)) {
                    cloudEvent=details;
                    return true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return false;
        }
    }
}
