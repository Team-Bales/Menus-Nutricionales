using Menus.Application.Auth;
using Menus.Application.Auth.Login;
using Menus.Application.Auth.Perfil;
using Menus.Application.Auth.RegistroNutricionista;
using Menus.Application.Auth.RegistroPaciente;
using Menus.Application.Comun;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Menus.Api.Controladores;

/// <summary>
/// Endpoints de autenticación: login, registro (solo Admin) y perfil.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController(IDespachador despachador) : ControllerBase
{
    /// <summary>Inicia sesión y devuelve un token JWT.</summary>
    /// <response code="200">Login exitoso; devuelve token, expiración y rol.</response>
    /// <response code="401">Credenciales inválidas (email inexistente o contraseña incorrecta).</response>
    /// <response code="422">La entrada no es válida.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<LoginResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<LoginResult> Login(LoginCommand comando, CancellationToken ct)
        => despachador.EnviarAsync(comando, ct);

    /// <summary>Registra un nuevo nutricionista (solo Admin).</summary>
    /// <response code="201">Nutricionista creado; devuelve el ID en el cuerpo.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">No es administrador.</response>
    /// <response code="409">Ya existe un usuario con ese email.</response>
    /// <response code="422">La entrada no es válida.</response>
    [HttpPost("registro/nutricionista")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<IdCreadoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<IdCreadoDto>> RegistrarNutricionista(
        RegistrarNutricionistaCommand comando, CancellationToken ct)
    {
        var creado = await despachador.EnviarAsync(comando, ct);
        return StatusCode(StatusCodes.Status201Created, creado);
    }

    /// <summary>Registra un nuevo paciente (solo Admin).</summary>
    /// <response code="201">Paciente creado; devuelve el ID en el cuerpo.</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">No es administrador.</response>
    /// <response code="409">Ya existe un usuario con ese email.</response>
    /// <response code="422">La entrada no es válida.</response>
    [HttpPost("registro/paciente")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<IdCreadoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<IdCreadoDto>> RegistrarPaciente(
        RegistrarPacienteCommand comando, CancellationToken ct)
    {
        var creado = await despachador.EnviarAsync(comando, ct);
        return StatusCode(StatusCodes.Status201Created, creado);
    }

    /// <summary>Obtiene el perfil del usuario autenticado.</summary>
    /// <response code="200">Perfil del usuario (email, rol, entidadId).</response>
    /// <response code="401">No autenticado.</response>
    [HttpGet("perfil")]
    [Authorize]
    [ProducesResponseType<PerfilDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public Task<PerfilDto> Perfil(CancellationToken ct)
        => despachador.EnviarAsync(new ObtenerPerfilQuery(), ct);
}
