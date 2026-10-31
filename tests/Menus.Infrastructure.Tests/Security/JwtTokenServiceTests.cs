using System.IdentityModel.Tokens.Jwt;
using Menus.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Menus.Infrastructure.Tests.Security;

public sealed class JwtTokenServiceTests
{
    private const string Key = "clave-de-prueba-con-mas-de-32-bytes-1234567890";
    private const string OtherKey = "otra-clave-de-prueba-con-mas-de-32-bytes-098765";
    private const string Issuer = "menus-nutricionales-api";

    private static JwtOptions CreateOptions(string key = Key, int expiresMinutes = 60) => new()
    {
        Key = key,
        Issuer = Issuer,
        ExpiresMinutes = expiresMinutes,
    };

    private static JwtTokenService CreateService(int expiresMinutes = 60) =>
        new(Options.Create(CreateOptions(expiresMinutes: expiresMinutes)));

    [Fact]
    public void GenerateToken_ValidArgs_ContainsExpectedClaims()
    {
        var userId = Guid.NewGuid();

        var token = CreateService().GenerateToken(
            userId,
            JwtRoles.Nutricionista,
            "Nutrición clínica",
            esJefeNutricion: true);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(Issuer, jwt.Issuer);
        Assert.Equal(userId.ToString(), jwt.Claims.Single(c => c.Type == JwtClaimTypes.Subject).Value);
        Assert.Equal(JwtRoles.Nutricionista, jwt.Claims.Single(c => c.Type == JwtClaimTypes.Role).Value);
        Assert.Equal("Nutrición clínica", jwt.Claims.Single(c => c.Type == JwtClaimTypes.Especialidad).Value);
        Assert.Equal(bool.TrueString, jwt.Claims.Single(c => c.Type == JwtClaimTypes.JefeNutricion).Value);
    }

    [Fact]
    public void GenerateToken_NotJefe_DoesNotIncludeJefeClaim()
    {
        var token = CreateService().GenerateToken(Guid.NewGuid(), JwtRoles.Nutricionista);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.DoesNotContain(jwt.Claims, c => c.Type == JwtClaimTypes.JefeNutricion);
    }

    [Fact]
    public void GenerateToken_WithoutEspecialidad_DoesNotIncludeEspecialidadClaim()
    {
        var token = CreateService().GenerateToken(Guid.NewGuid(), JwtRoles.Paciente);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.DoesNotContain(jwt.Claims, c => c.Type == JwtClaimTypes.Especialidad);
    }

    [Fact]
    public void GenerateToken_ExpiresAtConfiguredMinutes()
    {
        var token = CreateService(expiresMinutes: 15).GenerateToken(Guid.NewGuid(), JwtRoles.Admin);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(15, Math.Round((jwt.ValidTo - jwt.ValidFrom).TotalMinutes));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Desconocido")]
    [InlineData("jefeNutricion")]
    public void GenerateToken_InvalidRole_ThrowsArgumentException(string role)
    {
        var service = CreateService();

        Assert.Throws<ArgumentException>(() => service.GenerateToken(Guid.NewGuid(), role));
    }

    [Fact]
    public void GenerateToken_JefeWithNonNutricionistaRole_ThrowsArgumentException()
    {
        var service = CreateService();

        Assert.Throws<ArgumentException>(
            () => service.GenerateToken(Guid.NewGuid(), JwtRoles.Paciente, esJefeNutricion: true));
    }

    [Fact]
    public void Constructor_KeyMissing_ThrowsInvalidOperationException()
    {
        var options = new JwtOptions { Key = "", Issuer = Issuer, ExpiresMinutes = 60 };

        Assert.Throws<InvalidOperationException>(() => new JwtTokenService(Options.Create(options)));
    }

    [Fact]
    public void Constructor_KeyTooShort_ThrowsInvalidOperationException()
    {
        var options = new JwtOptions { Key = "corta", Issuer = Issuer, ExpiresMinutes = 60 };

        Assert.Throws<InvalidOperationException>(() => new JwtTokenService(Options.Create(options)));
    }

    [Fact]
    public void GenerateToken_ValidatesWithConfiguredParameters()
    {
        var options = CreateOptions();
        var service = new JwtTokenService(Options.Create(options));
        var userId = Guid.NewGuid();
        var token = service.GenerateToken(userId, JwtRoles.Nutricionista, "Pediatría");

        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(
            token,
            JwtBearerConfiguration.CreateTokenValidationParameters(options),
            out var validatedToken);

        Assert.NotNull(validatedToken);
        Assert.Equal(userId.ToString(), principal.FindFirst(JwtClaimTypes.Subject)?.Value);
        Assert.Equal(JwtRoles.Nutricionista, principal.FindFirst(JwtClaimTypes.Role)?.Value);
        Assert.Equal("Pediatría", principal.FindFirst(JwtClaimTypes.Especialidad)?.Value);
    }

    [Fact]
    public void GenerateToken_ValidatedWithAnotherKey_ThrowsSecurityTokenException()
    {
        var service = CreateService();
        var token = service.GenerateToken(Guid.NewGuid(), JwtRoles.Admin);
        var otherOptions = CreateOptions(key: OtherKey);

        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };

        Assert.ThrowsAny<SecurityTokenException>(() => handler.ValidateToken(
            token,
            JwtBearerConfiguration.CreateTokenValidationParameters(otherOptions),
            out _));
    }

    [Fact]
    public void Validar_token_expone_el_rol_para_IsInRole()
    {
        var options = CreateOptions();
        var service = new JwtTokenService(Options.Create(options));
        var userId = Guid.NewGuid();
        var token = service.GenerateToken(userId, JwtRoles.Nutricionista, "Pediatría");

        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(
            token,
            JwtBearerConfiguration.CreateTokenValidationParameters(options),
            out var validatedToken);

        Assert.NotNull(validatedToken);
        Assert.True(principal.IsInRole(JwtRoles.Nutricionista));
        Assert.False(principal.IsInRole(JwtRoles.Paciente));
        Assert.Equal(userId.ToString(), principal.Identity!.Name);
    }
}
