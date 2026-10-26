// Member Claims Demo
// Copyright (c) 2026 Ahmed Ibrahim (https://www.linkedin.com/in/ahmed-ibrahim-151a21182)
// Licensed under the MIT License. See LICENSE in the repository root.

namespace MemberClaims.Api.Errors;

/// <summary>The requested record does not exist. Mapped to HTTP 404.</summary>
public sealed class NotFoundException(string message) : Exception(message);

/// <summary>The request breaks a business rule. Mapped to HTTP 422.</summary>
public sealed class BusinessRuleException(string message) : Exception(message);
