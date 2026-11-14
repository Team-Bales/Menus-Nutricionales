using Bunit;
using Menus.Client.Catalogo;
using Menus.Client.Pages;
using Menus.Client.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Client.Tests.Pages;

/// <summary>Pruebas del detalle y la edición de relaciones de una dieta del issue #66.</summary>
public sealed class DetalleDietaTests : BunitContext
{
    [Fact]
    public void Muestra_los_grupos_y_las_restricciones_de_la_dieta()
    {
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());

        var cut = Render<DetalleDieta>(ps => ps.Add(p => p.Id, CatalogoApiFalsa.DietaId));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Dieta basal", cut.Markup);
            Assert.Contains("Cereales", cut.Markup);
            Assert.Contains("Sin gluten", cut.Markup);
        });
    }

    [Fact]
    public void Agregar_grupo_llama_a_la_api_y_recarga_sin_navegar()
    {
        var grupoNuevo = Guid.NewGuid();
        var api = new CatalogoApiFalsa
        {
            Grupos = [new(grupoNuevo, "Leguminosas")]
        };
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<DetalleDieta>(ps => ps.Add(p => p.Id, CatalogoApiFalsa.DietaId));
        cut.WaitForAssertion(() => Assert.Contains("Dieta basal", cut.Markup));

        cut.Find("#grupo").Change(grupoNuevo.ToString());
        cut.Find(".col-sm-2 button").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(api.UltimoGrupoDeDietaAgregado);
            Assert.Equal(CatalogoApiFalsa.DietaId, api.UltimoGrupoDeDietaAgregado!.Value.DietaId);
            Assert.Equal(grupoNuevo, api.UltimoGrupoDeDietaAgregado.Value.GrupoId);
            Assert.Equal(TipoCobertura.Requerido, api.UltimoGrupoDeDietaAgregado.Value.Tipo);
        });
    }

    [Fact]
    public void Error_al_modificar_muestra_el_mensaje_de_la_api()
    {
        var api = new CatalogoApiFalsa
        {
            ErrorAlEliminarDieta = ErrorDePrueba.Conflicto("El grupo no puede quitarse.")
        };
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<DetalleDieta>(ps => ps.Add(p => p.Id, CatalogoApiFalsa.DietaId));
        cut.WaitForAssertion(() => Assert.Contains("Dieta basal", cut.Markup));

        cut.FindAll("button").First(button => button.TextContent == "Quitar").Click();

        cut.WaitForAssertion(() => Assert.Contains("El grupo no puede quitarse.", cut.Markup));
    }

    [Fact]
    public void Agregar_sin_elegir_grupo_muestra_el_error_de_validacion()
    {
        var api = new CatalogoApiFalsa();
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<DetalleDieta>(ps => ps.Add(p => p.Id, CatalogoApiFalsa.DietaId));
        cut.WaitForAssertion(() => Assert.Contains("Dieta basal", cut.Markup));

        cut.Find(".col-sm-2 button").Click();

        cut.WaitForAssertion(() => Assert.Contains("Selecciona un grupo nutricional.", cut.Markup));
        Assert.Null(api.UltimoGrupoDeDietaAgregado);
    }

    [Fact]
    public void Agregar_y_quitar_restriccion_llaman_a_la_api_con_los_ids_correctos()
    {
        var restriccionNueva = Guid.NewGuid();
        var api = new CatalogoApiFalsa
        {
            Restricciones = [new(restriccionNueva, "Sin lactosa")]
        };
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<DetalleDieta>(ps => ps.Add(p => p.Id, CatalogoApiFalsa.DietaId));
        cut.WaitForAssertion(() => Assert.Contains("Dieta basal", cut.Markup));

        cut.Find("#restriccion").Change(restriccionNueva.ToString());
        cut.Find(".col-auto button").Click();
        cut.WaitForAssertion(() => Assert.Equal((CatalogoApiFalsa.DietaId, restriccionNueva), api.UltimaRestriccionAgregada));

        cut.InvokeAsync(() => cut.FindAll("button").Last(button => button.TextContent == "Quitar").Click());
        cut.WaitForAssertion(() => Assert.Equal((CatalogoApiFalsa.DietaId, CatalogoApiFalsa.Restriccion), api.UltimaRestriccionQuitada));
    }

    [Fact]
    public void Error_al_cargar_la_dieta_se_muestra_de_forma_legible()
    {
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa
        {
            ErrorAlCargarDieta = ErrorDePrueba.Conflicto("No se pudo cargar la dieta.")
        });

        var cut = Render<DetalleDieta>(ps => ps.Add(p => p.Id, CatalogoApiFalsa.DietaId));

        cut.WaitForAssertion(() => Assert.Contains("No se pudo cargar la dieta.", cut.Markup));
    }
}
