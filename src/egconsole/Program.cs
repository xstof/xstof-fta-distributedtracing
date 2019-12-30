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
using Microsoft.Azure.EventHubs;

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

                // this could be handled by DI - doing it this way would be for a console app run outside of IHostedService
        var iKey = _config.GetValue<string>("iKey");
        var config = GetAppInsightsConfig(iKey);
        _telemClient = new TelemetryClient(config);

        
        // start root activity and record on it the custom activity tags/baggage
        var rootActivity = new Activity("Console Root");

        var submissionId = Guid.NewGuid().ToString();
        Console.WriteLine($"Submission Id is {submissionId}");

        rootActivity.AddTag("MyCustomCorrId", submissionId);
        rootActivity.AddBaggage("MyCustomCorrId", submissionId);

        // start req operation
        var reqOp = _telemClient.StartOperation<RequestTelemetry>(rootActivity);

        var t1=SendEventGridEvents(rootActivity, submissionId);
        var t2 =SendEventHubEvents(rootActivity);

        Task.WaitAll(t1,t2);

        _telemClient.StopOperation(reqOp);  
        
        Console.WriteLine("Use CTRL+C to exit");

                    
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
            module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.ServiceBus");
            module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.EventHubs");
            module.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("core.windows.net");
            module.Initialize(config);

            config.InstrumentationKey = iKey;

            config.TelemetryInitializers.Add(new HttpDependenciesParsingTelemetryInitializer());

            return config;
        }

        private async Task SendEventHubEvents(Activity rootActivity)
        {
            var aehConnectionString = _config.GetValue<string>("aehConnectionString");
            var aehName = _config.GetValue<string>("aehName");

            if ( string.IsNullOrEmpty(aehName) || string.IsNullOrEmpty(aehConnectionString)){
                throw new Exception("The powershell to deploy the function code should have setup this console app appsettings file for AEH");
            }

            var connectionStringBuilder = new EventHubsConnectionStringBuilder(aehConnectionString)
            {
                EntityPath = aehName
            };

            var eventHubClient = EventHubClient.CreateFromConnectionString(connectionStringBuilder.ToString());

            await SendEventHubEvents(eventHubClient, 5);

            await eventHubClient.CloseAsync();

        }

        private static async Task SendEventHubEvents(EventHubClient ehClient , int numMessagesToSend)
        {
            for (var i = 0; i < numMessagesToSend; i++)
            {
                try
                {
                    var message = $"Message {i}";
                    Console.WriteLine($"Sending message: {message}");
                    await ehClient.SendAsync(new EventData(System.Text.Encoding.UTF8.GetBytes(message)));
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"{DateTime.Now} > Exception: {exception.Message}");
                }

                await Task.Delay(10);
            }

            Console.WriteLine($"{numMessagesToSend} AEH messages sent.");
        }

        private async Task SendEventGridEvents(Activity rootActivity, string submissionId)
        {
            var aegTopicUrl = _config.GetValue<string>("aegTopicUrl");
            var aegTopicKey = _config.GetValue<string>("aegTopicKey");

            if ( string.IsNullOrEmpty(aegTopicUrl) || string.IsNullOrEmpty(aegTopicUrl)){
                throw new Exception("The powershell to deploy the function code should have setup this console app appsettings file for AEG");
            }

            // raise event
            using (var httpClient = new HttpClient())
            {
      
                var cloudEvent = new CloudEvent<dynamic>(){
                    SpecVersion = "1.0",
                    Type="com.example.someevent",
                    Source="MyContent",
                    Id ="A234-1234-1234",
                    Time = DateTime.UtcNow.ToString("o"),
                    DataContentType = "application/json",
                    Data=null,
                    TraceParent = Activity.Current.Id,   // <= check this out :-)
                    TraceState=$"MyCustomCorrId={submissionId}"
                };
                Console.WriteLine(cloudEvent.TraceParent);

                var httpRequest = new HttpRequestMessage(HttpMethod.Post,aegTopicUrl);

                httpRequest.Content = new StringContent(JsonSerializer.Serialize(cloudEvent));
                httpRequest.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/cloudevents+json");
                // obtain the key for your AEG deployment
                httpRequest.Headers.Add("aeg-sas-key",aegTopicKey);

                var result =await httpClient.SendAsync(httpRequest);

                 _telemClient.TrackTrace($"Console App Closes EG publish {result.StatusCode}");
            }
  
            Console.WriteLine("Event Grid Event(s) Submitted!");
  
        }
    }
}
