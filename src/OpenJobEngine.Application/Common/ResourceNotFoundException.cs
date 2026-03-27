namespace OpenJobEngine.Application.Common;

public sealed class ResourceNotFoundException(string message) : Exception(message);
