
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Newtonsoft.Json;
using OkCoin.API.Models;
using OkCoin.API.Responses;
using OkCoin.API.Services.Interfaces;
using OkCoin.API.Utils;
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

    public async Task UpdateTotalTournamentRewardAsync()
    {
        try
        {
            _logger.LogInformation($"{nameof(UpdateTotalTournamentRewardAsync)} - Begin.");
            
            var users = await _userCollection.Find(_ => true)
                                    .SortByDescending(c => c.TournamentBalance)
                                    .ThenBy(c => c.TournamentBalanceUpdatedAt)
                                    .Limit(Constants.GameSettings.LimitTournament)
                                    .ToListAsync();

            await _userCollection.UpdateManyAsync(_ => true, 
                                        Builders<User>.Update.Set(p => p.IsReceiveTournamentReward, false)
                                                                    .Set(p => p.TotalTournamentReward, 0)
                                                                    .Set(u => u.TournamentBalance, 0)
                                                                    .Set(u => u.TournamentBalanceUpdatedAt, DateTime.UtcNow));


            var UpdateTotalTournamentRewardAsyncForTop = users.Select((item, index) => {
                
                var totalTournament = (Constants.GameSettings.LimitTournament - index) * Constants.GameSettings.TournamentReward;
                return new UpdateOneModel<User>(Builders<User>.Filter.Eq(u => u.Id, item.Id),
                                                Builders<User>.Update.Set(u => u.TotalTournamentReward, totalTournament)
                                                                            .Set(u => u.Balance, item.Balance+(decimal)totalTournament)
                                                                            .Set(u => u.BalanceUpdatedAt, DateTime.UtcNow));
            });

            await _userCollection.BulkWriteAsync(UpdateTotalTournamentRewardAsyncForTop);

            _logger.LogInformation($"{nameof(UpdateTotalTournamentRewardAsync)} - End.");
        }
        catch (System.Exception ex)
        {
            _logger.LogError($"{nameof(UpdateTotalTournamentRewardAsync)} - Error: {ex.Message}");
            throw;
        }
    }

    public async Task<ResponseDto<bool>> ClaimTournamentRewardAsync(string userId)
    {
        try
        {
            _logger.LogInformation($"{nameof(ClaimTournamentRewardAsync)} - Begin.");
            var user = await _userCollection.Find(x => x.Id == userId).FirstOrDefaultAsync();
            
            if (user == null)
            {
                return new ResponseDto<bool>
                {
                    Success = false,
                    Message = "User not found"
                };
            } 

            if (user.IsReceiveTournamentReward) 
            {
                return new ResponseDto<bool>
                {
                    Success = false,
                    Message = "User received tournament reward already!"
                };
            }

            user.IsReceiveTournamentReward = true;

            await _userCollection.ReplaceOneAsync(c => c.Id == user.Id, user);

            _logger.LogInformation($"{nameof(ClaimTournamentRewardAsync)} - End.");

            return new ResponseDto<bool>
            {
                Success = true,
                Data = true
            };
        }
        catch (System.Exception ex)
        {
            _logger.LogError($"{nameof(ClaimTournamentRewardAsync)} - Error: {ex.Message}");
            return new ResponseDto<bool>();
        }
    }
}
