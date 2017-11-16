using Microsoft.Azure.EventHubs.Processor;
using System;
using Microsoft.Azure.EventHubs;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace IoTHubEventConsumer
{
    class Program
    {
        static string eventHubConnectionString = "{event_hub_connection_string}";
        static string eventHubName = "{event_hub_name}";
        static string storageConnectionString = "{storage_connection_string}";
        static string leaseContainerName = "{lease_container_name}";

        static void Main(string[] args)
        {
            Console.WriteLine("Hello Codiax!");

            //initialize event processor host
            var eventProcessorHost = new EventProcessorHost(eventHubName, "$Default", eventHubConnectionString, storageConnectionString, leaseContainerName);

            //register event processor
            eventProcessorHost.RegisterEventProcessorAsync<EventProcessor>().Wait();

            Console.ReadLine();

            //unregister event processor
            eventProcessorHost.UnregisterEventProcessorAsync().Wait();
        }
    }

    class EventProcessor : IEventProcessor
    {
        public async Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Connection closed on partition {context.PartitionId} with reason {reason}.");
            Console.ResetColor();
        }

        public async Task OpenAsync(PartitionContext context)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Connection opened on partition {context.PartitionId}.");
            Console.ResetColor();
        }

        public async Task ProcessErrorAsync(PartitionContext context, Exception error)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Exception \"{error.Message}\" occured on partition {context.PartitionId}.");
            Console.ResetColor();
        }

        public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            foreach (EventData message in messages)
            {
                Console.WriteLine("Event received:");

                //message is disposable
                using (message)
                {
                    Console.WriteLine($"Timestamp: {message.SystemProperties.EnqueuedTimeUtc}");

                    //get properties
                    var property = message.Properties.FirstOrDefault(p => p.Key == "EventType");
                    Console.WriteLine($"Property: {property.Key}; Value: {property.Value}");

                    //get body
                    var body = message.Body.ToArray();

                    var jsonBody = System.Text.Encoding.UTF8.GetString(body);

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Body: {jsonBody}");
                    Console.ResetColor();
                }
            }

            //create checkpoint
            await context.CheckpointAsync();
        }
    }
}
