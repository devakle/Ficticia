using BuildingBlocks.Infrastructure;
using BuildingBlocks.Infrastructure.Logging;
using BuildingBlocks.Infrastructure.Middleware;
using FluentValidation;
using Modules.People.Application.People.Commands;
using Modules.People.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// BuildingBlocks (middlewares + validation pipeline)
builder.Services.AddBuildingBlocks();

// Redis (opcional; si no lo tenés aún, borrá estas 2 líneas)
builder.Services.AddStackExchangeRedisCache(opt => opt.Configuration = "localhost:6379");

// People module (DbContext + repos + UoW + cache)
builder.Services.AddPeopleModule(builder.Configuration);

// MediatR: escanear handlers People
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreatePersonCommand).Assembly);
});

// Validators: escanear validators People
builder.Services.AddValidatorsFromAssembly(typeof(Modules.People.Application.Validators.CreatePersonValidator).Assembly);

var app = builder.Build();

await Api.Host.Seed.SeedPeopleDefaults.SeedAsync(app.Services);

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();

app.MapControllers();

app.Run();
