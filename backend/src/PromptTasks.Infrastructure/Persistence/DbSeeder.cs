using Microsoft.EntityFrameworkCore;
using PromptTasks.Domain.Users;
using PromptTasks.Domain.Workflows;

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

        if (!await context.WorkflowTemplates.AnyAsync(template => template.OwnerId == User.SystemUserId, cancellationToken))
        {
            context.WorkflowTemplates.Add(WorkflowDefaults.BuildTemplate(User.SystemUserId));
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
