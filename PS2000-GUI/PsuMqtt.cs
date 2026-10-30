using System.Globalization;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Protocol;
using PS2000.Driver;

namespace PS2000_GUI;

// The UI's MQTT client. Publishes commands to the PSU client and keeps the latest state it reports.
public class PsuMqtt(IConfiguration config, ILogger<PsuMqtt> logger) : BackgroundService
{
    private readonly string brokerHost = config["Mqtt:Host"] ?? "localhost";
    private readonly int brokerPort = int.Parse(config["Mqtt:Port"] ?? "1883");
    private readonly string topicPrefix = $"/PSU/PSU2000/{config["Mqtt:PsuId"] ?? "1"}";
    private readonly IMqttClient client = new MqttClientFactory().CreateMqttClient();

    private bool psuOnline;

    public double? Voltage { get; private set; }
    public DeviceInfo? DeviceInfo { get; private set; }
    public string? LastError { get; private set; }

    // We can only reach the PSU when both the broker connection and the PSU client are up.
    public bool Online => client.IsConnected && psuOnline;

    public async Task SendCommand(string command, string payload)
    {
        LastError = null;
        await client.PublishStringAsync($"{topicPrefix}/cmd/{command}", payload, MqttQualityOfServiceLevel.AtLeastOnce);
    }

    // Every second: reconnect to the broker if the connection is down.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        client.ApplicationMessageReceivedAsync += OnMessageReceived;

        while (!stoppingToken.IsCancellationRequested)
        {
            if (!client.IsConnected)
            {
                try
                {
                    await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer(brokerHost, brokerPort).Build(), stoppingToken);
                    await client.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
                        .WithTopicFilter($"{topicPrefix}/state/#", MqttQualityOfServiceLevel.AtLeastOnce)
                        .WithTopicFilter($"{topicPrefix}/status", MqttQualityOfServiceLevel.AtLeastOnce)
                        .Build(), stoppingToken);
                    logger.LogInformation("Connected to MQTT broker {Host}:{Port}", brokerHost, brokerPort);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning("MQTT broker unreachable: {Message}", ex.Message);
                }
            }

            await Task.Delay(1000, stoppingToken);
        }
    }

    private Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic = e.ApplicationMessage.Topic[(topicPrefix.Length + 1)..];
        var payload = e.ApplicationMessage.ConvertPayloadToString();

        switch (topic)
        {
            case "status":
                psuOnline = payload == "online";
                break;
            case "state/voltage":
                if (double.TryParse(payload, CultureInfo.InvariantCulture, out var volts))
                    Voltage = volts;
                break;
            case "state/deviceinfo":
                DeviceInfo = JsonSerializer.Deserialize<DeviceInfo>(payload);
                break;
            case "state/error":
                LastError = string.IsNullOrEmpty(payload) ? null : payload;
                break;
        }

        return Task.CompletedTask;
    }
}
