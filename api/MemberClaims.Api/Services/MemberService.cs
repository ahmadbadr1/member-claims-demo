// Member Claims Demo
// Copyright (c) 2026 Ahmed Ibrahim (https://www.linkedin.com/in/ahmed-badr-ibrahim)
// Licensed under the MIT License. See LICENSE in the repository root.

using MemberClaims.Api.Data;
using MemberClaims.Api.Domain;
using MemberClaims.Api.Dtos;
using MemberClaims.Api.Errors;
using Microsoft.EntityFrameworkCore;

namespace MemberClaims.Api.Services;

public interface IMemberService
{
    Task<PagedResult<MemberSummaryDto>> SearchAsync(string? search, int page, int pageSize, CancellationToken cancellationToken);

    Task<MemberDetailDto> GetAsync(int id, CancellationToken cancellationToken);

    Task<ClaimDto> AddClaimAsync(int memberId, CreateClaimRequest request, CancellationToken cancellationToken);
}

public sealed class MemberService(AppDbContext db, TimeProvider clock) : IMemberService
{
    private const int MaxPageSize = 100;

    public async Task<PagedResult<MemberSummaryDto>> SearchAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = db.Members.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            // StartsWith becomes LIKE 'term%', which can use the LastName index (sargable).
            query = query.Where(m =>
                m.LastName.StartsWith(term) ||
                m.FirstName.StartsWith(term) ||
                m.MemberNumber.StartsWith(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Projecting to a DTO with Select makes EF build one SQL query,
        // including the claim count, so there is no N+1 problem.
        var items = await query
            .OrderBy(m => m.LastName)
            .ThenBy(m => m.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MemberSummaryDto(
                m.Id,
                m.MemberNumber,
                m.FirstName + " " + m.LastName,
                m.EmployerName,
                m.Status,
                m.Claims.Count))
            .ToListAsync(cancellationToken);

        return new PagedResult<MemberSummaryDto>(items, page, pageSize, totalCount);
    }

    public async Task<MemberDetailDto> GetAsync(int id, CancellationToken cancellationToken)
    {
        var member = await db.Members
            .AsNoTracking()
            .Where(m => m.Id == id)
            .Select(m => new MemberDetailDto(
                m.Id,
                m.MemberNumber,
                m.FirstName,
                m.LastName,
                m.EmployerName,
                m.Status,
                m.EnrolledOn,
                m.Claims.Sum(c => (decimal?)c.Amount) ?? 0m,
                m.Claims
                    .OrderByDescending(c => c.ServiceDate)
                    .Select(c => new ClaimDto(
                        c.Id, c.ClaimNumber, c.ServiceDate, c.Amount, c.Description, c.Status, c.CreatedAtUtc))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        return member ?? throw new NotFoundException($"Member {id} was not found.");
    }

    public async Task<ClaimDto> AddClaimAsync(int memberId, CreateClaimRequest request, CancellationToken cancellationToken)
    {
        var member = await db.Members
            .Where(m => m.Id == memberId)
            .Select(m => new { m.Id, m.Status })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Member {memberId} was not found.");

        if (member.Status != MemberStatus.Active)
        {
            throw new BusinessRuleException("Claims can only be added for active members.");
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var serviceDate = request.ServiceDate!.Value;

        if (serviceDate > DateOnly.FromDateTime(now))
        {
            throw new BusinessRuleException("The service date can't be in the future.");
        }

        var claim = new Claim
        {
            MemberId = memberId,
            ClaimNumber = $"CLM-{now:yyyyMMddHHmmssfff}-{Random.Shared.Next(100, 999)}",
            ServiceDate = serviceDate,
            Amount = decimal.Round(request.Amount, 2),
            Description = request.Description.Trim(),
            Status = ClaimStatus.Submitted,
            CreatedAtUtc = now
        };

        db.Claims.Add(claim);
        await db.SaveChangesAsync(cancellationToken);

        return new ClaimDto(claim.Id, claim.ClaimNumber, claim.ServiceDate, claim.Amount,
            claim.Description, claim.Status, claim.CreatedAtUtc);
    }
}
