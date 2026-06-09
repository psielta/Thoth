namespace Thoth.Application.Common.Exceptions;

public sealed class ConflictException(string message) : Exception(message);
