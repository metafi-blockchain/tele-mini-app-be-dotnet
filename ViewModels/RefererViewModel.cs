namespace OkCoin.API.ViewModels;

public class RefererViewModel
{
    public string Id { get; set; } = null!;
    public string TelegramUsername { get; set; } = null!;
    public string TelegramId { get; set; } = null!;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsTelegramPremium { get; set; } = false;
}