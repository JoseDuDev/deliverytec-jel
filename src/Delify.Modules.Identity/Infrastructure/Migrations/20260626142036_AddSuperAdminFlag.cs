using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Delify.Modules.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSuperAdminFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSuperAdmin",
                schema: "identity",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSuperAdmin",
                schema: "identity",
                table: "AspNetUsers");
        }
    }
}
