namespace MemberClaims.Api.Domain;

public enum MemberStatus
{
    Active,
    Inactive
}

/// <summary>A plan member (for example, a union member covered by a benefits fund).</summary>
public sealed class Member
{
    public int Id { get; set; }

    /// <summary>Business key shown to users, e.g. "M-100045".</summary>
    public string MemberNumber { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string EmployerName { get; set; } = string.Empty;

    public MemberStatus Status { get; set; } = MemberStatus.Active;

    public DateOnly EnrolledOn { get; set; }

    public List<Claim> Claims { get; set; } = new();
}
