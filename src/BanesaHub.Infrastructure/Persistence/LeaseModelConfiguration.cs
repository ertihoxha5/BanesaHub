using BanesaHub.Domain.Properties;
using BanesaHub.Domain.Tenants;
using Microsoft.EntityFrameworkCore;

namespace BanesaHub.Infrastructure.Persistence;

internal static class LeaseModelConfiguration
{
    public static void Configure(ModelBuilder builder)
    {
        builder.Entity<Lease>(lease =>
        {
            lease.ToTable("Leases", table =>
            {
                table.HasCheckConstraint("CK_Leases_Status", "[Status] IN (1, 2, 3, 4)");
                table.HasCheckConstraint("CK_Leases_EndedOn",
                    "([Status] = 3 AND [EndedOn] IS NOT NULL) OR ([Status] <> 3 AND [EndedOn] IS NULL)");
            });
            lease.HasAlternateKey(item => new { item.OrganizationId, item.Id });
            lease.HasIndex(item => new { item.OrganizationId, item.UnitId, item.Status });
            lease.HasOne<Unit>().WithMany()
                .HasForeignKey(item => new { item.OrganizationId, item.UnitId })
                .HasPrincipalKey(item => new { item.OrganizationId, item.Id })
                .OnDelete(DeleteBehavior.Restrict);
            lease.HasOne<Tenant>().WithMany()
                .HasForeignKey(item => new { item.OrganizationId, item.TenantId })
                .HasPrincipalKey(item => new { item.OrganizationId, item.Id })
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<LeaseVersion>(version =>
        {
            version.ToTable("LeaseVersions", table =>
            {
                table.HasCheckConstraint("CK_LeaseVersions_VersionNumber", "[VersionNumber] > 0");
                table.HasCheckConstraint("CK_LeaseVersions_Dates",
                    "[EffectiveFrom] >= [StartDate] AND ([EndDate] IS NULL OR [EndDate] >= [EffectiveFrom])");
                table.HasCheckConstraint("CK_LeaseVersions_Money",
                    "[MonthlyRent] >= 0 AND ([Deposit] IS NULL OR [Deposit] >= 0)");
            });
            version.HasKey(item => new { item.OrganizationId, item.LeaseId, item.VersionNumber });
            version.Property(item => item.MonthlyRent).HasPrecision(18, 2);
            version.Property(item => item.Deposit).HasPrecision(18, 2);
            version.HasOne<Lease>().WithMany()
                .HasForeignKey(item => new { item.OrganizationId, item.LeaseId })
                .HasPrincipalKey(item => new { item.OrganizationId, item.Id })
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
