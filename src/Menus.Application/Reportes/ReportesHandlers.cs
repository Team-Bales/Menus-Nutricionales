using Menus.Application.Comun;
using Menus.Domain.Abstractions.Repositories;

namespace Menus.Application.Reportes;

// Manejadores (handlers) para las consultas de reportes.
// Cada uno valida la existencia del recurso correspondiente, invoca el puerto y aplica ordenamiento de presentación.

#region C1: ListarMenusAutomaticosHandler

/// <summary>
/// Maneja <see cref="ListarMenusAutomaticosQuery"/>: menús automáticos de una dieta.
/// Comprueba que la dieta existe (IDietaRepository).
/// </summary>
internal sealed class ListarMenusAutomaticosHandler(
    IConsultasInformacionales consultas,
    IDietaRepository dietas)
    : IHandler<ListarMenusAutomaticosQuery, IReadOnlyList<MenuAutomaticoDto>>
{
    public async Task<IReadOnlyList<MenuAutomaticoDto>> HandleAsync(ListarMenusAutomaticosQuery solicitud, CancellationToken ct)
    {
        // Verificar existencia de la dieta
        if (await dietas.GetByIdAsync(solicitud.DietaId, ct) is null)
        {
            throw new NoEncontradoException("dieta", solicitud.DietaId);
        }

        var datos = await consultas.ConsultarMenusAutomaticosAsync(solicitud.DietaId, ct);
        return datos.AplicarOrden(solicitud.Orden);
    }
}

#endregion

#region C2: ListarAlimentosMasUsadosHandler

/// <summary>
/// Maneja <see cref="ListarAlimentosMasUsadosQuery"/>: alimentos más usados en menús finales de una dieta.
/// Comprueba que la dieta existe (IDietaRepository).
/// </summary>
internal sealed class ListarAlimentosMasUsadosHandler(
    IConsultasInformacionales consultas,
    IDietaRepository dietas)
    : IHandler<ListarAlimentosMasUsadosQuery, IReadOnlyList<AlimentoMasUsadoDto>>
{
    public async Task<IReadOnlyList<AlimentoMasUsadoDto>> HandleAsync(ListarAlimentosMasUsadosQuery solicitud, CancellationToken ct)
    {
        // Verificar existencia de la dieta
        if (await dietas.GetByIdAsync(solicitud.DietaId, ct) is null)
        {
            throw new NoEncontradoException("dieta", solicitud.DietaId);
        }

        var datos = await consultas.ConsultarAlimentosMasUsadosAsync(solicitud.DietaId, solicitud.Top, ct);
        return datos.AplicarOrden(solicitud.Orden);
    }
}

#endregion

#region C3: ListarDictamenesHandler

/// <summary>
/// Maneja <see cref="ListarDictamenesQuery"/>: dictámenes de un revisor.
/// Comprueba que el nutricionista existe (INutricionistaRepository).
/// No exige que siga siendo jefe: el reporte es histórico.
/// </summary>
internal sealed class ListarDictamenesHandler(
    IConsultasInformacionales consultas,
    INutricionistaRepository nutricionistas)
    : IHandler<ListarDictamenesQuery, IReadOnlyList<DictamenDto>>
{
    public async Task<IReadOnlyList<DictamenDto>> HandleAsync(ListarDictamenesQuery solicitud, CancellationToken ct)
    {
        // Verificar existencia del revisor
        if (await nutricionistas.GetByIdAsync(solicitud.RevisorId, ct) is null)
        {
            throw new NoEncontradoException("nutricionista", solicitud.RevisorId);
        }

        var datos = await consultas.ConsultarDictamenesAsync(solicitud.RevisorId, ct);
        return datos.AplicarOrden(solicitud.Orden);
    }
}

#endregion

#region C4: ListarDesempenoMenuHandler

/// <summary>
/// Maneja <see cref="ListarDesempenoMenuQuery"/>: desempeño ante un menú.
/// Comprueba que el menú existe (IMenuRepository).
/// </summary>
internal sealed class ListarDesempenoMenuHandler(
    IConsultasInformacionales consultas,
    IMenuRepository menus)
    : IHandler<ListarDesempenoMenuQuery, IReadOnlyList<DesempenoItemDto>>
{
    public async Task<IReadOnlyList<DesempenoItemDto>> HandleAsync(ListarDesempenoMenuQuery solicitud, CancellationToken ct)
    {
        // Verificar existencia del menú
        if (await menus.GetByIdAsync(solicitud.MenuId, ct) is null)
        {
            throw new NoEncontradoException("menú", solicitud.MenuId);
        }

        var datos = await consultas.ConsultarDesempenoMenuAsync(solicitud.MenuId, ct);
        return datos.AplicarOrden(solicitud.Orden);
    }
}

#endregion

#region C5: CompararDietasHandler

/// <summary>
/// Maneja <see cref="CompararDietasQuery"/>: comparación de dietas en tres dimensiones.
/// Comprueba que cada dieta existe (IDietaRepository).
/// Cada sección se ordena independientemente según su propio parámetro.
/// </summary>
internal sealed class CompararDietasHandler(
    IConsultasInformacionales consultas,
    IDietaRepository dietas)
    : IHandler<CompararDietasQuery, ComparacionDietasDto>
{
    public async Task<ComparacionDietasDto> HandleAsync(CompararDietasQuery solicitud, CancellationToken ct)
    {
        // Verificar existencia de cada dieta
        foreach (var dietaId in solicitud.Dietas)
        {
            if (await dietas.GetByIdAsync(dietaId, ct) is null)
            {
                throw new NoEncontradoException("dieta", dietaId);
            }
        }

        // Ejecutar las tres consultas de forma secuencial (el lector es Scoped y usa un único DbContext;
        // EF Core no admite operaciones simultáneas sobre el mismo contexto)
        var porGrupo = await consultas.ConsultarComparacionPorGrupoAsync(solicitud.Dietas, solicitud.SoloFinales, ct);
        var porNivel = await consultas.ConsultarComparacionPorNivelAsync(solicitud.Dietas, solicitud.SoloFinales, ct);
        var criterios = await consultas.ConsultarCriteriosEquilibrioAsync(solicitud.Dietas, solicitud.SoloFinales, ct);

        // Aplicar ordenamiento independiente a cada sección
        return new ComparacionDietasDto(
            porGrupo.AplicarOrden(solicitud.OrdenGrupos),
            porNivel.AplicarOrden(solicitud.OrdenNiveles),
            criterios.AplicarOrden(solicitud.OrdenEquilibrio));
    }
}

#endregion

#region C6: ObtenerCorrelacionNutricionalHandler

/// <summary>
/// Maneja <see cref="ObtenerCorrelacionNutricionalQuery"/>: correlación nutricional en tres partes.
/// No comprueba un recurso concreto (C6 es transversal).
/// Cada sección se ordena independientemente según su propio parámetro.
/// <para>
/// Los parámetros <c>Desde</c> y <c>Hasta</c> sin offset se interpretan en la zona horaria del servidor;
/// se recomienda enviarlos en UTC o con offset explícito.
/// </para>
/// </summary>
internal sealed class ObtenerCorrelacionNutricionalHandler(
    IConsultasInformacionales consultas)
    : IHandler<ObtenerCorrelacionNutricionalQuery, CorrelacionNutricionalDto>
{
    public async Task<CorrelacionNutricionalDto> HandleAsync(ObtenerCorrelacionNutricionalQuery solicitud, CancellationToken ct)
    {
        // Ejecutar las tres consultas de forma secuencial (el lector es Scoped y usa un único DbContext;
        // EF Core no admite operaciones simultáneas sobre el mismo contexto)
        var correlaciones = await consultas.ConsultarCorrelacionPorDietaAsync(solicitud.Desde, solicitud.Hasta, ct);
        var itemsMasRechazados = await consultas.ConsultarItemsMasRechazadosAsync(10, solicitud.MinimoConsumos, ct); // límite fijo 10
        var revalorizaciones = await consultas.ConsultarRevalorizadosAsync(ct);

        // Aplicar ordenamiento independiente a cada sección
        return new CorrelacionNutricionalDto(
            correlaciones.AplicarOrden(solicitud.OrdenCorrelacion),
            itemsMasRechazados.AplicarOrden(solicitud.OrdenRechazos),
            revalorizaciones.AplicarOrden(solicitud.OrdenRevalorizaciones));
    }
}

#endregion
