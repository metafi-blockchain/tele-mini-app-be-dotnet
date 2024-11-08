
using OkCoin.API.Responses;
using OkCoin.API.ViewModels;

namespace OkCoin.API.Services.Interfaces;

public interface IDashboardService 
{
    Task SyncTournamentRankingAsync();
    
    Task UpdateTotalTournamentRewardAsync();

    Task<ResponseDto<List<TournamentRankingResponseModel>>> GetTournamentRankingAsync();

    Task<ResponseDto<bool>> ClaimTournamentRewardAsync(string userId);
}

