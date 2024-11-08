
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using OkCoin.API.Services.Interfaces;
using OkCoin.API.Utils;

namespace OkCoin.API.Endpoints;

public static class DashboardEndpoint
{
    public static void MapDashboardEndpoint(this WebApplication app)
    {
        app.MapGet("/tournament_ranking",  async ([FromServices] IDashboardService dashboardService) =>
            {
                var response = await dashboardService.GetTournamentRankingAsync();
                return Results.Ok(response);
            })
            .RequireAuthorization().WithName("TournamentRanking").WithOpenApi();

            
        app.MapGet("/claim_tournament_reward",  async ([FromServices] IDashboardService dashboardService, ClaimsPrincipal userClaimsPrincipal) =>
            {
                var id = userClaimsPrincipal.FindFirst(Constants.CustomClaimTypes.UserId)?.Value;
                if(string.IsNullOrEmpty(id)) return Results.BadRequest("User not found");
                var response = await dashboardService.ClaimTournamentRewardAsync(id);
                return Results.Ok(response);
            })
            .RequireAuthorization().WithName("ClaimTournamentReward").WithOpenApi();


    }
}