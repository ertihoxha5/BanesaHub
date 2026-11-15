using BanesaHub.Domain.Organizations;
using BanesaHub.Domain.Properties;
using BanesaHub.Domain.Tenants;
using Microsoft.EntityFrameworkCore;

namespace BanesaHub.Infrastructure.Persistence;

internal static class TenantModelConfiguration
{
    public static void Configure(ModelBuilder builder)
    {
        builder.Entity<Tenant>(tenant =>
        {
            tenant.HasAlternateKey(item => new { item.OrganizationId, item.Id });
            tenant.Property(item => item.FullName).HasMaxLength(200);
            tenant.Property(item => item.Email).HasMaxLength(256);
            tenant.Property(item => item.Phone).HasMaxLength(32);
            tenant.HasIndex(item => new { item.OrganizationId, item.FullName });
            tenant.HasOne<Organization>().WithMany().HasForeignKey(item => item.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<OccupancyPeriod>(period =>
        {
            period.ToTable("OccupancyPeriods", table => table.HasCheckConstraint(
                "CK_OccupancyPeriods_DateOrder", "[EndDate] IS NULL OR [EndDate] >= [StartDate]"));
            period.HasIndex(item => new { item.OrganizationId, item.UnitId, item.StartDate }).IsUnique();
            period.HasIndex(item => new { item.OrganizationId, item.TenantId, item.StartDate });
            period.HasOne<Tenant>().WithMany()
                .HasForeignKey(item => new { item.OrganizationId, item.TenantId })
                .HasPrincipalKey(item => new { item.OrganizationId, item.Id })
                .OnDelete(DeleteBehavior.Restrict);
            period.HasOne<Unit>().WithMany()
                .HasForeignKey(item => new { item.OrganizationId, item.UnitId })
                .HasPrincipalKey(item => new { item.OrganizationId, item.Id })
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
