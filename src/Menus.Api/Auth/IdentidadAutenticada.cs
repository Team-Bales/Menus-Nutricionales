using System.Security.Claims;
using Menus.Application.Comun;
using Microsoft.AspNetCore.Http;

namespace Menus.Api.Auth;

/// <summary>
/// Implementación de <see cref="IIdentidadAutenticada"/> que lee los claims del token JWT.
/// </summary>
/// <remarks>
/// Lee exclusivamente <c>sub</c> (UsuarioId), <c>entidad_id</c> (EntidadId) y <c>role</c> (Rol) del token.
/// No consulta ni enriquece con campos role-specific.
/// </remarks>
public sealed class IdentidadAutenticada : IIdentidadAutenticada
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public IdentidadAutenticada(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UsuarioId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var subClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user?.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(subClaim) || !Guid.TryParse(subClaim, out var usuarioId))
            {
                throw new InvalidOperationException("El token no contiene un claim 'sub' válido.");
            }

            return usuarioId;
        }
    }

    public Guid? EntidadId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var entidadIdClaim = user?.FindFirst("entidad_id")?.Value;

            if (string.IsNullOrWhiteSpace(entidadIdClaim))
            {
                return null;
            }

            return Guid.TryParse(entidadIdClaim, out var entidadId) ? entidadId : null;
        }
    }

    public string Rol
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var roleClaim = user?.FindFirst(ClaimTypes.Role)?.Value
                ?? user?.FindFirst("role")?.Value;

            if (string.IsNullOrWhiteSpace(roleClaim))
            {
                throw new InvalidOperationException("El token no contiene un claim 'role' válido.");
            }

            return roleClaim;
        }
    }
}
