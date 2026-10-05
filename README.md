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

The solution has three logical layers: the web application, the driver library, and the physical PSU. The web application reads the port configuration and creates an `IPowerSupply` through `PowerSupplyFactory.Create(comPort)`. Startup reads and API endpoints use that interface; HTTP responses and display formatting remain in the web application.

The driver owns serial communication, protocol frames, checksums, response decoding, and voltage scaling. It opens and disposes a port for each public operation. Voltage operations read the nominal voltage internally, so they do not require a prior call to `GetDeviceInfo()`. Voltage settings must be finite and between zero and the device's nominal voltage.

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

Connect the PSU and set `PS2000:ComPort` in `PS2000-GUI/appsettings.json` (or `PS2000-GUI/appsettings.Development.json` when running in Development). The checked-in setting is `/dev/ttyACM0` for Linux. On Windows, use the actual COM port assigned to the PSU, for example:

```json
{
  "PS2000": {
    "ComPort": "COM3"
  }
}
```

`COM3` is only an example, not a fallback. Missing or blank configuration stops startup with a configuration error. The app also stops if it cannot open the port or read device information and voltage; it does not start with placeholder device values.

From the solution directory, run:

```bash
dotnet run --project PS2000-GUI
```

Once running, open the app in a browser at the URL shown in the console output. Enable remote control before setting voltage or changing output. Use **Get** to refresh the voltage reading; the dashboard does not poll automatically.

## Verification status

Software checks completed on October 5, 2026:

- Release solution build passed with zero warnings and errors.
- Missing port configuration stopped startup with a clear configuration error.
- A nonexistent port stopped startup and identified the unavailable port.
- Serial and protocol implementation details reside in the driver library; the GUI has no direct `System.IO.Ports` package reference.

Physical testing was deferred because the PSU was not connected. The following checks remain pending:

- Confirm device identity and nominal voltage against the connected PSU.
- Compare voltage readback with the PSU display.
- In a suitable bench setup, enable remote control, set a suitable test voltage, and verify the result.
- Verify power output on/off and remote control on/off against the PSU's actual state.

The build and startup checks do not establish that hardware operations work correctly.
