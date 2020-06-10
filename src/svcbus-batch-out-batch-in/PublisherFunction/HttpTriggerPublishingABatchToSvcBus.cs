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
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.ServiceBus.Core;
using System.Collections.Generic;

namespace SvcbusBatchInBatchOut
{
    public class HttpTriggerPublishingABatchToSvcBus
    {
        private static MessageSender sendClient;
        private readonly TelemetryClient telemetryClient;

        public HttpTriggerPublishingABatchToSvcBus(TelemetryConfiguration telemetryConfiguration){
            this.telemetryClient = new TelemetryClient(telemetryConfiguration);
        }

        [FunctionName("HttpTriggerPublishingABatchToSvcBus")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            // [ServiceBus("%ServiceBusQueueName%", Connection = "ServiceBusConnection")] IAsyncCollector<Message> messages,
            ILogger log)
        {
            int batchSize = 100;  // publish a batch of 100 messages to service bus at a time

            log.LogInformation("C# HTTP trigger function processed a request.");

            // create the sendClient
            var conn = Environment.GetEnvironmentVariable("ServiceBusConnection");
            var queueName = Environment.GetEnvironmentVariable("ServiceBusQueueName");
            if (sendClient == null || sendClient.IsClosedOrClosing)
                sendClient = new MessageSender(conn, queueName);            

            var activityId = System.Diagnostics.Activity.Current.Id;
            System.Diagnostics.Activity.Current.AddTag("SampleName", "BatchOutBatchIn");
            System.Diagnostics.Activity.Current.AddBaggage("SampleName", "BatchOutBatchIn");
            System.Diagnostics.Activity.Current.AddTag("SampleActor", "BatchPublisher");

            IList<Message> batchedMessages = new List<Message>();
            for(int i= 0; i<batchSize; i++){
                var msgText = $"msg {i.ToString()}";
                var msg = new Message(System.Text.UTF8Encoding.UTF8.GetBytes(msgText));
                batchedMessages.Add(msg);
            }

            var metric = telemetryClient.GetMetric("NumberOfMessagesInBatchSubmitted");
            metric.TrackValue(batchSize);

            string responseMessage = $"This HTTP triggered function executed successfully and published a message batch onto Service Bus.  Activity.Current Id = {activityId}";

            await sendClient.SendAsync(batchedMessages);
            return new OkObjectResult(responseMessage);
        }
    }
}
