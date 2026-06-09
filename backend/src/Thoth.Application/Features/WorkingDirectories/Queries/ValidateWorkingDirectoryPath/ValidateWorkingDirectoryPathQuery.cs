using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.WorkingDirectories.Queries.ValidateWorkingDirectoryPath;

public sealed record ValidateWorkingDirectoryPathQuery(string AbsolutePath) : IRequest<ValidatePathResponse>;
