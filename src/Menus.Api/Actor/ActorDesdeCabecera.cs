using Menus.Application.Comun;

namespace Menus.Api.Actor;

/// <summary>
/// <see cref="IActorActual"/> provisional: toma el identificador del nutricionista de la cabecera
/// <see cref="Cabecera"/> (ADR-010, §6).
/// </summary>
/// <remarks>
/// Sirve mientras no haya autenticación. Con el #74 se reemplaza por una implementación que lee el token, y los
/// manejadores no cambian. La cabecera solo se lee cuando un caso de uso pide <see cref="Id"/>.
/// </remarks>
/// <param name="accesor">Acceso a la petición en curso.</param>
public sealed class ActorDesdeCabecera(IHttpContextAccessor accesor) : IActorActual
{
    /// <summary>Nombre de la cabecera con el identificador del nutricionista.</summary>
    public const string Cabecera = "X-Actor-Id";

    public Guid Id
    {
        get
        {
            var valor = accesor.HttpContext?.Request.Headers[Cabecera].ToString();
            if (string.IsNullOrWhiteSpace(valor))
            {
                throw new ActorNoIdentificadoException($"Falta la cabecera {Cabecera} con el identificador del nutricionista.");
            }

            return Guid.TryParse(valor, out var id)
                ? id
                : throw new ActorNoIdentificadoException($"La cabecera {Cabecera} no es un identificador válido.");
        }
    }
}
