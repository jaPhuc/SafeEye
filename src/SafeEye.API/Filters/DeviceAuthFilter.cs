using SafeEye.Domain.Entities;
using SafeEye.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SafeEye.API.Filters;

/// <summary>
/// Validates the X-Device-Key header and stores the IoTDevice in HttpContext.Items["Device"].
/// Apply with [ServiceFilter(typeof(DeviceAuthFilter))] on a controller or action.
/// </summary>
public sealed class DeviceAuthFilter(IIoTDeviceRepository devices) : IAsyncActionFilter
{
    public const string DeviceKey = "Device";

    public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
    {
        var key = ctx.HttpContext.Request.Headers["X-Device-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(key))
        {
            ctx.Result = new UnauthorizedObjectResult(new { error = "Missing X-Device-Key header." });
            return;
        }

        var device = await devices.GetByDeviceKeyAsync(key);
        if (device is null)
        {
            ctx.Result = new UnauthorizedObjectResult(new { error = "Unknown IoT device." });
            return;
        }

        ctx.HttpContext.Items[DeviceKey] = device;
        await next();
    }
}

public sealed class DeviceKeyOperationFilter : Swashbuckle.AspNetCore.SwaggerGen.IOperationFilter
{
    public void Apply(Microsoft.OpenApi.Models.OpenApiOperation operation, Swashbuckle.AspNetCore.SwaggerGen.OperationFilterContext context)
    {
        var hasFilter = context.MethodInfo.DeclaringType?
            .GetCustomAttributes(true)
            .Concat(context.MethodInfo.GetCustomAttributes(true))
            .Any(a => a.GetType() == typeof(ServiceFilterAttribute)
                && ((ServiceFilterAttribute)a).ServiceType == typeof(DeviceAuthFilter));

        if (hasFilter == true)
        {
            operation.Security =
            [
                .. operation.Security ?? [],
                new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "DeviceKey"
                            }
                        },
                        []
                    }
                }
            ];
        }
    }
}

public static class HttpContextExtensions
{
    public static IoTDevice GetDevice(this HttpContext ctx)
        => ctx.Items[DeviceAuthFilter.DeviceKey] as IoTDevice
           ?? throw new InvalidOperationException("DeviceAuthFilter not applied.");

    public static Guid GetUserId(this HttpContext ctx)
    {
        var v = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(v, out var id) ? id : throw new UnauthorizedAccessException("User ID claim missing.");
    }
}