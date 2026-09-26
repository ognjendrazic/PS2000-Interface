# PS2000 Interface

A small web GUI for controlling a PS2000B-series programmable power supply over its serial (RS-232) protocol.

## Features

- Reads device info on startup (make, model, serial number, article number, nominal voltage)
- View current output voltage
- Set output voltage
- Toggle power output on/off
- Toggle remote control mode on/off
- Simple static web UI served alongside the API

## Project structure

The solution is split into layers. The front end knows nothing about serial ports; it talks to the power supply only through the `IPowerSupply` interface.

```
PS2000-Webui.slnx
├── PS2000-GUI/          Front end: ASP.NET minimal API + static web UI (wwwroot/)
└── PS2000.Driver/       Class library: PS2000 serial protocol
    ├── IPowerSupply.cs         Public interface used by the front end
    ├── PowerSupplyFactory.cs   Static factory: PowerSupplyFactory.Create(comPort)
    ├── DeviceInfo.cs           Device identity and nominal voltage
    └── PS2000PowerSupply.cs    Internal implementation of IPowerSupply
```

```
PS2000-GUI  →  PS2000.Driver  →  PS2000B (serial)
```

## Dependencies

- [.NET 10 SDK](https://dotnet.microsoft.com/)
- `System.IO.Ports`, used by `PS2000.Driver` (restored automatically via NuGet)
- A PS2000B-series power supply connected over a serial port

## Build

```bash
dotnet build
```

## Run

```bash
dotnet run --project PS2000-GUI
```

By default the app connects to `COM3`. Set the serial port via `PS2000-GUI/appsettings.json` (or `PS2000-GUI/appsettings.Development.json`) using the `PS2000:ComPort` key, e.g.:

```json
{
  "PS2000": {
    "ComPort": "/dev/ttyACM0"
  }
}
```

Once running, open the app in a browser at the URL shown in the console output.
