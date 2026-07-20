using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Delify.Modules.Dinein.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionOpenedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OpenedByName",
                schema: "dinein",
                table: "Sessions",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OpenedByWaiterId",
                schema: "dinein",
                table: "Sessions",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OpenedByName",
                schema: "dinein",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "OpenedByWaiterId",
                schema: "dinein",
                table: "Sessions");
        }
    }
}
