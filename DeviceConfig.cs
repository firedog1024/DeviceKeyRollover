using System;
using System.Security.Cryptography;
using System.Globalization;
using System.Web;
using System.Text;
using System.Configuration;
using System.Text.Json;

namespace Microsoft.Azure.Devices.Client.Samples
{
    /// <summary>
    /// Device Configuration object for serialization
    /// </summary>
    internal class DeviceConfig
    {
        public string DeviceId  { get; set; } = string.Empty;
        public string  HubHostname  { get; set; } = string.Empty;
        public string DeviceKey  { get; set; } = string.Empty;
    }

    /// <summary>
    /// Device Configuration Helper class for serializing/deserializing the device configuration
    /// </summary>
    internal class DeviceConfigHelper
    {
        private DeviceConfig _deviceConfig;
        private string _filePath = string.Empty;

        public DeviceConfigHelper(string configFilePath)
        {
            _filePath = configFilePath;

            if (Path.Exists(_filePath))
            {
                // read in the values from the config data file
                using FileStream stream = File.OpenRead(_filePath);
                _deviceConfig = JsonSerializer.Deserialize<DeviceConfig>(stream);
            }
            else
            {
                _deviceConfig = new DeviceConfig(); // needs better default values
            }
        }

        public bool SaveConfig()
        {
            // prevent the unicode escaping in the device key value
            JsonSerializerOptions jso = new JsonSerializerOptions();
            jso.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

            using FileStream stream = File.Create(_filePath);
            JsonSerializer.Serialize(stream, _deviceConfig, jso);
            stream.Flush();
            return true;
        }

        public string DeviceId
        {
            get => _deviceConfig.DeviceId;       
        }

        public string HubHostname
        {
            get => _deviceConfig.HubHostname;       
        }

        public string DeviceKey
        {
            get => _deviceConfig.DeviceKey;
            set
            {
                _deviceConfig.DeviceKey = value;
            }
        }

        public TransportType TransportTypeProtocol     
        {
            get => TransportType.Mqtt;       
        }
    }
}