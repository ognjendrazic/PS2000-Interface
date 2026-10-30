# PS2000 Interface

A small web GUI for controlling a PS2000B-series programmable power supply. The web app sends commands over MQTT to a separate PSU client, which controls the device over its serial (RS-232) protocol and reports its state back.

## Features

- Reads device info (make, model, serial number, article number, nominal voltage)
- View and set the output voltage
- Toggle power output and remote control mode
- Shows when the PSU client is offline, and the last error it reported

## Project structure

The web app and the PSU client do not reference each other. They only communicate through the MQTT broker.

```
PS2000-Webui.slnx
├── PS2000-GUI/          Web app: minimal API + static web UI + UI MQTT client (PsuMqtt.cs)
├── PS2000.PSUClient/    Console app: MQTT client in front of IPowerSupply
└── PS2000.Driver/       Class library: PS2000 serial protocol, IPowerSupply, factory, dummy
```

## Dependencies

- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Mosquitto](https://mosquitto.org/) MQTT broker on port 1883
- A PS2000B-series power supply on a serial port (optional, a dummy is used without one)
- Optional: [MQTT Explorer](https://mqtt-explorer.com/) to inspect the topics

## Broker setup

Install and configure Mosquitto (listener on port 1883, localhost) and MQTT Explorer.

## Topics

Prefix: `/PSU/PSU2000/{id}/` (default id `1`).

| Topic | Direction | Payload | Retained |
|---|---|---|---|
| `cmd/voltage` | UI → PSU | volts, e.g. `12.5` | no |
| `cmd/output` | UI → PSU | `true` / `false` | no |
| `cmd/remote` | UI → PSU | `true` / `false` | no |
| `state/voltage` | PSU → UI | volts, e.g. `12.50` | yes |
| `state/deviceinfo` | PSU → UI | JSON | yes |
| `state/error` | PSU → UI | error message | no |
| `status` | PSU → UI | `online` / `offline` (last will) | yes |

## Build and run

```bash
dotnet build
dotnet run --project PS2000.PSUClient
dotnet run --project PS2000-GUI
```

Run the last two in separate terminals. Without a power supply on `/dev/ttyACM0` or a `COM` port, the PSU client uses the in-memory dummy.

Open the URL shown in the web app's console. **Set** and the toggles only send a command (`202 Accepted`); use **Get** to read the new voltage. If the PSU client is offline, commands are refused with `503`.

The broker address and PSU ID default to `localhost:1883` and `1`. Override them with `MQTT_HOST`, `MQTT_PORT` and `PSU_ID` for the PSU client, or `Mqtt__Host`, `Mqtt__Port` and `Mqtt__PsuId` for the web app.
