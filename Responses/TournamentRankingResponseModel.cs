
namespace OkCoin.API.Responses;

public class TournamentRankingResponseModel
{
    public string TelegramId { get; set; } = null!;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public decimal TournamentBalance { get; set; }
}