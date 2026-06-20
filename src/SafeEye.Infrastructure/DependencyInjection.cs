// ILocationUpdateRepository removed
// FirebaseRealtimeListenerService added as IHostedService
using SafeEye.Application.Common.Interfaces;
using SafeEye.Domain.Repositories;
using SafeEye.Infrastructure.Firebase;
using SafeEye.Infrastructure.Persistence;
using SafeEye.Infrastructure.Persistence.Repositories;
using SafeEye.Infrastructure.Realtime;
using SafeEye.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SafeEye.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(opts =>
            opts.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IIoTDeviceRepository, IoTDeviceRepository>();
        services.AddScoped<IGuardianDeviceRepository, GuardianDeviceRepository>();
        services.AddScoped<ISosEventRepository, SosEventRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        // ILocationUpdateRepository removed

        services.AddSingleton<IJwtService, JwtService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IGoogleAuthService, GoogleAuthService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IFirebaseUserService, FirebaseUserService>();
        services.AddScoped<IRealtimeNotifier, SignalRRealtimeNotifier>();

        var credPath = configuration["Firebase:CredentialsPath"];
        using var sp = services.BuildServiceProvider();
        NotificationService.TryInitFirebase(credPath, sp.GetRequiredService<ILogger<NotificationService>>());

        // ← NEW: Firebase RTDB SSE listener
        services.AddHttpClient("firebase-rtdb");
        services.AddHostedService<FirebaseRealtimeListenerService>();

        return services;
    }
}