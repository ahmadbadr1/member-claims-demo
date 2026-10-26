// Member Claims Demo
// Copyright (c) 2026 Ahmed Ibrahim (https://www.linkedin.com/in/ahmed-badr-ibrahim)
// Licensed under the MIT License. See LICENSE in the repository root.

using MemberClaims.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace MemberClaims.Api.Data;

/// <summary>Creates a small, realistic demo dataset the first time the API starts.</summary>
public static class SeedData
{
    public static async Task InitializeAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Members.AnyAsync(cancellationToken))
        {
            return;
        }

        var employers = new[] { "Great Lakes Freight", "Motor City Logistics", "Wolverine Distribution" };
        var people = new (string First, string Last)[]
        {
            ("Carol", "Anderson"), ("Marcus", "Bennett"), ("Aaliyah", "Carter"), ("David", "Nguyen"),
            ("Fatima", "Hassan"), ("James", "Kowalski"), ("Maria", "Lopez"), ("Omar", "Haddad"),
            ("Priya", "Shah"), ("Robert", "Miller"), ("Sofia", "Rossi"), ("Tyler", "Brooks")
        };

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var claimSequence = 1;

        for (var i = 0; i < people.Length; i++)
        {
            var member = new Member
            {
                MemberNumber = $"M-{100001 + i}",
                FirstName = people[i].First,
                LastName = people[i].Last,
                EmployerName = employers[i % employers.Length],
                Status = i % 7 == 6 ? MemberStatus.Inactive : MemberStatus.Active,
                EnrolledOn = today.AddYears(-(1 + i % 5)).AddDays(-i * 11)
            };

            var claimCount = i % 4;
            for (var c = 0; c < claimCount; c++)
            {
                member.Claims.Add(new Claim
                {
                    ClaimNumber = $"CLM-SEED-{claimSequence++:D6}",
                    ServiceDate = today.AddDays(-(10 + c * 23 + i)),
                    Amount = 85.50m + (i * 37) + (c * 112.25m),
                    Description = c % 2 == 0 ? "Office visit" : "Prescription",
                    Status = c == 0 ? ClaimStatus.Approved : ClaimStatus.Submitted,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-(5 + c)),
                    ReviewedAtUtc = c == 0 ? DateTime.UtcNow.AddDays(-4) : null
                });
            }

            db.Members.Add(member);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
