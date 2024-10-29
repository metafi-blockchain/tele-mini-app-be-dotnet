using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using OkCoin.API.Services;
using OkCoin.API.Utils;
    
namespace OkCoin.API.Endpoints;

public static class TapEndpoint
{
    public static void MapTapEndpoint(this WebApplication app)
    {
        app.MapPost("/tap",  async ([FromQuery]int value, [FromQuery]long startTime, [FromServices] ITappingService farmingService, ClaimsPrincipal userClaimsPrincipal) =>
            {
                var id = userClaimsPrincipal.FindFirst(Constants.CustomClaimTypes.UserId)?.Value;
                if(string.IsNullOrEmpty(id)) return Results.BadRequest("User not found");
                var harvest = await farmingService.TapAsync(id, value, startTime);
                return Results.Ok(harvest);
            })
            .RequireAuthorization().WithName("Tapped").WithOpenApi();

        app.MapPost("/infinity-tap",  async ([FromQuery]int value, [FromServices] ITappingService farmingService, ClaimsPrincipal userClaimsPrincipal) =>
            {
                var id = userClaimsPrincipal.FindFirst(Constants.CustomClaimTypes.UserId)?.Value;
                if(string.IsNullOrEmpty(id)) return Results.BadRequest("User not found");
                var harvest = await farmingService.InfinityTapAsync(id, value);
                return Results.Ok(harvest);
            })
            .RequireAuthorization().WithName("InfinityTap").WithOpenApi();

        app.MapPost("/energy-refill",  async ([FromServices] ITappingService farmingService, ClaimsPrincipal userClaimsPrincipal) =>
            {
                var id = userClaimsPrincipal.FindFirst(Constants.CustomClaimTypes.UserId)?.Value;
                if(string.IsNullOrEmpty(id)) return Results.BadRequest("User not found");
                var harvest = await farmingService.RefillFullEnergy(id);
                return Results.Ok(harvest);
            })
            .RequireAuthorization().WithName("RefillFullEnergy").WithOpenApi();

        app.MapGet("/tapbot-claim", async ([FromServices] ITappingService farmingService, ClaimsPrincipal userClaimsPrincipal) =>
            {
                var id = userClaimsPrincipal.FindFirst(Constants.CustomClaimTypes.UserId)?.Value;
                if(string.IsNullOrEmpty(id)) return Results.BadRequest("User not found");
                var harvest = await farmingService.ClaimBotTap(id);
                return Results.Ok(harvest);
            })
            .RequireAuthorization().WithName("TapBotClaim").WithOpenApi();
    }
}