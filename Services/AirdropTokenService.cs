using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using OkCoin.API.Models;
using OkCoin.API.Services.Interfaces;
using OkCoin.API.ViewModels;

namespace OkCoin.API.Services
{
    public class AirdropTokenService : IAirdropTokenService
    {
        private readonly IMongoCollection<User> _userCollection;
        private readonly IUserService _userService;
        private readonly List<AirdropTokenDocuments> _airdropTokenDocuments;

        public AirdropTokenService(IOptions<DbSettings> myDatabaseSettings, IOptions<List<AirdropTokenDocuments>> airdropTokenDocumentOption, IUserService userService)
        {
            _userService = userService;
            _airdropTokenDocuments = airdropTokenDocumentOption.Value;
            var client = new MongoClient(myDatabaseSettings.Value.ConnectionString);
            var database = client.GetDatabase(myDatabaseSettings.Value.DatabaseName);
            _userCollection = database.GetCollection<User>(nameof(User));
        }

        public async Task<ResponseDto<long>> GetAirdropTokenAsync(string userId)
        {
            if (!ObjectId.TryParse(userId, out _))
                return new ResponseDto<long>()
                {
                    Message = "Invalid Id",
                    Success = false
                };

            var user = await _userCollection.Find(x => x.Id == userId).FirstOrDefaultAsync();

            if (user == null)
            {
                return new ResponseDto<long>()
                {
                    Message = "User is not found",
                    Success = false
                };
            }

            if (user.IsReceiveAirdrop)
            {
                return new ResponseDto<long>()
                {
                    Message = "This user has already received the point reward.",
                    Success = false
                };
            }

            return new ResponseDto<long>
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
                    Message = "This user has already received the point reward.",
                    Success = false
                };
            }

            user.IsReceiveAirdrop = true;
            user.AmountToken = GetInitialAirdropToken(user.TelegramId);

            _ = _userService.UpdateAsync(userId, user);

            return new ResponseDto<bool>()
            {
                Message = "Congratulations on receiving airdop tokens!",
                Success = true,
                Data = true
            };
        }

        private long GetInitialAirdropToken(string telegramIdStr)
        {
            long point = 0;

            var telegramIdDouble = Convert.ToDouble(telegramIdStr) / 1000000;

            foreach (var item in _airdropTokenDocuments.OrderByDescending(c => c.Year))
            {
                if (telegramIdDouble > item.Ids)
                {
                    point = item.AirdropToken;
                    break;
                }
            }

            return point;
        }
    }
}