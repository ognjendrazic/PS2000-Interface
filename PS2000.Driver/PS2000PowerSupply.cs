namespace PS2000.Driver;

internal class PS2000PowerSupply(string comPort) : IPowerSupply
{
    public DeviceInfo GetDeviceInfo() => throw new NotImplementedException();

    public double GetVoltage() => throw new NotImplementedException();

    public void SetVoltage(double volts) => throw new NotImplementedException();

    public void SetPowerOutput(bool on) => throw new NotImplementedException();

    public void SetRemoteControl(bool on) => throw new NotImplementedException();
}
