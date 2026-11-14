using Menus.Infrastructure.Mongo;

namespace Menus.Infrastructure.Mongo;

/// <summary>
/// Opciones de configuración para la tarea de conciliación RM-10.
/// </summary>
/// <remarks>
/// Sección de configuración: <c>Conciliacion</c>. Los valores por defecto corresponden a la opción A1
/// recomendada en el ADR-009 (cada 24 h, primera ejecución a los 5 min).
/// </remarks>
public sealed class ConciliacionOptions
{
    /// <summary>Nombre de la sección en <c>appsettings.json</c>.</summary>
    public const string SectionName = "Conciliacion";

    /// <summary>
    /// Interruptor global: si es <see langword="false"/>, no se registra el <see cref="ConciliacionHostedService"/>
    /// y la conciliación solo puede invocarse a mano.
    /// </summary>
    public bool Habilitada { get; set; } = true;

    /// <summary>Intervalo entre ejecuciones periódicas (por defecto 24 h).</summary>
    public TimeSpan Intervalo { get; set; } = TimeSpan.FromHours(24);

    /// <summary>Retraso antes de la primera ejecución tras el arranque (por defecto 5 min).</summary>
    public TimeSpan RetrasoInicial { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Tamaño de lote para el recorrido por cursor de <c>_id</c> (por defecto 500).</summary>
    public int TamanoLote { get; set; } = 500;
}
