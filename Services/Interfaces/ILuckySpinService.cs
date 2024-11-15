
using OkCoin.API.Models;
using OkCoin.API.Responses;
using OkCoin.API.ViewModels;

namespace OkCoin.API.Services;

public interface ILuckySpinService
{
    Task<ResponseDto<LuckySpinResponseModel>> GetPuzzlePieceOfImageAsync(string userId);

    Task UpdateRemainingSpinEverydayAsync();

    Task<List<User>> GetAllUsers();
}
