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

    private void ComoPaciente()
        => AddAuthorization().SetAuthorized("paciente@centro.test")
            .SetRoles(RolesDeUsuario.Paciente)
            .SetClaims(new Claim("entidad_id", AdministracionApiFalsa.PacienteId.ToString()));
}
