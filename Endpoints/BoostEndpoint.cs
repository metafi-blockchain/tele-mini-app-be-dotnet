using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using OkCoin.API.Services;
using OkCoin.API.Utils;
using OkCoin.API.ViewModels;

namespace OkCoin.API.Endpoints;

public static class BoostEndpoint
{
    public static void MapBoostEndpoint(this WebApplication app)
    {
        app.MapGet("/boost-list",  (ITappingService farmingService) =>
            {
                var upgrades = farmingService.GetUpgradeItems();
                return Results.Ok(upgrades);
            })
            .RequireAuthorization().WithName("GetAllUpgradeItems").WithOpenApi();

        app.MapPost("/boost/{type}",  async ([FromRoute]BoostType type,[FromServices] ITappingService farmingService, ClaimsPrincipal userClaimsPrincipal) =>
            {
                var id = userClaimsPrincipal.FindFirst(Constants.CustomClaimTypes.UserId)?.Value;
                if(string.IsNullOrEmpty(id)) return Results.BadRequest("User not found");
                var upgrade = await farmingService.DoUpgradeAsync(id, type);
                return Results.Ok(upgrade);
            })
            .RequireAuthorization().WithName("UpgradeItem").WithOpenApi();
    }
}