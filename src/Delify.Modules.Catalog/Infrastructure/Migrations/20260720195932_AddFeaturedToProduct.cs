using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Delify.Modules.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFeaturedToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FeaturedOrder",
                schema: "catalog",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                schema: "catalog",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeaturedOrder",
                schema: "catalog",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsFeatured",
                schema: "catalog",
                table: "Products");
        }
    }
}
