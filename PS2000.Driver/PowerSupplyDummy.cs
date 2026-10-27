namespace PS2000.Driver;

internal class PowerSupplyDummy : IPowerSupply
{
    private const float NominalVoltage = 60;

    private double voltage;
    private bool outputOn;
    private bool remoteControlOn;

    public DeviceInfo GetDeviceInfo()
    {
        return new DeviceInfo("EA Dummy", "PS2000 (Dummy)", "555-000-222", "35320100", NominalVoltage);
    }

    public double GetVoltage()
    {
        return voltage;
    }

    public void SetVoltage(double volts)
    {
        if (!double.IsFinite(volts) || volts < 0)
            throw new ArgumentOutOfRangeException(nameof(volts), volts, "Voltage must be finite and non-negative.");

        if (volts > NominalVoltage)
            throw new ArgumentOutOfRangeException(nameof(volts), volts, $"Voltage must not exceed {NominalVoltage} V.");

        voltage = volts;
    }

    public void SetPowerOutput(bool on)
    {
        outputOn = on;
    }

    public void SetRemoteControl(bool on)
    {
        remoteControlOn = on;
    }
}
