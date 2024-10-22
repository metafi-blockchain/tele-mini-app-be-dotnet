namespace OkCoin.API.Models;

public enum InGameTransactionType
{
    TapReward,
    Upgrade,
    TapBotReward,
    PremiumBotReward,
    ReferralReward,
    BuyPremium,
    InfinityTapReward,
    TaskReward,
    TopUp,
    VarCheckRevert,
    PayCommission
}

public enum WithdrawRequestStatus
{
    Pending,
    Completed,
    Failed,
}

public class DbSettings
{
    public string ConnectionString { get; set; } = null!;
    public string DatabaseName { get; set; } = null!;
    public string RedisConnectionString { get; set; } = null!;
}

public class JwtSettings
{
    public string SecretKey { get; init; } = null!;
    public string Issuer { get; init; } = null!;
    public string Audience { get; init; } = null!;
    public string BotToken { get; init; } = null!;
}

public class TonChainSettings
{
    public string? WalletAddress { get; init; } = null!;
    public bool IsMainNet { get; init; }
}

public class CronJobSettings
{
    public bool SyncTonTransaction { get; set; } = true;
    public bool AIBotRewardDistribution { get; set; } = true;
    public bool InGameTransaction { get; set; } = true;
}

public class TelegramSettings
{
    public string BotToken { get; init; } = null!;
    public string WebhookUrl { get; init; } = null!;
}

public class GameSettings
{
    public string ReferralReward { get; init; } =
        "1:200,2:500,5:1000,10:2000,20:5000,50:10000,100:20000,200:50000,500:100000,1000:200000,2000:500000,5000:1000000";

    public string UserLevelRankingReward { get; init; } = "1:1000:200000,2:2000:200000,3:5000:200000,4:10000:200000";
}