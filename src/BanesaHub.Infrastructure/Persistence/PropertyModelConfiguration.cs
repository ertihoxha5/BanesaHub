using BanesaHub.Domain.Organizations;
using BanesaHub.Domain.Properties;
using Microsoft.EntityFrameworkCore;

namespace BanesaHub.Infrastructure.Persistence;

internal static class PropertyModelConfiguration
{
    public static void Configure(ModelBuilder builder)
    {
        builder.Entity<Property>(property =>
        {
            property.ToTable("Properties", table => table.HasCheckConstraint(
                "CK_Properties_Status", "[Status] IN (1, 2, 3)"));
            property.HasAlternateKey(item => new { item.OrganizationId, item.Id });
            property.Property(item => item.Name).HasMaxLength(200);
            property.Property(item => item.Address).HasMaxLength(300);
            property.Property(item => item.Category).HasMaxLength(100);
            property.HasOne<Organization>().WithMany().HasForeignKey(item => item.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Building>(building =>
        {
            building.ToTable("Buildings", table => table.HasCheckConstraint(
                "CK_Buildings_FloorCount", "[FloorCount] IS NULL OR [FloorCount] > 0"));
            building.HasAlternateKey(item => new { item.OrganizationId, item.Id });
            building.Property(item => item.Name).HasMaxLength(200);
            building.HasOne<Property>().WithMany()
                .HasForeignKey(item => new { item.OrganizationId, item.PropertyId })
                .HasPrincipalKey(item => new { item.OrganizationId, item.Id })
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Unit>(unit =>
        {
            unit.ToTable("Units", table =>
            {
                table.HasCheckConstraint("CK_Units_Area", "[Area] IS NULL OR [Area] > 0");
                table.HasCheckConstraint("CK_Units_Status", "[Status] IN (1, 2, 3)");
            });
            unit.Property(item => item.Area).HasPrecision(12, 2);
            unit.Property(item => item.Number).HasMaxLength(50);
            unit.HasIndex(item => new { item.OrganizationId, item.BuildingId, item.Number }).IsUnique();
            unit.HasOne<Building>().WithMany()
                .HasForeignKey(item => new { item.OrganizationId, item.BuildingId })
                .HasPrincipalKey(item => new { item.OrganizationId, item.Id })
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
