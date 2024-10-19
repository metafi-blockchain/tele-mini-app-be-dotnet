using OkCoin.API.Responses;
using OkCoin.API.ViewModels;

namespace OkCoin.API.Services.Interfaces
{
    public interface IAirdropTokenService
    {
        Task<ResponseDto<AirdropTokenResponseModel>> GetAirdropTokenAsync(string userId);
        Task<ResponseDto<bool>> ConfirmReceivedAirdropTokenAsync(string userId);
    }
}
