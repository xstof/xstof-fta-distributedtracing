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
// use the config service?

            var config = TelemetryConfiguration.CreateDefault();
            var module = new DependencyTrackingTelemetryModule();
            module.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("core.windows.net");
            module.Initialize(config);

            config.InstrumentationKey = "4deeb3cd-f582-414c-96a0-64d5eee2eccb";

            config.TelemetryInitializers.Add(new HttpDependenciesParsingTelemetryInitializer());
            var client = new TelemetryClient(config);

            // start req operation
            var reqOp = client.StartOperation<RequestTelemetry>("ConsoleStart");
            var operationId = reqOp.Telemetry.Id.Replace("|", "").Split('.')[0];
            var requestId = reqOp.Telemetry.Id.Replace("|", "").Split('.')[1];

            var submissionId = Guid.NewGuid().ToString();
            
            // start dep operation
            var dependencyOperation = client.StartOperation<DependencyTelemetry>($"EventGridDependency", operationId, requestId );

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
                    TraceParent = Activity.Current.Id,   // <= check this out :-)
                    TraceState=$"MySubmissionId={submissionId}"
                };

                var httpRequest = new HttpRequestMessage(HttpMethod.Post,"https://aicorr4-egtopic-cs.westeurope-1.eventgrid.azure.net/api/events");
                httpRequest.Content = new StringContent(JsonSerializer.Serialize(cloudEvent));
                //httpRequest.Content.Headers.Add("Content-Type","application/cloudevents+json");
                httpRequest.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/cloudevents+json");
                // obtain the key for your AEG deployment
                httpRequest.Headers.Add("aeg-sas-key","uzQg+BiUSRIQc0i2/9DNqcSPt8x+lNLNnSKTH3DBL2k=");

                var result =await httpClient.SendAsync(httpRequest);

                //stop dep operation
                client.StopOperation(dependencyOperation);

                // stop req operation
                client.StopOperation(reqOp);

                Console.WriteLine($"Console App Closes EG publish {result.StatusCode}");
                client.TrackTrace($"Console App Closes EG publish {result.StatusCode}");
            }
            
            client.Flush();
 
            Console.WriteLine("Event Submitted!");
            Console.ReadLine();
        }
    }
}
