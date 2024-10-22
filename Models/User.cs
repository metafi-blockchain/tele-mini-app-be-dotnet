using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using OkCoin.API.Utils;
using OkCoin.API.ViewModels;

namespace OkCoin.API.Models;
[BsonIgnoreExtraElements]
public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    [BsonElement("t_uname")]
    public required string TelegramUsername { get; set; }
    [BsonElement("t_id")]
    public required string TelegramId { get; set; }
    [BsonElement("t_fname")]
    public string FirstName { get; set; } = string.Empty;
    [BsonElement("t_lname")]
    public string LastName { get; set; } = string.Empty;
    [BsonElement("t_premium")]
    public bool IsTelegramPremium { get; set; } = false;
    [BsonElement("t_lang")]
    public string DefaultLanguage { get; set; } = "en";
    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [BsonElement("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [BsonElement("t_ref")]
    public string RefererId { get; set; } = string.Empty;
    [BsonElement("t_ref_count")]
    public int RefererCount { get; set; } = 0;
    [BsonElement("grand_balance")]
    public decimal GrandBalance { get; set; } = 0;
    [BsonElement("balance")]
    public decimal Balance { get; set; } = 0;
    [BsonElement("balance_updated_at")]
    public DateTime BalanceUpdatedAt { get; set; } = DateTime.UtcNow;
    [BsonElement("tap_balance")]
    public decimal TapBalance { get; set; } = 0;
    [BsonElement("tap_balance_updated_at")]
    public DateTime TapBalanceUpdatedAt { get; set; } = DateTime.UtcNow;
    [BsonElement("available_energy")]
    public int AvailableTapCount { get; set; } = 500;
    [BsonElement("recharge_speed")]
    public decimal RechargeSpeedValue { get; set; } = 1.0m;
    [BsonElement("multi_tap_value")]
    public decimal MultiTapValue { get; set; } = 1.0m;
    [BsonElement("energy_limit")]
    public int EnergyLimitValue { get; set; } = 500;
    [BsonElement("last_tapped")]
    public DateTime LastTapped { get; set; } = DateTime.UtcNow;
    [BsonElement("multi_tap_level")]
    public int MultiTapLevel { get; set; } = 1;
    [BsonElement("energy_limit_level")]
    public int EnergyLimitLevel { get; set; } = 1;
    [BsonElement("recharge_speed_level")]
    public int RechargeSpeedLevel { get; set; } = 1;
    [BsonElement("is_admin")]
    public bool IsAdmin { get; set; } = false;
    [BsonElement("have_tap_bot")]
    public bool HaveTapBot { get; set; } = false;
    [BsonElement("have_premium_bot")]
    public bool HavePremiumBot { get; set; } = false;
    [BsonElement("premium_bot_at")]
    public DateTime? PremiumBotAt { get; set; }
    [BsonElement("ton_balance")]
    public long TonBalance { get; set; } = 0;
    [BsonElement("is_receive_airdrop")]
    public bool IsReceiveAirdrop { get; set; }
    [BsonElement("amount_token")]
    public long AmountToken { get; set; } = 0;
    [BsonElement("receive_address")]
    public string ReceiveAddress { get; set; } = string.Empty;

    public UserViewModel ToUserViewModel() => new UserViewModel
    {
        Id = Id ?? string.Empty,
        TelegramUsername = TelegramUsername,
        TelegramId = TelegramId,
        FirstName = FirstName,
        LastName = LastName,
        IsTelegramPremium = IsTelegramPremium,
        LastTapped = LastTapped,
        Balance = Balance,
        MultiTapValue = MultiTapValue,
        RechargeSpeedValue = RechargeSpeedValue,
        EnergyLimitValue = EnergyLimitValue,
        MultiTapLevel = MultiTapLevel,
        RechargeSpeedLevel = RechargeSpeedLevel,
        EnergyLimitLevel = EnergyLimitLevel,
        AvailableEnergy = this.CalculateAvailableTap(),
        InfinityTapUsed = 0,
        HaveTapBot = HaveTapBot,
        HavePremiumBot = HavePremiumBot,
        TonBalance = TonBalance,
        IsReceiveAirdrop = IsReceiveAirdrop,
        AmountToken = AmountToken,
        ReceiveAddress = ReceiveAddress,
        IsAdmin = IsAdmin
    };
}