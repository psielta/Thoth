using Microsoft.EntityFrameworkCore;
using PromptTasks.Domain.Users;

namespace PromptTasks.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        if (!await context.Users.AnyAsync(user => user.Id == User.SystemUserId, cancellationToken))
        {
            context.Users.Add(new User
            {
                Id = User.SystemUserId,
                DisplayName = "system",
                IsSystem = true,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
