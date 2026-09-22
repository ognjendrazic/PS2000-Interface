using System.IO.Ports;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var deviceMake = "";
var deviceModel = "";
var serialNumber = "";
var articleNumber = "";
var maxVoltage = "";
var currentVoltage = "";
var nominalVoltage = 0f;

try
{
    using var port = OpenPort();

    deviceModel = QueryString(port, objectNumber: 0);
    deviceMake = QueryString(port, objectNumber: 8);
    serialNumber = QueryString(port, objectNumber: 1);
    articleNumber = QueryString(port, objectNumber: 6);

    nominalVoltage = QueryFloat(port, objectNumber: 2);
    maxVoltage = $"{nominalVoltage:F2} V";
    currentVoltage = $"{QueryActualVoltage(port, nominalVoltage):F2} V";
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Could not read device info from PS2000 at startup");
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/device", () => new
{
    make = deviceMake,
    model = deviceModel,
    serial = serialNumber,
    article = articleNumber,
    maxVoltage,
    currentVoltage
});

app.MapPost("/api/output/{on:bool}", (bool on) =>
{
    using var port = OpenPort();
    SetPowerOutput(port, on);
    return Results.Ok();
});

app.MapPost("/api/remote/{on:bool}", (bool on) =>
{
    using var port = OpenPort();
    SetRemoteControl(port, on);
    return Results.Ok();
});

app.MapGet("/api/voltage", () =>
{
    using var port = OpenPort();
    return Results.Ok(new { volts = QueryActualVoltage(port, nominalVoltage) });
});

app.MapPost("/api/voltage/{volts:double}", (double volts) =>
{
    using var port = OpenPort();
    SetVoltage(port, nominalVoltage, volts);
    return Results.Ok(new { volts = QueryActualVoltage(port, nominalVoltage) });
});

app.Run();

SerialPort OpenPort()
{
    var comPort = builder.Configuration["PS2000:ComPort"] ?? "COM3";
    var port = new SerialPort(comPort, 115200, Parity.None, 8, StopBits.One);
    port.Open();
    Thread.Sleep(500);
    return port;
}

byte[] SendQuery(SerialPort port, int objectNumber)
{
    const int startDelimiter = 0x7F;
    const int deviceNode = 0x00;
    var checksum = startDelimiter + deviceNode + objectNumber;

    byte[] query =
    [
        (byte)startDelimiter,
        (byte)deviceNode,
        (byte)objectNumber,
        (byte)(checksum >> 8),
        (byte)checksum
    ];

    port.Write(query, 0, query.Length);
    Thread.Sleep(500);

    var response = new byte[port.BytesToRead];
    port.Read(response, 0, response.Length);
    return response;
}

void SendControl(SerialPort port, int objectNumber, byte[] data)
{
    const int deviceNode = 0x00;
    var startDelimiter = 0xF0 + (data.Length - 1);

    var checksum = startDelimiter + deviceNode + objectNumber;
    foreach (var dataByte in data)
        checksum += dataByte;

    var frame = new byte[3 + data.Length + 2];
    frame[0] = (byte)startDelimiter;
    frame[1] = (byte)deviceNode;
    frame[2] = (byte)objectNumber;
    data.CopyTo(frame, 3);
    frame[^2] = (byte)(checksum >> 8);
    frame[^1] = (byte)checksum;

    port.Write(frame, 0, frame.Length);
    Thread.Sleep(500);

    var response = new byte[port.BytesToRead];
    port.Read(response, 0, response.Length);

    var errorCode = response[3];
    if (errorCode != 0)
        throw new Exception($"PS2000 rejected object {objectNumber} with error code {errorCode}");
}

string QueryString(SerialPort port, int objectNumber)
{
    var response = SendQuery(port, objectNumber);
    var dataLength = (response[0] & 0x0F) + 1;
    return Encoding.ASCII.GetString(response, 3, dataLength).TrimEnd('\0');
}

float QueryFloat(SerialPort port, int objectNumber)
{
    var response = SendQuery(port, objectNumber);
    byte[] floatBytes = [response[6], response[5], response[4], response[3]];
    return BitConverter.ToSingle(floatBytes);
}

double QueryActualVoltage(SerialPort port, float nominalVoltage)
{
    var response = SendQuery(port, objectNumber: 71);
    var actualVoltagePercent = (response[5] << 8) | response[6];
    return actualVoltagePercent * nominalVoltage / 25600.0;
}

void SetPowerOutput(SerialPort port, bool on)
{
    SendControl(port, objectNumber: 54, data: [0x01, on ? (byte)0x01 : (byte)0x00]);
}

void SetRemoteControl(SerialPort port, bool on)
{
    SendControl(port, objectNumber: 54, data: [0x10, on ? (byte)0x10 : (byte)0x00]);
}

void SetVoltage(SerialPort port, float nominalVoltage, double volts)
{
    var percent = (int)Math.Round(volts / nominalVoltage * 25600);
    SendControl(port, objectNumber: 50, data: [(byte)(percent >> 8), (byte)percent]);
}
