using System.Net;
using Bunit;
using Menus.Client.Catalogo;
using Menus.Client.Pages;
using Menus.Client.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Client.Tests;

/// <summary>Pruebas de las pantallas del catálogo del issue #65.</summary>
public sealed class CatalogoPagesTests : BunitContext
{
    [Fact]
    public void Alimentos_muestra_los_selectores_desde_el_cliente_y_la_pagina_de_resultados()
    {
        Services.AddSingleton<ICatalogoApi>(new CatalogoFalso());

        var cut = Render<CatalogoAlimentos>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Cereales", cut.Markup);
            Assert.Contains("Plato fuerte", cut.Markup);
            Assert.Contains("Medio", cut.Markup);
            Assert.Contains("Avena", cut.Markup);
        });
    }

    [Fact]
    public void Alimentos_muestra_el_error_legible_de_la_api_al_eliminar()
    {
        Services.AddSingleton<ICatalogoApi>(new CatalogoFalso { ErrorAlEliminarAlimento = true });
        var cut = Render<CatalogoAlimentos>();

        cut.WaitForAssertion(() => Assert.Contains("Avena", cut.Markup));
        cut.FindAll("button").Single(button => button.TextContent == "Eliminar").Click();

        cut.WaitForAssertion(() => Assert.Contains("El alimento está en uso.", cut.Markup));
    }

    [Fact]
    public void Platos_muestra_la_cantidad_de_ingredientes_del_listado()
    {
        Services.AddSingleton<ICatalogoApi>(new CatalogoFalso());

        var cut = Render<CatalogoPlatos>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Avena con leche", cut.Markup);
            Assert.Contains(">2<", cut.Markup);
        });
    }

    private sealed class CatalogoFalso : ICatalogoApi
    {
        private static readonly Guid Grupo = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid Tipo = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid Nivel = Guid.Parse("33333333-3333-3333-3333-333333333333");
        private static readonly Guid AlimentoId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        private static readonly Guid PlatoId = Guid.Parse("55555555-5555-5555-5555-555555555555");

        public bool ErrorAlEliminarAlimento { get; init; }

        public Task<IReadOnlyList<Nomenclador>> ListarGruposAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Nomenclador>>([new(Grupo, "Cereales")]);
        public Task<IReadOnlyList<Nomenclador>> ListarRestriccionesAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Nomenclador>>([]);
        public Task<IReadOnlyList<Nomenclador>> ListarProgramasAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Nomenclador>>([]);
        public Task<IReadOnlyList<Nomenclador>> ListarEspecialidadesAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Nomenclador>>([]);
        public Task<IReadOnlyList<Nomenclador>> ListarTiposPreparacionAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Nomenclador>>([new(Tipo, "Plato fuerte")]);
        public Task<IReadOnlyList<Nomenclador>> ListarNivelesCaloricosAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Nomenclador>>([new(Nivel, "Medio")]);
        public Task<Guid> CrearNomencladorAsync(TipoDeNomenclador tipo, NomencladorFormulario formulario, CancellationToken ct = default) => Task.FromResult(Guid.NewGuid());
        public Task EliminarNomencladorAsync(TipoDeNomenclador tipo, Guid id, CancellationToken ct = default) => Task.CompletedTask;
        public Task<Pagina<Dieta>> ListarDietasAsync(Guid? programa, int page, int size, CancellationToken ct = default) => Task.FromResult(new Pagina<Dieta>([], 0, page, size));
        public Task<DietaDetalle> ObtenerDietaAsync(Guid id, CancellationToken ct = default) => Task.FromResult(new DietaDetalle(id, "Dieta", Guid.Empty, [], []));
        public Task AgregarGrupoDietaAsync(Guid dietaId, GrupoCubiertoFormulario grupo, CancellationToken ct = default) => Task.CompletedTask;
        public Task QuitarGrupoDietaAsync(Guid dietaId, Guid grupoId, CancellationToken ct = default) => Task.CompletedTask;
        public Task AgregarRestriccionDietaAsync(Guid dietaId, Guid restriccionId, CancellationToken ct = default) => Task.CompletedTask;
        public Task QuitarRestriccionDietaAsync(Guid dietaId, Guid restriccionId, CancellationToken ct = default) => Task.CompletedTask;
        public Task<Pagina<Alimento>> ListarAlimentosAsync(Guid? grupo, Guid? tipo, Guid? nivel, int page, int size, CancellationToken ct = default) => Task.FromResult(new Pagina<Alimento>([new(AlimentoId, "Avena", Grupo, Tipo, Nivel, Guid.NewGuid())], 1, page, size));
        public Task<IReadOnlyList<Alimento>> ListarTodosLosAlimentosAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Alimento>>([new(AlimentoId, "Avena", Grupo, Tipo, Nivel, Guid.NewGuid())]);
        public Task<Alimento> ObtenerAlimentoAsync(Guid id, CancellationToken ct = default) => Task.FromResult(new Alimento(AlimentoId, "Avena", Grupo, Tipo, Nivel, Guid.NewGuid()));
        public Task<Guid> CrearAlimentoAsync(ClasificacionFormulario alimento, CancellationToken ct = default) => Task.FromResult(AlimentoId);
        public Task ActualizarAlimentoAsync(Guid id, ClasificacionFormulario alimento, CancellationToken ct = default) => Task.CompletedTask;
        public Task EliminarAlimentoAsync(Guid id, CancellationToken ct = default) => ErrorAlEliminarAlimento ? Task.FromException(new ApiException(HttpStatusCode.Conflict, "El alimento está en uso.")) : Task.CompletedTask;
        public Task<Pagina<Plato>> ListarPlatosAsync(Guid? grupo, Guid? tipo, Guid? nivel, int page, int size, CancellationToken ct = default) => Task.FromResult(new Pagina<Plato>([new(PlatoId, "Avena con leche", Grupo, Tipo, Nivel, Guid.NewGuid(), 2)], 1, page, size));
        public Task<PlatoDetalle> ObtenerPlatoAsync(Guid id, CancellationToken ct = default) => Task.FromResult(new PlatoDetalle(PlatoId, "Avena con leche", Grupo, Tipo, Nivel, Guid.NewGuid(), [new(AlimentoId, "Avena", 100m), new(Guid.NewGuid(), "Leche", 200m)]));
        public Task<Guid> CrearPlatoAsync(ClasificacionFormulario plato, IReadOnlyList<IngredienteFormulario> ingredientes, CancellationToken ct = default) => Task.FromResult(PlatoId);
        public Task ActualizarPlatoAsync(Guid id, ClasificacionFormulario plato, CancellationToken ct = default) => Task.CompletedTask;
        public Task EliminarPlatoAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
        public Task AgregarIngredienteAsync(Guid platoId, IngredienteFormulario ingrediente, CancellationToken ct = default) => Task.CompletedTask;
        public Task QuitarIngredienteAsync(Guid platoId, Guid alimentoId, CancellationToken ct = default) => Task.CompletedTask;
    }
}
