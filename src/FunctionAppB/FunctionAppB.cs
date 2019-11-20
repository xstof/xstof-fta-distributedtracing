using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FTA.AICorrelation
{
    public class FunctionAppB
    {

        private readonly HttpClient _httpClient;
        public FunctionAppB(IHttpClientFactory clientFactory){
            this._httpClient = clientFactory.CreateClient();
        }

        [FunctionName("ReceiveFromSvcBus")]
        public async Task Run([ServiceBusTrigger("%serviceBusQueueName%", Connection = "ServiceBusConnection")]string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");
            Activity currActivity = Activity.Current;
            
            DumpActivity(currActivity, log);

            // to see results of this call, go to: http://requestbin.net/r/14ugjfo1?inspect
            await _httpClient.GetAsync("http://requestbin.net/r/14ugjfo1");
        }

        private void DumpActivity(Activity act, ILogger log)
        {
            Console.WriteLine($"Activity id: {act.Id}");
            Console.WriteLine($"Activity operation name: {act.OperationName}");
            Console.WriteLine($"Activity parent: {act.Parent}");
            Console.WriteLine($"Activity parent id: {act.ParentId}");
            Console.WriteLine($"Activity root id: {act.RootId}");
            foreach(var tag in act.Tags){
                Console.WriteLine($"  - Activity tag: {tag.Key}: {tag.Value}");
            }
            foreach(var bag in act.Baggage){
                Console.WriteLine($"  - Activity baggage: {bag.Key}: {bag.Value}");
            }
        }

        [FunctionName("SendBackHttpResponseFromB")]
        public async Task<IActionResult> RunSendBackResp(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "http")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# SendBackHttpResponseFromB HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            DumpActivity(Activity.Current, log);
            
            // to see results of this call, go to: http://requestbin.net/r/14ugjfo1?inspect
            await _httpClient.GetAsync("http://requestbin.net/r/14ugjfo1");

            return (ActionResult)new OkObjectResult($"");
        }
    }
}
