using System;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

public class ServiceBusPublisherService : IHostedService
{
    private readonly IConfiguration _config;
    private readonly TelemetryClient _telemetryClient;
    private readonly MessageSender _msgSender;
    public ServiceBusPublisherService(IConfiguration config, 
                                      TelemetryClient telemetryClient,
                                      MessageSender msgSender){
        this._config = config;
        this._telemetryClient = telemetryClient;
        this._msgSender = msgSender;

        Console.WriteLine($"starting publishing of servicebus messages with ikey: {config.GetValue<string>("iKey")}");
    }


    public async Task StartAsync(CancellationToken cancellationToken)
    {
        string rootId = Guid.NewGuid().ToString();
        Console.WriteLine($"Root Id is {rootId}");

        // Start root activity and record on it the custom activity tags/baggage:
        var rootActivity = new Activity("Console Root");
        rootActivity.AddTag("MyCustomConsoleRootId", rootId);
        rootActivity.AddBaggage("MyBaggage", rootId);

        string batchId = Guid.NewGuid().ToString();
        Console.WriteLine($"Batch Id is {batchId}");

        // Start console root operation:
        var rootOperation = _telemetryClient.StartOperation<RequestTelemetry>(rootActivity);
        Console.WriteLine($"Activity Id of the console root is: {rootActivity.Id}");

        // Publish the batch of messages:
        await publishBatchOfServiceBusMessages(batchId);

        // Finish console root operation:
        _telemetryClient.StopOperation(rootOperation);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _telemetryClient.Flush();
        return Task.CompletedTask;
    }

    private async Task publishBatchOfServiceBusMessages(string batchId)
    {
        var sender = this._msgSender;

        dynamic data = new[]
        {
            new {name = "Einstein", firstName = "Albert"},
            new {name = "Heisenberg", firstName = "Werner"},
            new {name = "Curie", firstName = "Marie"},
            new {name = "Hawking", firstName = "Steven"},
            new {name = "Newton", firstName = "Isaac"},
            new {name = "Bohr", firstName = "Niels"},
            new {name = "Faraday", firstName = "Michael"},
            new {name = "Galilei", firstName = "Galileo"},
            new {name = "Kepler", firstName = "Johannes"},
            new {name = "Kopernikus", firstName = "Nikolaus"}
        };

        for (int i = 0; i < data.Length; i++)
        {
            var message = new Message(Encoding.UTF8.GetBytes(
                JsonSerializer.Serialize(data[i])
            )){
                ContentType = "application/json",
                Label = "Scientist",
                MessageId = i.ToString(),
                TimeToLive = TimeSpan.FromMinutes(2)
            };

            await sender.SendAsync(message);
        }
    }
}