using PS2000.Driver;

var builder = WebApplication.CreateBuilder(args);
var comPort = builder.Configuration["PS2000:ComPort"];
if (string.IsNullOrWhiteSpace(comPort))
    throw new InvalidOperationException("Configure PS2000:ComPort before starting the application.");

IPowerSupply powerSupply = PowerSupplyFactory.Create(comPort);
var app = builder.Build();

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
    powerSupply.SetVoltage(volts);
    return Results.Ok(new { volts = powerSupply.GetVoltage() });
});

app.Run();
