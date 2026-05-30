using PromptTasks.Application.Common.Interfaces;

namespace PromptTasks.Infrastructure.Services;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
