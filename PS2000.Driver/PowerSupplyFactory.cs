namespace PS2000.Driver;

public static class PowerSupplyFactory
{
    public static IPowerSupply Create(string comPort) => new PS2000PowerSupply(comPort);
}
