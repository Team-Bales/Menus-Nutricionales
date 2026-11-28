namespace Menus.Client.GestionMenus;

/// <summary>Persona que creó o dictaminó un menú.</summary>
public sealed record NutricionistaDelMenu(Guid Id, string Nombre);

/// <summary>Dictamen registrado para un menú.</summary>
public sealed record DictamenDeMenu(NutricionistaDelMenu Revisor, DateTime Fecha, bool Aprobado, string? Observaciones);

/// <summary>Proporción de ítems de un tipo de preparación.</summary>
public sealed record ProporcionDelMenu(Guid TipoPreparacionId, string Nombre, decimal Proporcion);

/// <summary>Grupo nutricional o ítem incluido en un menú.</summary>
public sealed record ElementoDelMenu(Guid Id, string Nombre);

/// <summary>Detalle de un menú para su consulta y dictamen.</summary>
public sealed record MenuDetalle(
    Guid Id,
    Guid DietaId,
    DateTime FechaCreacion,
    NutricionistaDelMenu Creador,
    bool EsAutomatico,
    int CantidadTotalAlimentos,
    string Estado,
    DictamenDeMenu? Dictamen,
    Guid? ReemplazaA,
    IReadOnlyList<ProporcionDelMenu> Distribucion,
    IReadOnlyList<ElementoDelMenu> GruposNutricionales,
    IReadOnlyList<ElementoDelMenu> Items);

/// <summary>Entrada del dictamen que envía el jefe de nutrición.</summary>
public sealed class DictamenFormulario
{
    /// <summary>Observaciones clínicas del revisor.</summary>
    public string? Observaciones { get; set; }
}
