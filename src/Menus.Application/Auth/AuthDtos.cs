using Menus.Application.Comun;

namespace Menus.Application.Auth;

/// <summary>
/// Solicitud de inicio de sesión.
/// </summary>
/// <param name="Email">Email del usuario.</param>
/// <param name="Password">Contraseña en texto plano.</param>
public sealed record LoginCommand(string Email, string Password) : ICommand<LoginResult>;

/// <summary>
/// Resultado del inicio de sesión exitoso.
/// </summary>
/// <param name="Token">Token JWT de acceso.</param>
/// <param name="Expira">Instante UTC en que el token expira.</param>
/// <param name="Rol">Rol del usuario autenticado.</param>
public sealed record LoginResult(string Token, DateTime Expira, string Rol);

/// <summary>
/// Solicitud de registro de un nutricionista (solo Admin).
/// </summary>
/// <param name="Nombre">Nombre del nutricionista.</param>
/// <param name="Email">Email del nutricionista.</param>
/// <param name="Password">Contraseña en texto plano.</param>
/// <param name="EspecialidadId">Identificador de la especialidad del nutricionista.</param>
public sealed record RegistrarNutricionistaCommand(string Nombre, string Email, string Password, Guid EspecialidadId) : ICommand<IdCreadoDto>;

/// <summary>
/// Solicitud de registro de un paciente (solo Admin).
/// </summary>
/// <param name="Nombre">Nombre del paciente.</param>
/// <param name="Email">Email del paciente.</param>
/// <param name="Password">Contraseña en texto plano.</param>
/// <param name="Edad">Edad del paciente en años.</param>
/// <param name="SalaId">Identificador de la sala de hospitalización.</param>
public sealed record RegistrarPacienteCommand(string Nombre, string Email, string Password, int Edad, Guid SalaId) : ICommand<IdCreadoDto>;

/// <summary>
/// Consulta para obtener el perfil del usuario autenticado.
/// </summary>
public sealed record ObtenerPerfilQuery() : IQuery<PerfilDto>;

/// <summary>
/// DTO con la información mínima del perfil del usuario autenticado.
/// </summary>
/// <param name="Email">Email del usuario.</param>
/// <param name="Rol">Rol del usuario.</param>
/// <param name="EntidadId">Identificador de la entidad de dominio asociada, si corresponde.</param>
public sealed record PerfilDto(string Email, string Rol, Guid? EntidadId);
