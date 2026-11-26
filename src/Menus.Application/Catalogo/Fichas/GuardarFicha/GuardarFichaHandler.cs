using Menus.Application.Comun;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;
using Menus.Domain.Excepciones;

namespace Menus.Application.Catalogo.Fichas.GuardarFicha;

/// <summary>
/// Crea o reemplaza la ficha nutricional de un alimento o un plato (#283, RF-04).
/// </summary>
/// <remarks>
/// <list type="number">
/// <item>El ítem tiene que existir y ser de la clase de la ruta (RM-07): si no, 404.</item>
/// <item>Solo escribe la ficha quien describió el ítem (RS-02, ampliada como en el ADR-012): si no, 409.</item>
/// <item>La ficha de un alimento no lleva desglose (RM-03); la de un plato puede no llevarlo, pero si lo lleva tiene
/// que nombrar exactamente los alimentos de su composición (RM-06, ADR-009, C1): si no, 422.</item>
/// <item><c>origen</c>, <c>tipo</c>, el <c>nombre</c> de cada ingrediente y <c>actualizadoEn</c> los pone el
/// servidor.</item>
/// </list>
/// No escribe en PostgreSQL, así que no hay nada que confirmar antes de escribir en MongoDB (regla de orden del
/// documento 2, §3).
/// </remarks>
internal sealed class GuardarFichaHandler(
    IAlimentoRepository alimentos,
    IPlatoRepository platos,
    IActorActual actor,
    IAlmacenDeFichas almacen) : IHandler<GuardarFichaCommand, FichaNutricionalDto>
{
    public async Task<FichaNutricionalDto> HandleAsync(GuardarFichaCommand solicitud, CancellationToken ct)
    {
        var nutricionistaId = actor.Id;
        IReadOnlyList<IngredienteDeFichaDto>? desglose = null;

        if (solicitud.Clase == ClaseDeItem.Plato)
        {
            var plato = await platos.GetConIngredientesAsync(solicitud.ItemId, ct)
                ?? throw new NoEncontradoException("plato", solicitud.ItemId);
            ReglasDeLaFicha.ExigirDescriptor(plato, nutricionistaId);

            if (solicitud.Ingredientes is not null)
            {
                desglose = await ArmarDesgloseAsync(plato, solicitud.Ingredientes, ct);
            }
        }
        else
        {
            var alimento = await alimentos.GetByIdAsync(solicitud.ItemId, ct)
                ?? throw new NoEncontradoException("alimento", solicitud.ItemId);
            ReglasDeLaFicha.ExigirDescriptor(alimento, nutricionistaId);

            if (solicitud.Ingredientes is not null)
            {
                throw new DatoInvalidoException(
                    "ingredientes",
                    "La ficha de un alimento no lleva desglose de ingredientes: solo la de un plato (RM-03).",
                    "RM-03");
            }
        }

        var porcion = solicitud.PorcionReferencia is { } p ? new PorcionDeReferenciaDto(p.Cantidad, p.Unidad.Trim()) : null;
        var ficha = new FichaNutricionalDto(
            solicitud.ItemId,
            ReglasDeLaFicha.Tabla(solicitud.Clase),
            ReglasDeLaFicha.Tipo(solicitud.Clase),
            porcion,
            solicitud.Macronutrientes,
            solicitud.Micronutrientes,
            solicitud.Alergenos?.Select(a => a.Trim()).ToList(),
            solicitud.ModoPreparacion,
            desglose,
            AhoraEnMilisegundos());

        await almacen.GuardarAsync(ficha, ct);
        return ficha;
    }

    /// <summary>
    /// Comprueba RM-06 y arma el desglose con el nombre de cada alimento, en el orden en que llegó.
    /// </summary>
    private async Task<IReadOnlyList<IngredienteDeFichaDto>> ArmarDesgloseAsync(
        Plato plato,
        IReadOnlyList<IngredienteDeFichaEntrada> ingredientes,
        CancellationToken ct)
    {
        var composicion = plato.Ingredientes.Select(i => i.AlimentoId).ToHashSet();
        var enviados = ingredientes.Select(i => i.AlimentoId).ToHashSet();

        if (!composicion.SetEquals(enviados))
        {
            var faltan = composicion.Except(enviados).ToList();
            var sobran = enviados.Except(composicion).ToList();
            var detalle = string.Join(
                "; ",
                new[]
                {
                    faltan.Count > 0 ? $"faltan {string.Join(", ", faltan)}" : null,
                    sobran.Count > 0 ? $"no están en la composición {string.Join(", ", sobran)}" : null,
                }.Where(d => d is not null));
            throw new DatoInvalidoException(
                "ingredientes",
                $"El desglose tiene que nombrar exactamente los alimentos de la composición del plato (RM-06): {detalle}.",
                "RM-06");
        }

        // Todo ingrediente de la composición es un alimento existente (RR-02, con FK): el nombre siempre está.
        var nombres = (await alimentos.GetByIdsAsync(composicion, ct)).ToDictionary(a => a.Id, a => a.Nombre);
        return [.. ingredientes.Select(i => new IngredienteDeFichaDto(i.AlimentoId, nombres[i.AlimentoId], i.Aporte))];
    }

    /// <summary>
    /// La hora actual en UTC, truncada a milisegundos: es la precisión de las fechas de MongoDB, así que la respuesta
    /// coincide con lo que devuelve una consulta posterior.
    /// </summary>
    private static DateTime AhoraEnMilisegundos()
    {
        var ahora = DateTime.UtcNow;
        return new DateTime(ahora.Ticks - (ahora.Ticks % TimeSpan.TicksPerMillisecond), DateTimeKind.Utc);
    }
}
