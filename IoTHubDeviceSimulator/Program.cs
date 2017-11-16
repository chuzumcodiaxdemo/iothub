using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Threading.Tasks;

namespace IoTHubDeviceSimulator
{
    class Program
    {
        static string iotHubHostName = "{iot_hub_host_name}";
        static DeviceClient client;
        static string deviceId;
        static string deviceKey;
        static TwinCollection reportedProperties = new TwinCollection();

        static void Main(string[] args)
        {
            Console.WriteLine("Hello Codiax!");

            while (true)
            {
                Console.WriteLine("Options:");
                Console.WriteLine("1 - Set device");
                Console.WriteLine("2 - Publish event");
                Console.WriteLine("x - Exit");
                Console.Write("Option: ");
                var option = Console.ReadLine();
                if (option == "x")
                {
                    client.SetMethodHandlerAsync("StartWaterPump", null, null).Wait();
                    client.CloseAsync().Wait();
                    break;
                }
                switch (option)
                {
                    case "1":
                        SetDevice();
                        break;
                    case "2":
                        PublishEvent();
                        break;
                    default:
                        Console.WriteLine("Option not valid!");
                        break;
                }
            }
        }

        static void SetDevice()
        {
            Console.WriteLine();
            Console.Write("Device Id: ");
            deviceId = Console.ReadLine();

            Console.Write("Device Key: ");
            deviceKey = Console.ReadLine();

            //connection string
            string deviceConnectionString = $"HostName={iotHubHostName};DeviceId={deviceId};SharedAccessKey={deviceKey}";

            //initialize client
            client = DeviceClient.CreateFromConnectionString(deviceConnectionString);

            //start receiving messages
            //ReceiveMessages();

            //set handler for direct method
            //client.SetMethodHandlerAsync("StartWaterPump", StartWaterPump, null).Wait();

            //InitTwinProperties();
            //set handler for direct method
            //client.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChanged, null).Wait();
        }

        static void PublishEvent()
        {
            var random = new Random();
            var temperature = random.Next(10, 30);

            //create body
            var body = new
            {
                Temperature = temperature,
            };

            //serialize JSON
            var jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(body);

            //create message
            Message message = new Message(System.Text.Encoding.UTF8.GetBytes(jsonBody));

            //add properties
            message.Properties.Add("EventType", "Temperature");

            //send event
            client.SendEventAsync(message).Wait();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Temperature sent to IoT Hub: {temperature}");
            Console.ResetColor();
        }

        static async Task ReceiveMessages()
        {
            Console.WriteLine("Receiving cloud to device messages");
            while (true)
            {
                //receive messages
                Message message = await client.ReceiveAsync();
                if (message == null)
                {
                    continue;
                }

                //get message body
                var body = message.GetBytes();

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Received message: {0}", System.Text.Encoding.UTF8.GetString(body));
                Console.ResetColor();

                await client.CompleteAsync(message);
            }
        }

        static Task<MethodResponse> StartWaterPump(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"StartWaterPump method invoked with request: {methodRequest.DataAsJson}");

            //get request data
            dynamic request = Newtonsoft.Json.JsonConvert.DeserializeObject(methodRequest.DataAsJson);

            var result = new
            {
                Result = $"Pump was started for {request.Time} seconds."
            };

            //serialize JSON
            var resultJson = Newtonsoft.Json.JsonConvert.SerializeObject(result);

            //return result
            return Task.FromResult(new MethodResponse(System.Text.Encoding.UTF8.GetBytes(resultJson), 200));
        }

        static async Task InitTwinProperties()
        {
            Console.WriteLine("Report initial running config:");

            //set reported property
            TwinCollection runningConfig = new TwinCollection
            {
                ["configId"] = "1",
                ["runningState"] = "Stopped"
            };
            reportedProperties["runningConfig"] = runningConfig;

            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(reportedProperties));

            //update reported properties
            await client.UpdateReportedPropertiesAsync(reportedProperties);
        }

        static async Task OnDesiredPropertyChanged(TwinCollection desiredProperties, object userContext)
        {
            Console.WriteLine("Desired property change:");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(desiredProperties));
            Console.ResetColor();

            var currentPropertyConfig = reportedProperties["runningConfig"];
            var desiredPropertyConfig = desiredProperties["runningConfig"];

            if (desiredPropertyConfig != null)
            {
                Console.WriteLine("Initiating config change");
                currentPropertyConfig["status"] = "Pending";
                currentPropertyConfig["pendingConfig"] = desiredPropertyConfig;

                //update reported properties
                await client.UpdateReportedPropertiesAsync(reportedProperties);

                CompleteConfigChange();
            }
        }

        static async void CompleteConfigChange()
        {
            var currentRunningConfig = reportedProperties["runningConfig"];

            Console.WriteLine("Simulating running state changed...");
            await Task.Delay(10 * 1000); //delay 10 seconds

            Console.WriteLine("Completing config change...");
            
            currentRunningConfig["runningState"] = currentRunningConfig["pendingConfig"]["runningState"];
            currentRunningConfig["status"] = "Success";
            currentRunningConfig["pendingConfig"] = null;

            //update reported properties
            await client.UpdateReportedPropertiesAsync(reportedProperties);

            Console.WriteLine("Config change complete.");
        }
    }
}
