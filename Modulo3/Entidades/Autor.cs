﻿using BibliotecaAPI.Validaciones;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Modulo3.Entidades
{
    public class Autor
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "El campo {0} es requerido")] // esto es para ASP, para decirle que si llega un autor sin nombre que no ejecute la peticion
        [StringLength(150, ErrorMessage = "El campo {0} debe tener {1} caracteres o menos")]
        [PrimeraLetraMayuscula]
        public required string Apellidos { get; set; } // en cambio aca el required es de C#
        [Required(ErrorMessage = "El campo {0} es requerido")] // esto es para ASP, para decirle que si llega un autor sin nombre que no ejecute la peticion
        [StringLength(150, ErrorMessage = "El campo {0} debe tener {1} caracteres o menos")]
        [PrimeraLetraMayuscula]
        public required string Nombres { get; set; } // en cambio aca el required es de C#
        [StringLength(20, ErrorMessage = "El campo {0} debe tener {1} caracteres o menos")]
        public string? Identificacion { get; set; }
        // el unicode en falso nos permite utilizar varchar en lugar de nvarchar, por lo que somos mas rigurosos en que caracteres guardar
        [Unicode(false)]
        public string? Foto { get; set; }
        public List<AutorLibro> Libros { get; set; } = [];
    }
}
