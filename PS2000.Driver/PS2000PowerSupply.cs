using System.IO.Ports;
using System.Text;

namespace PS2000.Driver;

internal class PS2000PowerSupply(string comPort) : IPowerSupply
{
    public DeviceInfo GetDeviceInfo()
    {
        using var port = OpenPort();
        var model = QueryString(port, objectNumber: 0);
        var make = QueryString(port, objectNumber: 8);
        var serialNumber = QueryString(port, objectNumber: 1);
        var articleNumber = QueryString(port, objectNumber: 6);
        var nominalVoltage = QueryNominalVoltage(port);

        return new DeviceInfo(make, model, serialNumber, articleNumber, nominalVoltage);
    }

    public double GetVoltage()
    {
        using var port = OpenPort();
        var nominalVoltage = QueryNominalVoltage(port);
        return QueryActualVoltage(port, nominalVoltage);
    }

    public void SetVoltage(double volts)
    {
        if (!double.IsFinite(volts) || volts < 0)
            throw new ArgumentOutOfRangeException(nameof(volts), volts, "Voltage must be finite and non-negative.");

        using var port = OpenPort();
        var nominalVoltage = QueryNominalVoltage(port);
        if (volts > nominalVoltage)
            throw new ArgumentOutOfRangeException(nameof(volts), volts, $"Voltage must not exceed {nominalVoltage} V.");

        SetVoltage(port, nominalVoltage, volts);
    }

    public void SetPowerOutput(bool on)
    {
        using var port = OpenPort();
        SetPowerOutput(port, on);
    }

    public void SetRemoteControl(bool on)
    {
        using var port = OpenPort();
        SetRemoteControl(port, on);
    }

    private SerialPort OpenPort()
    {
        var port = new SerialPort(comPort, 115200, Parity.None, 8, StopBits.One);
        port.Open();
        Thread.Sleep(500);
        return port;
    }

    private byte[] SendQuery(SerialPort port, int objectNumber)
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

    private void SendControl(SerialPort port, int objectNumber, byte[] data)
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

    private string QueryString(SerialPort port, int objectNumber)
    {
        var response = SendQuery(port, objectNumber);
        var dataLength = (response[0] & 0x0F) + 1;
        return Encoding.ASCII.GetString(response, 3, dataLength).TrimEnd('\0');
    }

    private float QueryFloat(SerialPort port, int objectNumber)
    {
        var response = SendQuery(port, objectNumber);
        byte[] floatBytes = [response[6], response[5], response[4], response[3]];
        return BitConverter.ToSingle(floatBytes);
    }

    private float QueryNominalVoltage(SerialPort port)
    {
        var nominalVoltage = QueryFloat(port, objectNumber: 2);
        if (!float.IsFinite(nominalVoltage) || nominalVoltage <= 0)
            throw new InvalidOperationException("PS2000 returned an invalid nominal voltage; expected a finite value greater than zero.");

        return nominalVoltage;
    }

    private double QueryActualVoltage(SerialPort port, float nominalVoltage)
    {
        var response = SendQuery(port, objectNumber: 71);
        var actualVoltagePercent = (response[5] << 8) | response[6];
        return actualVoltagePercent * nominalVoltage / 25600.0;
    }

    private void SetPowerOutput(SerialPort port, bool on)
    {
        SendControl(port, objectNumber: 54, data: [0x01, on ? (byte)0x01 : (byte)0x00]);
    }

    private void SetRemoteControl(SerialPort port, bool on)
    {
        SendControl(port, objectNumber: 54, data: [0x10, on ? (byte)0x10 : (byte)0x00]);
    }

    private void SetVoltage(SerialPort port, float nominalVoltage, double volts)
    {
        var percent = (int)Math.Round(volts / nominalVoltage * 25600);
        SendControl(port, objectNumber: 50, data: [(byte)(percent >> 8), (byte)percent]);
    }
}
