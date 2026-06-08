namespace SafeEye.Domain.Exceptions;

public abstract class AppException(string message) : Exception(message);

public class NotFoundException(string entity, object key)
    : AppException($"{entity} with key '{key}' was not found.");

public class ConflictException(string message) : AppException(message);

public class ForbiddenException(string message = "Access denied.") : AppException(message);

public class UnauthorizedException(string message = "Authentication required.") : AppException(message);