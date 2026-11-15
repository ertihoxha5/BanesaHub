using BanesaHub.Domain.Properties;
using BanesaHub.Domain.Organizations;
using BanesaHub.Domain.Tenants;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BanesaHub.Infrastructure.Persistence;

public sealed class BanesaHubDbContext(DbContextOptions<BanesaHubDbContext> options)
    : IdentityDbContext<IdentityUser<Guid>, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Membership> Memberships => Set<Membership>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Building> Buildings => Set<Building>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<OccupancyPeriod> OccupancyPeriods => Set<OccupancyPeriod>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        PropertyModelConfiguration.Configure(builder);
        TenantModelConfiguration.Configure(builder);

        builder.Entity<AuditEvent>(audit =>
        {
            audit.Property(item => item.Action).HasMaxLength(80);
            audit.HasIndex(item => new { item.OrganizationId, item.At });
            audit.HasOne<Organization>().WithMany()
                .HasForeignKey(item => item.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Organization>(organization =>
        {
            organization.Property(item => item.Name).HasMaxLength(200);
            organization.Property(item => item.Slug).HasMaxLength(100);
            organization.HasIndex(item => item.Slug).IsUnique();
        });

        builder.Entity<OrganizationInvitation>(invitation =>
        {
            invitation.ToTable("OrganizationInvitations", table => table.HasCheckConstraint(
                "CK_OrganizationInvitations_Role", "[Role] IN (2, 3, 4, 5)"));
            invitation.Property(item => item.NormalizedEmail).HasMaxLength(256);
            invitation.Property(item => item.TokenHash).HasMaxLength(64).IsUnicode(false);
            invitation.HasIndex(item => item.TokenHash).IsUnique();
            invitation.Property(item => item.AcceptedAt).IsConcurrencyToken();
            invitation.HasOne<Organization>().WithMany()
                .HasForeignKey(item => item.OrganizationId).OnDelete(DeleteBehavior.Restrict);
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
