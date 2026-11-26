using Menus.Domain;

namespace Menus.Domain.Tests;

public sealed class RolesUsuarioTests
{
    [Theory]
    [InlineData(RolesUsuario.Admin)]
    [InlineData(RolesUsuario.Nutricionista)]
    [InlineData(RolesUsuario.Paciente)]
    public void EsValido_RolesValidos_RetornaTrue(string rol)
    {
        Assert.True(RolesUsuario.EsValido(rol));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Admin ")]
    [InlineData("admin")]
    [InlineData("NUTRICIONISTA")]
    [InlineData("Invalido")]
    [InlineData(null)]
    public void EsValido_RolesInvalidos_RetornaFalse(string? rol)
    {
        Assert.False(RolesUsuario.EsValido(rol));
    }

    [Fact]
    public void Personal_son_el_nutricionista_y_el_admin()
    {
        Assert.Equal(new[] { RolesUsuario.Nutricionista, RolesUsuario.Admin }, RolesUsuario.Personal);
    }

    [Theory]
    [InlineData(RolesUsuario.Admin)]
    [InlineData(RolesUsuario.Nutricionista)]
    public void EsPersonal_el_nutricionista_y_el_admin_lo_son(string rol)
    {
        Assert.True(RolesUsuario.EsPersonal(rol));
    }

    [Theory]
    [InlineData(RolesUsuario.Paciente)]
    [InlineData("")]
    [InlineData("admin")]
    [InlineData("Admin ")]
    [InlineData("Invalido")]
    [InlineData(null)]
    public void EsPersonal_el_paciente_y_los_roles_invalidos_no_lo_son(string? rol)
    {
        Assert.False(RolesUsuario.EsPersonal(rol));
    }
}
