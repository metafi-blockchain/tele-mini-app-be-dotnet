
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using OkCoin.API.Models;
using OkCoin.API.Services;
using OkCoin.API.Utils;
using OkCoin.API.ViewModels;

namespace OkCoin.API.Endpoints;

public static class TaskEndpoint
{
    public static void MapTaskEndpoint(this WebApplication app) 
    {
        app.MapGet("/my-tasks",  ([FromServices] ITaskService taskService, ClaimsPrincipal userClaimsPrincipal) =>
            {
                var id = userClaimsPrincipal.FindFirst(Constants.CustomClaimTypes.UserId)?.Value;
                if(string.IsNullOrEmpty(id)) return Results.BadRequest("User not found");
                var tasks = taskService.MyTasks(id);
                return Results.Ok(tasks);
            })
            .RequireAuthorization().WithName("MyTasks").WithOpenApi();

        app.MapPost("/complete-task",  async ([FromBody]TaskSubmissionViewModel task, [FromServices] ITaskService taskService, ClaimsPrincipal userClaimsPrincipal) =>
            {
                var id = userClaimsPrincipal.FindFirst(Constants.CustomClaimTypes.UserId)?.Value;
                if(string.IsNullOrEmpty(id)) return Results.BadRequest("User not found");
                var completeTask = await taskService.CompleteTask(id, task.TaskId ?? string.Empty, task.Code);
                return Results.Ok(completeTask);
            })
            .RequireAuthorization().WithName("CompleteTask").WithOpenApi();

        app.MapPost("/insert-task",  async ([FromBody]TaskItem task, [FromServices] ITaskService taskService, ClaimsPrincipal userClaimsPrincipal) =>
            {
                var id = userClaimsPrincipal.FindFirst(Constants.CustomClaimTypes.UserId)?.Value;

                if(string.IsNullOrEmpty(id)) return Results.BadRequest("User not found");

                var respon = await taskService.CreateTaskItem(task);
                return Results.Ok(respon);
            })
            .RequireAuthorization().WithName("InsertTask").WithOpenApi();

        
        app.MapPut("/update-task",  async ([FromBody]TaskItem task, [FromServices] ITaskService taskService, ClaimsPrincipal userClaimsPrincipal) =>
            {
                var id = userClaimsPrincipal.FindFirst(Constants.CustomClaimTypes.UserId)?.Value;

                if(string.IsNullOrEmpty(id)) return Results.BadRequest("User not found");

                var respon = await taskService.UpdateTaskItem(task);
                return Results.Ok(respon);
            })
            .RequireAuthorization().WithName("UpdateTask").WithOpenApi();

        
        app.MapDelete("/delete-task/{taskId}",  async ([FromRoute]string taskId, [FromServices] ITaskService taskService, ClaimsPrincipal userClaimsPrincipal) =>
            {
                var id = userClaimsPrincipal.FindFirst(Constants.CustomClaimTypes.UserId)?.Value;

                if(string.IsNullOrEmpty(id)) return Results.BadRequest("User not found");

                var respon = await taskService.DeleteTaskItem(taskId);

                return Results.Ok(respon);
            })
            .RequireAuthorization().WithName("DeleteTask").WithOpenApi();

        
    }
}