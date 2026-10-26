// Member Claims Demo
// Copyright (c) 2026 Ahmed Ibrahim (https://www.linkedin.com/in/ahmed-badr-ibrahim)
// Licensed under the MIT License. See LICENSE in the repository root.

using MemberClaims.Api.Data;
using MemberClaims.Api.Domain;
using MemberClaims.Api.Dtos;
using MemberClaims.Api.Errors;
using MemberClaims.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace MemberClaims.Api.Tests;

public sealed class MemberServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);

    // Each test gets its own in-memory database, so tests never affect each other.
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new AppDbContext(options);

        db.Members.AddRange(
            new Member
            {
                Id = 1, MemberNumber = "M-1", FirstName = "Carol", LastName = "Anderson",
                EmployerName = "Great Lakes Freight", Status = MemberStatus.Active,
                Claims =
                {
                    new Claim { Id = 10, ClaimNumber = "C-10", Amount = 100m, Description = "Office visit",
                                ServiceDate = new DateOnly(2026, 9, 1), CreatedAtUtc = Now.UtcDateTime },
                    new Claim { Id = 11, ClaimNumber = "C-11", Amount = 50.25m, Description = "Prescription",
                                ServiceDate = new DateOnly(2026, 10, 1), CreatedAtUtc = Now.UtcDateTime }
                }
            },
            new Member
            {
                Id = 2, MemberNumber = "M-2", FirstName = "Marcus", LastName = "Bennett",
                EmployerName = "Motor City Logistics", Status = MemberStatus.Inactive
            });

        db.SaveChanges();
        return db;
    }

    private static MemberService CreateService(AppDbContext db) => new(db, new FixedTimeProvider(Now));

    [Fact]
    public async Task Search_ByLastNamePrefix_ReturnsMatchWithClaimCount()
    {
        await using var db = CreateDb();

        var result = await CreateService(db).SearchAsync("And", page: 1, pageSize: 20, CancellationToken.None);

        var member = Assert.Single(result.Items);
        Assert.Equal("Carol Anderson", member.FullName);
        Assert.Equal(2, member.ClaimCount);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task Get_ReturnsClaimsNewestFirst_AndTotal()
    {
        await using var db = CreateDb();

        var member = await CreateService(db).GetAsync(1, CancellationToken.None);

        Assert.Equal("C-11", member.Claims[0].ClaimNumber);
        Assert.Equal(150.25m, member.TotalClaimed);
    }

    [Fact]
    public async Task Get_UnknownMember_ThrowsNotFound()
    {
        await using var db = CreateDb();

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService(db).GetAsync(999, CancellationToken.None));
    }

    [Fact]
    public async Task AddClaim_FutureServiceDate_IsRejected()
    {
        await using var db = CreateDb();
        var request = new CreateClaimRequest
        {
            ServiceDate = new DateOnly(2026, 12, 31),
            Amount = 75m,
            Description = "Lab work"
        };

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => CreateService(db).AddClaimAsync(1, request, CancellationToken.None));
    }

    [Fact]
    public async Task AddClaim_InactiveMember_IsRejected()
    {
        await using var db = CreateDb();
        var request = new CreateClaimRequest
        {
            ServiceDate = new DateOnly(2026, 10, 20),
            Amount = 75m,
            Description = "Lab work"
        };

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => CreateService(db).AddClaimAsync(2, request, CancellationToken.None));
    }

    [Fact]
    public async Task AddClaim_Valid_SavesSubmittedClaim()
    {
        await using var db = CreateDb();
        var request = new CreateClaimRequest
        {
            ServiceDate = new DateOnly(2026, 10, 20),
            Amount = 75.555m,
            Description = "  Lab work  "
        };

        var claim = await CreateService(db).AddClaimAsync(1, request, CancellationToken.None);

        Assert.Equal(ClaimStatus.Submitted, claim.Status);
        Assert.Equal(75.56m, claim.Amount);            // rounded to cents
        Assert.Equal("Lab work", claim.Description);   // trimmed
        Assert.Equal(3, await db.Claims.CountAsync(c => c.MemberId == 1));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
