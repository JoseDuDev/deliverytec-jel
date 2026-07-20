using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Delify.Modules.Payments.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "OrderId",
                schema: "payments",
                table: "Payments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "TableSessionId",
                schema: "payments",
                table: "Payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TableSessionId",
                schema: "payments",
                table: "Payments",
                column: "TableSessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_TableSessionId",
                schema: "payments",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "TableSessionId",
                schema: "payments",
                table: "Payments");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrderId",
                schema: "payments",
                table: "Payments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
