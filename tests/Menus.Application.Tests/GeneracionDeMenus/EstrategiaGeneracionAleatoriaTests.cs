using Menus.Application.GeneracionDeMenus;
using Menus.Domain.Entidades;
using Menus.Domain.Excepciones;
using Menus.Domain.Nomenclators;
using Menus.Domain.ValueObjects;

namespace Menus.Application.Tests.GeneracionDeMenus;

/// <summary>Pruebas de <see cref="EstrategiaGeneracionAleatoria"/> (#98).</summary>
public sealed class EstrategiaGeneracionAleatoriaTests
{
    private static readonly Guid GrupoCereales = Guid.NewGuid();
    private static readonly Guid GrupoProteinas = Guid.NewGuid();
    private static readonly Guid Descriptor = Guid.NewGuid();

    private readonly EstrategiaGeneracionAleatoria _estrategia = new();

    private static Alimento NuevoAlimento(Guid grupoId, Guid tipoId)
        => new(Guid.NewGuid(), $"Item {Guid.NewGuid():N}", grupoId, tipoId, NivelCalorico.Medio.Id, Descriptor);

    [Fact]
    public void Selecciona_la_cantidad_total_pedida_respetando_la_proporcion_por_tipo()
    {
        var entrantes = Enumerable.Range(0, 10).Select(_ => NuevoAlimento(GrupoCereales, TipoPreparacion.Entrante.Id)).ToList();
        var platosFuertes = Enumerable.Range(0, 10).Select(_ => NuevoAlimento(GrupoCereales, TipoPreparacion.PlatoFuerte.Id)).ToList();
        IReadOnlyList<ItemAlimentario> disponibles = [.. entrantes, .. platosFuertes];

        var distribucion = DistribucionTiposPreparacion.CrearDesdeTuplas(
            [(TipoPreparacion.Entrante.Id, 0.3m), (TipoPreparacion.PlatoFuerte.Id, 0.7m)]);
        var cobertura = CoberturaGruposNutricionales.Crear([GrupoCereales]);
        var cantidad = CantidadTotalAlimentos.Crear(10);

        var seleccionados = _estrategia.SeleccionarItems(disponibles, distribucion, cobertura, cantidad);

        Assert.Equal(10, seleccionados.Count);
        Assert.Equal(10, seleccionados.Distinct().Count());
        Assert.True(seleccionados.All(id => disponibles.Any(i => i.Id == id)));

        var deEntrante = seleccionados.Count(id => entrantes.Any(i => i.Id == id));
        var dePlatoFuerte = seleccionados.Count(id => platosFuertes.Any(i => i.Id == id));
        Assert.Equal(3, deEntrante);
        Assert.Equal(7, dePlatoFuerte);
    }

    [Fact]
    public void Prioriza_items_de_los_grupos_que_cubre_el_menu()
    {
        var delGrupoCubierto = NuevoAlimento(GrupoCereales, TipoPreparacion.Entrante.Id);
        var deOtroGrupo = Enumerable.Range(0, 5).Select(_ => NuevoAlimento(GrupoProteinas, TipoPreparacion.Entrante.Id)).ToList();
        IReadOnlyList<ItemAlimentario> disponibles = [delGrupoCubierto, .. deOtroGrupo];

        var distribucion = DistribucionTiposPreparacion.CrearDesdeTuplas([(TipoPreparacion.Entrante.Id, 1m)]);
        var cobertura = CoberturaGruposNutricionales.Crear([GrupoCereales]);
        var cantidad = CantidadTotalAlimentos.Crear(1);

        var seleccionados = _estrategia.SeleccionarItems(disponibles, distribucion, cobertura, cantidad);

        Assert.Equal([delGrupoCubierto.Id], seleccionados);
    }

    [Fact]
    public void No_repite_items_entre_los_seleccionados()
    {
        IReadOnlyList<ItemAlimentario> disponibles =
            [NuevoAlimento(GrupoCereales, TipoPreparacion.Entrante.Id), NuevoAlimento(GrupoCereales, TipoPreparacion.Entrante.Id)];

        var distribucion = DistribucionTiposPreparacion.CrearDesdeTuplas([(TipoPreparacion.Entrante.Id, 1m)]);
        var cobertura = CoberturaGruposNutricionales.Crear([GrupoCereales]);
        var cantidad = CantidadTotalAlimentos.Crear(2);

        var seleccionados = _estrategia.SeleccionarItems(disponibles, distribucion, cobertura, cantidad);

        Assert.Equal(2, seleccionados.Distinct().Count());
    }

    [Fact]
    public void Lanza_ReglaDeNegocio_si_no_hay_suficientes_items_compatibles_de_un_tipo()
    {
        IReadOnlyList<ItemAlimentario> disponibles = [NuevoAlimento(GrupoCereales, TipoPreparacion.Entrante.Id)];

        var distribucion = DistribucionTiposPreparacion.CrearDesdeTuplas([(TipoPreparacion.Entrante.Id, 1m)]);
        var cobertura = CoberturaGruposNutricionales.Crear([GrupoCereales]);
        var cantidad = CantidadTotalAlimentos.Crear(5);

        Assert.Throws<ReglaDeNegocioException>(
            () => _estrategia.SeleccionarItems(disponibles, distribucion, cobertura, cantidad));
    }

    [Fact]
    public void La_cantidad_por_tipo_se_redondea_pero_el_total_siempre_coincide()
    {
        var entrantes = Enumerable.Range(0, 10).Select(_ => NuevoAlimento(GrupoCereales, TipoPreparacion.Entrante.Id)).ToList();
        var platosFuertes = Enumerable.Range(0, 10).Select(_ => NuevoAlimento(GrupoCereales, TipoPreparacion.PlatoFuerte.Id)).ToList();
        var postres = Enumerable.Range(0, 10).Select(_ => NuevoAlimento(GrupoCereales, TipoPreparacion.Postre.Id)).ToList();
        IReadOnlyList<ItemAlimentario> disponibles = [.. entrantes, .. platosFuertes, .. postres];

        // Un tercio para cada uno: redondear ingenuamente perdería o sobraría un ítem.
        var tercio = 1m / 3m;
        var distribucion = DistribucionTiposPreparacion.CrearDesdeTuplas(
            [(TipoPreparacion.Entrante.Id, tercio), (TipoPreparacion.PlatoFuerte.Id, tercio), (TipoPreparacion.Postre.Id, 1m - (2 * tercio))]);
        var cobertura = CoberturaGruposNutricionales.Crear([GrupoCereales]);
        var cantidad = CantidadTotalAlimentos.Crear(10);

        var seleccionados = _estrategia.SeleccionarItems(disponibles, distribucion, cobertura, cantidad);

        Assert.Equal(10, seleccionados.Count);
    }
}
