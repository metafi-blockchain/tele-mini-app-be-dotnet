namespace OkCoin.API.Responses
{
    public class WithdrawResponseModel
    {
        public string Address { get; set; } = null!;
        public decimal Amount { get; set; }
    }
}
