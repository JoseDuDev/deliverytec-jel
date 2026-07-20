using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Delify.Modules.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceFee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ServiceFeeEnabled",
                schema: "catalog",
                table: "Establishments",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceFeePercent",
                schema: "catalog",
                table: "Establishments",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 10m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ServiceFeeEnabled",
                schema: "catalog",
                table: "Establishments");

            migrationBuilder.DropColumn(
                name: "ServiceFeePercent",
                schema: "catalog",
                table: "Establishments");
        }
    }
}
