using SafeEye.API.Middleware;
using SafeEye.Application;
using SafeEye.Infrastructure;
using SafeEye.Infrastructure.Persistence;
using SafeEye.Infrastructure.Realtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Application + Infrastructure ─────────────────────────────────────────────
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// ── SignalR ───────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ── Controllers ───────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

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

app.MapControllers();
app.MapHub<TrackingHub>("/hubs/tracking");
app.MapHealthChecks("/health");

app.Run();