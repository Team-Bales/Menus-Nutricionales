using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Menus.Api;
using Menus.Application.Auth;
using Menus.Application.Auth.RegistroNutricionista;
using Menus.Application.Auth.RegistroPaciente;
using Menus.Application.Comun;
using Menus.Domain;
using Menus.Domain.Abstractions.Security;
using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Menus.Infrastructure.Persistence;
using Menus.Infrastructure.Persistence.Repositories;
using Menus.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Auth;

/// <summary>
/// Pruebas de integración end-to-end para los endpoints de autenticación.
/// Usan Testcontainers para PostgreSQL y WebApplicationFactory para la API.
/// </summary>
[Collection("PostgreSQL")]
public sealed class AuthEndpointsTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    // Configuración JWT de prueba (clave de 32+ bytes para HS256)
    private const string TestJwtKey = "clave-secreta-de-prueba-que-es-suficientemente-larga-para-hs256";
    private const string TestJwtIssuer = "Menus.Test";
    private const string TestJwtExpiresMinutes = "60";

    public AuthEndpointsTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ReiniciarAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                // Configurar JWT ANTES de que Program ejecute AddInfrastructure.
                // UseSetting establece valores en la configuración del host que WebApplication.CreateBuilder recoge.
                builder.UseSetting("Jwt:Key", TestJwtKey);
                builder.UseSetting("Jwt:Issuer", TestJwtIssuer);
                builder.UseSetting("Jwt:ExpiresMinutes", TestJwtExpiresMinutes);
                // No configurar AuthBootstrap para que no interfiera

                builder.ConfigureTestServices(services =>
                {
                    // Sobrescribir la configuración de BD para usar la del fixture
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<MenusDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<MenusDbContext>(options =>
                        options.UseNpgsql(_fixture.CadenaDeConexion));
                });
            });

        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private static string GenerarTokenAdmin(Guid usuarioId, Guid? entidadId = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtClaimTypes.Subject, usuarioId.ToString()),
            new(JwtClaimTypes.Role, RolesUsuario.Admin),
        };
        if (entidadId.HasValue)
        {
            claims.Add(new Claim(JwtClaimTypes.EntidadId, entidadId.Value.ToString()));
        }
        var token = new JwtSecurityToken(
            issuer: TestJwtIssuer,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerarTokenNutricionista(Guid usuarioId, Guid entidadId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtClaimTypes.Subject, usuarioId.ToString()),
            new(JwtClaimTypes.Role, RolesUsuario.Nutricionista),
            new(JwtClaimTypes.EntidadId, entidadId.ToString()),
        };
        var token = new JwtSecurityToken(
            issuer: TestJwtIssuer,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static async Task AssertCredencialesInvalidasAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status401Unauthorized, problem.Status);
        Assert.Equal("Unauthorized", problem.Title);
        Assert.Equal("Credenciales inválidas.", problem.Detail);
    }

    [Fact]
    public async Task Login_EmailInexistente_Devuelve401Indistinguible()
    {
        var request = new LoginCommand("noexiste@test.com", "password123");

        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        await AssertCredencialesInvalidasAsync(response);
    }

    [Fact]
    public async Task Login_PasswordIncorrecta_Devuelve401Indistinguible()
    {
        // Crear un usuario primero
        var usuarioId = Guid.NewGuid();
        var passwordHasher = new BCryptPasswordHasher();
        var passwordHash = passwordHasher.Hash("passwordCorrecto");

        await using (var ctx = _fixture.CrearContexto())
        {
            var usuario = Usuario.Crear(usuarioId, "test@test.com", passwordHash, RolesUsuario.Admin, null);
            ctx.Usuarios.Add(usuario);
            await ctx.SaveChangesAsync();
        }

        var request = new LoginCommand("test@test.com", "passwordIncorrecto");

        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        await AssertCredencialesInvalidasAsync(response);
    }

    [Fact]
    public async Task Login_CredencialesCorrectas_DevuelveTokenYRol()
    {
        var usuarioId = Guid.NewGuid();
        var passwordHasher = new BCryptPasswordHasher();
        var passwordHash = passwordHasher.Hash("password123");

        await using (var ctx = _fixture.CrearContexto())
        {
            var usuario = Usuario.Crear(usuarioId, "test@test.com", passwordHash, RolesUsuario.Admin, null);
            ctx.Usuarios.Add(usuario);
            await ctx.SaveChangesAsync();
        }

        var request = new LoginCommand("test@test.com", "password123");

        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<LoginResult>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.Token));
        Assert.Equal(RolesUsuario.Admin, result.Rol);
        Assert.True(result.Expira > DateTime.UtcNow);
    }

    [Fact]
    public async Task RegistroNutricionista_SinToken_Devuelve401()
    {
        var request = new RegistrarNutricionistaCommand("Dr. Nutri", "nutri@test.com", "password123", Guid.NewGuid());

        var response = await _client.PostAsJsonAsync("/api/auth/registro/nutricionista", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RegistroNutricionista_TokenNoAdmin_Devuelve403()
    {
        var usuarioId = Guid.NewGuid();
        var entidadId = Guid.NewGuid();
        var token = GenerarTokenNutricionista(usuarioId, entidadId);

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var request = new RegistrarNutricionistaCommand("Dr. Nutri", "nutri@test.com", "password123", Guid.NewGuid());

        var response = await _client.PostAsJsonAsync("/api/auth/registro/nutricionista", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task RegistroNutricionista_TokenAdmin_CreaYRetorna201()
    {
        // Crear especialidad primero
        var especialidadId = Guid.NewGuid();
        await using (var ctx = _fixture.CrearContexto())
        {
            var especialidad = new Especialidad(especialidadId, $"Especialidad Auth {especialidadId:N}");
            ctx.Especialidades.Add(especialidad);
            await ctx.SaveChangesAsync();
        }

        var adminId = Guid.NewGuid();
        var token = GenerarTokenAdmin(adminId);

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var request = new RegistrarNutricionistaCommand("Dr. Nutri", "nutri@test.com", "password123", especialidadId);

        var response = await _client.PostAsJsonAsync("/api/auth/registro/nutricionista", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<IdCreadoDto>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);

        // Verificar que se persistió en BD
        await using (var ctx = _fixture.CrearContexto())
        {
            var nutricionista = await ctx.Nutricionistas.FirstOrDefaultAsync(n => n.Id == result.Id);
            Assert.NotNull(nutricionista);
            Assert.Equal("Dr. Nutri", nutricionista.Nombre);
            Assert.Equal(especialidadId, nutricionista.EspecialidadId);

            var usuario = await ctx.Usuarios.FirstOrDefaultAsync(u => u.EntidadId == result.Id && u.Rol == RolesUsuario.Nutricionista);
            Assert.NotNull(usuario);
            Assert.Equal("nutri@test.com", usuario.Email);
        }

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task RegistroPaciente_TokenAdmin_CreaYRetorna201()
    {
        // Crear sala primero
        var salaId = Guid.NewGuid();
        await using (var ctx = _fixture.CrearContexto())
        {
            var sala = new Sala(salaId, "Sala 1");
            ctx.Salas.Add(sala);
            await ctx.SaveChangesAsync();
        }

        var adminId = Guid.NewGuid();
        var token = GenerarTokenAdmin(adminId);

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var request = new RegistrarPacienteCommand("Paciente Test", "paciente@test.com", "password123", 30, salaId);

        var response = await _client.PostAsJsonAsync("/api/auth/registro/paciente", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<IdCreadoDto>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);

        // Verificar que se persistió en BD
        await using (var ctx = _fixture.CrearContexto())
        {
            var paciente = await ctx.Pacientes.FirstOrDefaultAsync(p => p.Id == result.Id);
            Assert.NotNull(paciente);
            Assert.Equal("Paciente Test", paciente.Nombre);
            Assert.Equal(30, paciente.Edad);
            Assert.Equal(salaId, paciente.SalaId);

            var usuario = await ctx.Usuarios.FirstOrDefaultAsync(u => u.EntidadId == result.Id && u.Rol == RolesUsuario.Paciente);
            Assert.NotNull(usuario);
            Assert.Equal("paciente@test.com", usuario.Email);
        }

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task BootstrapAdmin_Idempotente_PrimeraCreaSegundaNoDuplica()
    {
        // Este test verifica el comportamiento del AuthBootstrapService
        // Usamos una configuración efímera para el test
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AuthBootstrap:Email"] = "bootstrap@test.com",
                ["AuthBootstrap:Password"] = "bootstrap123",
                ["Jwt:Key"] = "clave-secreta-de-prueba-que-es-suficientemente-larga-para-hs256",
                ["Jwt:Issuer"] = "Menus.Test",
                ["Jwt:ExpiresMinutes"] = "60",
            })
            .Build();

        await using var provider = CrearBootstrapServiceProvider(config);

        // scope1: primera ejecución del bootstrap
        using (var scope1 = provider.CreateScope())
        {
            var bootstrap1 = scope1.ServiceProvider.GetRequiredService<Menus.Api.Auth.AuthBootstrapService>();
            await bootstrap1.StartAsync(CancellationToken.None);
        }

        // scope2 NUEVO: leer Admin con contexto fresco
        Guid adminId1;
        using (var scope2 = provider.CreateScope())
        {
            var ctx = scope2.ServiceProvider.GetRequiredService<MenusDbContext>();
            var admin1 = await ctx.Usuarios.FirstOrDefaultAsync(u => u.Rol == RolesUsuario.Admin);
            Assert.NotNull(admin1);
            Assert.Equal("bootstrap@test.com", admin1.Email);
            adminId1 = admin1.Id;
        }

        // scope3 NUEVO: segunda ejecución del bootstrap
        using (var scope3 = provider.CreateScope())
        {
            var bootstrap2 = scope3.ServiceProvider.GetRequiredService<Menus.Api.Auth.AuthBootstrapService>();
            await bootstrap2.StartAsync(CancellationToken.None);
        }

        // scope4 NUEVO: verificar idempotencia con contexto fresco
        using (var scope4 = provider.CreateScope())
        {
            var ctx = scope4.ServiceProvider.GetRequiredService<MenusDbContext>();
            var adminCount = await ctx.Usuarios.CountAsync(u => u.Rol == RolesUsuario.Admin);
            Assert.Equal(1, adminCount);

            var admin2 = await ctx.Usuarios.FirstOrDefaultAsync(u => u.Rol == RolesUsuario.Admin);
            Assert.NotNull(admin2);
            Assert.Equal(adminId1, admin2.Id);
        }
    }

    private ServiceProvider CrearBootstrapServiceProvider(IConfiguration config)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(config);
        services.AddDbContext<MenusDbContext>(options => options.UseNpgsql(_fixture.CadenaDeConexion));
        services.AddScoped<Menus.Domain.Abstractions.Repositories.IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<Menus.Domain.Abstractions.IUnitOfWork, UnitOfWork>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<Menus.Api.Auth.AuthBootstrapService>();
        return services.BuildServiceProvider();
    }
}
