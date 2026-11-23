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

/// <summary>Datos editables de una sala.</summary>
public sealed class SalaFormulario
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;
}
