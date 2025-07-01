using Microsoft.EntityFrameworkCore;

namespace Modulo3.Utilidades
{
    // metodo estatico ya que es un metodo de extension para extender el httpcontext y que reciba por cabecera
    // .. la cant total de registros
    public static class HttpContextExtensions
    {
        // lo hacemos de tipo generico (<T>) porque quiero poder utilizarlo con autores, libros, coment, etc...
        public async static Task
           InsertarParametrosPaginacionEnCabecera<T>(this HttpContext httpContext,
           IQueryable<T> queryable)
        {
            if (httpContext is null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            double cantidad = await queryable.CountAsync();
            httpContext.Response.Headers.Append("cantidad-total-registros", cantidad.ToString());
        }
    }
}
