# DeviceKeyRollover

## Introduction
Sample code for an Azure IoT Hub device that illustrates rolling over the symmetric device key via a cloud-to-device message.  The code is written in C# using .Net Core 8. 

The symmetric device key 

The sample accepts cloud-to-device messages and processes specific requests to roll over the device key.  During the roll over process the device is disconnected from the IoT Hub and reconnected using the newly provided symetrical key.  The trigger for the key roll over is if cloud-to-device message contains a property named "deviceKey" and the expected value is a symmetric device key.

The sample also sends a periodic simple JSON telemetry payload to the IoT Hub every 10 seconds.  The telemetry looks like this (actual valkues are random within the range 20 - 35 and 60 - 80 respectively):

```
{
    "temperature" : 33,
    "humidity" : 67
}
```

Along with a message property "temperatureAlert" that is true or false depending if the temperature is over the threshold of 30.

## Requirements


### Pre-requisites

The code was wriitten using C# and the .Net Core runtimve version 8.0.  .Net core can be downloaded for your OS from [here](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).

The necessary NuGet libraries to compile and run the code are as follows:

| Package Name                                          | Release Version                                           |
| ---                                                   | ---                                                       |
| Microsoft.Azure.Devices.Client                        | [![NuGet][iothub-device-release]][iothub-device-nuget]    |
| Microsoft.Azure.Devices                               | [![NuGet][iothub-service-release]][iothub-service-nuget]  |
| Microsoft.Azure.Devices.Shared                        | [![NuGet][iothub-shared-release]][iothub-shared-nuget]    |
| Microsoft.Azure.Devices.Provisioning.Client           | [![NuGet][dps-device-release]][dps-device-nuget]          |
| Microsoft.Azure.Devices.Provisioning.Transport.Mqtt   | [![NuGet][dps-device-mqtt-release]][dps-device-mqtt-nuget]|

For additional information on the Azure IoT C# SDK see the [GitHub repo here](https://github.com/Azure/azure-iot-sdk-csharp)

Once the pre-requisites have been installed you will need to conadd values specific to your instance of IoT Hub and device you identity you wish to test with.  These values should be entered into the deviceConfig.json file.

```
{
    "DeviceId":"<device name>",
    "HubHostname":"<Azure IoT Hub host name>",
    "DeviceKey":"<Current device key value>"
}
```

The HubHostname value can be gotten from [Azure Portal](https://portal.azure.com) by going to your IoT hub and copying the Hostname in the Overview tab.  Then in the Devices pageyou can find the device identity for the value of DeviceId.  Finally DeviceKey value can be found by clicking on the device you wish to test with then copying the Primary Key.

## Compiling and running

To build the code from the command line use:

```
dotnet build
```

Assuming a successful build you can execue the code with:

```
dotnet run
```

The output should be similar to the following:

```

```
## 
