using Delify.Modules.Orders.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Delify.Modules.Orders.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(OrdersDbContext))]
    [Migration("20260627000001_AddDeliveryFeeToOrder")]
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
