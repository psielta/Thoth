using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.AppSettings.Queries.GetAppSettings;

public sealed record GetAppSettingsQuery : IRequest<AppSettingsDto>;
