using BuildingBlocks.Infrastructure;
using BuildingBlocks.Infrastructure.Logging;
using BuildingBlocks.Infrastructure.Middleware;
using FluentValidation;
using Modules.People.Application.People.Commands;
using Modules.People.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Modules.Identity.Infrastructure.Persistence;
using System.Text;
using Microsoft.OpenApi;
using Modules.AI.Infrastructure;
using Modules.AI.Application.Conditions.Commands;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAiModule(builder.Configuration);

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(NormalizeConditionCommand).Assembly);
});

builder.Services.AddControllers();

// Identity Db
builder.Services.AddDbContext<IdentityDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("IdentityDb")));

// Identity Core
builder.Services
    .AddIdentityCore<IdentityUser>(opt =>
    {
        opt.Password.RequiredLength = 8;
        opt.Password.RequireDigit = true;
        opt.Password.RequireUppercase = true;
        opt.Password.RequireLowercase = true;
        opt.Password.RequireNonAlphanumeric = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<IdentityDbContext>()
    .AddSignInManager();

// JWT Auth
var jwt = builder.Configuration.GetSection("Jwt");
var jwtIssuer = jwt["Issuer"] ?? "Ficticia.Api";
var jwtAudience = jwt["Audience"] ?? "Ficticia.Web";
var jwtKey = jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key is required.");
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization(opt =>
{
    // Roles (ejemplo)
    opt.AddPolicy("People.Read", p => p.RequireRole("Admin", "Manager", "Viewer"));
    opt.AddPolicy("People.Write", p => p.RequireRole("Admin", "Manager"));
    opt.AddPolicy("Attributes.Manage", p => p.RequireRole("Admin"));
});

// Swagger
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Ficticia API", Version = "v1" });

    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    };

    c.AddSecurityDefinition("Bearer", scheme);

    c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer", doc, null), new List<string>() }
    });
});


// BuildingBlocks (middlewares + validation pipeline)
builder.Services.AddBuildingBlocks();

// Redis opcional
var redisConnection = builder.Configuration.GetValue<string>("Redis:ConnectionString");
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(opt => opt.Configuration = redisConnection);
}

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

// Seed DB
await Api.Host.Seed.SeedPeopleDefaults.SeedAsync(app.Services);
await Api.Host.Seed.SeedIdentityDefaults.SeedAsync(app.Services);

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
