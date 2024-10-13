using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using OkCoin.API.Services;
using OkCoin.API.Utils;
using OkCoin.API.ViewModels;

namespace OkCoin.API.Endpoints;

public static class UserEndpoint
{
    public static void MapUserEndpoint(this WebApplication app)
    {
        app.MapGet("/me", async (IUserService userService, ClaimsPrincipal userClaimsPrincipal) =>
            {
                var id = userClaimsPrincipal.FindFirst(Constants.CustomClaimTypes.UserId)?.Value;
                if(string.IsNullOrEmpty(id)) return Results.BadRequest("User not found");
                var user = await userService.MeAsync(id);
                return !user.Success ? Results.BadRequest("User not found") : Results.Ok(user);
            })
            .RequireAuthorization()
            .WithName("Me")
            .WithOpenApi();

        app.MapPost("/auth", async ([FromBody] LoginViewModel loginViewModel, IUserService userService) =>
        {
            //var qString = "user=%7B%22id%22%3A772406078%2C%22first_name%22%3A%22M%C3%A8o%20%C4%90en%22%2C%22last_name%22%3A%22%22%2C%22username%22%3A%22tonny_vu%22%2C%22language_code%22%3A%22en%22%2C%22is_premium%22%3Atrue%2C%22allows_write_to_pm%22%3Atrue%7D&chat_instance=3230204669168232189&chat_type=private&auth_date=1718027496&hash=7256ca3c00b26617f233fb35edcb300db172ed06eca8576e16885ee16e5e107b";
            if (string.IsNullOrEmpty(loginViewModel.TelegramData)) return Results.BadRequest("Telegram data is required");
            var token = await userService.AuthenticateAsync(loginViewModel);
            if(!token.Success) return Results.BadRequest(token);
            return Results.Ok(token);
        }).WithName("AuthenticateUser").WithOpenApi();
        
        app.MapGet("/referer-list", async ([FromServices] IUserService userService, ClaimsPrincipal userClaimsPrincipal) =>
            {
                var id = userClaimsPrincipal.FindFirst(Constants.CustomClaimTypes.UserId)?.Value;
                if(string.IsNullOrEmpty(id)) return Results.BadRequest("User not found");
                var re = await userService.ListReferrals(id);
                return Results.Ok(re);
            })
            .RequireAuthorization().WithName("RefererList").WithOpenApi();
        
        
        
        app.MapPost("/withdraw-request", async ([FromBody] WithdrawRequestViewModel withdrawRequestViewModel, [FromServices] ITonChainService tonChainService, ClaimsPrincipal userClaimsPrincipal) =>
            {
                var id = userClaimsPrincipal.FindFirst(Constants.CustomClaimTypes.UserId)?.Value;
                if(string.IsNullOrEmpty(id)) return Results.BadRequest("User not found");
                var re = await tonChainService.WithdrawAsync(withdrawRequestViewModel, id);
                return Results.Ok(re);
            })
            .RequireAuthorization().WithName("WithdrawRequest").WithOpenApi();
        
        app.MapGet("/tran-status", async ([FromServices] ITonChainService tonChainService, ClaimsPrincipal userClaimsPrincipal, [FromQuery]string? timestamp) =>
            {
                var id = userClaimsPrincipal.FindFirst(Constants.CustomClaimTypes.TelegramId)?.Value;
                if(string.IsNullOrEmpty(id)) return Results.BadRequest("User not found");
                var re = await tonChainService.GetTranStatus(id, timestamp);
                return Results.Ok(re);
            })
            .RequireAuthorization().WithName("TonTranStatus").WithOpenApi();


        app.MapGet("/receive_point_reward", async ([FromServices] IUserService userService, ClaimsPrincipal userClaimsPrincipal) =>
        {
            var id = userClaimsPrincipal.FindFirst(Constants.CustomClaimTypes.UserId)?.Value;
            if (string.IsNullOrEmpty(id)) return Results.BadRequest("User not found");
            
            var response = await userService.GetPointRewardAsync(id);

            return Results.Ok(response);
        }).RequireAuthorization().WithName("ReceivePointReward").WithOpenApi();

        app.MapPost("/list-withdraw-request", async ([FromServices] ITonChainService tonChainService, ClaimsPrincipal userClaimsPrincipal) =>
        {
            var userId = userClaimsPrincipal.FindFirst(Constants.CustomClaimTypes.UserId)?.Value;
            
            if (string.IsNullOrEmpty(userId)) return Results.BadRequest("User not found");

            var response = await tonChainService.GetListWithdrawByUserIdAsync(userId);

            return Results.Ok(response);
        }).RequireAuthorization().WithName("ListWithdrawRequest").WithOpenApi();
    }
}