﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modulo3.Migrations
{
    /// <inheritdoc />
    public partial class CrearSPUsuariosMalaPaga : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE PROCEDURE Usuarios_SetearMalaPaga
	-- Add the parameters for the stored procedure here
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	
	UPDATE AspNetUsers
	SET
	MalaPaga = 'True'
	FROM Facturas
	INNER JOIN AspNetUsers
	ON AspNetUsers.Id = Facturas.UsuarioId
	WHERE Pagada = 'False' AND FechaLimiteDePago < GETDATE()

END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP PROCEDURE Usuarios_SetearMalaPaga");
        }
    }
}
