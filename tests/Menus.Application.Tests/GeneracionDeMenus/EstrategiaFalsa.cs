using Menus.Application.GeneracionDeMenus;
using Menus.Domain.Entidades;
using Menus.Domain.ValueObjects;

namespace Menus.Application.Tests.GeneracionDeMenus;

/// <summary>
/// <see cref="IEstrategiaGeneracionMenu"/> falsa para probar <c>GenerarMenuAutomaticamenteHandler</c> (#98) sin
/// depender del algoritmo real: devuelve los ids configurados y registra con qué la llamaron.
/// </summary>
internal sealed class EstrategiaFalsa : IEstrategiaGeneracionMenu
{
    public IReadOnlyList<Guid> Resultado { get; set; } = [];

    public IReadOnlyList<ItemAlimentario>? ItemsRecibidos { get; private set; }

    public IReadOnlyList<Guid> SeleccionarItems(
        IReadOnlyList<ItemAlimentario> itemsCompatibles,
        DistribucionTiposPreparacion distribucion,
        CoberturaGruposNutricionales cobertura,
        CantidadTotalAlimentos cantidadTotal)
    {
        ItemsRecibidos = itemsCompatibles;
        return Resultado;
    }
}
