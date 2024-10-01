namespace OkCoin.API.ViewModels;

public class BoostItemViewModel
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public decimal Price { get; set; } = 0.0m;
    public bool IsToken { get; set; } = true;
    public decimal Value { get; set; } = 0.0m;
    public BoostType Type { get; set; }
    public int Level { get; set; } = 1;
}

public enum BoostType
{
    RechargeSpeed,
    MultiTap,
    EnergyLimit,
    TapBot,
    PremiumBot,
}