namespace MemberClaims.Api.Errors;

/// <summary>The requested record does not exist. Mapped to HTTP 404.</summary>
public sealed class NotFoundException(string message) : Exception(message);

/// <summary>The request breaks a business rule. Mapped to HTTP 422.</summary>
public sealed class BusinessRuleException(string message) : Exception(message);
