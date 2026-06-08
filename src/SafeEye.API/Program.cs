using System.Text;
using SafeEye.API.Filters;
using SafeEye.API.Middleware;
using SafeEye.Application;
using SafeEye.Infrastructure;
using SafeEye.Infrastructure.Persistence;
using SafeEye.Infrastructure.Realtime;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Application + Infrastructure ─────────────────────────────────────────────
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// ── JWT Authentication ────────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };

        // Allow JWT in SignalR query string (?access_token=...)
        opts.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) &&
                    ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    ctx.Token = token;
                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization();

// ── SignalR ───────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ── Controllers ───────────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ── Filters ───────────────────────────────────────────────────────────────────
builder.Services.AddScoped<DeviceAuthFilter>();

// ── CORS ──────────────────────────────────────────────────────────────────────
var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["*"];
builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(p =>
        p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SafeEye API",
        Version = "v1",
        Description = "Guardian-side backend: track IoT devices, receive SOS alerts.",
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste your access token here.",
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
    c.AddSecurityDefinition("DeviceKey", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "X-Device-Key",
        Description = "IoT device authentication key.",
    });
});

// ── Health checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("postgres");

// ── Build ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Apply EF migrations on startup ───────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    app.Logger.LogInformation("Database migrations applied.");
}

// ── Middleware pipeline ───────────────────────────────────────────────────────
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SafeEye API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<TrackingHub>("/hubs/tracking");
app.MapHealthChecks("/health");

app.Run();