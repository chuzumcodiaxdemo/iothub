﻿using Microsoft.Azure.Devices;
using System;

namespace IoTHubDeviceManager
{
    class Program
    {
        static string iotHubConnectionString = "{iot_hub_connection_string}";

        //initialize manager
        static RegistryManager manager = RegistryManager.CreateFromConnectionString(iotHubConnectionString);

        //initialize service client
        static ServiceClient client = ServiceClient.CreateFromConnectionString(iotHubConnectionString);

        static void Main(string[] args)
        {
            Console.WriteLine("Hello Codiax!");

            while (true)
            {
                Console.WriteLine("Options:");
                Console.WriteLine("1 - List devices");
                Console.WriteLine("2 - Create device");
                Console.WriteLine("3 - Send message");
                Console.WriteLine("4 - Invoke direct method");
                Console.WriteLine("5 - Read reported property");
                Console.WriteLine("6 - Set desired property");
                Console.WriteLine("x - Exit");
                Console.Write("Option: ");
                var option = Console.ReadLine();
                if (option == "x")
                {
                    break;
                }
                switch (option)
                {
                    case "1":
                        ListDevices();
                        break;
                    case "2":
                        CreateDevice();
                        break;
                    case "3":
                        SendMessage();
                        break;
                    case "4":
                        InvokeMethod();
                        break;
                    case "5":
                        ReadReportedProperties();
                        break;
                    case "6":
                        SetDesiredProperty();
                        break;
                    default:
                        Console.WriteLine("Option not valid!");
                        break;
                }
            }
            manager.CloseAsync().Wait();
            client.CloseAsync().Wait();
        }

        static void ListDevices()
        {
            //get device list
            var list = manager.GetDevicesAsync(1000).Result;

            foreach (var device in list)
            {
                //print device
                Console.WriteLine($"DeviceId: {device.Id}; DeviceKey: {device.Authentication.SymmetricKey.PrimaryKey}");
            }
        }

        static void CreateDevice()
        {
            Console.WriteLine();
            Console.Write("Device Id: ");
            var deviceId = Console.ReadLine();

            //create device
            var device = new Device(deviceId)
            {
                //authentication key
                Authentication = new AuthenticationMechanism
                {
                    SymmetricKey = new SymmetricKey
                    {
                        //base64 keys
                        PrimaryKey = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                        SecondaryKey = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                    }
                }
            };

            manager.AddDeviceAsync(device).Wait();
        }

        static void SendMessage()
        {
            Console.WriteLine();
            Console.Write("Device Id: ");
            var deviceId = Console.ReadLine();

            var random = new Random();

            //create body
            var body = new
            {
                RandomNumber = random.Next(),
            };

            //serialize JSON
            var jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(body);

            //create message
            Message message = new Message(System.Text.Encoding.UTF8.GetBytes(jsonBody));

            //send event
            client.SendAsync(deviceId, message).Wait();

            Console.WriteLine($"Message sent to device {deviceId}: {jsonBody}");
        }

        static void InvokeMethod()
        {
            Console.WriteLine();
            Console.Write("Device Id: ");
            var deviceId = Console.ReadLine();

            var random = new Random();

            //create request
            var request = new
            {
                Time = random.Next(0, 5),
            };

            //initialize method invocation
            var methodInvocation = new CloudToDeviceMethod("StartWaterPump") { ResponseTimeout = TimeSpan.FromSeconds(30) };

            //set request data
            methodInvocation.SetPayloadJson(Newtonsoft.Json.JsonConvert.SerializeObject(request));

            //get result
            var response = client.InvokeDeviceMethodAsync(deviceId, methodInvocation).Result;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Method invoked with status {response.Status} and body: {response.GetPayloadAsJson()}");
            Console.ResetColor();
        }

        static void ReadReportedProperties()
        {
            Console.WriteLine();
            Console.Write("Device Id: ");
            var deviceId = Console.ReadLine();

            //create query
            var query = manager.CreateQuery($"SELECT * FROM devices WHERE deviceId = '{deviceId}'");

            //fetch results
            var results = query.GetNextAsTwinAsync().Result;

            foreach (var result in results)
            {
                Console.WriteLine("Config report for: {0}", result.DeviceId);

                //get desired property
                var desiredProperty = result.Properties.Desired.Contains("runningConfig")
                    ? result.Properties.Desired["runningConfig"]
                    : null;

                //get reported property
                var reportedProperty = result.Properties.Reported.Contains("runningConfig")
                    ? result.Properties.Reported["runningConfig"]
                    : null;

                Console.WriteLine("Desired runningConfig: {0}", Newtonsoft.Json.JsonConvert.SerializeObject(desiredProperty));
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Reported runningConfig: {0}", Newtonsoft.Json.JsonConvert.SerializeObject(reportedProperty));
                Console.ResetColor();
            }
        }

        static void SetDesiredProperty()
        {
            Console.WriteLine();
            Console.Write("Device Id: ");
            var deviceId = Console.ReadLine();

            Console.WriteLine();
            Console.Write("Running state: ");
            var state = Console.ReadLine();

            //get device twin
            var twin = manager.GetTwinAsync(deviceId).Result;
            var property = new
            {
                properties = new
                {
                    desired = new
                    {
                        runningConfig = new
                        {
                            runningState = state
                        }
                    }
                }
            };

            //serialize property
            var jsonProperty = Newtonsoft.Json.JsonConvert.SerializeObject(property);

            //update twin desired property
            manager.UpdateTwinAsync(twin.DeviceId, jsonProperty, twin.ETag);
            Console.WriteLine("Updated desired property");
        }
    }
}
