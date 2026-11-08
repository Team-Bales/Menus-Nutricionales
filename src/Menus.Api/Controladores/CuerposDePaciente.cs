namespace Menus.Api.Controladores;

/// <summary>Cuerpo de <c>PUT /api/pacientes/{id}</c>: los datos nuevos del paciente. El id va en la ruta.</summary>
/// <param name="Nombre">Nuevo nombre.</param>
/// <param name="Edad">Nueva edad en años enteros, mayor o igual que cero (RD-06).</param>
/// <param name="SalaId">Sala a la que pertenece; si cambia, el paciente se traslada.</param>
public sealed record DatosDePaciente(string Nombre, int Edad, Guid SalaId);

/// <summary>
/// Cuerpo de <c>POST /api/pacientes/{id}/dietas</c>: la dieta en la que se inscribe. El id del paciente va en la
/// ruta.
/// </summary>
/// <param name="DietaId">Identificador de la dieta.</param>
public sealed record InscripcionEnDieta(Guid DietaId);
