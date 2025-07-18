﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modulo3.Migrations
{
    /// <inheritdoc />
    public partial class CreaStoredProcedure_Facturas_Crear : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE PROCEDURE Facturas_Crear
	-- Add the parameters for the stored procedure here
	@fechaInicio datetime,
	@fechaFin datetime
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	DECLARE @montoPorCadaPeticion decimal(4,4) = 1.0/2 -- 1 dolar por cada dos peticiones

INSERT INTO Facturas(UsuarioId, Monto, FechaEmision, FechaLimiteDePago, Pagada)
SELECT
UsuarioId,
COUNT(*) * @montoPorCadaPeticion as monto,
GETDATE() AS FechaEmision,
DATEADD(d, 60, GETDATE()) as fechaLimiteDePago,
0 as Pagada
FROM Peticiones
INNER JOIN LlavesAPI
ON LlavesAPI.Id = Peticiones.LlaveId
WHERE LlavesAPI.TipoLlave != 1 AND FechaPeticion >= @fechaInicio AND FechaPeticion < @fechaFin
GROUP BY UsuarioId

INSERT INTO FacturasEmitidas(Mes, Año)
SELECT
	CASE MONTH(GETDATE())
	WHEN 1 THEN 12
	ELSE MONTH(GETDATE()) - 1 END AS Mes,

	CASE MONTH(GETDATE())
	WHEN 1 THEN YEAR(GETDATE()) - 1
	ELSE YEAR(GETDATE()) END AS Año
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE Facturas_Crear");
        }
    }
}
