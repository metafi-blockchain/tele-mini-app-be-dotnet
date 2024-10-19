namespace OkCoin.API.Models
{
    public class AirdropTokenDocument
    {
        public long AirdropToken { get; set; }
        public List<TelegramIdsThroughYears> TelegramIdsThroughYears { get; set; } = null!;
    }
}
