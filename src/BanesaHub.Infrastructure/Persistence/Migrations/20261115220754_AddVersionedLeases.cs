using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanesaHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVersionedLeases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Leases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leases", x => x.Id);
                    table.UniqueConstraint("AK_Leases_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_Leases_Status", "[Status] IN (1, 2, 3, 4)");
                    table.ForeignKey(
                        name: "FK_Leases_Tenants_OrganizationId_TenantId",
                        columns: x => new { x.OrganizationId, x.TenantId },
                        principalTable: "Tenants",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Leases_Units_OrganizationId_UnitId",
                        columns: x => new { x.OrganizationId, x.UnitId },
                        principalTable: "Units",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaseVersions",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    MonthlyRent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Deposit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaseVersions", x => new { x.OrganizationId, x.LeaseId, x.VersionNumber });
                    table.CheckConstraint("CK_LeaseVersions_Dates", "[EffectiveFrom] >= [StartDate] AND ([EndDate] IS NULL OR [EndDate] >= [EffectiveFrom])");
                    table.CheckConstraint("CK_LeaseVersions_Money", "[MonthlyRent] >= 0 AND ([Deposit] IS NULL OR [Deposit] >= 0)");
                    table.CheckConstraint("CK_LeaseVersions_VersionNumber", "[VersionNumber] > 0");
                    table.ForeignKey(
                        name: "FK_LeaseVersions_Leases_OrganizationId_LeaseId",
                        columns: x => new { x.OrganizationId, x.LeaseId },
                        principalTable: "Leases",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Leases_OrganizationId_TenantId",
                table: "Leases",
                columns: new[] { "OrganizationId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_Leases_OrganizationId_UnitId_Status",
                table: "Leases",
                columns: new[] { "OrganizationId", "UnitId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeaseVersions");

            migrationBuilder.DropTable(
                name: "Leases");
        }
    }
}
