using Menus.Application.Catalogo.Nomencladores;
using Menus.Application.Catalogo.Nomencladores.NivelesCaloricos;
using Menus.Application.Catalogo.Nomencladores.TiposPreparacion;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Tests.Catalogo.Nomencladores;

/// <summary>Pruebas de los listados de los catálogos cerrados: devuelven lo que define el dominio, en su orden.</summary>
public sealed class CatalogosCerradosTests
{
    [Fact]
    public async Task ListarTiposPreparacion_devuelve_los_cuatro_en_el_orden_del_catalogo()
    {
        var lista = await new ListarTiposPreparacionHandler()
            .HandleAsync(new ListarTiposPreparacionQuery(), CancellationToken.None);

        Assert.Equal(
            [
                new NomencladorDto(TipoPreparacion.Entrante.Id, "Entrante"),
                new NomencladorDto(TipoPreparacion.PlatoFuerte.Id, "Plato fuerte"),
                new NomencladorDto(TipoPreparacion.Postre.Id, "Postre"),
                new NomencladorDto(TipoPreparacion.Bebida.Id, "Bebida"),
            ],
            lista);
    }

    [Fact]
    public async Task ListarNivelesCaloricos_devuelve_los_tres_en_el_orden_del_catalogo()
    {
        var lista = await new ListarNivelesCaloricosHandler()
            .HandleAsync(new ListarNivelesCaloricosQuery(), CancellationToken.None);

        Assert.Equal(
            [NivelCalorico.Bajo.Id, NivelCalorico.Medio.Id, NivelCalorico.Alto.Id],
            lista.Select(n => n.Id));
        Assert.Equal(
            [NivelCalorico.Bajo.Nombre, NivelCalorico.Medio.Nombre, NivelCalorico.Alto.Nombre],
            lista.Select(n => n.Nombre));
    }
}
