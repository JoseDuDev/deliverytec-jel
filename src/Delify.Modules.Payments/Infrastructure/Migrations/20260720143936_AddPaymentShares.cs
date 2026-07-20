using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Delify.Modules.Payments.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentShares : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ShareCount",
                schema: "payments",
                table: "Payments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShareIndex",
                schema: "payments",
                table: "Payments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_GatewayPaymentId",
                schema: "payments",
                table: "Payments",
                column: "GatewayPaymentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_GatewayPaymentId",
                schema: "payments",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ShareCount",
                schema: "payments",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ShareIndex",
                schema: "payments",
                table: "Payments");
        }
    }
}
