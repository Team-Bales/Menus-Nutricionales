namespace Menus.Client.PanelPaciente;

/// <summary>Un ítem aceptado o rechazado durante un servicio de menú.</summary>
public sealed record ConsumoDePaciente(Guid ItemId, string Nombre, bool Aceptacion);

/// <summary>Un menú servido al paciente, con sus consumos registrados.</summary>
public sealed record ServicioDePaciente(Guid MenuId, Guid DietaId, DateOnly Fecha, IReadOnlyList<ConsumoDePaciente> Consumos);

/// <summary>Resultado clínico de una valoración nutricional.</summary>
public sealed record ResultadoNutricional(Guid Id, string Nombre, int Valor);

/// <summary>Valoración de un menú asignado al paciente.</summary>
public sealed record ValoracionDePaciente(Guid Id, Guid MenuId, Guid PacienteId, Guid NutricionistaId, ResultadoNutricional Resultado, DateTime FechaValoracion);
