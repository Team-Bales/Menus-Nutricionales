using System.Net;
using System.Security.Claims;
using Bunit;
using Bunit.TestDoubles;
using Menus.Client;
using Menus.Client.Administracion;
using Menus.Client.Catalogo;
using Menus.Client.Pages;
using Menus.Client.Services;
using Menus.Client.Tests.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Client.Tests.Pages;

/// <summary>Pruebas de la gestión de pacientes del issue #77.</summary>
public sealed class PacientesTests : BunitContext
{
    [Fact]
    public void Listado_muestra_nombre_edad_sala_y_cantidad_de_dietas()
    {
        ComoAdmin();
        Services.AddSingleton<IAdministracionApi>(new AdministracionApiFalsa());
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());

        var cut = Render<Pacientes>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Yudivián Almeida", cut.Markup);
            Assert.Contains(">45<", cut.Markup);
            Assert.Contains("Sala 101", cut.Markup);
            Assert.Contains(">1<", cut.Markup);
        });
    }

    [Fact]
    public void Filtrar_envia_la_sala_la_dieta_y_reinicia_la_pagina()
    {
        ComoAdmin();
        var api = new AdministracionApiFalsa();
        Services.AddSingleton<IAdministracionApi>(api);
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());
        var cut = Render<Pacientes>();
        cut.WaitForAssertion(() => Assert.Contains("Yudivián Almeida", cut.Markup));

        cut.Find("#sala").Change(AdministracionApiFalsa.SalaId.ToString());
        cut.Find("#dieta").Change(CatalogoApiFalsa.DietaId.ToString());
        cut.FindAll("button").Single(b => b.TextContent == "Filtrar").Click();

        cut.WaitForAssertion(() => Assert.Equal(
            (AdministracionApiFalsa.SalaId, CatalogoApiFalsa.DietaId, 1, 10),
            api.UltimaConsultaPacientes));
    }

    [Fact]
    public void Ver_detalle_navega_al_paciente_elegido()
    {
        ComoAdmin();
        Services.AddSingleton<IAdministracionApi>(new AdministracionApiFalsa());
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        var cut = Render<Pacientes>();
        cut.WaitForAssertion(() => Assert.Contains("Yudivián Almeida", cut.Markup));

        cut.FindAll("button").Single(b => b.TextContent == "Ver detalle").Click();

        Assert.EndsWith($"/admin/pacientes/{AdministracionApiFalsa.PacienteId}", nav.Uri);
    }

    [Fact]
    public void Un_nutricionista_solo_puede_elegir_sus_dietas_y_ve_sus_pacientes()
    {
        ComoNutricionista();
        var api = new AdministracionApiFalsa();
        var catalogo = new CatalogoApiFalsa();
        catalogo.Dietas.Add(new Dieta(Guid.NewGuid(), "Otra dieta", CatalogoApiFalsa.Programa, 0, 0));
        Services.AddSingleton<IAdministracionApi>(api);
        Services.AddSingleton<ICatalogoApi>(catalogo);

        var cut = Render<Pacientes>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Yudivián Almeida", cut.Markup);
            Assert.Contains("Dieta basal", cut.Markup);
        });
        Assert.DoesNotContain("Otra dieta", cut.Markup);
        Assert.Equal(CatalogoApiFalsa.DietaId, api.UltimaConsultaPacientes!.Value.Dieta);
    }

    [Fact]
    public void Un_nutricionista_sin_dietas_no_ve_pacientes()
    {
        ComoNutricionista();
        var api = new AdministracionApiFalsa { Detalle = new(AdministracionApiFalsa.NutricionistaId, "Ana Pérez", CatalogoApiFalsa.Especialidad, false, []) };
        Services.AddSingleton<IAdministracionApi>(api);
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());

        var cut = Render<Pacientes>();

        cut.WaitForAssertion(() => Assert.Contains("No tienes dietas asignadas", cut.Markup));
        Assert.Null(api.UltimaConsultaPacientes);
    }

    [Fact]
    public void Un_nutricionista_con_varias_dietas_no_lista_hasta_elegir_una()
    {
        ComoNutricionista();
        var otra = Guid.NewGuid();
        var api = new AdministracionApiFalsa
        {
            Detalle = new(
                AdministracionApiFalsa.NutricionistaId,
                "Ana Pérez",
                CatalogoApiFalsa.Especialidad,
                false,
                [new(CatalogoApiFalsa.DietaId), new(otra)]),
        };
        var catalogo = new CatalogoApiFalsa();
        catalogo.Dietas.Add(new Dieta(otra, "Otra dieta", CatalogoApiFalsa.Programa, 0, 0));
        Services.AddSingleton<IAdministracionApi>(api);
        Services.AddSingleton<ICatalogoApi>(catalogo);

        var cut = Render<Pacientes>();

        cut.WaitForAssertion(() => Assert.Contains("Selecciona una de tus dietas", cut.Markup));
        Assert.Null(api.UltimaConsultaPacientes);

        cut.FindAll("button").Single(b => b.TextContent == "Filtrar").Click();

        Assert.Null(api.UltimaConsultaPacientes);

        cut.FindAll("button").Single(b => b.TextContent == "Limpiar").Click();
        cut.WaitForAssertion(() => Assert.Contains("Selecciona una de tus dietas", cut.Markup));
        Assert.Null(api.UltimaConsultaPacientes);
    }

    [Fact]
    public void Listado_vacio_muestra_el_estado_correspondiente()
    {
        ComoAdmin();
        var api = new AdministracionApiFalsa { Pacientes = [] };
        Services.AddSingleton<IAdministracionApi>(api);
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());

        var cut = Render<Pacientes>();

        cut.WaitForAssertion(() => Assert.Contains("No hay pacientes con esos filtros.", cut.Markup));
    }

    [Fact]
    public void Un_error_de_la_api_se_muestra_al_cargar_el_listado()
    {
        ComoAdmin();
        var api = new AdministracionApiFalsa { Error = new(HttpStatusCode.InternalServerError, "No se pudo cargar.") };
        Services.AddSingleton<IAdministracionApi>(api);
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());

        var cut = Render<Pacientes>();

        cut.WaitForAssertion(() => Assert.Contains("No se pudo cargar.", cut.Markup));
    }

    [Fact]
    public void Un_paciente_que_entra_a_la_gestion_de_pacientes_no_ve_la_pagina()
    {
        AddAuthorization().SetAuthorized("paciente@centro.test").SetRoles(RolesDeUsuario.Paciente);
        Services.AddSingleton<IAutenticacionApi>(new AutenticacionApiFalsa());
        Services.AddSingleton<ISesionAutenticada>(new SesionAutenticadaFalsa());
        Services.AddSingleton<IAdministracionApi>(new AdministracionApiFalsa());
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());
        Services.GetRequiredService<BunitNavigationManager>().NavigateTo("/admin/pacientes");

        var cut = Render<App>();

        cut.WaitForAssertion(() => Assert.Contains("Sin permisos para esta página", cut.Markup));
        Assert.DoesNotContain("Pacientes ingresados", cut.Markup);
    }

    [Fact]
    public void Un_nutricionista_sin_entidad_id_muestra_un_error()
    {
        AddAuthorization().SetAuthorized("nutri@centro.test").SetRoles(RolesDeUsuario.Nutricionista);
        var api = new AdministracionApiFalsa();
        Services.AddSingleton<IAdministracionApi>(api);
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());

        var cut = Render<Pacientes>();

        cut.WaitForAssertion(() => Assert.Contains("La sesión no identifica al nutricionista.", cut.Markup));
        Assert.Null(api.UltimaConsultaPacientes);
    }

    [Fact]
    public void Limpiar_quita_los_filtros_y_reinicia_la_pagina()
    {
        ComoAdmin();
        var api = new AdministracionApiFalsa();
        Services.AddSingleton<IAdministracionApi>(api);
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());
        var cut = Render<Pacientes>();
        cut.WaitForAssertion(() => Assert.Contains("Yudivián Almeida", cut.Markup));

        cut.Find("#sala").Change(AdministracionApiFalsa.SalaId.ToString());
        cut.Find("#dieta").Change(CatalogoApiFalsa.DietaId.ToString());
        cut.FindAll("button").Single(b => b.TextContent == "Filtrar").Click();
        cut.WaitForAssertion(() => Assert.Equal(
            (AdministracionApiFalsa.SalaId, CatalogoApiFalsa.DietaId, 1, 10),
            api.UltimaConsultaPacientes));

        cut.FindAll("button").Single(b => b.TextContent == "Limpiar").Click();

        cut.WaitForAssertion(() => Assert.Equal((null, null, 1, 10), api.UltimaConsultaPacientes));
    }

    [Fact]
    public void Un_error_al_listar_se_muestra_sin_perder_los_filtros()
    {
        ComoAdmin();
        var api = new AdministracionApiFalsa { ErrorAlListarPacientes = new(HttpStatusCode.InternalServerError, "No se pudo listar.") };
        Services.AddSingleton<IAdministracionApi>(api);
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());

        var cut = Render<Pacientes>();

        cut.WaitForAssertion(() => Assert.Contains("No se pudo listar.", cut.Markup));
        Assert.Contains("id=\"sala\"", cut.Markup);
    }

    [Fact]
    public void Una_fila_sin_detalle_muestra_guion_y_cero_dietas()
    {
        ComoAdmin();
        var otro = Guid.NewGuid();
        var api = new AdministracionApiFalsa
        {
            Pacientes = [new(otro, "Paciente sin detalle", 30, AdministracionApiFalsa.SalaId)],
            DetalleDelPaciente = new(
                AdministracionApiFalsa.PacienteId,
                "Otro paciente",
                20,
                new(AdministracionApiFalsa.SalaId, "Sala 101"),
                []),
        };
        api.PacientesSinDetalle.Add(otro);
        Services.AddSingleton<IAdministracionApi>(api);
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());

        var cut = Render<Pacientes>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Paciente sin detalle", cut.Markup);
            Assert.Contains("—", cut.Markup);
            Assert.Contains(">0<", cut.Markup);
        });
    }

    [Fact]
    public void Un_nutricionista_con_una_dieta_la_mantiene_al_limpiar()
    {
        ComoNutricionista();
        var api = new AdministracionApiFalsa();
        Services.AddSingleton<IAdministracionApi>(api);
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());
        var cut = Render<Pacientes>();
        cut.WaitForAssertion(() => Assert.Contains("Yudivián Almeida", cut.Markup));

        cut.FindAll("button").Single(b => b.TextContent == "Limpiar").Click();

        cut.WaitForAssertion(() => Assert.Equal(
            (null, CatalogoApiFalsa.DietaId, 1, 10),
            api.UltimaConsultaPacientes));
    }

    [Fact]
    public void Paginar_avanza_y_retrocede()
    {
        ComoAdmin();
        var pacientes = Enumerable.Range(1, 11)
            .Select(i => new Paciente(Guid.NewGuid(), $"Paciente {i}", 30, AdministracionApiFalsa.SalaId))
            .ToList();
        var api = new AdministracionApiFalsa { Pacientes = pacientes };
        Services.AddSingleton<IAdministracionApi>(api);
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());
        var cut = Render<Pacientes>();
        cut.WaitForAssertion(() => Assert.Contains("Paciente 1", cut.Markup));

        cut.FindAll("button").Single(b => b.TextContent == "Siguiente").Click();
        cut.WaitForAssertion(() => Assert.Equal(2, api.UltimaConsultaPacientes!.Value.Page));
        cut.WaitForAssertion(() => Assert.Contains("Siguiente", cut.Markup));

        cut.FindAll("button").Single(b => b.TextContent == "Anterior").Click();

        cut.WaitForAssertion(() => Assert.Equal(1, api.UltimaConsultaPacientes!.Value.Page));
    }

    private void ComoAdmin()
        => AddAuthorization().SetAuthorized("admin@centro.test").SetRoles(RolesDeUsuario.Admin);

    private void ComoNutricionista()
        => AddAuthorization().SetAuthorized("nutri@centro.test")
            .SetRoles(RolesDeUsuario.Nutricionista)
            .SetClaims(new Claim("entidad_id", AdministracionApiFalsa.NutricionistaId.ToString()));
}
