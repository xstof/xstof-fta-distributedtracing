using System.Linq;
// using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]
public class Startup : FunctionsStartup
{
    // we want to use TelemetryClient, as described here: https://docs.microsoft.com/en-us/azure/azure-functions/functions-monitoring?tabs=cmd#log-custom-telemetry-in-c-functions
    public override void Configure(IFunctionsHostBuilder builder)
    {
        //see: https://stackoverflow.com/questions/58397520/how-to-use-dependency-inject-for-telemetryconfiguration-in-azure-function
        // var configDescriptor = builder.Services.SingleOrDefault(tc => tc.ServiceType == typeof(TelemetryConfiguration));
        // if (configDescriptor?.ImplementationFactory != null)
        // {
        //     var implFactory = configDescriptor.ImplementationFactory;
        //     builder.Services.Remove(configDescriptor);
        //     builder.Services.AddSingleton(provider =>
        //     {
        //         if (implFactory.Invoke(provider) is TelemetryConfiguration config)
        //         {
        //             var newConfig = TelemetryConfiguration.Active;
        //             newConfig.ApplicationIdProvider = config.ApplicationIdProvider;
        //             newConfig.InstrumentationKey = config.InstrumentationKey;

        //             return newConfig;
        //         }
        //         return null;
        //     });
        // }


        // builder.Services.AddHttpClient();
        // System.Net.GlobalProxySelection.Select = new System.Net.WebProxy("127.0.0.1", 8888);
    }
}