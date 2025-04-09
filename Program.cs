// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//using CommandLine;
using System;
using System.Reflection.Metadata;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class Program
    {        
        /// <summary>
        /// A sample to illustrate how to send a C2D message to a device to roll over the device key used by the device to connect to IoT Hub
        /// </summary>

        const string configFilePath = "/Users/ianhollier/Documents/code/firmware/deviceConfig.json";

        public static async Task<int> Main(string[] args)
        {
            var sample = new DeviceKeyReceiveSample(configFilePath);

            await sample.RunSampleAsync();

            Console.WriteLine("Done.");
            return 0;
        }
    }
}