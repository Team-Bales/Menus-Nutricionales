using Menus.Domain.Abstractions.Security;
using Menus.Infrastructure.Security;

namespace Menus.Infrastructure.Tests.Security;

public sealed class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_PasswordValido_RetornaHashNoVacio()
    {
        var hash = _hasher.Hash("miPassword123");

        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.StartsWith("$2a$12$", hash); // BCrypt estándar con work factor 12
    }

    [Fact]
    public void Hash_MismoPassword_GeneraHashesDistintos()
    {
        var hash1 = _hasher.Hash("miPassword123");
        var hash2 = _hasher.Hash("miPassword123");

        Assert.NotEqual(hash1, hash2); // Salt diferente cada vez
    }

    [Fact]
    public void Verify_PasswordCorrecto_RetornaTrue()
    {
        var password = "miPassword123";
        var hash = _hasher.Hash(password);

        var resultado = _hasher.Verify(password, hash);

        Assert.True(resultado);
    }

    [Fact]
    public void Verify_PasswordIncorrecto_RetornaFalse()
    {
        var hash = _hasher.Hash("miPassword123");

        var resultado = _hasher.Verify("otroPassword", hash);

        Assert.False(resultado);
    }

    [Fact]
    public void Verify_HashMalformado_RetornaFalse()
    {
        var resultado = _hasher.Verify("password", "no-es-un-hash-valido");

        Assert.False(resultado);
    }

    [Fact]
    public void Verify_PasswordVacio_LanzaExcepcion()
    {
        var hash = _hasher.Hash("password");

        Assert.Throws<ArgumentException>(() => _hasher.Verify("", hash));
        Assert.Throws<ArgumentException>(() => _hasher.Verify("   ", hash));
    }

    [Fact]
    public void Verify_HashVacio_LanzaExcepcion()
    {
        Assert.Throws<ArgumentException>(() => _hasher.Verify("password", ""));
        Assert.Throws<ArgumentException>(() => _hasher.Verify("password", "   "));
    }

    [Fact]
    public void VerifyDummy_EjecutaSinLanzar()
    {
        // No debe lanzar, solo consumir CPU equivalente a una verificación real
        _hasher.VerifyDummy("cualquierPassword");
    }

    [Fact]
    public void VerifyDummy_PasswordVacio_LanzaExcepcion()
    {
        Assert.Throws<ArgumentException>(() => _hasher.VerifyDummy(""));
        Assert.Throws<ArgumentException>(() => _hasher.VerifyDummy("   "));
    }
}