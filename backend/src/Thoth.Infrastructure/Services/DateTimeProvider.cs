using Thoth.Application.Common.Interfaces;

namespace Thoth.Infrastructure.Services;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
