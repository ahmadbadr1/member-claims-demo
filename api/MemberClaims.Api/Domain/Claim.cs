// Member Claims Demo
// Copyright (c) 2026 Ahmed Ibrahim (https://www.linkedin.com/in/ahmed-ibrahim-151a21182)
// Licensed under the MIT License. See LICENSE in the repository root.

using HotChocolate;

namespace MemberClaims.Api.Domain;

public enum ClaimStatus
{
    Submitted,
    Approved,
    Denied
}

/// <summary>A benefit claim submitted for a member.</summary>
public sealed class Claim
{
    public int Id { get; set; }

    public int MemberId { get; set; }

    public Member? Member { get; set; }

    /// <summary>Business key, unique, e.g. "CLM-20261024-000123".</summary>
    public string ClaimNumber { get; set; } = string.Empty;

    public DateOnly ServiceDate { get; set; }

    /// <summary>Money is always decimal, never double.</summary>
    public decimal Amount { get; set; }

    public string Description { get; set; } = string.Empty;

    public ClaimStatus Status { get; set; } = ClaimStatus.Submitted;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ReviewedAtUtc { get; set; }

    /// <summary>Optimistic concurrency token (SQL Server rowversion).</summary>
    [GraphQLIgnore]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
