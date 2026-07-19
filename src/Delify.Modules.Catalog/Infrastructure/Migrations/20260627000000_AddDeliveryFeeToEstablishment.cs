using Delify.Modules.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Delify.Modules.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(CatalogDbContext))]
    [Migration("20260627000000_AddDeliveryFeeToEstablishment")]
    public partial class AddDeliveryFeeToEstablishment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryFee",
                schema: "catalog",
                table: "Establishments",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryFee",
                schema: "catalog",
                table: "Establishments");
        }
    }
}
