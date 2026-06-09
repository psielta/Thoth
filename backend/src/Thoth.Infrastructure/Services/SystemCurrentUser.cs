using Thoth.Application.Common.Interfaces;
using Thoth.Domain.Users;

namespace Thoth.Infrastructure.Services;

public sealed class SystemCurrentUser : ICurrentUser
{
    public Guid UserId => User.SystemUserId;
}
