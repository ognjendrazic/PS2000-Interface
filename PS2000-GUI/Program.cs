using PS2000_GUI;

var builder = WebApplication.CreateBuilder(args);

// One MQTT client for the whole app, also started as a background service.
builder.Services.AddSingleton<PsuMqtt>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<PsuMqtt>());

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

var psu = app.Services.GetRequiredService<PsuMqtt>();

// Commands only reach the PSU when it is online, so refuse them otherwise instead of losing them silently.
async Task<IResult> Send(string command, string payload)
{
    if (!psu.Online)
        return Results.Json(new { error = "The PSU is offline." }, statusCode: 503);

    await psu.SendCommand(command, payload);
    return Results.Accepted();
}

app.MapGet("/api/status", () => new { online = psu.Online, error = psu.LastError });

app.MapGet("/api/device", () =>
{
    if (psu.DeviceInfo is not { } info)
        return Results.StatusCode(503);

    return Results.Ok(new
    {
        make = info.Make,
        model = info.Model,
        serial = info.SerialNumber,
        article = info.ArticleNumber,
        maxVoltage = $"{info.NominalVoltage:F2} V",
        currentVoltage = psu.Voltage is { } volts ? $"{volts:F2} V" : null
    });
});

app.MapGet("/api/voltage", () =>
{
    if (psu.Voltage is not { } volts)
        return Results.StatusCode(503);

    return Results.Ok(new { volts });
});

app.MapPost("/api/output/{on:bool}", (bool on) => Send("output", on ? "true" : "false"));

app.MapPost("/api/remote/{on:bool}", (bool on) => Send("remote", on ? "true" : "false"));

app.MapPost("/api/voltage/{volts:double}", (double volts) =>
{
    if (psu.DeviceInfo is not { } info)
        return Task.FromResult(Results.Json(new { error = "The PSU is offline." }, statusCode: 503));

    if (!double.IsFinite(volts) || volts < 0 || volts > info.NominalVoltage)
        return Task.FromResult(Results.BadRequest(new { error = $"Voltage must be between 0 and {info.NominalVoltage:F2} V." }));

    return Send("voltage", volts.ToString(System.Globalization.CultureInfo.InvariantCulture));
});

app.Run();
