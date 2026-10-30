using System.Globalization;
using System.IO.Ports;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Protocol;
using PS2000.Driver;

// MQTT Configuration
var brokerHost = Environment.GetEnvironmentVariable("MQTT_HOST") ?? "localhost";
var brokerPort = int.Parse(Environment.GetEnvironmentVariable("MQTT_PORT") ?? "1883");
var psuId = Environment.GetEnvironmentVariable("PSU_ID") ?? "1";
var topicPrefix = $"/PSU/PSU2000/{psuId}";

// Windows assigns a varying COMx port, Linux always uses /dev/ttyACM0.
var comPort = SerialPort.GetPortNames().FirstOrDefault(port => port.StartsWith("COM") || port == "/dev/ttyACM0");

// Create the power supply object. If no real device is found, use a dummy implementation.
IPowerSupply powerSupply;
if (comPort is not null)
    powerSupply = PowerSupplyFactory.Create(PowerSupplyType.Real, comPort);
else
{
    Console.WriteLine("No PS2000 found, using the dummy power supply.");
    powerSupply = PowerSupplyFactory.Create(PowerSupplyType.Dummy);
}

// Only one thread may use the serial port at a time.
var psuLock = new object();
double? lastPublishedVoltage = null;

var client = new MqttClientFactory().CreateMqttClient();
var options = new MqttClientOptionsBuilder()
    .WithTcpServer(brokerHost, brokerPort)
    .WithCleanSession()
    .WithWillTopic($"{topicPrefix}/status")
    .WithWillPayload("offline")
    .WithWillRetain()
    .WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
    .Build();

client.ApplicationMessageReceivedAsync += async e =>
{
    var topic = e.ApplicationMessage.Topic;
    var payload = e.ApplicationMessage.ConvertPayloadToString();
    Console.WriteLine($"Received {topic} = {payload}");

    try
    {
        ApplyCommand(topic[(topicPrefix.Length + 1)..], payload);
        await Publish("state/error", "", retain: false);
        await PublishVoltage();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Rejected: {ex.Message}");
        await Publish("state/error", ex.Message, retain: false);
    }
};

void ApplyCommand(string command, string payload)
{
    lock (psuLock)
    {
        switch (command)
        {
            case "cmd/voltage":
                powerSupply.SetVoltage(double.Parse(payload, CultureInfo.InvariantCulture));
                break;
            case "cmd/output":
                powerSupply.SetPowerOutput(bool.Parse(payload));
                break;
            case "cmd/remote":
                powerSupply.SetRemoteControl(bool.Parse(payload));
                break;
            default:
                throw new ArgumentException($"Unknown command '{command}'.");
        }
    }
}

// Only publishes when the value changed, to keep the broker quiet.
async Task PublishVoltage()
{
    double voltage;
    lock (psuLock)
        voltage = powerSupply.GetVoltage();

    if (voltage == lastPublishedVoltage)
        return;

    lastPublishedVoltage = voltage;
    await Publish("state/voltage", voltage.ToString("F2", CultureInfo.InvariantCulture), retain: true);
}

async Task Publish(string subTopic, string payload, bool retain)
{
    await client.PublishStringAsync($"{topicPrefix}/{subTopic}", payload, MqttQualityOfServiceLevel.AtLeastOnce, retain);
}

async Task ConnectAndAnnounce()
{
    await client.ConnectAsync(options);
    await client.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
        .WithTopicFilter($"{topicPrefix}/cmd/#", MqttQualityOfServiceLevel.AtLeastOnce)
        .Build());

    await Publish("status", "online", retain: true);

    DeviceInfo info;
    lock (psuLock)
        info = powerSupply.GetDeviceInfo();
    await Publish("state/deviceinfo", JsonSerializer.Serialize(info), retain: true);

    lastPublishedVoltage = null; // Force a fresh voltage publish after (re)connecting.
    Console.WriteLine($"Connected to {brokerHost}:{brokerPort}, listening on {topicPrefix}/cmd/#");
}

// Ctrl+C stops the loop so we can publish "offline" ourselves. The last-will only fires on a lost connection.
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

// Every second: reconnect if the connection dropped, otherwise publish the voltage if it changed.
// The voltage is also read here so changes made on the device itself show up.
while (!cts.IsCancellationRequested)
{
    try
    {
        if (!client.IsConnected)
            await ConnectAndAnnounce();
        else
            await PublishVoltage();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"MQTT error: {ex.Message}");
    }

    await Task.Delay(1000, cts.Token).ContinueWith(_ => { });
}

if (client.IsConnected)
{
    await Publish("status", "offline", retain: true);
    await client.DisconnectAsync();
}
