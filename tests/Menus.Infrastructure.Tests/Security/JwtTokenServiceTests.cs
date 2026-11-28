using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Menus.Domain.Abstractions.Security;
using Menus.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Menus.Infrastructure.Tests.Security;

public sealed class JwtTokenServiceTests
{
    private const string Key = "clave-de-prueba-con-mas-de-32-bytes-1234567890";
    private const string OtherKey = "otra-clave-de-prueba-con-mas-de-32-bytes-098765";
    private const string Issuer = "menus-nutricionales-api";
    private const string Audience = "menus-nutricionales-api";
    private const string OtherAudience = "otra-audiencia";

    private static JwtOptions CreateOptions(
        string key = Key,
        string audience = Audience,
        int expiresMinutes = JwtOptions.DefaultExpiresMinutes,
        int clockSkewSeconds = JwtOptions.DefaultClockSkewSeconds) => new()
    {
        Key = key,
        Issuer = Issuer,
        Audience = audience,
        ExpiresMinutes = expiresMinutes,
        ClockSkewSeconds = clockSkewSeconds,
    };

    private static JwtTokenService CreateService(int expiresMinutes = JwtOptions.DefaultExpiresMinutes) =>
        new(Options.Create(CreateOptions(expiresMinutes: expiresMinutes)));

    private static ITokenEmitter CreateEmitter(int expiresMinutes = JwtOptions.DefaultExpiresMinutes) =>
        new JwtTokenService(Options.Create(CreateOptions(expiresMinutes: expiresMinutes)));

    [Fact]
    public void Emitir_ValidArgs_ContainsExpectedClaims()
    {
        var userId = Guid.NewGuid();
        var entidadId = Guid.NewGuid();

        var tokenEmitido = CreateEmitter().Emitir(
            userId,
            JwtRoles.Nutricionista,
            entidadId,
            "Nutrición clínica",
            esJefeNutricion: true);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokenEmitido.Token);

        Assert.Equal(Issuer, jwt.Issuer);
        Assert.Equal(userId.ToString(), jwt.Claims.Single(c => c.Type == JwtClaimTypes.Subject).Value);
        Assert.Equal(JwtRoles.Nutricionista, jwt.Claims.Single(c => c.Type == JwtClaimTypes.Role).Value);
        Assert.Equal(entidadId.ToString(), jwt.Claims.Single(c => c.Type == JwtClaimTypes.EntidadId).Value);
        Assert.Equal("Nutrición clínica", jwt.Claims.Single(c => c.Type == JwtClaimTypes.Especialidad).Value);
        Assert.Equal(bool.TrueString, jwt.Claims.Single(c => c.Type == JwtClaimTypes.JefeNutricion).Value);
    }

    [Fact]
    public void Emitir_Admin_SinEntidadId_NoIncluyeEntidadIdClaim()
    {
        var tokenEmitido = CreateEmitter().Emitir(Guid.NewGuid(), JwtRoles.Admin);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokenEmitido.Token);

        Assert.DoesNotContain(jwt.Claims, c => c.Type == JwtClaimTypes.EntidadId);
    }

    [Fact]
    public void Emitir_Paciente_ConEntidadId_IncluyeEntidadIdClaim()
    {
        var entidadId = Guid.NewGuid();
        var tokenEmitido = CreateEmitter().Emitir(Guid.NewGuid(), JwtRoles.Paciente, entidadId);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokenEmitido.Token);

        Assert.Equal(entidadId.ToString(), jwt.Claims.Single(c => c.Type == JwtClaimTypes.EntidadId).Value);
    }

    [Fact]
    public void Emitir_NotJefe_DoesNotIncludeJefeClaim()
    {
        var tokenEmitido = CreateEmitter().Emitir(Guid.NewGuid(), JwtRoles.Nutricionista);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokenEmitido.Token);

        Assert.DoesNotContain(jwt.Claims, c => c.Type == JwtClaimTypes.JefeNutricion);
    }

    [Fact]
    public void Emitir_WithoutEspecialidad_DoesNotIncludeEspecialidadClaim()
    {
        var tokenEmitido = CreateEmitter().Emitir(Guid.NewGuid(), JwtRoles.Paciente);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokenEmitido.Token);

        Assert.DoesNotContain(jwt.Claims, c => c.Type == JwtClaimTypes.Especialidad);
    }

    [Fact]
    public void Emitir_ExpiraEnMinutosConfigurados()
    {
        var tokenEmitido = CreateEmitter(expiresMinutes: 15).Emitir(Guid.NewGuid(), JwtRoles.Admin);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokenEmitido.Token);

        Assert.Equal(15, Math.Round((jwt.ValidTo - jwt.ValidFrom).TotalMinutes));
        // ExpiraUtc y ValidTo pueden diferir en milisegundos por la serialización
        Assert.True(Math.Abs((tokenEmitido.ExpiraUtc - jwt.ValidTo).TotalSeconds) < 2);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Desconocido")]
    [InlineData("jefeNutricion")]
    public void Emitir_InvalidRole_ThrowsArgumentException(string role)
    {
        var emitter = CreateEmitter();

        Assert.Throws<ArgumentException>(() => emitter.Emitir(Guid.NewGuid(), role));
    }

    [Fact]
    public void Emitir_JefeWithNonNutricionistaRole_ThrowsArgumentException()
    {
        var emitter = CreateEmitter();

        Assert.Throws<ArgumentException>(
            () => emitter.Emitir(Guid.NewGuid(), JwtRoles.Paciente, esJefeNutricion: true));
    }

    [Fact]
    public void Constructor_KeyMissing_ThrowsInvalidOperationException()
    {
        var options = new JwtOptions { Key = "", Issuer = Issuer, Audience = Audience, ExpiresMinutes = 60 };

        Assert.Throws<InvalidOperationException>(() => new JwtTokenService(Options.Create(options)));
    }

    [Fact]
    public void Constructor_KeyTooShort_ThrowsInvalidOperationException()
    {
        var options = new JwtOptions { Key = "corta", Issuer = Issuer, Audience = Audience, ExpiresMinutes = 60 };

        Assert.Throws<InvalidOperationException>(() => new JwtTokenService(Options.Create(options)));
    }

    [Fact]
    public void Emitir_IncluyeLaAudienciaConfigurada()
    {
        var tokenEmitido = CreateEmitter().Emitir(Guid.NewGuid(), JwtRoles.Admin);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokenEmitido.Token);

        Assert.Contains(Audience, jwt.Audiences);
    }

    [Fact]
    public void Emitir_PorDefecto_ExpiraEnQuinceMinutos()
    {
        var tokenEmitido = CreateEmitter().Emitir(Guid.NewGuid(), JwtRoles.Admin);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokenEmitido.Token);

        Assert.Equal(JwtOptions.DefaultExpiresMinutes, Math.Round((jwt.ValidTo - jwt.ValidFrom).TotalMinutes));
    }

    [Fact]
    public void Validar_token_con_audiencia_distinta_lanza_SecurityTokenInvalidAudienceException()
    {
        var options = CreateOptions();
        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: OtherAudience,
            claims: [new Claim(JwtClaimTypes.Subject, Guid.NewGuid().ToString())],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)),
                SecurityAlgorithms.HmacSha256));
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };

        Assert.Throws<SecurityTokenInvalidAudienceException>(() => handler.ValidateToken(
            new JwtSecurityTokenHandler().WriteToken(token),
            JwtBearerConfiguration.CreateTokenValidationParameters(options),
            out _));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public void TieneClaimsObligatorios_rechaza_tokens_sin_sub_o_role(bool conSubject, bool conRole)
    {
        var claims = new List<Claim>();
        if (conSubject)
        {
            claims.Add(new Claim(JwtClaimTypes.Subject, Guid.NewGuid().ToString()));
        }

        if (conRole)
        {
            claims.Add(new Claim(JwtClaimTypes.Role, JwtRoles.Admin));
        }

        Assert.False(JwtBearerConfiguration.TieneClaimsObligatorios(
            new ClaimsPrincipal(new ClaimsIdentity(claims))));
    }

    [Fact]
    public void TieneClaimsObligatorios_acepta_sub_y_role()
    {
        Claim[] claims =
        [
            new(JwtClaimTypes.Subject, Guid.NewGuid().ToString()),
            new(JwtClaimTypes.Role, JwtRoles.Admin),
        ];

        Assert.True(JwtBearerConfiguration.TieneClaimsObligatorios(
            new ClaimsPrincipal(new ClaimsIdentity(claims))));
    }

    [Fact]
    public void CreateTokenValidationParameters_UsaLaConfiguracionEndurecida()
    {
        var parametros = JwtBearerConfiguration.CreateTokenValidationParameters(
            CreateOptions(clockSkewSeconds: 10));

        Assert.True(parametros.ValidateAudience);
        Assert.Equal(Audience, parametros.ValidAudience);
        Assert.Equal(TimeSpan.FromSeconds(10), parametros.ClockSkew);
        Assert.True(parametros.RequireExpirationTime);
        Assert.True(parametros.RequireSignedTokens);
    }

    [Fact]
    public void Validate_AudienceVacia_LanzaInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(
            () => JwtBearerConfiguration.Validate(CreateOptions(audience: "")));
    }

    [Fact]
    public void Validate_ClockSkewNegativo_LanzaInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(
            () => JwtBearerConfiguration.Validate(CreateOptions(clockSkewSeconds: -1)));
    }

    [Fact]
    public void Validate_ClockSkewExcesivo_LanzaInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(
            () => JwtBearerConfiguration.Validate(
                CreateOptions(clockSkewSeconds: JwtOptions.MaxClockSkewSeconds + 1)));
    }

    [Fact]
    public void Validate_VigenciaExcesiva_LanzaInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(
            () => JwtBearerConfiguration.Validate(
                CreateOptions(expiresMinutes: JwtOptions.MaxExpiresMinutes + 1)));
    }

    [Fact]
    public void Emitir_ValidatesWithConfiguredParameters()
    {
        var options = CreateOptions();
        var emitter = new JwtTokenService(Options.Create(options));
        var userId = Guid.NewGuid();
        var tokenEmitido = emitter.Emitir(userId, JwtRoles.Nutricionista, null, "Pediatría");

        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(
            tokenEmitido.Token,
            JwtBearerConfiguration.CreateTokenValidationParameters(options),
            out var validatedToken);

        Assert.NotNull(validatedToken);
        Assert.Equal(userId.ToString(), principal.FindFirst(JwtClaimTypes.Subject)?.Value);
        Assert.Equal(JwtRoles.Nutricionista, principal.FindFirst(JwtClaimTypes.Role)?.Value);
        Assert.Equal("Pediatría", principal.FindFirst(JwtClaimTypes.Especialidad)?.Value);
    }

    [Fact]
    public void Emitir_ValidatedWithAnotherKey_ThrowsSecurityTokenException()
    {
        var emitter = CreateEmitter();
        var tokenEmitido = emitter.Emitir(Guid.NewGuid(), JwtRoles.Admin);
        var otherOptions = CreateOptions(key: OtherKey);

        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };

        Assert.ThrowsAny<SecurityTokenException>(() => handler.ValidateToken(
            tokenEmitido.Token,
            JwtBearerConfiguration.CreateTokenValidationParameters(otherOptions),
            out _));
    }

    [Fact]
    public void Validar_token_expirado_lanza_SecurityTokenExpiredException()
    {
        var options = CreateOptions();
        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)),
            SecurityAlgorithms.HmacSha256);

        var ahora = DateTime.UtcNow;
        var tokenExpirado = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: [new Claim(JwtClaimTypes.Subject, Guid.NewGuid().ToString())],
            notBefore: ahora.AddMinutes(-10),
            expires: ahora.AddMinutes(-5), // fuera del ClockSkew configurado (30 s)
            signingCredentials: credenciales);
        var token = new JwtSecurityTokenHandler().WriteToken(tokenExpirado);

        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };

        Assert.Throws<SecurityTokenExpiredException>(() => handler.ValidateToken(
            token,
            JwtBearerConfiguration.CreateTokenValidationParameters(options),
            out _));
    }

    [Fact]
    public void Validar_token_expone_el_rol_para_IsInRole()
    {
        var options = CreateOptions();
        var emitter = new JwtTokenService(Options.Create(options));
        var userId = Guid.NewGuid();
        var tokenEmitido = emitter.Emitir(userId, JwtRoles.Nutricionista, null, "Pediatría");

        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(
            tokenEmitido.Token,
            JwtBearerConfiguration.CreateTokenValidationParameters(options),
            out var validatedToken);

        Assert.NotNull(validatedToken);
        Assert.True(principal.IsInRole(JwtRoles.Nutricionista));
        Assert.False(principal.IsInRole(JwtRoles.Paciente));
        Assert.Equal(userId.ToString(), principal.Identity!.Name);
    }

    // Tests legacy para compatibilidad hacia atrás
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
    public void Validar_token_expone_el_rol_para_IsInRole_Legacy()
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
