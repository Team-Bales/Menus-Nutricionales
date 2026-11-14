using Bunit;
using Bunit.TestDoubles;
using Menus.Client.Pages;
using Menus.Client.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Client.Tests.Pages;

/// <summary>Pruebas de <see cref="DetallePlato"/>: composición y gestión de ingredientes (issue #65).</summary>
public sealed class DetallePlatoTests : BunitContext
{
    [Fact]
    public void Muestra_los_ingredientes_del_plato()
    {
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());

        var cut = Render<DetallePlato>(ps => ps.Add(p => p.Id, CatalogoApiFalsa.PlatoId));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Avena con leche", cut.Markup);
            Assert.Contains("Avena", cut.Markup);
            Assert.Contains("100", cut.Markup);
        });
    }

    [Fact]
    public void Agregar_ingrediente_llama_a_la_api_y_recarga_el_plato()
    {
        var alimentoNuevoId = Guid.NewGuid();
        var api = new CatalogoApiFalsa
        {
            Alimentos = [new(alimentoNuevoId, "Miel", CatalogoApiFalsa.Grupo, CatalogoApiFalsa.Tipo, CatalogoApiFalsa.Nivel, Guid.NewGuid())]
        };
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<DetallePlato>(ps => ps.Add(p => p.Id, CatalogoApiFalsa.PlatoId));
        cut.WaitForAssertion(() => Assert.Contains("Avena con leche", cut.Markup));

        cut.Find("#alimento").Change(alimentoNuevoId.ToString());
        cut.Find("#cantidad").Change("5");
        cut.Find("button.btn-primary").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(api.UltimoIngredienteAgregado);
            Assert.Equal(CatalogoApiFalsa.PlatoId, api.UltimoIngredienteAgregado!.Value.PlatoId);
            Assert.Equal(alimentoNuevoId, api.UltimoIngredienteAgregado.Value.AlimentoId);
            Assert.Equal(5m, api.UltimoIngredienteAgregado.Value.Cantidad);
        });
    }

    [Fact]
    public void Agregar_sin_elegir_alimento_muestra_un_error_y_no_llama_a_la_api()
    {
        var api = new CatalogoApiFalsa();
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<DetallePlato>(ps => ps.Add(p => p.Id, CatalogoApiFalsa.PlatoId));
        cut.WaitForAssertion(() => Assert.Contains("Avena con leche", cut.Markup));

        cut.Find("button.btn-primary").Click();

        cut.WaitForAssertion(() => Assert.Contains("Selecciona un alimento y una cantidad mayor que cero.", cut.Markup));
        Assert.Null(api.UltimoIngredienteAgregado);
    }

    [Fact]
    public void Muestra_el_error_de_la_api_si_falla_al_agregar_un_ingrediente()
    {
        var api = new CatalogoApiFalsa
        {
            Alimentos = [new(Guid.NewGuid(), "Miel", CatalogoApiFalsa.Grupo, CatalogoApiFalsa.Tipo, CatalogoApiFalsa.Nivel, Guid.NewGuid())],
            ErrorAlAgregarIngrediente = ErrorDePrueba.Conflicto("No se pudo agregar el ingrediente.")
        };
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<DetallePlato>(ps => ps.Add(p => p.Id, CatalogoApiFalsa.PlatoId));
        cut.WaitForAssertion(() => Assert.Contains("Avena con leche", cut.Markup));

        cut.Find("#alimento").Change(api.Alimentos[0].Id.ToString());
        cut.Find("#cantidad").Change("1");
        cut.Find("button.btn-primary").Click();

        cut.WaitForAssertion(() => Assert.Contains("No se pudo agregar el ingrediente.", cut.Markup));
    }

    [Fact]
    public void Quitar_ingrediente_llama_a_la_api_con_el_plato_y_el_alimento()
    {
        var api = new CatalogoApiFalsa();
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<DetallePlato>(ps => ps.Add(p => p.Id, CatalogoApiFalsa.PlatoId));
        cut.WaitForAssertion(() => Assert.Contains("Avena con leche", cut.Markup));

        cut.Find("button.btn-outline-danger").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(api.UltimoIngredienteQuitado);
            Assert.Equal(CatalogoApiFalsa.PlatoId, api.UltimoIngredienteQuitado!.Value.PlatoId);
            Assert.Equal(CatalogoApiFalsa.AlimentoId, api.UltimoIngredienteQuitado.Value.AlimentoId);
        });
    }

    [Fact]
    public void Muestra_el_error_de_la_api_si_falla_al_quitar_un_ingrediente()
    {
        var api = new CatalogoApiFalsa { ErrorAlQuitarIngrediente = ErrorDePrueba.Conflicto("No se pudo quitar.") };
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<DetallePlato>(ps => ps.Add(p => p.Id, CatalogoApiFalsa.PlatoId));
        cut.WaitForAssertion(() => Assert.Contains("Avena con leche", cut.Markup));

        cut.Find("button.btn-outline-danger").Click();

        cut.WaitForAssertion(() => Assert.Contains("No se pudo quitar.", cut.Markup));
    }

    [Fact]
    public void Muestra_el_error_de_la_api_si_falla_al_cargar_el_plato()
    {
        var api = new CatalogoApiFalsa { ErrorAlCargarPlato = ErrorDePrueba.Conflicto("No se pudo cargar el plato.") };
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<DetallePlato>(ps => ps.Add(p => p.Id, CatalogoApiFalsa.PlatoId));

        cut.WaitForAssertion(() => Assert.Contains("No se pudo cargar el plato.", cut.Markup));
    }

    [Fact]
    public void Editar_y_volver_navegan_a_las_rutas_correctas()
    {
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());
        var nav = Services.GetRequiredService<BunitNavigationManager>();

        var cut = Render<DetallePlato>(ps => ps.Add(p => p.Id, CatalogoApiFalsa.PlatoId));
        cut.WaitForAssertion(() => Assert.Contains("Avena con leche", cut.Markup));

        cut.Find("button.btn-outline-primary").Click();
        Assert.EndsWith($"/catalogo/platos/{CatalogoApiFalsa.PlatoId}/editar", nav.Uri);

        cut.Find("button.btn-outline-secondary").Click();
        Assert.EndsWith("/catalogo/platos", nav.Uri);
    }
}
