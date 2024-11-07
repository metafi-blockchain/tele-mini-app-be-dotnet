
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Newtonsoft.Json;
using OkCoin.API.Models;
using OkCoin.API.Responses;
using OkCoin.API.Services.Interfaces;
using OkCoin.API.ViewModels;
using static OkCoin.API.Utils.Constants;

namespace OkCoin.API.Services;

public class DashboardService : IDashboardService
{
    private readonly IMongoCollection<User> _userCollection;
    private readonly ICacheService _redisCacheService;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(IOptions<Models.DbSettings> myDatabaseSettings, ICacheService redisCacheService, ILogger<DashboardService> logger)
    {
        _redisCacheService = redisCacheService;
        _logger = logger;
        var client = new MongoClient(myDatabaseSettings.Value.ConnectionString);
            var database = client.GetDatabase(myDatabaseSettings.Value.DatabaseName);
            _userCollection = database.GetCollection<User>(nameof(User));
    }

    public async Task<ResponseDto<List<TournamentRankingResponseModel>>> GetTournamentRankingAsync()
    {
        try
        {
            _logger.LogInformation($"{nameof(SyncTournamentRankingAsync)} - Begin.");
            
            var tournamentRankingModel = await _redisCacheService.GetAsync<List<TournamentRankingResponseModel>>(RedisKeyConstants.TOURNAMENT_RANKING);

            _logger.LogInformation($"{nameof(SyncTournamentRankingAsync)} - End.");

            return new ResponseDto<List<TournamentRankingResponseModel>>
            {
                Success = true,
                Data = tournamentRankingModel
            };
        }
        catch (System.Exception ex)
        {
            _logger.LogError($"{nameof(GetTournamentRankingAsync)} - Error: {ex.Message}.");
            return new ResponseDto<List<TournamentRankingResponseModel>>
            {
                Success = false,
            };
        }
    }

    public async Task SyncTournamentRankingAsync()
    {
        try
        {
            _logger.LogInformation($"{nameof(SyncTournamentRankingAsync)} - Begin.");
            var users = await _userCollection.Find(_ => true)
                                    .SortByDescending(c => c.TournamentBalance)
                                    .ThenBy(c => c.TournamentBalanceUpdatedAt)
                                    .Limit(10)
                                    .ToListAsync();

            var tournamentRankingModel = users.Select(c => new TournamentRankingResponseModel{
                TelegramId = c.TelegramId,
                FirstName = c.FirstName,
                LastName = c.LastName,
                TournamentBalance = c.TournamentBalance
            });

            await _redisCacheService.Set(RedisKeyConstants.TOURNAMENT_RANKING, JsonConvert.SerializeObject(tournamentRankingModel));

            _logger.LogInformation($"{nameof(SyncTournamentRankingAsync)} - End.");
        }
        catch (System.Exception ex)
        {
            _logger.LogError($"{nameof(SyncTournamentRankingAsync)} - Error: {ex.Message}");
        }
    }
}
