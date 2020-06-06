using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.ServiceBus.Diagnostics;

namespace SvcbusBatchInBatchOut
{
    public class ServiceBusQueueBatchReceiver
    {
        private readonly TelemetryClient telemetryClient;

        public ServiceBusQueueBatchReceiver(TelemetryConfiguration telemetryConfiguration){
            this.telemetryClient = new TelemetryClient(telemetryConfiguration);
        }

        [FunctionName("ServiceBusQueueBatchReceiver")]
        public async Task Run(
            [ServiceBusTrigger("%ServiceBusQueueName%", Connection = "ServiceBusConnection")]
            Message[] messages, ILogger log)
        {
            log.LogInformation($"Batch trigger function received {messages.Length.ToString()} messages.");

            foreach(var msg in messages){
                // StartOperation is a helper method that allows correlation of 
                // current operations with nested operations/telemetry
                // and initializes start time and duration on telemetry items.
                
                // see: https://github.com/Azure/azure-service-bus-dotnet/blob/master/src/Microsoft.Azure.ServiceBus/Extensions/MessageDiagnosticsExtensions.cs
                var activity = msg.ExtractActivity("handle single message from batch");
                var operation = telemetryClient.StartOperation<RequestTelemetry>(activity);

                try
                {
                    var body = msg.Body;
                    var text = Encoding.UTF8.GetString(body);
                    telemetryClient.TrackTrace($"handling message: {text}");
                    // do actual work to process message:
                    // await ProcessMessage();
                }
                catch (Exception e)
                {
                    telemetryClient.TrackException(e);
                    throw;
                }
                finally
                {
                    // Update status code and success as appropriate.
                    telemetryClient.StopOperation(operation);
                }
            }
            
        }

        // [FunctionName("ServiceBusQueueSingleMessageReceiver")]
        // public void Run(
        //     [ServiceBusTrigger("%ServiceBusQueueName%", Connection = "ServiceBusConnection")]
        //     Message message, ILogger log)
        // {
        //     log.LogInformation($"Batch trigger function received a single message.");
        // }
    }
}
