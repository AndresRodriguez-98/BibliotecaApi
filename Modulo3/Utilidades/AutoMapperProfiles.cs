﻿using AutoMapper;
using Modulo3.DTOs;
using Modulo3.Entidades;

namespace Modulo3.Utilidades
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            CreateMap<LlaveAPI, LlaveDTO>();
            CreateMap<RestriccionDominio, RestriccionDominioDTO>();

            CreateMap<Autor, AutorDTO>()
                .ForMember(dto => dto.NombreCompleto,
                    config => config.MapFrom(autor => MapearNombreYApellidoAutor(autor)));

            CreateMap<Autor, AutorConLibrosDTO>()
               .ForMember(dto => dto.NombreCompleto,
                   config => config.MapFrom(autor => MapearNombreYApellidoAutor(autor)));

            CreateMap<AutorCreacionDTO, Autor>();
            CreateMap<AutorCreacionDTOConFoto, Autor>()
                .ForMember(ent => ent.Foto, config => config.Ignore());
            CreateMap<Autor, AutorPatchDTO>().ReverseMap();

            CreateMap<AutorLibro, LibroDTO>()
                .ForMember(dto => dto.Id, config => config.MapFrom(ent => ent.LibroId))
                .ForMember(dto => dto.Titulo, config => config.MapFrom(ent => ent.Libro!.Titulo));

            CreateMap<Libro, LibroDTO>();
            CreateMap<LibroCreacionDTO, Libro>()
                .ForMember(ent => ent.Autores, config =>
                    config.MapFrom(dto => dto.AutoresIds.Select(id => new AutorLibro { AutorId = id })));

            CreateMap<Libro, LibroConAutorDTO>();

            CreateMap<AutorLibro, AutorDTO>()
                .ForMember(dto => dto.Id, config => config.MapFrom(ent => ent.AutorId))
                .ForMember(dto => dto.NombreCompleto,
                    config => config.MapFrom(ent => MapearNombreYApellidoAutor(ent.Autor!)));

            CreateMap<LibroCreacionDTO, AutorLibro>()
                .ForMember(ent => ent.Libro,
                    config => config.MapFrom(dto => new Libro { Titulo = dto.Titulo }));

            CreateMap<ComentarioCreacionDTO, Comentario>();

            CreateMap<Comentario, ComentarioDTO>()
                // como en mi comentarioDTO tengo UsuarioEmail y en el identityUser tengo Email, necesito mappearlo:
                .ForMember(dto => dto.UsuarioEmail, config => config.MapFrom(ent => ent.Usuario!.Email));
            

            CreateMap<ComentarioPatchDTO, Comentario>().ReverseMap();

            CreateMap<Usuario, UsuarioDTO>();
        }

        private string MapearNombreYApellidoAutor(Autor autor) => $"{autor.Nombres} {autor.Apellidos}";
    }
}