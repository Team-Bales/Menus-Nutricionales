using Menus.Application.Auth;
using Menus.Application.Comun;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Abstractions.Security;
using Menus.Domain.Entidades;
using Menus.Domain.Excepciones;
using Menus.Domain.Nomenclators;
using Menus.Domain;

namespace Menus.Application.Auth.Login;

/// <summary>Manejador para <see cref="LoginCommand"/>.</summary>
public sealed class LoginHandler : IHandler<LoginCommand, LoginResult>
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly INutricionistaRepository _nutricionistaRepository;
    private readonly INomencladorRepository<Especialidad> _especialidadRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenEmitter _tokenEmitter;

    public LoginHandler(
        IUsuarioRepository usuarioRepository,
        INutricionistaRepository nutricionistaRepository,
        INomencladorRepository<Especialidad> especialidadRepository,
        IPasswordHasher passwordHasher,
        ITokenEmitter tokenEmitter)
    {
        _usuarioRepository = usuarioRepository;
        _nutricionistaRepository = nutricionistaRepository;
        _especialidadRepository = especialidadRepository;
        _passwordHasher = passwordHasher;
        _tokenEmitter = tokenEmitter;
    }

    public async Task<LoginResult> HandleAsync(LoginCommand solicitud, CancellationToken ct)
    {
        var emailNormalizado = Usuario.NormalizarEmail(solicitud.Email);
        var usuario = await _usuarioRepository.GetByEmailAsync(emailNormalizado, ct);

        if (usuario is null)
        {
            // Usuario inexistente: ejecutamos VerifyDummy para igualar el tiempo de respuesta
            _passwordHasher.VerifyDummy(solicitud.Password);
            throw new CredencialesInvalidasException();
        }

        var passwordValida = _passwordHasher.Verify(solicitud.Password, usuario.PasswordHash);
        if (!passwordValida)
        {
            throw new CredencialesInvalidasException();
        }

        // Login exitoso: preparamos los claims según el rol
        Guid? entidadId = usuario.EntidadId;
        string? especialidad = null;
        bool esJefeNutricion = false;

        // Para nutricionistas, cargamos la entidad para obtener especialidad y EsJefe
        if (usuario.Rol == RolesUsuario.Nutricionista && entidadId.HasValue)
        {
            var nutricionista = await _nutricionistaRepository.GetByIdAsync(entidadId.Value, ct);
            if (nutricionista is not null)
            {
                var especialidadEntidad = await _especialidadRepository.GetByIdAsync(nutricionista.EspecialidadId, ct);
                if (especialidadEntidad is not null)
                {
                    especialidad = especialidadEntidad.Nombre;
                }
                esJefeNutricion = nutricionista.EsJefe;
            }
        }

        var tokenEmitido = _tokenEmitter.Emitir(
            usuario.Id,
            usuario.Rol,
            entidadId,
            especialidad,
            esJefeNutricion);

        return new LoginResult(tokenEmitido.Token, tokenEmitido.ExpiraUtc, usuario.Rol);
    }
}