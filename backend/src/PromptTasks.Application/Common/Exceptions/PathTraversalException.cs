namespace PromptTasks.Application.Common.Exceptions;

public sealed class PathTraversalException(string message) : Exception(message);
