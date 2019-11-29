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
using System.Net.Http.Headers;

namespace FTA.AICorrelation
{
    public class FunctionAppA
    {

        private readonly HttpClient _httpClient;
        private readonly string _logicAppAUrl;

        private readonly string _functionAppBUrl;
        private readonly string _httpBinHost;
        private readonly string _httpBinUrl;

        public FunctionAppA(IHttpClientFactory clientFactory){
            // fetch url for external http req inspection service
            this._httpClient = clientFactory.CreateClient();
            this._httpBinHost = Environment.GetEnvironmentVariable("httpBinIp", EnvironmentVariableTarget.Process);
            this._httpBinUrl = "http://requestbin.net/r/1nvwpun1";

            // fetch url for logic app A
            this._logicAppAUrl = Environment.GetEnvironmentVariable("logicAppAUrl", EnvironmentVariableTarget.Process);

            //fetch url for function app B
            this._functionAppBUrl = Environment.GetEnvironmentVariable("functionAppBUrl", EnvironmentVariableTarget.Process);
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

        [FunctionName("InitiateFlowToExternalHTTPUrl")]
        public async Task<IActionResult> RunHttp(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "callexternalhttpurl")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# InitiateFlowToHTTP HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            
            // sending to http bin container which runs in ACI
            await _httpClient.GetAsync(_httpBinUrl);

            return (ActionResult)new OkObjectResult($"");
        }

        [FunctionName("InitiateFlowToHTTPWithResponseFromB")]
        public async Task<IActionResult> RunHttpWithResp(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "callhttpwithrespfromb")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# InitiateFlowToHTTPWithResponse HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
 
            // Activity.Current.AddBaggage("MyCustomCorrIdInBAGGAGE", "baggagevalue");

            DumpActivity(Activity.Current, log);
            
            // send to the second function app, assuming this runs locally on port 7072
            // TODO: fix this, move this into app settings, populated by ARM template
            await _httpClient.GetAsync($"{this._functionAppBUrl}/api/http");

            return (ActionResult)new OkObjectResult($"");
        }

        [FunctionName("InitiateFlowToLogicAppA")]
        public async Task<IActionResult> RunCallToLogicAppA(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "calllogicappa")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# InitiateFlowToLogicAppA HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
 
            Activity.Current.AddBaggage("MyCustomCorrId", "baggagevalue");

            DumpActivity(Activity.Current, log);
            
            // call Logic App A
            await _httpClient.PostAsync(_logicAppAUrl, null);

            return (ActionResult)new OkObjectResult($"");
        }

        [FunctionName("InitiateFlowToLogicAppAWithBaggage")]
        public async Task<IActionResult> RunCallToLogicAppAWithBaggage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "calllogicappa/{submissionId}")] HttpRequest req,
            string submissionId,
            ILogger log)
        {
            log.LogInformation("C# InitiateFlowToLogicAppAWithBaggage HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
 
            Activity.Current.AddTag("MyCustomCorrId", submissionId);
            Activity.Current.AddBaggage("MyCustomCorrId", submissionId);
            Activity.Current.AddBaggage("SomethingElse", "test123");

            DumpActivity(Activity.Current, log);
            
            // call Logic App A - pass on the body content of what came in
            var objectContent = new StringContent(requestBody);
            objectContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            await _httpClient.PostAsync(_logicAppAUrl, objectContent);

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
