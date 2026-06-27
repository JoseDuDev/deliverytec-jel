using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Delify.Modules.Orders.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryFeeToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryFee",
                schema: "orders",
                table: "Orders",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryFee",
                schema: "orders",
                table: "Orders");
        }
    }
}
