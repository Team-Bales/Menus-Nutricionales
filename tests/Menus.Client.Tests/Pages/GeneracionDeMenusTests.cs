using Bunit;
using Bunit.TestDoubles;
using Menus.Client.Catalogo;
using Menus.Client.GestionDeMenus;
using Menus.Client.Pages;
using Menus.Client.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Client.Tests.Pages;

/// <summary>Pruebas de la generación y el listado de menús (#81).</summary>
public sealed class GeneracionDeMenusTests : BunitContext
{
    /// <summary>La API de menús es solo para nutricionistas (ADR-016).</summary>
    public GeneracionDeMenusTests()
    {
        AddAuthorization().SetAuthorized("nutri@centro.test").SetRoles(RolesDeUsuario.Nutricionista);
    }

    [Fact]
    public void Generar_envia_la_dieta_la_cantidad_las_proporciones_y_los_grupos()
    {
        var (api, _) = Servicios();
        var cut = Render<GeneracionDeMenus>();
        cut.WaitForElement("#cantidad");

        CompletarFormulario(cut);
        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            var solicitud = Assert.IsType<SolicitudDeGeneracion>(api.UltimaSolicitud);
            Assert.Equal(CatalogoApiFalsa.DietaId, solicitud.DietaId);
            Assert.Equal(12, solicitud.CantidadTotalAlimentos);
            var proporcion = Assert.Single(solicitud.Distribucion);
            Assert.Equal(CatalogoApiFalsa.Tipo, proporcion.TipoPreparacionId);
            Assert.Equal(1m, proporcion.Proporcion);
            Assert.Equal([CatalogoApiFalsa.Grupo], solicitud.GruposNutricionalesIds);
            Assert.Contains("Menú generado correctamente.", cut.Markup);
        });
    }

    [Fact]
    public void El_listado_muestra_creador_fecha_origen_estado_y_parametros()
    {
        var (api, _) = Servicios();

        var cut = Render<GeneracionDeMenus>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Ana Pérez", cut.Markup);
            Assert.Contains("Luis García", cut.Markup);
            var fechaEsperada = api.Menus[0].FechaCreacion.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            Assert.Equal(fechaEsperada, cut.Find("tbody tr td").TextContent.Trim());
            Assert.DoesNotContain("1/15/2026", cut.Markup);
            Assert.Contains("Automático", cut.Markup);
            Assert.Contains("Manual", cut.Markup);
            Assert.Contains("Cantidad total: 12", cut.Markup);
            Assert.Contains("Plato fuerte", cut.Markup);
            Assert.Contains("Cereales", cut.Markup);
            Assert.Equal(CatalogoApiFalsa.DietaId, api.UltimaConsulta!.Value.DietaId);
            Assert.Equal(2, api.DetallesPedidos.Count);
        });
    }

    [Fact]
    public void No_envia_si_los_porcentajes_no_suman_cien()
    {
        var (api, _) = Servicios();
        var cut = Render<GeneracionDeMenus>();
        cut.WaitForElement("#cantidad");

        cut.Find("#cantidad").Change("12");
        cut.Find("#grupo-0").Change(true);
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Contains("Los porcentajes deben sumar 100.", cut.Markup));
        Assert.Null(api.UltimaSolicitud);
    }

    [Fact]
    public void No_envia_si_no_se_selecciona_un_grupo()
    {
        var (api, _) = Servicios();
        var cut = Render<GeneracionDeMenus>();
        cut.WaitForElement("#cantidad");

        cut.Find("#cantidad").Change("12");
        cut.Find("#porcentaje-0").Change("100");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Contains("Selecciona al menos un grupo nutricional.", cut.Markup));
        Assert.Null(api.UltimaSolicitud);
    }

    [Fact]
    public void Marcar_y_desmarcar_un_grupo_lo_agrega_y_lo_quita()
    {
        var (api, _) = Servicios();
        var cut = Render<GeneracionDeMenus>();
        cut.WaitForElement("#cantidad");

        CompletarFormulario(cut);
        cut.Find("#grupo-0").Change(false);
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Contains("Selecciona al menos un grupo nutricional.", cut.Markup));
        Assert.Null(api.UltimaSolicitud);
    }

    [Fact]
    public void Muestra_el_error_de_la_api_al_generar()
    {
        var (api, _) = Servicios(new MenusApiFalsa
        {
            ErrorAlGenerar = ErrorDePrueba.Conflicto("La dieta no tiene grupos configurados.")
        });
        var cut = Render<GeneracionDeMenus>();
        cut.WaitForElement("#cantidad");

        CompletarFormulario(cut);
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Contains("La dieta no tiene grupos configurados.", cut.Markup));
        Assert.Null(api.UltimaSolicitud);
    }

    [Fact]
    public void Sin_menus_muestra_el_estado_vacio()
    {
        Servicios(new MenusApiFalsa { Menus = [] });

        var cut = Render<GeneracionDeMenus>();

        cut.WaitForAssertion(() => Assert.Contains("No hay menús generados para esta dieta.", cut.Markup));
    }

    [Fact]
    public void Muestra_el_error_al_cargar_el_listado()
    {
        Servicios(new MenusApiFalsa
        {
            ErrorAlListar = ErrorDePrueba.Conflicto("No se pudieron cargar los menús.")
        });

        var cut = Render<GeneracionDeMenus>();

        cut.WaitForAssertion(() => Assert.Contains("No se pudieron cargar los menús.", cut.Markup));
    }

    [Fact]
    public void Muestra_el_error_al_cargar_un_detalle()
    {
        Servicios(new MenusApiFalsa
        {
            ErrorAlObtener = ErrorDePrueba.Conflicto("No se pudo cargar el detalle del menú.")
        });

        var cut = Render<GeneracionDeMenus>();

        cut.WaitForAssertion(() => Assert.Contains("No se pudo cargar el detalle del menú.", cut.Markup));
    }

    [Fact]
    public void Muestra_el_error_al_cargar_los_catalogos_y_no_intenta_listar()
    {
        var (api, _) = Servicios(catalogo: new CatalogoApiFalsa
        {
            ErrorAlCargarNomencladores = ErrorDePrueba.Conflicto("No se pudieron cargar los tipos de preparación.")
        });

        var cut = Render<GeneracionDeMenus>();

        cut.WaitForAssertion(() => Assert.Contains("No se pudieron cargar los tipos de preparación.", cut.Markup));
        Assert.Null(api.UltimaConsulta);
    }

    [Fact]
    public void Sin_dietas_muestra_un_aviso_y_no_intenta_listar()
    {
        var (api, _) = Servicios(catalogo: new CatalogoApiFalsa { Dietas = [] });

        var cut = Render<GeneracionDeMenus>();

        cut.WaitForAssertion(() => Assert.Contains("No hay dietas configuradas", cut.Markup));
        Assert.Null(api.UltimaConsulta);
    }

    [Fact]
    public void La_paginacion_avanza_y_retrocede()
    {
        var (api, _) = Servicios(new MenusApiFalsa { Total = 25 });
        var cut = Render<GeneracionDeMenus>();
        cut.WaitForAssertion(() => Assert.Contains("Ana Pérez", cut.Markup));

        cut.FindAll("button").Single(button => button.TextContent == "Siguiente").Click();
        cut.WaitForAssertion(() => Assert.Equal(2, api.UltimaConsulta!.Value.Page));

        cut.FindAll("button").Single(button => button.TextContent == "Anterior").Click();
        cut.WaitForAssertion(() => Assert.Equal(1, api.UltimaConsulta!.Value.Page));
    }

    [Fact]
    public void Buscar_menus_recarga_el_listado_para_la_dieta_elegida()
    {
        var (api, _) = Servicios();
        var cut = Render<GeneracionDeMenus>();
        cut.WaitForAssertion(() => Assert.Contains("Ana Pérez", cut.Markup));

        cut.FindAll("button").Single(button => button.TextContent == "Buscar menús").Click();

        cut.WaitForAssertion(() => Assert.Equal(CatalogoApiFalsa.DietaId, api.UltimaConsulta!.Value.DietaId));
    }

    [Fact]
    public void Generar_omite_los_tipos_con_porcentaje_cero()
    {
        var tipos = new List<Nomenclador>
        {
            new(CatalogoApiFalsa.Tipo, "Entrante"),
            new(Guid.NewGuid(), "Plato fuerte"),
            new(Guid.NewGuid(), "Postre"),
            new(Guid.NewGuid(), "Bebida")
        };
        var (api, _) = Servicios(catalogo: new CatalogoApiFalsa { Tipos = tipos });
        var cut = Render<GeneracionDeMenus>();
        cut.WaitForElement("#porcentaje-3");

        cut.Find("#cantidad").Change("20");
        cut.Find("#porcentaje-0").Change("25");
        cut.Find("#porcentaje-1").Change("50");
        cut.Find("#porcentaje-2").Change("25");
        cut.Find("#grupo-0").Change(true);
        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            var solicitud = Assert.IsType<SolicitudDeGeneracion>(api.UltimaSolicitud);
            Assert.Equal(3, solicitud.Distribucion.Count);
            Assert.Equal([CatalogoApiFalsa.Tipo, tipos[1].Id, tipos[2].Id], solicitud.Distribucion.Select(d => d.TipoPreparacionId));
            Assert.DoesNotContain(tipos[3].Id, solicitud.Distribucion.Select(d => d.TipoPreparacionId));
        });
    }

    [Fact]
    public void Cambiar_de_dieta_recarga_el_listado_desde_la_primera_pagina()
    {
        var otraDieta = Guid.NewGuid();
        var catalogo = new CatalogoApiFalsa
        {
            Dietas =
            [
                new(CatalogoApiFalsa.DietaId, "Dieta basal", CatalogoApiFalsa.Programa, 1, 1),
                new(otraDieta, "Blanda oncológica", CatalogoApiFalsa.Programa, 1, 1)
            ]
        };
        var (api, _) = Servicios(new MenusApiFalsa { Total = 25 }, catalogo);
        var cut = Render<GeneracionDeMenus>();
        cut.WaitForAssertion(() => Assert.Contains("Ana Pérez", cut.Markup));

        cut.FindAll("button").Single(button => button.TextContent == "Siguiente").Click();
        cut.WaitForAssertion(() => Assert.Equal(2, api.UltimaConsulta!.Value.Page));

        cut.Find("#dieta").Change(otraDieta.ToString());

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(otraDieta, api.UltimaConsulta!.Value.DietaId);
            Assert.Equal(1, api.UltimaConsulta!.Value.Page);
        });
    }

    [Fact]
    public void Buscar_menus_limpia_el_error_de_una_generacion_anterior()
    {
        Servicios(new MenusApiFalsa
        {
            ErrorAlGenerar = ErrorDePrueba.Conflicto("La dieta no tiene grupos configurados.")
        });
        var cut = Render<GeneracionDeMenus>();
        cut.WaitForElement("#cantidad");

        CompletarFormulario(cut);
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => Assert.Contains("La dieta no tiene grupos configurados.", cut.Markup));

        cut.FindAll("button").Single(button => button.TextContent == "Buscar menús").Click();

        cut.WaitForAssertion(() => Assert.DoesNotContain("La dieta no tiene grupos configurados.", cut.Markup));
        cut.WaitForAssertion(() => Assert.Contains("Ana Pérez", cut.Markup));
    }

    private (MenusApiFalsa Menus, CatalogoApiFalsa Catalogo) Servicios(
        MenusApiFalsa? menusApi = null,
        CatalogoApiFalsa? catalogo = null)
    {
        var menus = menusApi ?? new MenusApiFalsa();
        var catalogoApi = catalogo ?? new CatalogoApiFalsa();
        Services.AddSingleton<IMenusApi>(menus);
        Services.AddSingleton<ICatalogoApi>(catalogoApi);
        return (menus, catalogoApi);
    }

    private static void CompletarFormulario(IRenderedComponent<GeneracionDeMenus> cut, int cantidad = 12)
    {
        cut.WaitForElement("#cantidad");
        cut.Find("#cantidad").Change(cantidad.ToString());
        cut.Find("#porcentaje-0").Change("100");
        cut.Find("#grupo-0").Change(true);
    }
}
