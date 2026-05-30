using MediatR;
using Microsoft.Extensions.Logging;

namespace PromptTasks.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<TRequest> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling request {RequestName}", typeof(TRequest).Name);
        var response = await next(cancellationToken);
        logger.LogInformation("Handled request {RequestName}", typeof(TRequest).Name);
        return response;
    }
}
