
using Microsoft.AspNetCore.Mvc;
using OkCoin.API.Services.Interfaces;

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
    }
}