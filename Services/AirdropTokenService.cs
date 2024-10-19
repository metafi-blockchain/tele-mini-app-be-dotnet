using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using OkCoin.API.Models;
using OkCoin.API.Responses;
using OkCoin.API.Services.Interfaces;
using OkCoin.API.ViewModels;

namespace OkCoin.API.Services
{
    public class AirdropTokenService : IAirdropTokenService
    {
        private readonly IMongoCollection<User> _userCollection;
        private readonly IUserService _userService;
        private readonly AirdropTokenDocument _airdropTokenDocuments;

        public AirdropTokenService(IOptions<DbSettings> myDatabaseSettings, IOptions<AirdropTokenDocument> airdropTokenDocumentOption, IUserService userService)
        {
            _userService = userService;
            _airdropTokenDocuments = airdropTokenDocumentOption.Value;
            var client = new MongoClient(myDatabaseSettings.Value.ConnectionString);
            var database = client.GetDatabase(myDatabaseSettings.Value.DatabaseName);
            _userCollection = database.GetCollection<User>(nameof(User));
        }

        public async Task<ResponseDto<AirdropTokenResponseModel>> GetAirdropTokenAsync(string userId)
        {
            if (!ObjectId.TryParse(userId, out _))
                return new ResponseDto<AirdropTokenResponseModel>()
                {
                    Message = "Invalid Id",
                    Success = false
                };

            var user = await _userCollection.Find(x => x.Id == userId).FirstOrDefaultAsync();

            if (user == null)
            {
                return new ResponseDto<AirdropTokenResponseModel>()
                {
                    Message = "User is not found",
                    Success = false
                };
            }

            return new ResponseDto<AirdropTokenResponseModel>
            {
                Success = true,
                Data = GetInitialAirdropToken(user.TelegramId)
            };
        }

        public async Task<ResponseDto<bool>> ConfirmReceivedAirdropTokenAsync(string userId)
        {
            if (!ObjectId.TryParse(userId, out _))
                return new ResponseDto<bool>()
                {
                    Message = "Invalid Id",
                    Success = false
                };

            var user = await _userCollection.Find(x => x.Id == userId).FirstOrDefaultAsync();

            if (user == null)
            {
                return new ResponseDto<bool>()
                {
                    Message = "User is not found",
                    Success = false
                };
            }

            if (user.IsReceiveAirdrop)
            {
                return new ResponseDto<bool>()
                {
                    Message = "This user has already received the airdrop token.",
                    Success = false
                };
            }

            user.IsReceiveAirdrop = true;
            user.AmountToken = GetInitialAirdropToken(user.TelegramId).Airdrop;

            _ = _userService.UpdateAsync(userId, user);

            return new ResponseDto<bool>()
            {
                Message = "Congratulations on receiving airdop tokens!",
                Success = true,
                Data = true
            };
        }

        private AirdropTokenResponseModel GetInitialAirdropToken(string telegramIdStr)
        {
            var currentYear = DateTime.Now.Year;
            int yearJoinTelegram = currentYear;

            // convert telegramId million 
            var telegramIdDouble = Convert.ToDouble(telegramIdStr) / 1000000;

            foreach (var item in _airdropTokenDocuments.TelegramIdsThroughYears.OrderByDescending(c => c.Year))
            {
                if (telegramIdDouble >= item.Ids)
                {
                    yearJoinTelegram = item.Year;
                    break;
                }
            }

            if (yearJoinTelegram > currentYear)
            {
                return new AirdropTokenResponseModel
                {
                    Year = 0,
                    Airdrop = 0,
                };
            }

            var totalYear = currentYear - yearJoinTelegram;
            var airdrop = totalYear * _airdropTokenDocuments.AirdropToken;

            return new AirdropTokenResponseModel
            {
                Year = totalYear,
                Airdrop = airdrop,
            };
        }
    }
}