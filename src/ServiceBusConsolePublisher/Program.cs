using System;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ServiceBusConsolePublisher
{
    class Program
    {

        static async Task Main(string[] args)
        {
            await CreateHostBuilder(args).RunConsoleAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args){
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<ServiceBusPublisherService>();
                    services.AddSingleton<TelemetryClient>( (svcProvider => {
                        var config = svcProvider.GetService<IConfiguration>();
                        var appInsightsConfig = getAppInsightsConfig(config.GetValue<string>("iKey"));
                        return new TelemetryClient(appInsightsConfig);
                    }));
                    services.AddSingleton<MessageSender>((svcProvider) => {
                        var config = svcProvider.GetService<IConfiguration>();
                        var svcBusConnString = config.GetValue<string>("serviceBusConnectionString");
                        var svcBusTopicName = config.GetValue<string>("serviceBusTopicName");
                        return new MessageSender(svcBusConnString, svcBusTopicName);
                    });
                })
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: true);
                });
        }

        private static TelemetryConfiguration getAppInsightsConfig(string iKey){
            var config = TelemetryConfiguration.CreateDefault();
            
            var module = new DependencyTrackingTelemetryModule();
            module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.ServiceBus");
            module.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("core.windows.net");
            module.Initialize(config);

            config.InstrumentationKey = iKey;

            config.TelemetryInitializers.Add(new HttpDependenciesParsingTelemetryInitializer());

            return config;
        }
            
    }
}
