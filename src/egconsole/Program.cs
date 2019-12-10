using System;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using eg_webhook_api;
using System.Text.Json;
using Microsoft.ApplicationInsights.DependencyCollector;

namespace egconsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var config = TelemetryConfiguration.CreateDefault();
            var module = new DependencyTrackingTelemetryModule();
            module.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("core.windows.net");
            module.Initialize(config);
            config.InstrumentationKey = "2361c76b-4fe7-4349-bda1-c3fe73b3cefd";
            config.TelemetryInitializers.Add(new HttpDependenciesParsingTelemetryInitializer());
            var client = new TelemetryClient(config);

            var submissionId = Guid.NewGuid().ToString();
            var requestActivity = new Activity("Console App Start");
            requestActivity.Start();
            requestActivity.AddBaggage("MySubmissionId", submissionId);
            var requestOperation = client.StartOperation<RequestTelemetry>(requestActivity);

            using (var httpClient = new HttpClient())
            {
      
                var cloudEvent = new CloudEvent<dynamic>(){
                    SpecVersion = "1.0",
                    Type="com.example.someevent",
                    Source="MyContent",
                    Id ="A234-1234-1234",
                    Time = DateTime.UtcNow.ToString(),
                    DataContentType = "application/json",
                    Data=null,
                    TraceParent = requestActivity.RootId,
                    TraceState=$"MySubmissionId={submissionId}"
                };
                var httpRequest = new HttpRequestMessage(HttpMethod.Post,"https://begim-egtopic-cs.westeurope-1.eventgrid.azure.net/api/events");
                httpRequest.Content = new StringContent(JsonSerializer.Serialize(cloudEvent));
                //httpRequest.Content.Headers.Add("Content-Type","application/cloudevents+json");
                httpRequest.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/cloudevents+json");
                // obtain the key for your AEG deployment
                httpRequest.Headers.Add("aeg-sas-key","7dCVcy0te2hoXEb4lAc2UbUhEVL6RKgQPVqzEdDFqTA=");
                var result =await httpClient.SendAsync(httpRequest);

                Console.WriteLine($"Console App Closes EG publish {result.StatusCode}");
                client.TrackTrace($"Console App Closes EG publish {result.StatusCode}");
            }



            client.StopOperation(requestOperation);
            client.Flush();
 
            Console.WriteLine("Event Submitted!");
            Console.ReadLine();
        }
    }
}
