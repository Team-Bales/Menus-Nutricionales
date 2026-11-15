using Menus.Application.Auth;
using Menus.Application.Comun;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Excepciones;

namespace Menus.Application.Auth.Perfil;

/// <summary>Manejador para <see cref="ObtenerPerfilQuery"/>.</summary>
public sealed class ObtenerPerfilHandler : IHandler<ObtenerPerfilQuery, PerfilDto>
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IIdentidadAutenticada _identidadAutenticada;

    /// <summary>Crea el manejador con sus dependencias.</summary>
    /// <param name="usuarioRepository">Usuarios, para buscar la cuenta.</param>
    /// <param name="identidadAutenticada">Identidad del usuario que hace la petición, tomada del token.</param>
    public ObtenerPerfilHandler(
        IUsuarioRepository usuarioRepository,
        IIdentidadAutenticada identidadAutenticada)
    {
        _usuarioRepository = usuarioRepository;
        _identidadAutenticada = identidadAutenticada;
    }

    /// <exception cref="NoEncontradoException">Si la cuenta del token ya no existe.</exception>
    public async Task<PerfilDto> HandleAsync(ObtenerPerfilQuery solicitud, CancellationToken ct)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(_identidadAutenticada.UsuarioId, ct);
        if (usuario is null)
        {
            throw new NoEncontradoException("usuario", _identidadAutenticada.UsuarioId);
        }

        return new PerfilDto(usuario.Email, usuario.Rol, usuario.EntidadId);
    }
}
