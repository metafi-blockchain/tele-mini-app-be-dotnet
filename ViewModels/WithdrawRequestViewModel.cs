namespace OkCoin.API.ViewModels;

public class WithdrawRequestViewModel
{
    public string Address { get; set; } = null!;
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}