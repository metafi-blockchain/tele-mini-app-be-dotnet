using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using OkCoin.API.Services;
using OkCoin.API.Utils;
using OkCoin.API.ViewModels;

namespace OkCoin.API.Endpoints;

public static class OtherEndpoint
{
    public static void MapOtherEndpoint(this WebApplication app)
    {
        app.MapGet("/ping", () => Results.Ok("Pong")).WithName("Ping").WithOpenApi();
        app.MapGet("/init-data",  ([FromServices] ITaskService taskService, ClaimsPrincipal userClaimsPrincipal) =>
            {
                var id = userClaimsPrincipal.FindFirst(Constants.CustomClaimTypes.UserId)?.Value;
                if(string.IsNullOrEmpty(id)) return Results.BadRequest("User not found");
                var isAdmin = userClaimsPrincipal.FindFirst(Constants.CustomClaimTypes.IsAdmin)?.Value.ToLower() == "true";
                if (!isAdmin)
                {
                    var teleId = userClaimsPrincipal.FindFirst(Constants.CustomClaimTypes.TelegramId)?.Value;
                    var defaultAdminTelegramId = app.Configuration["DefaultAdminTelegramId"];
                    if(teleId != defaultAdminTelegramId) return Results.BadRequest("You are not admin");
                }
                taskService.CreateTasks();
                return Results.Ok("Done");
            })
            .RequireAuthorization().WithName("DummyTask").WithOpenApi();
        
        app.MapGet("/statistic", async ([FromServices] IStatisticService statisticService) =>
            {
                var stat = await statisticService.GetGameStatistic();
                return Results.Ok(new ResponseDto<GameStatisticViewModel>()
                {
                    Data = stat,
                    Success = true
                });
            })
            .RequireAuthorization().WithName("Statistic").WithOpenApi();
    }
}