namespace PS2000.Driver;

public interface IPowerSupply
{
    DeviceInfo GetDeviceInfo();
    double GetVoltage();
    void SetVoltage(double volts);
    void SetPowerOutput(bool on);
    void SetRemoteControl(bool on);
}
