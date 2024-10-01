using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OkCoin.API.Endpoints;
using OkCoin.API.Models;
using OkCoin.API.Services;
using OkCoin.API.Utils;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors();
builder.Services.AddAuthentication().AddJwtBearer(op =>
{
    op.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"] ?? string.Empty))
    };
    op.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var userId = context.Principal?.FindFirst(Constants.CustomClaimTypes.UserId)?.Value;
            if (userId != null)
            {
                var statisticService = context.HttpContext.RequestServices.GetRequiredService<IStatisticService>();
                await statisticService.MarkUserAsOnlineAsync(userId);
            }
        }
    };
});
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Api", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "Bearer {token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
    {
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            },
            Scheme = "oauth2",
            Name = "Bearer",
            In = ParameterLocation.Header,
        },
        new List<string>()
    }});
});
builder.Services.Configure<DbSettings>(
    builder.Configuration.GetSection("MyDatabase"));
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<TonChainSettings>(
    builder.Configuration.GetSection("TonChainSettings"));
builder.Services.Configure<CronJobSettings>(
    builder.Configuration.GetSection("CronJobSettings"));

builder.Services.AddSingleton<IUserService, UserService>();
builder.Services.AddSingleton<ITappingService, TappingService>();
builder.Services.AddSingleton<ITaskService, TaskService>();
builder.Services.AddSingleton<ICacheService, RedisCacheService>();
builder.Services.AddSingleton<IStatisticService, StatisticService>();
builder.Services.AddSingleton<ITonChainService, TonChainService>();
builder.Services.AddHttpClient();
builder.Services.AddHostedService<BackgroundCronJobService>();
var app = builder.Build();
app.UseCors(o =>
{
    o.AllowAnyOrigin();
    o.AllowAnyMethod();
    o.AllowAnyHeader();
});
app.UseAuthentication();
app.UseAuthorization();
// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapUserEndpoint();
app.MapBoostEndpoint();
app.MapTapEndpoint();
app.MapOtherEndpoint();

app.Run();