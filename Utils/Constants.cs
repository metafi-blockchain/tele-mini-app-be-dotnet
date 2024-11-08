namespace OkCoin.API.Utils;

public static class Constants
{
    public static class CustomClaimTypes
    {
        public const string UserId = "uid";
        public const string TelegramId = "teleid";
        public const string TelegramUsername = "tusername";
        public const string TelegramFirstName = "tfname";
        public const string TelegramLastName = "tlname";
        public const string TelegramPremium = "tpremium";
        public const string TelegramLanguage = "tlang";
        public const string IsAdmin = "isadmin";
    }
    
    public static class GameSettings
    {
        public const int AllowedInfinityTap = 3;
        public const int AllowedFullEnergyRefill = 3;
        public const string AllowedInfinityTapRedisKey = "{0}-ait";
        public const string AllowedFullEnergyRefillRedisKey = "{0}-afer";
        public const long DailyPremiumBotReward = 1000000;
        public const long PremiumBotPriceInNanoTon = 1_000_000_000;
        public const long MinimumWithdrawAmountInNanoTon = 1_000_000_000;
        public const long TonInNano = 1_000_000_000;
        public const double TonRewardForReferralLevel1 = 0.1;
        public const double TonRewardForReferralLevel2 = 0.05;
        public const double TournamentReward = 1_000_000;
        public const int LimitTournament = 10;

    }

    public static class RedisKeyConstants
    {
        public const  string TOURNAMENT_RANKING = "TournamentRanking";
    }
}