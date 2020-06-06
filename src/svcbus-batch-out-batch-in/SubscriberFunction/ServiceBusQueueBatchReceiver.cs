using System;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace SvcbusBatchInBatchOut
{
    public static class ServiceBusQueueBatchReceiver
    {
        // [FunctionName("ServiceBusQueueBatchReceiver")]
        // public static void Run(
        //     [ServiceBusTrigger("%ServiceBusQueueName%", Connection = "ServiceBusConnection")]
        //     Message[] messages, ILogger log)
        // {
        //     log.LogInformation($"Batch trigger function received {messages.Length.ToString()} messages.");
        // }

        [FunctionName("ServiceBusQueueSingleMessageReceiver")]
        public static void Run(
            [ServiceBusTrigger("%ServiceBusQueueName%", Connection = "ServiceBusConnection")]
            Message message, ILogger log)
        {
            log.LogInformation($"Batch trigger function received a single message.");
        }
    }
}
