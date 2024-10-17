using MongoDB.Bson.Serialization.Attributes;
using OkCoin.API.Models;
using OkCoin.API.Utils;

namespace OkCoin.API.ViewModels;

public class UserViewModel
{
    public string Id { get; set; } = null!;
    public string TelegramUsername { get; set; } = null!;
    public string TelegramId { get; set; } = null!;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsTelegramPremium { get; set; } = false;
    public DateTime LastTapped { get; set; } = DateTime.Now;
    public decimal RechargeSpeedValue { get; set; } = 1m;
    public decimal MultiTapValue { get; set; } = 1m;
    public int EnergyLimitValue { get; set; } = 2;
    public int AvailableEnergy { get; set; } = 0;
    public decimal Balance { get; set; } = 0m;
    public int MultiTapLevel { get; set; } = 1;
    public int RechargeSpeedLevel { get; set; } = 1;
    public int EnergyLimitLevel { get; set; } = 1;
    public int InfinityTapUsed { get; set; } = 0;
    public int FullEnergyRefillUsed { get; set; } = 0;
    public int AllowedInfinityTap { get; set; } = Constants.GameSettings.AllowedInfinityTap;
    public int AllowedFullEnergyRefill { get; set; } = Constants.GameSettings.AllowedFullEnergyRefill;
    public string UserRank { get; set; } = "Bronze";
    
    public bool HaveTapBot { get; set; } = false;    
    public bool HavePremiumBot { get; set; } = false;
    
    public long TonBalance { get; set; } = 0;
    public bool IsReceiveAirdrop { get; set; }
    public long AmountToken { get; set; } = 0;
}

public class MeResponse
{
    public UserViewModel User { get; set; } = new UserViewModel();
    public List<MyTaskViewModel> MyTasks { get; set; } = new List<MyTaskViewModel>();
    public List<BoostItemViewModel> BoostItems { get; set; } = new List<BoostItemViewModel>();
}