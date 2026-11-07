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
}
