using Bunit;
using Bunit.TestDoubles;
using Menus.Client.Catalogo;
using Menus.Client.Pages;
using Menus.Client.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Client.Tests.Pages;

/// <summary>Pruebas de <see cref="FormularioAlimento"/> (alta y edición, issue #65).</summary>
public sealed class FormularioAlimentoTests : BunitContext
{
    [Fact]
    public void Nuevo_crea_el_alimento_con_la_clasificacion_elegida_y_vuelve_al_listado()
    {
        var api = new CatalogoApiFalsa();
        Services.AddSingleton<ICatalogoApi>(api);
        var nav = Services.GetRequiredService<BunitNavigationManager>();

        var cut = Render<FormularioAlimento>(ps => ps.Add(p => p.Id, null));
        cut.WaitForElement("#nombre");

        cut.Find("#nombre").Change("Quinoa");
        cut.Find("#grupo").Change(CatalogoApiFalsa.Grupo.ToString());
        cut.Find("#tipo").Change(CatalogoApiFalsa.Tipo.ToString());
        cut.Find("#nivel").Change(CatalogoApiFalsa.Nivel.ToString());
        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(api.UltimaClasificacionCreada);
            Assert.Equal("Quinoa", api.UltimaClasificacionCreada!.Nombre);
            Assert.Equal(CatalogoApiFalsa.Grupo, api.UltimaClasificacionCreada.GrupoNutricionalId);
            Assert.EndsWith("/catalogo/alimentos", nav.Uri);
        });
    }

    [Fact]
    public void Nuevo_no_envia_nada_si_falta_elegir_un_grupo()
    {
        var api = new CatalogoApiFalsa();
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<FormularioAlimento>(ps => ps.Add(p => p.Id, null));
        cut.WaitForElement("#nombre");

        cut.Find("#nombre").Change("Quinoa");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Contains("Selecciona un grupo nutricional.", cut.Markup));
        Assert.Null(api.UltimaClasificacionCreada);
    }

    [Fact]
    public void Editar_precarga_la_clasificacion_del_alimento_y_no_permite_cambiar_el_nombre()
    {
        var api = new CatalogoApiFalsa();
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<FormularioAlimento>(ps => ps.Add(p => p.Id, CatalogoApiFalsa.AlimentoId));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Editar alimento", cut.Markup);
            var nombre = cut.Find("#nombre");
            Assert.Equal("Avena", nombre.GetAttribute("value"));
            Assert.NotNull(nombre.GetAttribute("disabled"));
        });
    }

    [Fact]
    public void Editar_actualiza_la_clasificacion_y_vuelve_al_listado()
    {
        var api = new CatalogoApiFalsa();
        Services.AddSingleton<ICatalogoApi>(api);
        var nav = Services.GetRequiredService<BunitNavigationManager>();

        var cut = Render<FormularioAlimento>(ps => ps.Add(p => p.Id, CatalogoApiFalsa.AlimentoId));
        cut.WaitForElement("#nombre");

        cut.Find("#nivel").Change(CatalogoApiFalsa.Nivel.ToString());
        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(api.UltimaActualizacion);
            Assert.Equal(CatalogoApiFalsa.AlimentoId, api.UltimaActualizacion!.Value.Id);
            Assert.EndsWith("/catalogo/alimentos", nav.Uri);
        });
    }

    [Fact]
    public void Muestra_el_error_de_la_api_si_falla_al_guardar()
    {
        var api = new CatalogoApiFalsa { ErrorAlGuardar = ErrorDePrueba.Conflicto("El nombre ya existe.") };
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<FormularioAlimento>(ps => ps.Add(p => p.Id, null));
        cut.WaitForElement("#nombre");

        cut.Find("#nombre").Change("Quinoa");
        cut.Find("#grupo").Change(CatalogoApiFalsa.Grupo.ToString());
        cut.Find("#tipo").Change(CatalogoApiFalsa.Tipo.ToString());
        cut.Find("#nivel").Change(CatalogoApiFalsa.Nivel.ToString());
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Contains("El nombre ya existe.", cut.Markup));
    }

    [Fact]
    public void Muestra_el_error_de_la_api_si_falla_al_cargar_el_alimento()
    {
        var api = new CatalogoApiFalsa { ErrorAlCargarAlimento = ErrorDePrueba.Conflicto("No se pudo cargar.") };
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<FormularioAlimento>(ps => ps.Add(p => p.Id, CatalogoApiFalsa.AlimentoId));

        cut.WaitForAssertion(() => Assert.Contains("No se pudo cargar.", cut.Markup));
    }

    [Fact]
    public void Cancelar_vuelve_al_listado_sin_guardar()
    {
        var api = new CatalogoApiFalsa();
        Services.AddSingleton<ICatalogoApi>(api);
        var nav = Services.GetRequiredService<BunitNavigationManager>();

        var cut = Render<FormularioAlimento>(ps => ps.Add(p => p.Id, null));
        cut.WaitForElement("#nombre");

        cut.Find("button.btn-outline-secondary").Click();

        Assert.EndsWith("/catalogo/alimentos", nav.Uri);
        Assert.Null(api.UltimaClasificacionCreada);
    }
}
