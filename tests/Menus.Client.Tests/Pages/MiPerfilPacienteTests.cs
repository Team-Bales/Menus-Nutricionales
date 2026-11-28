using System.Net;
using System.Security.Claims;
using Bunit;
using Bunit.TestDoubles;
using Menus.Client.Pages;
using Menus.Client.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Client.Tests.Pages;

/// <summary>Pruebas de la vista de solo lectura del propio paciente del issue #77.</summary>
public sealed class MiPerfilPacienteTests : BunitContext
{
    [Fact]
    public void Muestra_sus_datos_su_sala_y_sus_dietas_sin_acciones_de_edicion()
    {
        ComoPaciente();
        Services.AddSingleton<IAdministracionApi>(new AdministracionApiFalsa());

        var cut = Render<MiPerfilPaciente>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Yudivián Almeida", cut.Markup);
            Assert.Contains("Sala 101", cut.Markup);
            Assert.Contains("Dieta basal", cut.Markup);
        });
        Assert.DoesNotContain("Inscribir", cut.Markup);
        Assert.DoesNotContain("Desinscribir", cut.Markup);
    }

    [Fact]
    public void Sin_entidad_id_muestra_un_error()
    {
        AddAuthorization().SetAuthorized("paciente@centro.test").SetRoles(RolesDeUsuario.Paciente);
        Services.AddSingleton<IAdministracionApi>(new AdministracionApiFalsa());

        var cut = Render<MiPerfilPaciente>();

        cut.WaitForAssertion(() => Assert.Contains("La sesión no identifica al paciente.", cut.Markup));
    }

    [Fact]
    public void Sin_dietas_muestra_ninguna()
    {
        ComoPaciente();
        Services.AddSingleton<IAdministracionApi>(new AdministracionApiFalsa
        {
            DetalleDelPaciente = new(
                AdministracionApiFalsa.PacienteId,
                "Yudivián Almeida",
                45,
                new(AdministracionApiFalsa.SalaId, "Sala 101"),
                []),
        });

        var cut = Render<MiPerfilPaciente>();

        cut.WaitForAssertion(() => Assert.Contains("Ninguna.", cut.Markup));
    }

    [Fact]
    public void Un_error_de_la_api_se_muestra()
    {
        ComoPaciente();
        Services.AddSingleton<IAdministracionApi>(new AdministracionApiFalsa
        {
            Error = new(HttpStatusCode.InternalServerError, "No se pudo cargar tu ficha."),
        });

        var cut = Render<MiPerfilPaciente>();

        cut.WaitForAssertion(() => Assert.Contains("No se pudo cargar tu ficha.", cut.Markup));
    }

    private void ComoPaciente()
        => AddAuthorization().SetAuthorized("paciente@centro.test")
            .SetRoles(RolesDeUsuario.Paciente)
            .SetClaims(new Claim("entidad_id", AdministracionApiFalsa.PacienteId.ToString()));
}
