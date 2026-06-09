using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Mappings;
using Thoth.Application.Common.Models;
using Thoth.Domain.Notebooks;

namespace Thoth.Application.Features.Notebooks;

/// <summary>
/// Builds a <see cref="NotebookDto"/> with the derived note count and linked
/// working directory name. Shared by the create/update/archive/get handlers so
/// the single-notebook responses stay consistent.
/// </summary>
internal static class NotebookDtoFactory
{
    public static NotebookDto ToDtoWithDetails(this Notebook notebook, IApplicationDbContext context)
    {
        var noteCount = context.Notes.Count(note => note.NotebookId == notebook.Id && !note.IsArchived);
        var workingDirectoryName = notebook.WorkingDirectoryId is null
            ? null
            : context.WorkingDirectories
                .Where(directory => directory.Id == notebook.WorkingDirectoryId)
                .Select(directory => directory.Name)
                .FirstOrDefault();

        return notebook.ToDto(noteCount, workingDirectoryName);
    }
}
