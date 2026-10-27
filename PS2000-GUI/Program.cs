using System.IO.Ports;
using PS2000.Driver;

var builder = WebApplication.CreateBuilder(args);

// Windows assigns a varying COMx port, Linux always uses /dev/ttyACM0.
var comPort = SerialPort.GetPortNames().FirstOrDefault(port => port.StartsWith("COM") || port == "/dev/ttyACM0");

// Create the appropriate power supply instance based on the availability of a COM port.
IPowerSupply powerSupply;
if (comPort is not null)
    powerSupply = PowerSupplyFactory.Create(PowerSupplyType.Real, comPort);
else
    powerSupply = PowerSupplyFactory.Create(PowerSupplyType.Dummy);

var app = builder.Build();

if (comPort is null)
    app.Logger.LogWarning("No PS2000 found, using the dummy power supply.");

var deviceInfo = powerSupply.GetDeviceInfo();
var maxVoltage = $"{deviceInfo.NominalVoltage:F2} V";
var currentVoltage = $"{powerSupply.GetVoltage():F2} V";

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/device", () => new
{
    make = deviceInfo.Make,
    model = deviceInfo.Model,
    serial = deviceInfo.SerialNumber,
    article = deviceInfo.ArticleNumber,
    maxVoltage,
    currentVoltage
});

app.MapPost("/api/output/{on:bool}", (bool on) =>
{
    powerSupply.SetPowerOutput(on);
    return Results.Ok();
});

app.MapPost("/api/remote/{on:bool}", (bool on) =>
{
    powerSupply.SetRemoteControl(on);
    return Results.Ok();
});

app.MapGet("/api/voltage", () =>
{
    return Results.Ok(new { volts = powerSupply.GetVoltage() });
});

app.MapPost("/api/voltage/{volts:double}", (double volts) =>
{
    try
    {
        powerSupply.SetVoltage(volts);
    }
    catch (ArgumentOutOfRangeException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }

    return Results.Ok(new { volts = powerSupply.GetVoltage() });
});

app.Run();
