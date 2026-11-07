using Menus.Domain.Entidades;
using Menus.Domain.Excepciones;
using Menus.Domain;

namespace Menus.Domain.Tests.Entidades;

public sealed class UsuarioTests
{
    [Fact]
    public void Crear_Admin_SinEntidadId_EsValido()
    {
        var id = Guid.NewGuid();
        var usuario = Usuario.Crear(id, "admin@test.com", "hash", RolesUsuario.Admin, null);

        Assert.Equal(id, usuario.Id);
        Assert.Equal("admin@test.com", usuario.Email);
        Assert.Equal("hash", usuario.PasswordHash);
        Assert.Equal(RolesUsuario.Admin, usuario.Rol);
        Assert.Null(usuario.EntidadId);
    }

    [Fact]
    public void Crear_Nutricionista_ConEntidadId_EsValido()
    {
        var id = Guid.NewGuid();
        var entidadId = Guid.NewGuid();
        var usuario = Usuario.Crear(id, "nutri@test.com", "hash", RolesUsuario.Nutricionista, entidadId);

        Assert.Equal(entidadId, usuario.EntidadId);
    }

    [Fact]
    public void Crear_Paciente_ConEntidadId_EsValido()
    {
        var id = Guid.NewGuid();
        var entidadId = Guid.NewGuid();
        var usuario = Usuario.Crear(id, "paciente@test.com", "hash", RolesUsuario.Paciente, entidadId);

        Assert.Equal(entidadId, usuario.EntidadId);
    }

    [Fact]
    public void Crear_Admin_ConEntidadId_LanzaExcepcion()
    {
        var id = Guid.NewGuid();
        var entidadId = Guid.NewGuid();

        var ex = Assert.Throws<DatoInvalidoException>(() =>
            Usuario.Crear(id, "admin@test.com", "hash", RolesUsuario.Admin, entidadId));

        Assert.Equal("entidadId", ex.Campo);
    }

    [Fact]
    public void Crear_Nutricionista_SinEntidadId_LanzaExcepcion()
    {
        var id = Guid.NewGuid();

        var ex = Assert.Throws<DatoInvalidoException>(() =>
            Usuario.Crear(id, "nutri@test.com", "hash", RolesUsuario.Nutricionista, null));

        Assert.Equal("entidadId", ex.Campo);
    }

    [Fact]
    public void Crear_Paciente_SinEntidadId_LanzaExcepcion()
    {
        var id = Guid.NewGuid();

        var ex = Assert.Throws<DatoInvalidoException>(() =>
            Usuario.Crear(id, "paciente@test.com", "hash", RolesUsuario.Paciente, null));

        Assert.Equal("entidadId", ex.Campo);
    }

    [Fact]
    public void Crear_RolInvalido_LanzaExcepcion()
    {
        var id = Guid.NewGuid();

        var ex = Assert.Throws<DatoInvalidoException>(() =>
            Usuario.Crear(id, "test@test.com", "hash", "Invalido", null));

        Assert.Equal("rol", ex.Campo);
    }

    [Fact]
    public void Crear_EmailVacio_LanzaExcepcion()
    {
        var id = Guid.NewGuid();

        var ex = Assert.Throws<DatoInvalidoException>(() =>
            Usuario.Crear(id, "   ", "hash", RolesUsuario.Admin, null));

        Assert.Equal("email", ex.Campo);
    }

    [Fact]
    public void Crear_PasswordHashVacio_LanzaExcepcion()
    {
        var id = Guid.NewGuid();

        var ex = Assert.Throws<DatoInvalidoException>(() =>
            Usuario.Crear(id, "test@test.com", "   ", RolesUsuario.Admin, null));

        Assert.Equal("passwordHash", ex.Campo);
    }

    [Fact]
    public void NormalizarEmail_TrimYLowercase()
    {
        var normalizado = Usuario.NormalizarEmail("  TEST@EXAMPLE.COM  ");

        Assert.Equal("test@example.com", normalizado);
    }

    [Fact]
    public void ActualizarPasswordHash_CambiaElHash()
    {
        var usuario = Usuario.Crear(Guid.NewGuid(), "test@test.com", "hashViejo", RolesUsuario.Admin, null);

        usuario.ActualizarPasswordHash("hashNuevo");

        Assert.Equal("hashNuevo", usuario.PasswordHash);
    }

    [Fact]
    public void ActualizarPasswordHash_Vacio_LanzaExcepcion()
    {
        var usuario = Usuario.Crear(Guid.NewGuid(), "test@test.com", "hashViejo", RolesUsuario.Admin, null);

        var ex = Assert.Throws<DatoInvalidoException>(() =>
            usuario.ActualizarPasswordHash("   "));

        Assert.Equal("nuevoPasswordHash", ex.Campo);
    }

    [Fact]
    public void Igualdad_PorId()
    {
        var id = Guid.NewGuid();
        var u1 = Usuario.Crear(id, "a@a.com", "hash", RolesUsuario.Admin, null);
        var u2 = Usuario.Crear(id, "b@b.com", "hash", RolesUsuario.Nutricionista, Guid.NewGuid());

        Assert.Equal(u1, u2);
        Assert.True(u1 == u2);
    }
}