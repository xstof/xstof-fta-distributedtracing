using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace FTA.AICorrelation
{
    public static class ReceiveFromSvcBus
    {
        [FunctionName("ReceiveFromSvcBus")]
        public static void Run([ServiceBusTrigger("%serviceBusQueueName%", Connection = "ServiceBusConnection")]string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");
        }
    }
}
