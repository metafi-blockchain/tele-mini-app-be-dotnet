using Microsoft.AspNetCore.Mvc;
using OkCoin.API.Services;
using OkCoin.API.Utils;
using OkCoin.API.ViewModels;
using System.Security.Claims;

namespace OkCoin.API.Endpoints
{
    public static class TonChainEndpoint
    {
        public static void MapTonChainEndpointt(this WebApplication app)
        {
            app.MapPost("/withdraw-request", async ([FromBody] WithdrawRequestViewModel withdrawRequestViewModel, [FromServices] ITonChainService tonChainService, ClaimsPrincipal userClaimsPrincipal) =>
            {
                var id = userClaimsPrincipal.FindFirst(Constants.CustomClaimTypes.UserId)?.Value;
                if (string.IsNullOrEmpty(id)) return Results.BadRequest("User not found");
                var re = await tonChainService.WithdrawAsync(withdrawRequestViewModel, id);
                return Results.Ok(re);
            }).RequireAuthorization().WithName("WithdrawRequest").WithOpenApi();

            app.MapGet("/tran-status", async ([FromServices] ITonChainService tonChainService, ClaimsPrincipal userClaimsPrincipal, [FromQuery] string? timestamp) =>
            {
                var id = userClaimsPrincipal.FindFirst(Constants.CustomClaimTypes.TelegramId)?.Value;
                if (string.IsNullOrEmpty(id)) return Results.BadRequest("User not found");
                var re = await tonChainService.GetTranStatus(id, timestamp);
                return Results.Ok(re);
            }).RequireAuthorization().WithName("TonTranStatus").WithOpenApi();

            app.MapPost("/list-withdraw-request", async ([FromBody] ListWithdrawRequestViewModel request, [FromServices] ITonChainService tonChainService, ClaimsPrincipal userClaimsPrincipal) =>
            {
                var userId = userClaimsPrincipal.FindFirst(Constants.CustomClaimTypes.UserId)?.Value;

                if (string.IsNullOrEmpty(userId)) return Results.BadRequest("User not found");

                var isAdmin = userClaimsPrincipal.FindFirst(Constants.CustomClaimTypes.IsAdmin)?.Value.ToLower() == "true";
                if (!isAdmin)
                {
                    var teleId = userClaimsPrincipal.FindFirst(Constants.CustomClaimTypes.TelegramId)?.Value;
                    var defaultAdminTelegramId = app.Configuration["DefaultAdminTelegramId"];
                    if (teleId != defaultAdminTelegramId) return Results.BadRequest("You are not admin");
                }

                var response = await tonChainService.GetListWithdrawAsync(request);

                return Results.Ok(response);
            }).RequireAuthorization().WithName("ListWithdrawRequest").WithOpenApi();

            app.MapPost("/list-user-withdraw-request", async ([FromBody] ListWithdrawRequestViewModel request, [FromServices] ITonChainService tonChainService, ClaimsPrincipal userClaimsPrincipal) =>
            {
                var userId = userClaimsPrincipal.FindFirst(Constants.CustomClaimTypes.UserId)?.Value;

                if (string.IsNullOrEmpty(userId)) return Results.BadRequest("User not found");

                var response = await tonChainService.GetListWithdrawByUserIdAsync(userId, request);

                return Results.Ok(response);
            }).RequireAuthorization().WithName("ListUserWithdrawRequest").WithOpenApi();
        }
    }
}
