namespace Menus.Application.Comun;

/// <summary>
/// Resultado de un comando que no devuelve nada (por ejemplo, actualizar o eliminar). La API responde 204.
/// </summary>
/// <remarks>
/// Existe porque <see cref="ICommand{TResultado}"/> siempre tiene un resultado: así el despachador tiene una sola
/// firma para todos los casos de uso.
/// </remarks>
public sealed record Unidad
{
    /// <summary>El único valor.</summary>
    public static readonly Unidad Valor = new();

    private Unidad()
    {
    }
}
