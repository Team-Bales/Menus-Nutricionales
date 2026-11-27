using System.Net;
using System.Security.Claims;
using Bunit;
using Bunit.TestDoubles;
using Menus.Client.Pages;
using Menus.Client.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Client.Tests.Pages;

/// <summary>Pruebas del detalle de paciente del issue #77.</summary>
public sealed class DetallePacienteTests : BunitContext
{
    [Fact]
    public void Muestra_los_datos_las_dietas_y_permite_inscribir()
    {
        ComoAdmin();
        var api = new AdministracionApiFalsa
        {
            DetalleDelPaciente = new(
                AdministracionApiFalsa.PacienteId,
                "Yudivián Almeida",
                45,
                new(AdministracionApiFalsa.SalaId, "Sala 101"),
                []),
        };
        Servicios(api);

        var cut = Render<DetallePaciente>(ps => ps.Add(p => p.Id, AdministracionApiFalsa.PacienteId));
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Yudivián Almeida", cut.Markup);
            Assert.Contains("Sala 101", cut.Markup);
            Assert.Contains("Dieta basal", cut.Markup);
        });

        cut.Find("#dieta").Change(CatalogoApiFalsa.DietaId.ToString());
        cut.FindAll("button").Single(b => b.TextContent == "Inscribir en dieta").Click();

        cut.WaitForAssertion(() => Assert.Equal(
            (AdministracionApiFalsa.PacienteId, CatalogoApiFalsa.DietaId),
            api.UltimaInscripcion));
        cut.WaitForAssertion(() => Assert.Contains("Paciente inscrito en la dieta.", cut.Markup));
    }

    [Fact]
    public void Inscribir_en_una_dieta_ya_inscrita_avisa_sin_llamar_a_la_api()
    {
        ComoAdmin();
        var api = new AdministracionApiFalsa();
        Servicios(api);

        var cut = Render<DetallePaciente>(ps => ps.Add(p => p.Id, AdministracionApiFalsa.PacienteId));
        cut.WaitForAssertion(() => Assert.Contains("Dieta basal", cut.Markup));

        cut.Find("#dieta").Change(CatalogoApiFalsa.DietaId.ToString());
        cut.FindAll("button").Single(b => b.TextContent == "Inscribir en dieta").Click();

        cut.WaitForAssertion(() => Assert.Contains("El paciente ya está inscrito en esa dieta.", cut.Markup));
        Assert.Null(api.UltimaInscripcion);
    }

    [Fact]
    public void Solo_el_admin_puede_desinscribir()
    {
        ComoAdmin();
        var api = new AdministracionApiFalsa();
        Servicios(api);

        var cut = Render<DetallePaciente>(ps => ps.Add(p => p.Id, AdministracionApiFalsa.PacienteId));
        cut.WaitForAssertion(() => Assert.Contains("Dieta basal", cut.Markup));
        cut.FindAll("button").Single(b => b.TextContent == "Desinscribir").Click();

        cut.WaitForAssertion(() => Assert.Equal(
            (AdministracionApiFalsa.PacienteId, CatalogoApiFalsa.DietaId),
            api.UltimaDesinscripcion));
    }

    [Fact]
    public void Un_nutricionista_no_ve_el_boton_de_desinscribir()
    {
        ComoNutricionista();
        Servicios(new AdministracionApiFalsa());

        var cut = Render<DetallePaciente>(ps => ps.Add(p => p.Id, AdministracionApiFalsa.PacienteId));

        cut.WaitForAssertion(() => Assert.Contains("Dieta basal", cut.Markup));
        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent == "Desinscribir");
        Assert.Contains(cut.FindAll("button"), b => b.TextContent == "Inscribir en dieta");
    }

    [Fact]
    public void Un_nutricionista_no_ve_un_paciente_ajeno_a_sus_dietas()
    {
        ComoNutricionista();
        var api = new AdministracionApiFalsa
        {
            Detalle = new(
                AdministracionApiFalsa.NutricionistaId,
                "Ana Pérez",
                CatalogoApiFalsa.Especialidad,
                false,
                [new(Guid.NewGuid())]),
        };
        Servicios(api);

        var cut = Render<DetallePaciente>(ps => ps.Add(p => p.Id, AdministracionApiFalsa.PacienteId));

        cut.WaitForAssertion(() => Assert.Contains("Este paciente no pertenece a las dietas que atiendes.", cut.Markup));
        Assert.DoesNotContain("Yudivián Almeida", cut.Markup);
    }

    [Fact]
    public void Un_error_de_la_api_se_muestra()
    {
        ComoAdmin();
        var api = new AdministracionApiFalsa { Error = new(HttpStatusCode.NotFound, "El paciente no existe.") };
        Servicios(api);

        var cut = Render<DetallePaciente>(ps => ps.Add(p => p.Id, AdministracionApiFalsa.PacienteId));

        cut.WaitForAssertion(() => Assert.Contains("El paciente no existe.", cut.Markup));
    }

    private void Servicios(AdministracionApiFalsa api)
    {
        Services.AddSingleton<IAdministracionApi>(api);
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());
    }

    private void ComoAdmin()
        => AddAuthorization().SetAuthorized("admin@centro.test").SetRoles(RolesDeUsuario.Admin);

    private void ComoNutricionista()
        => AddAuthorization().SetAuthorized("nutri@centro.test")
            .SetRoles(RolesDeUsuario.Nutricionista)
            .SetClaims(new Claim("entidad_id", AdministracionApiFalsa.NutricionistaId.ToString()));
}
