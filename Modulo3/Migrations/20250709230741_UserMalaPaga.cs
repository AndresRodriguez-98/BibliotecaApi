using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modulo3.Migrations
{
    /// <inheritdoc />
    public partial class UserMalaPaga : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MalaPaga",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MalaPaga",
                table: "AspNetUsers");
        }
    }
}
