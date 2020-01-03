using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.EventHubs;
using System.Diagnostics;

namespace eg_webhook_api.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class AehDemoSend : ControllerBase
    {

        private readonly ILogger<EgWebHookController> _logger;
        //private readonly TelemetryClient _telemClient;
        private readonly IConfiguration _config;


         private string AEGSubscriptionName
            => HttpContext.Request.Headers["aeg-subscription-name"].FirstOrDefault() ;

        // correct way to obtain a AI telem client in ASP NET CORE is via the DI 
        public AehDemoSend(ILogger<EgWebHookController> logger,  IConfiguration config)
        {
            _config = config;
            _logger = logger;
            //_telemClient = telemClient;

            //DiagnosticListener.AllListeners.Subscribe (delegate (DiagnosticListener listener) {
            //    Console.WriteLine("subscribed");
            //    listener.Subscribe ((KeyValuePair<string, object> e) =>
            //        Console.WriteLine ($"Received Event {e.Key} with payload {e.Value.ToString ()}"));
            //});
        }

        [HttpPost]
        public async Task<IActionResult> SendEvents()
        {
            _logger.LogInformation("Post send events called");

            var aehConnectionString = _config.GetValue<string>("aehConnectionString");
            var aehName = _config.GetValue<string>("aehName");

            if ( string.IsNullOrEmpty(aehName) || string.IsNullOrEmpty(aehConnectionString)){
                throw new Exception("The powershell to deploy the function code should have setup this console app appsettings file for AEH");
            }

            var connectionStringBuilder = new EventHubsConnectionStringBuilder(aehConnectionString)
            {
                EntityPath = aehName
            };

            var eventHubClient = EventHubClient.CreateFromConnectionString(connectionStringBuilder.ToString());

            await SendEventHubEvents(eventHubClient, 5);

            await eventHubClient.CloseAsync();

            return Ok("Events sent");
        }

        private static async Task SendEventHubEvents(EventHubClient ehClient , int numMessagesToSend)
        {
            for (var i = 0; i < numMessagesToSend; i++)
            {
                try
                {
                    var message = $"Message {i}";
                    Console.WriteLine($"Sending message: {message}");
                    await ehClient.SendAsync(new EventData(System.Text.Encoding.UTF8.GetBytes(message)));
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"{DateTime.Now} > Exception: {exception.Message}");
                }

                await Task.Delay(10);
            }

            Console.WriteLine($"{numMessagesToSend} AEH messages sent.");
        }


    }
}