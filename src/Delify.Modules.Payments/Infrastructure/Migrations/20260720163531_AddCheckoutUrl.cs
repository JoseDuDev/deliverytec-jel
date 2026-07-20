using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Delify.Modules.Payments.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckoutUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CheckoutUrl",
                schema: "payments",
                table: "Payments",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheckoutUrl",
                schema: "payments",
                table: "Payments");
        }
    }
}
