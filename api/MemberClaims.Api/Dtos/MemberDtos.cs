// Member Claims Demo
// Copyright (c) 2026 Ahmed Ibrahim (https://www.linkedin.com/in/ahmed-ibrahim-151a21182)
// Licensed under the MIT License. See LICENSE in the repository root.

using System.ComponentModel.DataAnnotations;
using MemberClaims.Api.Domain;

namespace MemberClaims.Api.Dtos;

// DTOs keep the API contract separate from the EF Core entities.

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public sealed record MemberSummaryDto(
    int Id,
    string MemberNumber,
    string FullName,
    string EmployerName,
    MemberStatus Status,
    int ClaimCount);

public sealed record MemberDetailDto(
    int Id,
    string MemberNumber,
    string FirstName,
    string LastName,
    string EmployerName,
    MemberStatus Status,
    DateOnly EnrolledOn,
    decimal TotalClaimed,
    IReadOnlyList<ClaimDto> Claims);

public sealed record ClaimDto(
    int Id,
    string ClaimNumber,
    DateOnly ServiceDate,
    decimal Amount,
    string Description,
    ClaimStatus Status,
    DateTime CreatedAtUtc);

public sealed class CreateClaimRequest
{
    [Required]
    public DateOnly? ServiceDate { get; init; }

    [Range(typeof(decimal), "0.01", "100000")]
    public decimal Amount { get; init; }

    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Description { get; init; } = string.Empty;
}
