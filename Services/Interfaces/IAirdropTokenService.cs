using OkCoin.API.ViewModels;

namespace OkCoin.API.Services.Interfaces
{
    public interface IAirdropTokenService
    {
        Task<ResponseDto<long>> GetAirdropTokenAsync(string userId);
        Task<ResponseDto<bool>> ConfirmReceivedAirdropTokenAsync(string userId);
    }
}
