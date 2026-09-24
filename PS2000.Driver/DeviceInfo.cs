namespace PS2000.Driver;

public record DeviceInfo(
    string Make,
    string Model,
    string SerialNumber,
    string ArticleNumber,
    float NominalVoltage);
