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
 using Newtonsoft.Json;

namespace ServiceBusConsoleReceiver
{
    public class ServiceBusReceiverService : IHostedService
    {
        private readonly IConfiguration _config;
        private readonly TelemetryClient _telemetryClient;
        private readonly ISubscriptionClient _subscriptionClient;
        public ServiceBusReceiverService(IConfiguration config,
                                         TelemetryClient telemetryClient,
                                         ISubscriptionClient subClient)
        {
            this._config = config;
            this._telemetryClient = telemetryClient;
            this._subscriptionClient = subClient;

            Console.WriteLine($"starting receiving of servicebus messages with ikey: {config.GetValue<string>("iKey")}");
        }


        public async Task StartAsync(CancellationToken cancellationToken)
        {
            string rootId = Guid.NewGuid().ToString();
            Console.WriteLine($"Root Id is {rootId}");

            // Start root activity and record on it the custom activity tags/baggage:
            var rootActivity = new Activity("Receiver Console Root");

            // Start console root operation:
            var rootOperation = _telemetryClient.StartOperation<RequestTelemetry>(rootActivity);
            Console.WriteLine($"Activity Id of the console root is: {rootActivity.Id}");

            // Receive the batch of messages:
            await receiveMessagesFromTopic(cancellationToken, ConsoleColor.Blue);

            // Finish console root operation:
            _telemetryClient.StopOperation(rootOperation);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _telemetryClient.Flush();
            return Task.CompletedTask;
        }

        private async Task receiveMessagesFromTopic(CancellationToken cancellationToken, ConsoleColor color)
        {
            var receiver = this._subscriptionClient;

            var doneReceiving = new TaskCompletionSource<bool>();
            // close the receiver and factory when the CancellationToken fires 
            cancellationToken.Register(
                async () =>
                {
                    await receiver.CloseAsync();
                    doneReceiving.SetResult(true);
                });

            // register the RegisterMessageHandler callback
            receiver.RegisterMessageHandler(
                async (message, cancellationToken1) =>
                {
                    if (message.Label != null &&
                        message.ContentType != null &&
                        message.Label.Equals("Scientist", StringComparison.InvariantCultureIgnoreCase) &&
                        message.ContentType.Equals("application/json", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var body = message.Body;

                        dynamic scientist = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(body));

                        lock (Console.Out)
                        {
                            Console.ForegroundColor = color;
                            Console.WriteLine(
                                "\t\t\t\tMessage received: \n\t\t\t\t\t\tMessageId = {0}, \n\t\t\t\t\t\tSequenceNumber = {1}, \n\t\t\t\t\t\tEnqueuedTimeUtc = {2}," +
                                "\n\t\t\t\t\t\tExpiresAtUtc = {5}, \n\t\t\t\t\t\tContentType = \"{3}\", \n\t\t\t\t\t\tSize = {4},  \n\t\t\t\t\t\tContent: [ firstName = {6}, name = {7} ]",
                                message.MessageId,
                                message.SystemProperties.SequenceNumber,
                                message.SystemProperties.EnqueuedTimeUtc,
                                message.ContentType,
                                message.Size,
                                message.ExpiresAtUtc,
                                scientist.firstName,
                                scientist.name);
                            Console.ResetColor();
                        }
                        await receiver.CompleteAsync(message.SystemProperties.LockToken);
                    }
                    else
                    {
                        await receiver.DeadLetterAsync(message.SystemProperties.LockToken);//, "ProcessingError", "Don't know what to do with this message");
                }
                },
                new MessageHandlerOptions((e) => LogMessageHandlerException(e)) { AutoComplete = false, MaxConcurrentCalls = 1 });

            await doneReceiving.Task;
        }

        private Task LogMessageHandlerException(ExceptionReceivedEventArgs e)
        {
            Console.WriteLine("Exception: \"{0}\" {0}", e.Exception.Message, e.ExceptionReceivedContext.EntityPath);
            return Task.CompletedTask;
        }
    }
}