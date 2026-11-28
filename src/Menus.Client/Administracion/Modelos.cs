using System.ComponentModel.DataAnnotations;

namespace Menus.Client.Administracion;

/// <summary>Nutricionista mostrado en la administración.</summary>
public sealed record Nutricionista(Guid Id, string Nombre, Guid EspecialidadId, bool EsJefe);

/// <summary>Detalle de un nutricionista y sus dietas autorizadas.</summary>
public sealed record NutricionistaDetalle(Guid Id, string Nombre, Guid EspecialidadId, bool EsJefe, IReadOnlyList<DietaAutorizada> DietasAutorizadas);

/// <summary>Relación de autorización entre un nutricionista y una dieta.</summary>
public sealed record DietaAutorizada(Guid DietaId);

/// <summary>Sala de hospitalización.</summary>
public sealed record Sala(Guid Id, string Nombre);

/// <summary>Paciente mostrado en la gestión: los datos del listado.</summary>
public sealed record Paciente(Guid Id, string Nombre, int Edad, Guid SalaId);

/// <summary>Detalle de un paciente, con su sala y sus dietas inscritas.</summary>
public sealed record PacienteDetalle(Guid Id, string Nombre, int Edad, SalaDelPaciente Sala, IReadOnlyList<DietaInscrita> Dietas);

/// <summary>Sala de un paciente, con su nombre.</summary>
public sealed record SalaDelPaciente(Guid Id, string Nombre);

/// <summary>Dieta en la que está inscrito un paciente, con su nombre.</summary>
public sealed record DietaInscrita(Guid Id, string Nombre);

/// <summary>Datos editables de una sala.</summary>
public sealed class SalaFormulario
{
    /// <summary>Nombre de la sala; obligatorio, hasta 100 caracteres.</summary>
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;
}
