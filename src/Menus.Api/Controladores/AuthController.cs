using Menus.Api.Auth;
using Menus.Api.Seguridad;
using Menus.Application.Auth;
using Menus.Application.Auth.Login;
using Menus.Application.Auth.Perfil;
using Menus.Application.Auth.RegistroNutricionista;
using Menus.Application.Auth.RegistroPaciente;
using Menus.Application.Comun;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Menus.Api.Controladores;

/// <summary>
/// Endpoints de autenticación: login, registro (solo Admin) y perfil.
/// </summary>
/// <remarks>
/// Login lleva el límite de peticiones de <see cref="PoliticasDeTasa.Autenticacion"/>: es la puerta de entrada
/// y el punto donde tendría sentido una fuerza bruta. El registro no lo lleva porque ya exige el rol Admin, y
/// el perfil porque exige token.
/// </remarks>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController(IDespachador despachador) : ControllerBase
{
    /// <summary>Inicia sesión y devuelve un token JWT.</summary>
    /// <response code="200">Login exitoso; devuelve token, expiración y rol.</response>
    /// <response code="400">La solicitud está malformada (JSON inválido o cuerpo JSON vacío).</response>
    /// <response code="401">Credenciales inválidas (email inexistente o contraseña incorrecta).</response>
    /// <response code="422">La entrada no es válida según las reglas de negocio.</response>
    /// <response code="429">Se superó el límite de intentos; hay que esperar antes de reintentar.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(PoliticasDeTasa.Autenticacion)]
    [ProducesResponseType<LoginResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public Task<LoginResult> Login(LoginCommand comando, CancellationToken ct)
        => despachador.EnviarAsync(comando, ct);

    /// <summary>Registra un nuevo nutricionista (solo Admin).</summary>
    /// <response code="201">Nutricionista creado; devuelve el ID en el cuerpo.</response>
    /// <response code="400">La solicitud está malformada (JSON inválido o cuerpo JSON vacío).</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">No es administrador.</response>
    /// <response code="409">Ya existe un usuario con ese email.</response>
    /// <response code="422">La entrada no es válida según las reglas de negocio.</response>
    [HttpPost("registro/nutricionista")]
    [Authorize(Policy = Politicas.SoloAdmin)]
    [ProducesResponseType<IdCreadoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
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
    /// <response code="400">La solicitud está malformada (JSON inválido o cuerpo JSON vacío).</response>
    /// <response code="401">No autenticado.</response>
    /// <response code="403">No es administrador.</response>
    /// <response code="409">Ya existe un usuario con ese email.</response>
    /// <response code="422">La entrada no es válida según las reglas de negocio.</response>
    [HttpPost("registro/paciente")]
    [Authorize(Policy = Politicas.SoloAdmin)]
    [ProducesResponseType<IdCreadoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
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
