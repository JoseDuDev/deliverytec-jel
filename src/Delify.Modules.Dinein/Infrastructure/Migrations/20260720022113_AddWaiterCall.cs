using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Delify.Modules.Dinein.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWaiterCall : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WaiterCalls",
                schema: "dinein",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EstablishmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TableId = table.Column<Guid>(type: "uuid", nullable: false),
                    TableSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    TableNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Reason = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AcknowledgedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaiterCalls", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WaiterCalls_EstablishmentId_Status",
                schema: "dinein",
                table: "WaiterCalls",
                columns: new[] { "EstablishmentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WaiterCalls_TableId_Status",
                schema: "dinein",
                table: "WaiterCalls",
                columns: new[] { "TableId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WaiterCalls",
                schema: "dinein");
        }
    }
}
