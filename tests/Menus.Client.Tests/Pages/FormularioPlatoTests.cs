using Bunit;
using Bunit.TestDoubles;
using Menus.Client.Pages;
using Menus.Client.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Client.Tests.Pages;

/// <summary>Pruebas de <see cref="FormularioPlato"/> (alta y edición, issue #65).</summary>
public sealed class FormularioPlatoTests : BunitContext
{
    [Fact]
    public void Nuevo_crea_el_plato_con_la_clasificacion_y_el_ingrediente_elegidos()
    {
        var api = new CatalogoApiFalsa();
        Services.AddSingleton<ICatalogoApi>(api);
        var nav = Services.GetRequiredService<BunitNavigationManager>();

        var cut = Render<FormularioPlato>(ps => ps.Add(p => p.Id, null));
        cut.WaitForElement("#nombre");

        cut.Find("#nombre").Change("Avena con leche");
        cut.Find("#grupo").Change(CatalogoApiFalsa.Grupo.ToString());
        cut.Find("#tipo").Change(CatalogoApiFalsa.Tipo.ToString());
        cut.Find("#nivel").Change(CatalogoApiFalsa.Nivel.ToString());
        cut.Find("#alimento-0").Change(CatalogoApiFalsa.AlimentoId.ToString());
        cut.Find("#cantidad-0").Change("3");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(api.UltimaClasificacionCreada);
            Assert.Equal("Avena con leche", api.UltimaClasificacionCreada!.Nombre);
            Assert.NotNull(api.UltimosIngredientesCreados);
            var ingrediente = Assert.Single(api.UltimosIngredientesCreados!);
            Assert.Equal(CatalogoApiFalsa.AlimentoId, ingrediente.AlimentoId);
            Assert.Equal(3m, ingrediente.Cantidad);
            Assert.EndsWith("/catalogo/platos", nav.Uri);
        });
    }

    [Fact]
    public void Agregar_y_quitar_fila_de_ingrediente_cambia_la_cantidad_de_filas()
    {
        var api = new CatalogoApiFalsa();
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<FormularioPlato>(ps => ps.Add(p => p.Id, null));
        cut.WaitForElement("#nombre");

        Assert.Single(cut.FindAll(".row.g-2.align-items-end"));

        cut.Find("button.btn-outline-primary.btn-sm").Click();
        Assert.Equal(2, cut.FindAll(".row.g-2.align-items-end").Count);

        cut.FindAll("button.btn-outline-danger")[0].Click();
        Assert.Single(cut.FindAll(".row.g-2.align-items-end"));
    }

    [Fact]
    public void No_envia_nada_si_los_ingredientes_tienen_un_alimento_repetido()
    {
        var api = new CatalogoApiFalsa();
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<FormularioPlato>(ps => ps.Add(p => p.Id, null));
        cut.WaitForElement("#nombre");

        cut.Find("#nombre").Change("Avena con leche");
        cut.Find("#grupo").Change(CatalogoApiFalsa.Grupo.ToString());
        cut.Find("#tipo").Change(CatalogoApiFalsa.Tipo.ToString());
        cut.Find("#nivel").Change(CatalogoApiFalsa.Nivel.ToString());
        cut.Find("#alimento-0").Change(CatalogoApiFalsa.AlimentoId.ToString());
        cut.Find("#cantidad-0").Change("1");
        cut.Find("button.btn-outline-primary.btn-sm").Click();
        cut.Find("#alimento-1").Change(CatalogoApiFalsa.AlimentoId.ToString());
        cut.Find("#cantidad-1").Change("1");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Contains("sin repetir", cut.Markup));
        Assert.Null(api.UltimaClasificacionCreada);
    }

    [Fact]
    public void Editar_precarga_la_clasificacion_y_no_muestra_ingredientes_iniciales()
    {
        var api = new CatalogoApiFalsa();
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<FormularioPlato>(ps => ps.Add(p => p.Id, CatalogoApiFalsa.PlatoId));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Editar plato", cut.Markup);
            Assert.Equal("Avena con leche", cut.Find("#nombre").GetAttribute("value"));
            Assert.Empty(cut.FindAll(".row.g-2.align-items-end"));
        });
    }

    [Fact]
    public void Editar_actualiza_la_clasificacion_sin_pedir_ingredientes()
    {
        var api = new CatalogoApiFalsa();
        Services.AddSingleton<ICatalogoApi>(api);
        var nav = Services.GetRequiredService<BunitNavigationManager>();

        var cut = Render<FormularioPlato>(ps => ps.Add(p => p.Id, CatalogoApiFalsa.PlatoId));
        cut.WaitForElement("#nombre");

        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(api.UltimaActualizacion);
            Assert.Equal(CatalogoApiFalsa.PlatoId, api.UltimaActualizacion!.Value.Id);
            Assert.EndsWith("/catalogo/platos", nav.Uri);
        });
    }

    [Fact]
    public void Muestra_el_error_de_la_api_si_falla_al_cargar_los_alimentos_disponibles()
    {
        var api = new CatalogoApiFalsa { ErrorAlCargarAlimento = ErrorDePrueba.Conflicto("No se pudieron cargar los alimentos.") };
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<FormularioPlato>(ps => ps.Add(p => p.Id, null));

        cut.WaitForAssertion(() => Assert.Contains("No se pudieron cargar los alimentos.", cut.Markup));
    }

    [Fact]
    public void Muestra_el_error_de_la_api_si_falla_al_guardar()
    {
        var api = new CatalogoApiFalsa { ErrorAlGuardar = ErrorDePrueba.Conflicto("No se pudo crear el plato.") };
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<FormularioPlato>(ps => ps.Add(p => p.Id, null));
        cut.WaitForElement("#nombre");

        cut.Find("#nombre").Change("Avena con leche");
        cut.Find("#grupo").Change(CatalogoApiFalsa.Grupo.ToString());
        cut.Find("#tipo").Change(CatalogoApiFalsa.Tipo.ToString());
        cut.Find("#nivel").Change(CatalogoApiFalsa.Nivel.ToString());
        cut.Find("#alimento-0").Change(CatalogoApiFalsa.AlimentoId.ToString());
        cut.Find("#cantidad-0").Change("1");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Contains("No se pudo crear el plato.", cut.Markup));
    }
}
