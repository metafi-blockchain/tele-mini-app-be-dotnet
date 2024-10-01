namespace OkCoin.API.ViewModels;

public class TokenViewModel
{
    public string Token { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public DateTime Expires { get; set; } = DateTime.Now;
    public bool IsFirstLogin { get; set; } = false;
}