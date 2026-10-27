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

The web application picks a power supply through `PowerSupplyFactory.Create` and only talks to the `IPowerSupply` interface. The driver owns the serial protocol and opens a port for each operation. Voltage must be finite and between zero and the device's nominal voltage.

```
PS2000-Webui.slnx
├── PS2000-GUI/          Front end: ASP.NET minimal API + static web UI (wwwroot/)
└── PS2000.Driver/       Class library: PS2000 serial protocol
    ├── IPowerSupply.cs         Public interface used by the front end
    ├── PowerSupplyFactory.cs   Creates the real or dummy power supply
    ├── DeviceInfo.cs           Device identity and nominal voltage
    ├── PS2000PowerSupply.cs    Internal implementation for the real device
    └── PowerSupplyDummy.cs     Internal in-memory dummy used when no device is found
```

## Dependencies

- [.NET 10 SDK](https://dotnet.microsoft.com/)
- `System.IO.Ports`, used by `PS2000.Driver` (restored automatically via NuGet)
- A PS2000B-series power supply connected over a serial port (optional, a dummy is used without one)

## Build

```bash
dotnet build
```

## Run

```bash
dotnet run --project PS2000-GUI
```

On startup the app looks for the power supply on `/dev/ttyACM0` (Linux) or the first `COM` port (Windows). If none is found, it logs a warning and uses the dummy power supply, which keeps the voltage in memory so the UI can be used without hardware.

Open the app in a browser at the URL shown in the console output. Enable remote control before setting voltage or changing output. Use **Get** to refresh the voltage reading; the page does not poll automatically.
