using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]
public class Startup : FunctionsStartup
{
    // we want to use TelemetryClient, as described here: https://docs.microsoft.com/en-us/azure/azure-functions/functions-monitoring?tabs=cmd#log-custom-telemetry-in-c-functions
    public override void Configure(IFunctionsHostBuilder builder)
    {
        
        // builder.Services.AddHttpClient();
        // System.Net.GlobalProxySelection.Select = new System.Net.WebProxy("127.0.0.1", 8888);
    }
}