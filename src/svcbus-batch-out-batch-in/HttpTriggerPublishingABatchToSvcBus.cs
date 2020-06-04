using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.ServiceBus;

namespace SvcbusBatchInBatchOut
{
    public class HttpTriggerPublishingABatchToSvcBus
    {
        //private readonly TelemetryClient _telemetryClient;

        public HttpTriggerPublishingABatchToSvcBus(){
            
        }

        [FunctionName("HttpTriggerPublishingABatchToSvcBus")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            [ServiceBus("%ServiceBusQueueName%", Connection = "ServiceBusConnection")] IAsyncCollector<Message> messages,
            ILogger log)
        {
            int batchSize = 1;  // publish a batch of 10 messages to service bus at a time

            log.LogInformation("C# HTTP trigger function processed a request.");

            // string name = req.Query["name"];

            // string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            // dynamic data = JsonConvert.DeserializeObject(requestBody);
            // name = name ?? data?.name;

            for(int i= 0; i<batchSize; i++){
                var msgText = $"msg {i.ToString()}";
                var msg = new Message(System.Text.UTF8Encoding.UTF8.GetBytes(msgText));
                await messages.AddAsync(msg);
            }

            string activityId = System.Diagnostics.Activity.Current.Id ?? "(no id)";
            string responseMessage = $"This HTTP triggered function executed successfully and published a message batch onto Service Bus.  Activity.Current Id = {activityId}";

            return new OkObjectResult(responseMessage);
        }
    }
}