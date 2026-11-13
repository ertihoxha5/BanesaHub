using BanesaHub.Domain.Organizations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BanesaHub.Infrastructure.Persistence;

public sealed class BanesaHubDbContext(DbContextOptions<BanesaHubDbContext> options)
    : IdentityDbContext<IdentityUser<Guid>, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Membership> Memberships => Set<Membership>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Organization>(organization =>
        {
            organization.Property(item => item.Name).HasMaxLength(200);
            organization.Property(item => item.Slug).HasMaxLength(100);
            organization.HasIndex(item => item.Slug).IsUnique();
        });

        builder.Entity<Membership>(membership =>
        {
            membership.ToTable("Memberships", table => table.HasCheckConstraint(
                "CK_Memberships_Role", "[Role] IN (1, 2, 3, 4, 5)"));
            membership.HasKey(item => new { item.OrganizationId, item.UserId });
            membership.HasOne<Organization>().WithMany()
                .HasForeignKey(item => item.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            membership.HasOne<IdentityUser<Guid>>().WithMany()
                .HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
