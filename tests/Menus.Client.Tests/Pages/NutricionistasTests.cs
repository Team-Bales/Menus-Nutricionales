using Bunit;
using Bunit.TestDoubles;
using Menus.Client.Pages;
using Menus.Client.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Client.Tests.Pages;

/// <summary>Pruebas de la gestión de nutricionistas del issue #76.</summary>
public sealed class NutricionistasTests : BunitContext
{
    public NutricionistasTests() => AddAuthorization().SetAuthorized("admin@centro.test").SetRoles(RolesDeUsuario.Admin);

    [Fact]
    public void Listado_muestra_el_nutricionista_su_especialidad_y_las_dietas_autorizadas()
    {
        Services.AddSingleton<IAdministracionApi>(new AdministracionApiFalsa());
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());

        var cut = Render<Nutricionistas>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Ana Pérez", cut.Markup);
            Assert.Contains("Nutrición clínica", cut.Markup);
            Assert.Contains(">1<", cut.Markup);
        });
    }

    [Fact]
    public void Filtrar_envia_la_especialidad_y_reinicia_la_pagina()
    {
        var api = new AdministracionApiFalsa();
        Services.AddSingleton<IAdministracionApi>(api);
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());
        var cut = Render<Nutricionistas>();
        cut.WaitForAssertion(() => Assert.Contains("Ana Pérez", cut.Markup));

        cut.Find("#especialidad").Change(CatalogoApiFalsa.Especialidad.ToString());
        cut.FindAll("button").Single(b => b.TextContent == "Filtrar").Click();

        cut.WaitForAssertion(() => Assert.Equal((CatalogoApiFalsa.Especialidad, 1, 10), api.UltimaConsultaNutricionistas));
    }

    [Fact]
    public void Ver_detalle_navega_al_nutricionista_elegido()
    {
        Services.AddSingleton<IAdministracionApi>(new AdministracionApiFalsa());
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        var cut = Render<Nutricionistas>();
        cut.WaitForAssertion(() => Assert.Contains("Ana Pérez", cut.Markup));

        cut.FindAll("button").Single(b => b.TextContent == "Ver detalle").Click();

        Assert.EndsWith($"/admin/nutricionistas/{AdministracionApiFalsa.NutricionistaId}", nav.Uri);
    }

    [Fact]
    public void Limpiar_quita_el_filtro_y_la_paginacion_cambia_de_pagina()
    {
        var api = new AdministracionApiFalsa();
        Services.AddSingleton<IAdministracionApi>(api);
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());
        var cut = Render<Nutricionistas>();
        cut.WaitForAssertion(() => Assert.Contains("Ana Pérez", cut.Markup));

        cut.Find("#especialidad").Change(CatalogoApiFalsa.Especialidad.ToString());
        cut.FindAll("button").Single(b => b.TextContent == "Limpiar").Click();
        // La consulta sale antes de que la página termine de cargar (el detalle de cada fila): hasta entonces no
        // están los botones de paginación (#366).
        cut.WaitForAssertion(() =>
        {
            Assert.Equal((null, 1, 10), api.UltimaConsultaNutricionistas);
            Assert.DoesNotContain("Cargando", cut.Markup);
        });

        cut.FindAll("button").Single(b => b.TextContent == "Siguiente").Click();
        cut.WaitForAssertion(() =>
        {
            Assert.Equal(2, api.UltimaConsultaNutricionistas!.Value.Page);
            Assert.DoesNotContain("Cargando", cut.Markup);
        });
        cut.FindAll("button").Single(b => b.TextContent == "Anterior").Click();
        cut.WaitForAssertion(() => Assert.Equal(1, api.UltimaConsultaNutricionistas!.Value.Page));
    }

    [Fact]
    public void Listado_vacio_y_especialidad_desconocida_muestran_los_estados_correspondientes()
    {
        var api = new AdministracionApiFalsa { Nutricionistas = [] };
        Services.AddSingleton<IAdministracionApi>(api);
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());

        var cut = Render<Nutricionistas>();

        cut.WaitForAssertion(() => Assert.Contains("No hay nutricionistas con esos filtros.", cut.Markup));
    }

    [Fact]
    public void Especialidad_desconocida_se_muestra_con_guion()
    {
        var api = new AdministracionApiFalsa
        {
            Nutricionistas = [new(AdministracionApiFalsa.NutricionistaId, "Ana Pérez", Guid.NewGuid(), false)],
        };
        Services.AddSingleton<IAdministracionApi>(api);
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());

        var cut = Render<Nutricionistas>();

        cut.WaitForAssertion(() => Assert.Contains("—", cut.Markup));
    }

    [Fact]
    public void Nutricionista_jefe_se_muestra_como_jefe()
    {
        var api = new AdministracionApiFalsa
        {
            Nutricionistas = [new(AdministracionApiFalsa.NutricionistaId, "Ana Pérez", CatalogoApiFalsa.Especialidad, true)],
        };
        Services.AddSingleton<IAdministracionApi>(api);
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());

        var cut = Render<Nutricionistas>();

        cut.WaitForAssertion(() => Assert.Contains(">Sí<", cut.Markup));
    }

    [Fact]
    public void Error_al_cargar_se_muestra_de_forma_legible()
    {
        Services.AddSingleton<IAdministracionApi>(new AdministracionApiFalsa { Error = ErrorDePrueba.Conflicto("No se pudieron cargar los nutricionistas.") });
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());

        var cut = Render<Nutricionistas>();

        cut.WaitForAssertion(() => Assert.Contains("No se pudieron cargar los nutricionistas.", cut.Markup));
    }

    [Fact]
    public void Error_al_cargar_especialidades_se_muestra_sin_consultar_el_listado()
    {
        var api = new AdministracionApiFalsa();
        Services.AddSingleton<IAdministracionApi>(api);
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa
        {
            ErrorAlCargarNomencladores = ErrorDePrueba.Conflicto("No se pudieron cargar las especialidades."),
        });

        var cut = Render<Nutricionistas>();

        cut.WaitForAssertion(() => Assert.Contains("No se pudieron cargar las especialidades.", cut.Markup));
        Assert.Null(api.UltimaConsultaNutricionistas);
    }
}
