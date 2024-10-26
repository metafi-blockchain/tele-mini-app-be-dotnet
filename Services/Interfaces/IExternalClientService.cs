
namespace OkCoin.API.Services;

public interface IExternalClientService
{
    Task<T> GetDataAsync<T>(string endpoint);
}