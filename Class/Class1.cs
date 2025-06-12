using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using System.Text;

namespace ServiceSdkDemo.Lib
{
    
    public class IoTHubManager
    {
        private readonly ServiceClient client;
        private readonly RegistryManager registry;

        public IoTHubManager(ServiceClient client, RegistryManager registry)
        {
            this.client = client;
            this.registry = registry;
        }

        public async Task<int> ExecuteDeviceMethod(string methodName, string deviceId)
        {
            deviceId = deviceId.Replace(" ", "_");
            var method = new CloudToDeviceMethod(methodName);

            var result = await client.InvokeDeviceMethodAsync(deviceId, method);
            return result.Status;
        }

        public async Task DecreaseDesiredProductionRate(string deviceId)
        {
            Twin twin;
            deviceId = deviceId.Replace(" ", "_");
            try
            {
                twin = await registry.GetTwinAsync(deviceId);
            }
            catch (DeviceNotFoundException)
            {
                Console.WriteLine($"Urządzenie '{deviceId}' nie istnieje.");
                return;
            }

            if (twin == null)
            {
                Console.WriteLine($"Nie udało się pobrać Twin'a dla '{deviceId}'.");
                return;
            }

            int currentRate = 100;
            if (twin.Properties?.Desired != null && twin.Properties.Desired.Contains("ProductionRate"))
            {
                currentRate = (int)twin.Properties.Desired["ProductionRate"];
            }

            int newRate = Math.Max(currentRate - 10, 0);

            var newTwin = new Twin();
            newTwin.Properties.Desired["ProductionRate"] = newRate;

            await registry.UpdateTwinAsync(deviceId, newTwin, twin.ETag);

            Console.WriteLine($"Zmieniono ProductionRate: {currentRate} → {newRate}% dla {deviceId}");
        }





    }
}
