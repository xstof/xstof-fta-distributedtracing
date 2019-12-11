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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;

namespace egconsole
{
    class Program : IHostedService{

        
    public static async Task Main(string[] args)
    {
        await CreateHostBuilder(args).RunConsoleAsync();
    }


    private readonly IConfiguration _config;
    private TelemetryClient _telemClient;

    public Program (IConfiguration config){
        _config = config;
    }

      public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
               services.AddHostedService<Program>();
            })
            .ConfigureAppConfiguration((hostingContext, config) => {
                config.AddJsonFile("appsettings.json", optional: true);


            });

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var aegTopicUrl = _config.GetValue<string>("aegTopicUrl");
        var aegTopicKey = _config.GetValue<string>("aegTopicKey");
        var iKey = _config.GetValue<string>("iKey");

        if ( string.IsNullOrEmpty(aegTopicUrl) || string.IsNullOrEmpty(aegTopicUrl)){
            throw new Exception("The powershell to deploy the function code should have setup this console app appsettings file");
        }

        // this could be handled by DI - doing it this way would be for a console app run outside of IHostedService
        var config = GetAppInsightsConfig(iKey);
        _telemClient = new TelemetryClient(config);

        RunConsoleApp(aegTopicUrl, aegTopicKey);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
                  
        _telemClient.Flush();
        Console.WriteLine("AI Flushed, Closing");
        return Task.CompletedTask;
    }


        private TelemetryConfiguration GetAppInsightsConfig(string iKey){
            var config = TelemetryConfiguration.CreateDefault();
            var module = new DependencyTrackingTelemetryModule();
            module.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("core.windows.net");
            module.Initialize(config);

            config.InstrumentationKey = iKey;

            config.TelemetryInitializers.Add(new HttpDependenciesParsingTelemetryInitializer());

            return config;
        }

        private async Task RunConsoleApp(string aegTopicUrl, string aegTopicKey)
        {



            // start req operation
            var reqOp = _telemClient.StartOperation<RequestTelemetry>("ConsoleStart");
            var operationId = reqOp.Telemetry.Id.Replace("|", "").Split('.')[0];
            var requestId = reqOp.Telemetry.Id.Replace("|", "").Split('.')[1];

            var submissionId = Guid.NewGuid().ToString();
            
            // start dep operation
            var dependencyOperation = _telemClient.StartOperation<DependencyTelemetry>($"EventGridDependency", operationId, requestId );

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


                var httpRequest = new HttpRequestMessage(HttpMethod.Post,aegTopicUrl);

                httpRequest.Content = new StringContent(JsonSerializer.Serialize(cloudEvent));
                //httpRequest.Content.Headers.Add("Content-Type","application/cloudevents+json");
                httpRequest.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/cloudevents+json");
                // obtain the key for your AEG deployment

                httpRequest.Headers.Add("aeg-sas-key",aegTopicKey);

                var result =await httpClient.SendAsync(httpRequest);

                //stop dep operation
                _telemClient.StopOperation(dependencyOperation);

                // stop req operation
                _telemClient.StopOperation(reqOp);

                _telemClient.TrackTrace($"Console App Closes EG publish {result.StatusCode}");
            }
  
 
            Console.WriteLine("Event Submitted! Use CTRL+C to exit");
  
        }
    }
}
