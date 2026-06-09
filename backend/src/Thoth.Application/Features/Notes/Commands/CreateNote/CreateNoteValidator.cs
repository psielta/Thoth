using FluentValidation;
using Thoth.Domain.Notebooks;

namespace Thoth.Application.Features.Notes.Commands.CreateNote;

public sealed class CreateNoteValidator : AbstractValidator<CreateNoteCommand>
{
    public CreateNoteValidator()
    {
        RuleFor(command => command.NotebookId).NotEmpty();
        RuleFor(command => command.Title).NotEmpty().MaximumLength(Note.MaxTitleLength);
        RuleFor(command => command.ContentMarkdown).MaximumLength(Note.MaxContentLength);
    }
}
