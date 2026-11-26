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
}
