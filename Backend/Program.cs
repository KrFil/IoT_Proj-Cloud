using Azure.Messaging.ServiceBus;
using Backend;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Rest;
using Newtonsoft.Json;
using ServiceSdkDemo.Lib;
using System.Text;


const string serviceBusConnection = "Endpoint=sb://iiot-servicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=CbYbNeLxiQ6a6tArB7K6ilQ2Hk4swhOFv+ASbNfQDGg=";   
const string queueName = "alerts";
const string iotHubConnection = "HostName=ZajAzure.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=OqKYb/zPEkZvkPTtyNvNV+rL8/X7k1zOmAIoTAznNmI=\r\n";

var serviceClient = ServiceClient.CreateFromConnectionString(iotHubConnection);
var sbClient = new ServiceBusClient(serviceBusConnection);
var queueReceiver = sbClient.CreateReceiver(queueName);
var registryManager = RegistryManager.CreateFromConnectionString(iotHubConnection);


var manager = new IoTHubManager(serviceClient, registryManager);

var queues = new List<(string queueName, Func<AlertMessage, IoTHubManager, Task>)>
{
    ("alerts_prod", HandleKpiAsync),
    ("alerts_err", HandleErrorCountAsync),
    ("alerts_cod", HandleErrorCodeAsync)
};


Console.WriteLine("Start receiving from 3 queues...");

var tasks = queues.Select(async q =>
{
    var receiver = sbClient.CreateReceiver(q.Item1);

    while (true)
    {
        var msg = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(5));
        if (msg == null) continue;

        try
        {
            var alert = JsonConvert.DeserializeObject<AlertMessage>(msg.Body.ToString());
            await q.Item2(alert, manager); 
            await receiver.CompleteMessageAsync(msg);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd w kolejce {q.Item1}: {ex.Message}");
        }
    }
});

await Task.WhenAll(tasks);



static async Task HandleKpiAsync(AlertMessage alert, IoTHubManager manager)
{
    Console.WriteLine($"KPI dla {alert.DeviceId}: {alert.GoodProductionPercent}%");

    if (alert.GoodProductionPercent.HasValue && alert.GoodProductionPercent < 90)
    {
        await manager.DecreaseDesiredProductionRate(alert.DeviceId);
    }
}

static async Task HandleErrorCountAsync(AlertMessage alert, IoTHubManager manager)
{
    Console.WriteLine($"Liczba błędów > 3 dla {alert.DeviceId}: {alert.ErrorCount}");

    if (alert.ErrorCount.HasValue && alert.ErrorCount > 3)
    {
        var deviceId_ = alert.DeviceId.Replace(" ", "_");
        await manager.ExecuteDeviceMethod("EmergencyStop", deviceId_);
    }
}

static Task HandleErrorCodeAsync(AlertMessage alert, IoTHubManager manager)
{
    Console.WriteLine($"Nowy kod błędu dla {alert.DeviceId}: {alert.ErrorCode}");

    if (alert.ErrorCode.HasValue)
    {
        EmailService.SendEmail(alert.DeviceId, alert.ErrorCode.Value);
    }

    return Task.CompletedTask;
}
