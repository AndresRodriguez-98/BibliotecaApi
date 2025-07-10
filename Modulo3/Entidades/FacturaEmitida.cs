using Microsoft.EntityFrameworkCore;

namespace Modulo3.Entidades
{
    [PrimaryKey("Mes", "Año")]
    public class FacturaEmitida
    {
        public int Mes { get; set; }
        public int Año { get; set; }
    }
}
