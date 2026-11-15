using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanesaHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RecordLeaseEndDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "EndedOn",
                table: "Leases",
                type: "date",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Leases_EndedOn",
                table: "Leases",
                sql: "([Status] = 3 AND [EndedOn] IS NOT NULL) OR ([Status] <> 3 AND [EndedOn] IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Leases_EndedOn",
                table: "Leases");

            migrationBuilder.DropColumn(
                name: "EndedOn",
                table: "Leases");
        }
    }
}
