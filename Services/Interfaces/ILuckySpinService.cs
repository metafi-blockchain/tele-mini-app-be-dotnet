
using OkCoin.API.Responses;
using OkCoin.API.ViewModels;

namespace OkCoin.API.Services;

public interface ILuckySpinService
{
    Task<ResponseDto<LuckySpinResponseModel>> GetPuzzlePieceOfImageAsync(string userId);

    Task<string> UpdateRemainingSpinEverydayAsync();

    Task ReUpdateRemainingSpinEverydayAsync();
}
