namespace OkCoin.API.Options;

public class EndpointsOutSideOption
{
    public static string Key = "EndpointsOutSide";
    public string MainNet { get; set; } = string.Empty;
    public string TestNet { get; set; } = string.Empty;
    public string WalletAddressDetail { get; set; } = string.Empty;
}
