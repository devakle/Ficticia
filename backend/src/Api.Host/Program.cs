using BuildingBlocks.Infrastructure;
using BuildingBlocks.Infrastructure.Middleware;
using BuildingBlocks.Infrastructure.Logging;
using Modules.People.Infrastructure;
using Modules.People.Application.People.Commands;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBuildingBlocks();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddPeopleModule(builder.Configuration);

// MediatR: escanear el assembly del módulo
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreatePersonCommand).Assembly);
});

// FluentValidation: escanear validators del módulo
builder.Services.AddValidatorsFromAssembly(typeof(Modules.People.Application.Validators.CreatePersonValidator).Assembly);

var app = builder.Build();

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
