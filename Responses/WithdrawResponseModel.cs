namespace OkCoin.API.Responses
{
    public class WithdrawResponseModel
    {
        public string TelegramId { get; set; }
        public string Address { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
