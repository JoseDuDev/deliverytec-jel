using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Delify.Modules.Orders.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderTypeAndTableSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TableSessionId",
                schema: "orders",
                table: "Orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                schema: "orders",
                table: "Orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Delivery");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TableSessionId",
                schema: "orders",
                table: "Orders",
                column: "TableSessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_TableSessionId",
                schema: "orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TableSessionId",
                schema: "orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Type",
                schema: "orders",
                table: "Orders");
        }
    }
}
