
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OkCoin.API.Models;
using OkCoin.API.Responses;
using OkCoin.API.Utils;
using OkCoin.API.ViewModels;

namespace OkCoin.API.Services;

public class LuckySpinService : ILuckySpinService
{
    private readonly IMongoCollection<User> _userCollection;
    private readonly ILogger<LuckySpinService> _logger;

    public LuckySpinService(IOptions<DbSettings> myDatabaseSettings, ILogger<LuckySpinService> logger)
    {
        _logger = logger;
        var client = new MongoClient(myDatabaseSettings.Value.ConnectionString);
        var database = client.GetDatabase(myDatabaseSettings.Value.DatabaseName);
        _userCollection = database.GetCollection<User>(nameof(User));
    }

    public async Task<ResponseDto<LuckySpinResponseModel>> GetPuzzlePieceOfImageAsync(string userId)
    {
        try
        {
            _logger.LogInformation($"{nameof(GetPuzzlePieceOfImageAsync)} - Begin.");
            var user = await _userCollection.Find(x => x.Id == userId).FirstOrDefaultAsync();

            if (user == null)
            {
                return new ResponseDto<LuckySpinResponseModel>()
                {
                    Message = "User is not found",
                    Success = false
                };
            }

            if (user.RemainingSpin == 0)
            {
                return new ResponseDto<LuckySpinResponseModel>()
                {
                    Message = "You have no more spins left.",
                    Success = false
                };
            }

            Random random = new Random();

            var maxValue = Constants.GameSettings.MaxImageSoccerPlayer * Constants.GameSettings.MaxPuzzlePiecePerImageSoccerPlayer;

            int randomNumber = random.Next(0, maxValue); // 0 -> (max-1)

            var puzzlePieceOfImageArray = string.IsNullOrEmpty(user.PuzzlePieceOfImage)
                                                ? new List<int>()
                                                : user.PuzzlePieceOfImage.Split(",").Select(int.Parse).ToList();
            
            if(!puzzlePieceOfImageArray.Any(number => number == randomNumber)) 
            {
                if (user.TotalSpinPerformed >= Constants.GameSettings.TotalSpinRequired 
                    || !IsEnoughPuzzlePieceForOneImage(randomNumber, puzzlePieceOfImageArray))
                {
                    puzzlePieceOfImageArray.Add(randomNumber);
                    user.PuzzlePieceOfImage = string.Join(",", puzzlePieceOfImageArray);
                }else
                {
                    randomNumber = puzzlePieceOfImageArray.First();
                }
            }

            user.RemainingSpin--;
            user.TotalSpinPerformed++;

            _ = _userCollection.ReplaceOneAsync(c => c.Id == user.Id, user);

            _logger.LogInformation($"{nameof(GetPuzzlePieceOfImageAsync)} - End.");
            return new ResponseDto<LuckySpinResponseModel> 
            {
                Success = true,
                Data = new LuckySpinResponseModel 
                {
                    PuzzlePiece = randomNumber,
                    RemainingSpin = user.RemainingSpin
                }
            };
        }
        catch (System.Exception ex)
        {
            _logger.LogError($"{nameof(GetPuzzlePieceOfImageAsync)} - Error: {ex.Message}");
            return new ResponseDto<LuckySpinResponseModel>();
        } 
    }

    private bool IsEnoughPuzzlePieceForOneImage(int randomNumber, List<int> puzzlePieceOfImageArray)
    {
        var rangeIndex = randomNumber / Constants.GameSettings.MaxPuzzlePiecePerImageSoccerPlayer; // Integer division
        var rangeStart = rangeIndex * Constants.GameSettings.MaxPuzzlePiecePerImageSoccerPlayer;
        var rangeEnd = rangeStart + (Constants.GameSettings.MaxPuzzlePiecePerImageSoccerPlayer - 1);

        var totalNumber = puzzlePieceOfImageArray.Count(number => number >= rangeStart && number <= rangeEnd);

        return totalNumber == Constants.GameSettings.MaxPuzzlePiecePerImageSoccerPlayer - 1;
    }

    public async Task<string> UpdateRemainingSpinEverydayAsync()
    {
        try
        {
            _logger.LogInformation($"{nameof(UpdateRemainingSpinEverydayAsync)} - Begin.");

            var result = await _userCollection.UpdateManyAsync(_ => true, 
                                        Builders<User>.Update.Set(p => p.RemainingSpin, Constants.GameSettings.DonateRemainingSpinEveryday)
                                                                    .Set(p => p.DonateRemainingSpinAt, DateTime.UtcNow));

            _logger.LogInformation($"{nameof(UpdateRemainingSpinEverydayAsync)} - MatchedCount = {result.MatchedCount}, ModifiedCount = {result.ModifiedCount}");
            
            return $"MatchedCount = {result.MatchedCount}, ModifiedCount = {result.ModifiedCount}";
        }
        catch (System.Exception ex)
        {
            _logger.LogError($"{nameof(UpdateRemainingSpinEverydayAsync)} - Error: {ex.Message}");
            return "Error";
        }
    }

    public async Task ReUpdateRemainingSpinEverydayAsync()
    {
        try
        {
            _logger.LogInformation($"{nameof(ReUpdateRemainingSpinEverydayAsync)} - Begin.");

            var result = await _userCollection.UpdateManyAsync(c => c.DonateRemainingSpinAt.Date != DateTime.UtcNow.Date, 
                                        Builders<User>.Update.Set(p => p.RemainingSpin, Constants.GameSettings.DonateRemainingSpinEveryday)
                                                                    .Set(p => p.DonateRemainingSpinAt, DateTime.UtcNow));

            _logger.LogInformation($"{nameof(ReUpdateRemainingSpinEverydayAsync)} - MatchedCount = {result.MatchedCount}, ModifiedCount = {result.ModifiedCount}");
        }
        catch (System.Exception ex)
        {
            _logger.LogError($"{nameof(UpdateRemainingSpinEverydayAsync)} - Error: {ex.Message}");
        }
    }
}