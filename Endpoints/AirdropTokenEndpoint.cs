using Microsoft.AspNetCore.Mvc;
using OkCoin.API.Services.Interfaces;
using OkCoin.API.Utils;
using System.Security.Claims;

namespace OkCoin.API.Endpoints
{
    public static class AirdropTokenEndpoint
    {
        public static void MapAirdropTokenEndpoint(this WebApplication app)
        {
            app.MapGet("/get_airdrop_token", async ([FromServices] IAirdropTokenService airdropTokenService, ClaimsPrincipal userClaimsPrincipal) =>
            {
                var id = userClaimsPrincipal.FindFirst(Constants.CustomClaimTypes.UserId)?.Value;

                if (string.IsNullOrEmpty(id)) return Results.BadRequest("User not found");

                var response = await airdropTokenService.GetAirdropTokenAsync(id);

                return Results.Ok(response);
            }).RequireAuthorization().WithName("GetAirdopToken").WithOpenApi();

            app.MapPost("/confirm_receive_airdrop_token", async ([FromServices] IAirdropTokenService airdropTokenService, ClaimsPrincipal userClaimsPrincipal) =>
            {
                var id = userClaimsPrincipal.FindFirst(Constants.CustomClaimTypes.UserId)?.Value;

                if (string.IsNullOrEmpty(id)) return Results.BadRequest("User not found");

                var response = await airdropTokenService.ConfirmReceivedAirdropTokenAsync(id);

                return Results.Ok(response);
            }).RequireAuthorization().WithName("ConfirmReceiveTokenAirdopToken").WithOpenApi();
        }
    }
}
