using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace SvcbusBatchInBatchOut
{
    public static class ServiceBusQueueBatchReceiver
    {
        [FunctionName("ServiceBusQueueBatchReceiver")]
        public static void Run(
            [ServiceBusTrigger("%ServiceBusQueueName%", Connection = "ServiceBusConnection")]
            string message, ILogger log)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {message}");
        }
    }
}
