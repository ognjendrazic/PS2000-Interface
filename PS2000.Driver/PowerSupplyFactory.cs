namespace PS2000.Driver;

public enum PowerSupplyType
{
    Real,
    Dummy
}

public static class PowerSupplyFactory
{
    public static IPowerSupply Create(PowerSupplyType type, string comPort = "")
    {
        switch (type)
        {
            case PowerSupplyType.Real:
                return new PS2000PowerSupply(comPort);
            case PowerSupplyType.Dummy:
                return new PowerSupplyDummy();
            default:
                throw new ArgumentOutOfRangeException(nameof(type));
        }
    }
}
