using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modulo3.Migrations
{
    /// <inheritdoc />
    public partial class ComentarioEstaBorrado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EstaBorrado",
                table: "Comentarios",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstaBorrado",
                table: "Comentarios");
        }
    }
}
