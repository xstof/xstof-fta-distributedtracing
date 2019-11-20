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
using System.Text;
using System.Diagnostics;
using System.Net.Http;

namespace FTA.AICorrelation
{
    public class FunctionAppA
    {

        private readonly HttpClient _httpClient;
        public FunctionAppA(IHttpClientFactory clientFactory){
            this._httpClient = clientFactory.CreateClient();
        }

        [FunctionName("InitiateFlowToQueue")]
        public IActionResult RunQueue(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "queue/{submissionId}")] HttpRequest req,
            string submissionId,
            [ServiceBus("%serviceBusQueueName%", Connection = "ServiceBusConnection")] out Message msg,
            ILogger log)
        {
            log.LogInformation($"C# InitiateFlowToQueue HTTP trigger function processed a request with submission id: {submissionId}.");

            string requestBody = new StreamReader(req.Body).ReadToEnd();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            
            var initialMessage = new {
                IntialSubmissionId = submissionId
            };

            var msgBody = JsonConvert.SerializeObject(initialMessage);

            msg = new Message(Encoding.UTF8.GetBytes(msgBody));
            
            Activity currActivity = Activity.Current;
            currActivity.AddTag("MySubmissionId", submissionId);

            return (ActionResult)new OkObjectResult($"");
        }

        [FunctionName("InitiateFlowToQueueWithCustomBagage")]
        public IActionResult RunQueueWithCustomBagage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "queue-with-bagage/{submissionId}")] HttpRequest req,
            string submissionId,
            [ServiceBus("%serviceBusQueueName%", Connection = "ServiceBusConnection")] out Message msg,
            ILogger log)
        {
            log.LogInformation($"C# InitiateFlowToQueueWithCustomBagage HTTP trigger function processed a request with submission id: {submissionId}.");

            string requestBody = new StreamReader(req.Body).ReadToEnd();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            
            var initialMessage = new {
                IntialSubmissionId = submissionId
            };

            var msgBody = JsonConvert.SerializeObject(initialMessage);

            msg = new Message(Encoding.UTF8.GetBytes(msgBody));
            
            Activity currActivity = Activity.Current;

            currActivity.AddTag("MyCustomCorrId", submissionId);
            currActivity.AddBaggage("MyCustomCorrId", submissionId);

            // currActivity.AddTag("MySubmissionId", submissionId);

            DumpActivity(Activity.Current, log);

            return (ActionResult)new OkObjectResult($"");
        }

        [FunctionName("InitiateFlowToHTTP")]
        public async Task<IActionResult> RunHttp(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "http")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# InitiateFlowToHTTP HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            
            // to see results of this call, go to: http://requestbin.net/r/1ccpqtm1?inspect
            await _httpClient.GetAsync("http://requestbin.net/r/1ccpqtm1");

            return (ActionResult)new OkObjectResult($"");
        }

         [FunctionName("InitiateFlowToHTTPWithResponse")]
        public async Task<IActionResult> RunHttpWithResp(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "httpwithresp")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# InitiateFlowToHTTPWithResponse HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            DumpActivity(Activity.Current, log);
            
            // to see results of this call, go to: http://requestbin.net/r/1ccpqtm1?inspect
            await _httpClient.GetAsync("http://localhost:7072/api/http");

            return (ActionResult)new OkObjectResult($"");
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
    }
}
