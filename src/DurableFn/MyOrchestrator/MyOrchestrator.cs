using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

// DURABLE FUNCTIONS CORRELATIONS ARE A WORK IN PROGRESS HERE:
// https://github.com/Azure/durabletask/tree/correlation/samples/Correlation.Samples#distributed-tracing-for-durable-task
namespace MyOrchestrator
{
    public static class MyOrchestrator
    {
        [FunctionName("MyOrchestrator")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var outputs = new List<string>();

            // Replace "hello" with the name of your Durable Activity Function.
            outputs.Add(await context.CallActivityAsync<string>("MyOrchestrator_Hello", "Tokyo"));
            outputs.Add(await context.CallActivityAsync<string>("MyOrchestrator_Hello", "Seattle"));
            outputs.Add(await context.CallActivityAsync<string>("MyOrchestrator_Hello", "London"));
            outputs.Add(await context.CallActivityAsync<string>("FlakeyFunction", "Christof (flakey)"));
            outputs.Add(await context.CallActivityAsync<string>("MyOrchestrator_Hello", "Christof (for sure)"));

            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }

        [FunctionName("MyOrchestrator_Hello")]
        public static string SayHello([ActivityTrigger] string name, ILogger log)
        {
            log.LogInformation($"Saying hello to {name}.");
            return $"Hello {name}!";
        }

        [FunctionName("FlakeyFunction")]
        public static string SayFlakeyHello([ActivityTrigger] string name, ILogger log)
        {
            log.LogInformation($"Maybe saying hello to {name}.");
            var r = new Random();
            var randomNR = r.Next(100);
            var willSucceed = (randomNR % 3 == 0);

            if(willSucceed){
                return $"Hello {name}!";
            }
            else{
                throw new ApplicationException("Randomly failed saying hello.  Please retry later.");
            }
            
        }

        [FunctionName("MyOrchestrator_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("MyOrchestrator", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}