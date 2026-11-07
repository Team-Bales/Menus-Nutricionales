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

                    // Configurar JWT con valores de test
                    services.Configure<JwtOptions>(options =>
                    {
                        options.Key = "clave-secreta-de-prueba-que-es-suficientemente-larga-para-hs256";
                        options.Issuer = "Menus.Test";
                        options.ExpiresMinutes = 60;
                    });
                });

                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Jwt:Key"] = "clave-secreta-de-prueba-que-es-suficientemente-larga-para-hs256",
                        ["Jwt:Issuer"] = "Menus.Test",
                        ["Jwt:ExpiresMinutes"] = "60",
                        // No configurar AuthBootstrap para que no interfiera
                    });
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
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("clave-secreta-de-prueba-que-es-suficientemente-larga-para-hs256"));
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
            issuer: "Menus.Test",
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerarTokenNutricionista(Guid usuarioId, Guid entidadId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("clave-secreta-de-prueba-que-es-suficientemente-larga-para-hs256"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtClaimTypes.Subject, usuarioId.ToString()),
            new(JwtClaimTypes.Role, RolesUsuario.Nutricionista),
            new(JwtClaimTypes.EntidadId, entidadId.ToString()),
        };
        var token = new JwtSecurityToken(
            issuer: "Menus.Test",
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task Login_EmailInexistente_Devuelve401Indistinguible()
    {
        var request = new LoginCommand("noexiste@test.com", "password123");

        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(401, problem.Status);
        Assert.Equal("Credenciales inválidas.", problem.Title);
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

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(401, problem.Status);
        Assert.Equal("Credenciales inválidas.", problem.Title);
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
            var especialidad = new Especialidad(especialidadId, "Nutrición Clínica");
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

        await using var ctx = _fixture.CrearContexto();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(config);
        services.AddScoped(_ => ctx);
        services.AddScoped<Menus.Domain.Abstractions.Repositories.IUsuarioRepository, Menus.Infrastructure.Persistence.Repositories.UsuarioRepository>();
        services.AddScoped<Menus.Domain.Abstractions.IUnitOfWork, Menus.Infrastructure.Persistence.Repositories.UnitOfWork>();
        services.AddScoped<Menus.Domain.Abstractions.Security.IPasswordHasher, Menus.Infrastructure.Security.BCryptPasswordHasher>();
        services.AddScoped<Menus.Api.Auth.AuthBootstrapService>();

        var provider = services.BuildServiceProvider();

        // Primera ejecución
        var bootstrap1 = provider.GetRequiredService<Menus.Api.Auth.AuthBootstrapService>();
        await bootstrap1.StartAsync(CancellationToken.None);

        var admin1 = await ctx.Usuarios.FirstOrDefaultAsync(u => u.Rol == RolesUsuario.Admin);
        Assert.NotNull(admin1);
        Assert.Equal("bootstrap@test.com", admin1.Email);
        var adminId1 = admin1.Id;

        // Segunda ejecución
        var bootstrap2 = provider.GetRequiredService<Menus.Api.Auth.AuthBootstrapService>();
        await bootstrap2.StartAsync(CancellationToken.None);

        var adminCount = await ctx.Usuarios.CountAsync(u => u.Rol == RolesUsuario.Admin);
        Assert.Equal(1, adminCount);

        var admin2 = await ctx.Usuarios.FirstOrDefaultAsync(u => u.Rol == RolesUsuario.Admin);
        Assert.Equal(adminId1, admin2?.Id);
    }
}
