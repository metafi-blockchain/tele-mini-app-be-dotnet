
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using OkCoin.API.Services;
using OkCoin.API.Utils;

namespace OkCoin.API.Endpoints;

public static class LuckySpinEndpoint
{
    public static void MapLuckySpinEndpoint(this WebApplication app)
    {
        app.MapGet("/get-puzzle-piece-image", async ([FromServices] ILuckySpinService luckyDrawService, ClaimsPrincipal userClaimsPrincipal) =>
        {
            var id = userClaimsPrincipal.FindFirst(Constants.CustomClaimTypes.UserId)?.Value;

            if (string.IsNullOrEmpty(id)) return Results.BadRequest("User not found");

            var response = await luckyDrawService.GetPuzzlePieceOfImageAsync(id);

            return Results.Ok(response);
        }).RequireAuthorization().WithName("PuzzlePieceOfImage").WithOpenApi();

        app.MapGet("/update-remaining-spin-everyday", async ([FromServices] ILuckySpinService luckyDrawService, ClaimsPrincipal userClaimsPrincipal) =>
        {
            var id = userClaimsPrincipal.FindFirst(Constants.CustomClaimTypes.UserId)?.Value;

            if (string.IsNullOrEmpty(id)) return Results.BadRequest("User not found");

            var isAdmin = userClaimsPrincipal.FindFirst(Constants.CustomClaimTypes.IsAdmin)?.Value.ToLower() == "true";
            if (!isAdmin)
            {
                var teleId = userClaimsPrincipal.FindFirst(Constants.CustomClaimTypes.TelegramId)?.Value;
                var defaultAdminTelegramId = app.Configuration["DefaultAdminTelegramId"];
                if (teleId != defaultAdminTelegramId) return Results.BadRequest("You are not admin");
            }

            var response = await luckyDrawService.UpdateRemainingSpinEverydayAsync();

            return Results.Ok(response);
        }).RequireAuthorization().WithName("UpdateRemainingSpinEveryday").WithOpenApi();
    }
}
