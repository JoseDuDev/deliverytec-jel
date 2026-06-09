using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Delify.Modules.Delivery.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "delivery");

            migrationBuilder.CreateTable(
                name: "DeliveryOrders",
                schema: "delivery",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    ProviderOrderId = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    QuotedPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    PickupAddress = table.Column<string>(type: "text", nullable: false),
                    DropoffAddress = table.Column<string>(type: "text", nullable: false),
                    TrackingUrl = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryOrders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrders_OrderId",
                schema: "delivery",
                table: "DeliveryOrders",
                column: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryOrders",
                schema: "delivery");
        }
    }
}
