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
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "{submissionId}")] HttpRequest req,
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

        [FunctionName("InitiateFlowToHTTP")]
        public async Task<IActionResult> RunHttp(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# InitiateFlowToHTTP HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            
            // to see results of this call, go to: http://requestbin.net/r/1ccpqtm1?inspect
            await _httpClient.GetAsync("http://requestbin.net/r/1ccpqtm1");

            return (ActionResult)new OkObjectResult($"");
        }
    }
}
