﻿using System.ComponentModel.DataAnnotations;

namespace Modulo3.DTOs
{
    public class ComentarioDTO
    {
        public Guid Id { get; set; }
        public required string Cuerpo { get; set; }
        public DateTime FechaPublicacion { get; set; }
        public required string UsuarioId { get; set; }
        public required string UsuarioEmail { get; set; }
    }
}
