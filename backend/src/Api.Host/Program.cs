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

var builder = WebApplication.CreateBuilder(args);

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
var keyBytes = Encoding.UTF8.GetBytes(jwt["Key"]!);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"],

            ValidateAudience = true,
            ValidAudience = jwt["Audience"],

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

    c.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer", null, null), new List<string>() }
    });
});


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
