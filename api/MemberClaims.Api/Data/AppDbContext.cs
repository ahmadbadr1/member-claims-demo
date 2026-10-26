// Member Claims Demo
// Copyright (c) 2026 Ahmed Ibrahim (https://www.linkedin.com/in/ahmed-badr-ibrahim)
// Licensed under the MIT License. See LICENSE in the repository root.

using MemberClaims.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace MemberClaims.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Member> Members => Set<Member>();

    public DbSet<Claim> Claims => Set<Claim>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Member>(member =>
        {
            member.Property(m => m.MemberNumber).HasMaxLength(20).IsRequired();
            member.Property(m => m.FirstName).HasMaxLength(100).IsRequired();
            member.Property(m => m.LastName).HasMaxLength(100).IsRequired();
            member.Property(m => m.EmployerName).HasMaxLength(200).IsRequired();
            member.Property(m => m.Status).HasConversion<string>().HasMaxLength(20);

            member.HasIndex(m => m.MemberNumber).IsUnique();

            // Supports the sargable "LastName LIKE 'abc%'" search.
            member.HasIndex(m => new { m.LastName, m.FirstName });

            member.HasMany(m => m.Claims)
                  .WithOne(c => c.Member)
                  .HasForeignKey(c => c.MemberId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Claim>(claim =>
        {
            claim.Property(c => c.ClaimNumber).HasMaxLength(30).IsRequired();
            claim.Property(c => c.Amount).HasPrecision(18, 2);
            claim.Property(c => c.Description).HasMaxLength(200).IsRequired();
            claim.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
            claim.Property(c => c.RowVersion).IsRowVersion();

            claim.HasIndex(c => c.ClaimNumber).IsUnique();

            // Covers the background worker's query: pending claims ordered by age.
            claim.HasIndex(c => new { c.Status, c.CreatedAtUtc });
        });
    }
}
