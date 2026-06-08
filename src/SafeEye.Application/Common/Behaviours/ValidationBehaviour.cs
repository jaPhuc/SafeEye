using FluentValidation;
using MediatR;

namespace SafeEye.Application.Common.Behaviours;

public sealed class ValidationBehaviour<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!validators.Any()) return await next();

        var ctx = new ValidationContext<TRequest>(request);
        var errors = (await Task.WhenAll(validators.Select(v => v.ValidateAsync(ctx, ct))))
                     .SelectMany(r => r.Errors)
                     .Where(f => f != null)
                     .ToList();

        if (errors.Count > 0) throw new ValidationException(errors);

        return await next();
    }
}