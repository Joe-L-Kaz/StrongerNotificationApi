using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Stronger.Infrastructure;
using StrongerNotificationApi.Application.Extensions;
using StrongerNotificationApi.Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "CustomAuthentication";
}).AddScheme<AuthenticationSchemeOptions, CustomAuthenticationMiddleware>("CustomAuthentication", options => { });

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services
    .AddApplicationLayer()
    .AddInfrastructureLayer(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
