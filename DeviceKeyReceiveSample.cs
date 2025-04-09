// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Samples
{
    /// <summary>
    /// 
    // This sample demonstrates sending a cloud-to-device message to a device client containing a new symmetric device key for connecting to the IoT Hub.  
    // On receiving the new device key the device disconnects from the IoT Hub and reconnects using the new device key.  The code also contains a simple 
    // functionality sending device telemetry to the hub every 10 seconds to illustrate that it is sending data before and after the device key change.
    //
    // The cloud-to-device message can be sent to the device from the Azure portal blade for the device or using the Azure IoT Explorer application 
    // (https://learn.microsoft.com/en-us/azure/iot/howto-use-iot-explorer).  The new device key should be sent as a property of the cloud-to-device 
    // message with the key name: 'deviceKey' and value: 'the new valid symmetric device key'.
    //
    // Note: The key is stored across sessions in the deviceConfig.json file it should be initialized with correct values prior to the first run of this code.
    // In production it is not recommended to store the symmetric device key in the open like this instead either encrypt the value or store within a secure enclave.
    // It is also advisable that the device use a reported property to indicate the current version or last updated date of the device key so admins know what devices 
    // are on what version of the device key.  Finally note that cloud-to-device messages have a life cycle and when applied to the hub they have a limited life time 
    // before they are expired and will not be sent to the device should it connect after the cloud-to-device message expires.
    //
    // Alternative stratergies for updating the device key might be to use a desired property to send a new device key to a device and once it has been applied to the 
    // device send a reported property status that the key has been changed so it can be deleted from the device twin.  If the devices are online this could also be 
    // performed via a direct method call passing in the new device key.  The processing of the key and subsequent disconnect/reconnect processing will be the same.
    ///
    /// </summary>
    public class DeviceKeyReceiveSample
    {
        private DeviceClient _deviceClient;
        private SemaphoreSlim _processMessageSemaphore;
        private CancellationTokenSource _cts;
        private DeviceConfigHelper _deviceConfigHelper ;
        private static readonly Random _randomGenerator = new();
        private bool _isConnected = false;

        public DeviceKeyReceiveSample(string configFilePath)
        {
            _deviceConfigHelper = new DeviceConfigHelper(configFilePath);

            // connect to the hub using the current device key in the device config class
            var authMethod = new DeviceAuthenticationWithRegistrySymmetricKey(_deviceConfigHelper.DeviceId, _deviceConfigHelper.DeviceKey);
            _deviceClient = DeviceClient.Create(_deviceConfigHelper.HubHostname, authMethod, _deviceConfigHelper.TransportTypeProtocol);
            _isConnected = true;

            _cts = new CancellationTokenSource();
            _processMessageSemaphore = new SemaphoreSlim(1, 1);
        }

        public async Task RunSampleAsync()
        {
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                _cts.Cancel();
                Console.WriteLine("Sample execution cancellation requested; will exit.");
            };
            Console.WriteLine($"{DateTime.Now}> Press Control+C at any time to quit the sample.");

            if (_deviceConfigHelper.TransportTypeProtocol != TransportType.Http1)
            {
                // Now subscribe to receive C2D messages through a callback (which isn't supported over HTTP).
                await _deviceClient.SetReceiveMessageHandlerAsync(OnC2dMessageReceivedAsync, _deviceClient);
                Console.WriteLine($"\n{DateTime.Now}> Subscribed to receive C2D messages over callback.");

                // start the task to send telemetry to the IoT Hub
                var task1 = Task.Run(() => SendMessagesAsync(_cts.Token));

                // Now wait to receive C2D messages through the callback.
                Console.WriteLine($"\n{DateTime.Now}> Device {_deviceConfigHelper.DeviceId} waiting for C2D messages from the hub...");
                Console.WriteLine($"{DateTime.Now}> Use the Azure Portal IoT Hub blade or Azure IoT Explorer to send a message to this device.");

                try
                {
                    await Task.Delay(-1, _cts.Token);
                }
                catch (TaskCanceledException)
                {
                    // Done running.
                    await _deviceClient.CloseAsync();
                    _isConnected = false;
                }
                finally
                {
                    // Now unsubscibe from receiving the callback.
                    await _deviceClient.SetReceiveMessageHandlerAsync(null, null);
                    _processMessageSemaphore.Dispose();
                    _cts.Dispose();
                }
            }
        }

        private static Message PrepareMessage(int messageId)
        {
            const int temperatureThreshold = 30;

            int temperature = _randomGenerator.Next(20, 35);
            int humidity = _randomGenerator.Next(60, 80);
            string messagePayload = $"{{\"temperature\":{temperature},\"humidity\":{humidity}}}";

            var eventMessage = new Message(Encoding.UTF8.GetBytes(messagePayload))
            {
                MessageId = messageId.ToString(),
                ContentEncoding = Encoding.UTF8.ToString(),
                ContentType = "application/json",
            };
            eventMessage.Properties.Add("temperatureAlert", (temperature > temperatureThreshold) ? "true" : "false");

            return eventMessage;
        }

        private void SendMessagesAsync(CancellationToken cancellationToken)
        {
            int messageCount = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_isConnected)
                {
                    Console.WriteLine($"Device sending message {++messageCount} to IoT hub.");
                    using Message message = PrepareMessage(messageCount);
                    _deviceClient.SendEventAsync(message, cancellationToken);
                    Console.WriteLine($"Device sent message {messageCount} to IoT hub.");
                }

                cancellationToken.WaitHandle.WaitOne(10000);
            }
        }

        // Process incoming device-to-cloud messages
        private async Task OnC2dMessageReceivedAsync(Message receivedMessage, object _)
        {
            try
            {
                // Use a semaphore to ensure C2D messages are processed in order - a requirement of IoT hub.
                await _processMessageSemaphore.WaitAsync(_cts.Token).ConfigureAwait(false);
                Console.WriteLine($"{DateTime.Now}> C2D message callback - message received with Id={receivedMessage.MessageId}.");
                PrintMessage(receivedMessage);

                // Acknowledge that the message is received before performing a disconnect and reconnect
                await _deviceClient.CompleteAsync(receivedMessage);
                Console.WriteLine($"{DateTime.Now}> Completed C2D message with Id={receivedMessage.MessageId}.");

                if (receivedMessage.Properties.ContainsKey("deviceKey"))
                {
                    // replace the device key in out device config class
                    _deviceConfigHelper.DeviceKey = receivedMessage.Properties["deviceKey"];

                    // disconnecting from the IoT Hub
                    Console.WriteLine("Disconnecting from IoT Hub");
                    await _deviceClient.CloseAsync();
                    _isConnected = false;

                    // Reconnecting to the IoT Hub with the new device key
                    Console.WriteLine("Reconnecting to the hub with the new device key");
                    var authMethod = new DeviceAuthenticationWithRegistrySymmetricKey(_deviceConfigHelper.DeviceId, _deviceConfigHelper.DeviceKey);
                    _deviceClient = DeviceClient.Create(_deviceConfigHelper.HubHostname, authMethod, _deviceConfigHelper.TransportTypeProtocol);
                    _isConnected = true;
                    Console.WriteLine("Connected to the hub with the new device key");

                    // setup the C2D listener on the the new device client
                    await _deviceClient.SetReceiveMessageHandlerAsync(OnC2dMessageReceivedAsync, _deviceClient);

                    _deviceConfigHelper.SaveConfig();
                    Console.WriteLine("Saving the new device key to the device config JSON file");
                }
            }
            finally
            {
                receivedMessage.Dispose();
                _processMessageSemaphore.Release();
            }
        }

        private static void PrintMessage(Message receivedMessage)
        {
            string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
            var formattedMessage = new StringBuilder($"Received message: [{messageData}]\n");

            // User set application properties can be retrieved from the Message.Properties dictionary.
            foreach (KeyValuePair<string, string> prop in receivedMessage.Properties)
            {
                formattedMessage.AppendLine($"\tProperty: key={prop.Key}, value={prop.Value}");
            }
            // System properties can be accessed using their respective accessors, e.g. ContentType.
            formattedMessage.AppendLine($"\tContent type: {receivedMessage.ContentType}");

            Console.WriteLine($"{DateTime.Now}> {formattedMessage}");
        }
    }
}