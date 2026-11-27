using System.Net;
using System.Security.Claims;
using Bunit;
using Bunit.TestDoubles;
using Menus.Client.Catalogo;
using Menus.Client.MenusGenerados;
using Menus.Client.Pages;
using Menus.Client.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Client.Tests.Pages;

/// <summary>Pruebas de la generación y el listado de menús del issue #81.</summary>
public sealed class GestionDeMenusTests : BunitContext
{
    private static readonly Guid OtraDieta = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    [Fact]
    public void Listado_muestra_creador_fecha_y_parametros_usados()
    {
        ComoNutricionista();
        Servicios();

        var cut = Render<GestionDeMenus>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Ana Pérez", cut.Markup);
            Assert.Contains(MenusApiFalsa.Fecha.ToLocalTime().ToString("dd/MM/yyyy"), cut.Markup);
            Assert.Contains("10 alimentos", cut.Markup);
            Assert.Contains("Plato fuerte", cut.Markup);
            Assert.Contains("Cereales", cut.Markup);
            Assert.Contains("Pendiente", cut.Markup);
        });
    }

    [Fact]
    public void Con_una_sola_dieta_se_selecciona_y_se_lista_sola()
    {
        ComoNutricionista();
        var menus = new MenusApiFalsa();
        Servicios(menus: menus);

        var cut = Render<GestionDeMenus>();

        cut.WaitForAssertion(() =>
        {
            var consulta = menus.UltimaConsulta!.Value;
            Assert.Equal(CatalogoApiFalsa.DietaId, consulta.DietaId);
            Assert.Null(consulta.Estado);
            Assert.Equal(1, consulta.Page);
            Assert.Equal(10, consulta.Size);
        });
    }

    [Fact]
    public void Generar_envia_los_parametros_elegidos()
    {
        ComoNutricionista();
        var menus = new MenusApiFalsa();
        Servicios(menus: menus);
        var cut = Render<GestionDeMenus>();
        cut.WaitForAssertion(() => Assert.Contains("Generar menú automático", cut.Markup));

        cut.Find("#cantidad").Change("12");
        cut.Find($"#proporcion-{CatalogoApiFalsa.Tipo}").Change("1");
        cut.Find($"#grupo-{CatalogoApiFalsa.Grupo}").Change(true);
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Generar menú").Click();

        cut.WaitForAssertion(() => Assert.NotNull(menus.UltimaGeneracion));
        var generacion = menus.UltimaGeneracion!;
        Assert.Equal(CatalogoApiFalsa.DietaId, generacion.DietaId);
        Assert.Equal(12, generacion.CantidadTotalAlimentos);
        Assert.Equal([new ProporcionDeMenu(CatalogoApiFalsa.Tipo, 1m)], generacion.Distribucion);
        Assert.Equal([CatalogoApiFalsa.Grupo], generacion.GruposNutricionalesIds);
        cut.WaitForAssertion(() => Assert.Contains("Menú generado correctamente.", cut.Markup));
    }

    [Fact]
    public void Generar_valida_dieta_cantidad_proporciones_y_grupos()
    {
        ComoNutricionista();
        var menus = new MenusApiFalsa();
        Servicios(menus: menus);
        var cut = Render<GestionDeMenus>();
        cut.WaitForAssertion(() => Assert.Contains("Generar menú automático", cut.Markup));

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Generar menú").Click();
        cut.WaitForAssertion(() => Assert.Contains("Las proporciones deben sumar 1.", cut.Markup));

        cut.Find($"#proporcion-{CatalogoApiFalsa.Tipo}").Change("2");
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Generar menú").Click();
        cut.WaitForAssertion(() => Assert.Contains("Cada proporción debe estar entre 0 y 1.", cut.Markup));

        cut.Find($"#proporcion-{CatalogoApiFalsa.Tipo}").Change("1");
        cut.Find("#cantidad").Change("0");
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Generar menú").Click();
        cut.WaitForAssertion(() => Assert.Contains("La cantidad total de alimentos debe ser mayor que cero.", cut.Markup));

        cut.Find("#cantidad").Change("10");
        cut.Find($"#grupo-{CatalogoApiFalsa.Grupo}").Change(true);
        cut.Find($"#grupo-{CatalogoApiFalsa.Grupo}").Change(false);
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Generar menú").Click();
        cut.WaitForAssertion(() => Assert.Contains("Selecciona al menos un grupo nutricional.", cut.Markup));

        Assert.Null(menus.UltimaGeneracion);
    }

    [Fact]
    public void Un_error_al_generar_se_muestra()
    {
        ComoNutricionista();
        var menus = new MenusApiFalsa { ErrorAlGenerar = new(HttpStatusCode.Conflict, "No hay ítems compatibles.") };
        Servicios(menus: menus);
        var cut = Render<GestionDeMenus>();
        cut.WaitForAssertion(() => Assert.Contains("Generar menú automático", cut.Markup));

        cut.Find($"#proporcion-{CatalogoApiFalsa.Tipo}").Change("1");
        cut.Find($"#grupo-{CatalogoApiFalsa.Grupo}").Change(true);
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Generar menú").Click();

        cut.WaitForAssertion(() => Assert.Contains("No hay ítems compatibles.", cut.Markup));
    }

    [Fact]
    public void Con_varias_dietas_hay_que_elegir_una_para_listar()
    {
        ComoNutricionista();
        var administracion = new AdministracionApiFalsa
        {
            Detalle = new(
                AdministracionApiFalsa.NutricionistaId,
                "Ana Pérez",
                CatalogoApiFalsa.Especialidad,
                false,
                [new(CatalogoApiFalsa.DietaId), new(OtraDieta)]),
        };
        var catalogo = new CatalogoApiFalsa
        {
            Dietas =
            [
                new(CatalogoApiFalsa.DietaId, "Dieta basal", CatalogoApiFalsa.Programa, 1, 1),
                new(OtraDieta, "Dieta blanda", CatalogoApiFalsa.Programa, 1, 1),
            ],
        };
        var menus = new MenusApiFalsa();
        Servicios(administracion, catalogo, menus);

        var cut = Render<GestionDeMenus>();

        cut.WaitForAssertion(() => Assert.Contains("Selecciona una dieta para ver sus menús.", cut.Markup));
        Assert.Null(menus.UltimaConsulta);

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Generar menú").Click();
        cut.WaitForAssertion(() => Assert.Contains("Selecciona una dieta.", cut.Markup));

        cut.Find("#dieta").Change(OtraDieta);

        cut.WaitForAssertion(() =>
        {
            var consulta = menus.UltimaConsulta!.Value;
            Assert.Equal(OtraDieta, consulta.DietaId);
            Assert.Null(consulta.Estado);
            Assert.Equal(1, consulta.Page);
        });

        cut.Find("#dieta").Change(Guid.Empty);
        cut.WaitForAssertion(() => Assert.Contains("Selecciona una dieta para ver sus menús.", cut.Markup));
    }

    [Fact]
    public void Sin_dietas_asignadas_se_avisa()
    {
        ComoNutricionista();
        var administracion = new AdministracionApiFalsa
        {
            Detalle = new(
                AdministracionApiFalsa.NutricionistaId,
                "Ana Pérez",
                CatalogoApiFalsa.Especialidad,
                false,
                []),
        };
        var menus = new MenusApiFalsa();
        Servicios(administracion, menus: menus);

        var cut = Render<GestionDeMenus>();

        cut.WaitForAssertion(() => Assert.Contains("No tienes dietas asignadas.", cut.Markup));
        Assert.Null(menus.UltimaConsulta);
    }

    [Fact]
    public void Un_nutricionista_sin_entidad_id_muestra_un_error()
    {
        AddAuthorization().SetAuthorized("nutri@centro.test").SetRoles(RolesDeUsuario.Nutricionista);
        var menus = new MenusApiFalsa();
        Servicios(menus: menus);

        var cut = Render<GestionDeMenus>();

        cut.WaitForAssertion(() => Assert.Contains("La sesión no identifica al nutricionista.", cut.Markup));
        Assert.Null(menus.UltimaConsulta);
    }

    [Fact]
    public void Filtrar_por_estado_y_limpiar_el_filtro()
    {
        ComoNutricionista();
        var menus = new MenusApiFalsa();
        Servicios(menus: menus);
        var cut = Render<GestionDeMenus>();
        cut.WaitForAssertion(() => Assert.Contains("Ana Pérez", cut.Markup));

        cut.Find("#estado").Change("Final");
        cut.FindAll("button").Single(b => b.TextContent == "Filtrar").Click();
        cut.WaitForAssertion(() =>
        {
            var consulta = menus.UltimaConsulta!.Value;
            Assert.Equal(CatalogoApiFalsa.DietaId, consulta.DietaId);
            Assert.Equal("Final", consulta.Estado);
            Assert.Equal(1, consulta.Page);
            Assert.Equal(10, consulta.Size);
        });

        cut.FindAll("button").Single(b => b.TextContent == "Limpiar").Click();
        cut.WaitForAssertion(() =>
        {
            var consulta = menus.UltimaConsulta!.Value;
            Assert.Equal(CatalogoApiFalsa.DietaId, consulta.DietaId);
            Assert.Null(consulta.Estado);
            Assert.Equal(1, consulta.Page);
            Assert.Equal(10, consulta.Size);
        });
    }

    [Fact]
    public void Un_error_al_listar_se_muestra_y_el_listado_vacio_tiene_su_mensaje()
    {
        ComoNutricionista();
        var menus = new MenusApiFalsa { ErrorAlListar = new(HttpStatusCode.InternalServerError, "No se pudo listar.") };
        Servicios(menus: menus);

        var cut = Render<GestionDeMenus>();
        cut.WaitForAssertion(() => Assert.Contains("No se pudo listar.", cut.Markup));

        menus.ErrorAlListar = null;
        menus.Menus = [];
        cut.FindAll("button").Single(b => b.TextContent == "Filtrar").Click();
        cut.WaitForAssertion(() => Assert.Contains("Esta dieta no tiene menús generados.", cut.Markup));
    }

    [Fact]
    public void Un_error_al_cargar_los_datos_de_la_sesion_se_muestra()
    {
        ComoNutricionista();
        var administracion = new AdministracionApiFalsa
        {
            Error = new(HttpStatusCode.InternalServerError, "No se pudo cargar el nutricionista."),
        };
        Servicios(administracion);

        var cut = Render<GestionDeMenus>();

        cut.WaitForAssertion(() => Assert.Contains("No se pudo cargar el nutricionista.", cut.Markup));
        Assert.DoesNotContain("No tienes dietas asignadas.", cut.Markup);
    }

    [Fact]
    public void El_estado_se_muestra_con_su_color_y_un_menu_sin_detalle_solo_con_la_cantidad()
    {
        ComoNutricionista();
        var sinDetalle = Guid.NewGuid();
        var menus = new MenusApiFalsa
        {
            Menus =
            [
                new(MenusApiFalsa.MenuId, CatalogoApiFalsa.DietaId, MenusApiFalsa.Fecha,
                    new(AdministracionApiFalsa.NutricionistaId, "Ana Pérez"), true, 10, "Final", null, null),
                new(sinDetalle, CatalogoApiFalsa.DietaId, MenusApiFalsa.Fecha,
                    new(AdministracionApiFalsa.NutricionistaId, "Ana Pérez"), true, 8, "Rechazado", null, null),
            ],
        };
        menus.SinDetalle.Add(sinDetalle);
        Servicios(menus: menus);

        var cut = Render<GestionDeMenus>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("text-bg-success", cut.Markup);
            Assert.Contains("text-bg-danger", cut.Markup);
            var fila = cut.FindAll("tr").Single(tr => tr.TextContent.Contains("8 alimentos"));
            Assert.DoesNotContain("Grupos:", fila.TextContent);
            Assert.Contains(cut.FindAll("tr"), tr => tr.TextContent.Contains("Grupos:"));
        });
    }

    [Fact]
    public void Paginar_avanza_y_retrocede()
    {
        ComoNutricionista();
        var menus = new MenusApiFalsa
        {
            Menus = [.. Enumerable.Range(1, 11).Select(i =>
                new Menu(Guid.NewGuid(), CatalogoApiFalsa.DietaId, MenusApiFalsa.Fecha,
                    new(AdministracionApiFalsa.NutricionistaId, $"Creador {i}"), true, 10, "Pendiente", null, null))],
        };
        Servicios(menus: menus);
        var cut = Render<GestionDeMenus>();
        cut.WaitForAssertion(() => Assert.Contains("Creador 1", cut.Markup));

        cut.FindAll("button").Single(b => b.TextContent == "Siguiente").Click();
        cut.WaitForAssertion(() => Assert.Equal(2, menus.UltimaConsulta!.Value.Page));
        cut.WaitForAssertion(() => Assert.NotNull(cut.FindAll("button").SingleOrDefault(b => b.TextContent == "Anterior")));

        cut.FindAll("button").Single(b => b.TextContent == "Anterior").Click();
        cut.WaitForAssertion(() => Assert.Equal(1, menus.UltimaConsulta!.Value.Page));
    }

    private void ComoNutricionista()
        => AddAuthorization()
            .SetAuthorized("nutri@centro.test")
            .SetRoles(RolesDeUsuario.Nutricionista)
            .SetClaims(new Claim("entidad_id", AdministracionApiFalsa.NutricionistaId.ToString()));

    private void Servicios(
        AdministracionApiFalsa? administracion = null,
        CatalogoApiFalsa? catalogo = null,
        MenusApiFalsa? menus = null)
    {
        Services.AddSingleton<IAdministracionApi>(administracion ?? new AdministracionApiFalsa());
        Services.AddSingleton<ICatalogoApi>(catalogo ?? new CatalogoApiFalsa());
        Services.AddSingleton<IMenusApi>(menus ?? new MenusApiFalsa());
    }
}
