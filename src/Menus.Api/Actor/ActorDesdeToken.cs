using Menus.Application.Comun;
using Menus.Domain;
using Menus.Infrastructure.Security;

namespace Menus.Api.Actor;

/// <summary>
/// <see cref="IActorActual"/> que toma al nutricionista del token (#74): el claim <c>entidad_id</c> de un usuario con
/// rol <c>Nutricionista</c> es el identificador del nutricionista (ADR-010, §6).
/// </summary>
/// <remarks>
/// Reemplaza a la cabecera <c>X-Actor-Id</c>, que el cliente podía inventar. Los manejadores no cambian. Las acciones
/// que usan al actor exigen <see cref="Auth.Politicas.SoloNutricionista"/>, así que los dos errores de abajo solo
/// aparecen si una acción se registra sin esa política.
/// </remarks>
/// <param name="accesor">Acceso a la petición en curso.</param>
public sealed class ActorDesdeToken(IHttpContextAccessor accesor) : IActorActual
{
    public Guid Id
    {
        get
        {
            var usuario = accesor.HttpContext?.User;
            if (usuario is null || !usuario.IsInRole(RolesUsuario.Nutricionista))
            {
                throw new ActorNoIdentificadoException("La petición no viene de un nutricionista autenticado.");
            }

            return Guid.TryParse(usuario.FindFirst(JwtClaimTypes.EntidadId)?.Value, out var id)
                ? id
                : throw new ActorNoIdentificadoException(
                    $"El token del nutricionista no trae su identificador ({JwtClaimTypes.EntidadId}).");
        }
    }
}
