using System.Security.Claims;
using Menus.Client.Services;

namespace Menus.Client.Tests.Services;

/// <summary>Pruebas de <see cref="RolesDeUsuario.EsPersonal"/> (#316).</summary>
public sealed class RolesDeUsuarioTests
{
    private static ClaimsPrincipal Con(params string[] roles)
        => new(new ClaimsIdentity(roles.Select(r => new Claim(ClaimTypes.Role, r)), "prueba"));

    [Theory]
    [InlineData(RolesDeUsuario.Admin)]
    [InlineData(RolesDeUsuario.Nutricionista)]
    public void El_admin_y_el_nutricionista_son_personal(string rol)
    {
        Assert.True(RolesDeUsuario.EsPersonal(Con(rol)));
    }

    [Fact]
    public void El_paciente_no_es_personal()
    {
        Assert.False(RolesDeUsuario.EsPersonal(Con(RolesDeUsuario.Paciente)));
    }

    [Fact]
    public void Sin_roles_no_es_personal()
    {
        Assert.False(RolesDeUsuario.EsPersonal(Con()));
        Assert.False(RolesDeUsuario.EsPersonal(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    [Fact]
    public void Reconoce_al_nutricionista_jefe_desde_el_claim_del_token()
    {
        var jefe = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, RolesDeUsuario.Nutricionista), new Claim(RolesDeUsuario.JefeNutricion, bool.TrueString)],
            "prueba"));

        Assert.True(RolesDeUsuario.EsJefeNutricion(jefe));
        Assert.False(RolesDeUsuario.EsJefeNutricion(Con(RolesDeUsuario.Nutricionista)));
        Assert.False(RolesDeUsuario.EsJefeNutricion(Con(RolesDeUsuario.Admin)));
    }
}
