using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Domain.Users;

namespace PromptTasks.Infrastructure.Services;

public sealed class SystemCurrentUser : ICurrentUser
{
    public Guid UserId => User.SystemUserId;
}
