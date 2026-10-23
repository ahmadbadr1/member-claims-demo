using MemberClaims.Api.Data;
using MemberClaims.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace MemberClaims.Api.GraphQL;

/// <summary>
/// GraphQL entry point (Hot Chocolate). One endpoint, /graphql; the client chooses the fields.
/// UseProjection turns the requested fields into a single SQL SELECT, so asking for
/// members with their claims does not cause N+1 queries.
/// </summary>
public sealed class Query
{
    [UsePaging(DefaultPageSize = 20, MaxPageSize = 100, IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Member> GetMembers(AppDbContext db) =>
        db.Members.AsNoTracking().OrderBy(m => m.LastName).ThenBy(m => m.FirstName);

    [UseFirstOrDefault]
    [UseProjection]
    public IQueryable<Member> GetMemberByNumber(string memberNumber, AppDbContext db) =>
        db.Members.AsNoTracking().Where(m => m.MemberNumber == memberNumber);
}
