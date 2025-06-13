using Azure.Messaging.ServiceBus;
using Backend;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Rest;
using Newtonsoft.Json;
using ServiceSdkDemo.Lib;
using System.Text;

Console.WriteLine("Podaj IoT Hub connection string:");
string iotHubConnection = Console.ReadLine();

Console.WriteLine("Podaj Service Bus connection string:");
string serviceBusConnection = Console.ReadLine();

Console.WriteLine("Podaj nazwę kolejki KPI:");
string queueKpi = Console.ReadLine();

Console.WriteLine("Podaj nazwę kolejki ErrorCount:");
string queueErrorCount = Console.ReadLine();

Console.WriteLine("Podaj nazwę kolejki ErrorCode:");
string queueErrorCode = Console.ReadLine();


var serviceClient = ServiceClient.CreateFromConnectionString(iotHubConnection);
var registryManager = RegistryManager.CreateFromConnectionString(iotHubConnection);
var manager = new IoTHubManager(serviceClient, registryManager);
var sbClient = new ServiceBusClient(serviceBusConnection);


var queues = new List<(string queueName, Func<AlertMessage, IoTHubManager, Task>)>
{
    (queueKpi, HandleKpiAsync),
    (queueErrorCount, HandleErrorCountAsync),
    (queueErrorCode, HandleErrorCodeAsync)
};

Console.WriteLine("START");

var tasks = queues.Select(async q =>
{
    var receiver = sbClient.CreateReceiver(q.queueName);

    while (true)
    {
        var msg = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(5));
        if (msg == null) continue;

        try
        {
            var alert = JsonConvert.DeserializeObject<AlertMessage>(msg.Body.ToString());
            alert.DeviceId = alert.DeviceId.Replace(" ", "_");

            await q.Item2(alert, manager);
            await receiver.CompleteMessageAsync(msg);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd w kolejce {q.queueName}: {ex.Message}");
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
        Console.WriteLine($"Próba EmergencyStop na {deviceId_}...");
        await manager.ExecuteDeviceMethod("EmergencyStop", deviceId_);
        Console.WriteLine($"Poprawnie wywołano EmergencyStop na {deviceId_}");
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
