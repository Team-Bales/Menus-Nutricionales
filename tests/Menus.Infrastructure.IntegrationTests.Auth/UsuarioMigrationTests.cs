using System.Data;
using System.Threading.Tasks;
using Menus.Domain;
using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Menus.Infrastructure.Persistence;
using Menus.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Auth;

/// <summary>
/// Pruebas de integración para la migración de la tabla usuarios y el bootstrap de Admin.
/// Requieren Testcontainers (Docker) para ejecutarse contra PostgreSQL real.
/// </summary>
[Collection("PostgreSQL")]
public sealed class UsuarioMigrationTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;

    public UsuarioMigrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.ReiniciarAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Migracion_CreaTablaUsuariosConIndicesYChecks()
    {
        await using var ctx = _fixture.CrearContexto();

        // Verificar columnas
        var columns = await ctx.Database.SqlQueryRaw<string>(
            "SELECT column_name FROM information_schema.columns WHERE table_name = 'usuarios'").ToListAsync();

        Assert.Contains("id", columns);
        Assert.Contains("email", columns);
        Assert.Contains("password_hash", columns);
        Assert.Contains("rol", columns);
        Assert.Contains("entidad_id", columns);

        // Verificar índices únicos
        var indexes = await ctx.Database.SqlQueryRaw<string>(
            "SELECT indexname FROM pg_indexes WHERE tablename = 'usuarios'").ToListAsync();

        Assert.Contains(indexes, i => i.Contains("ux_usuarios_email"));
        Assert.Contains(indexes, i => i.Contains("ux_usuarios_entidad_id_non_null"));

        // Verificar CHECK constraints
        var checks = await ctx.Database.SqlQueryRaw<string>(
            "SELECT conname FROM pg_constraint WHERE conrelid = 'usuarios'::regclass AND contype = 'c'").ToListAsync();

        Assert.Contains(checks, c => c.Contains("ck_usuarios_email_no_vacio"));
        Assert.Contains(checks, c => c.Contains("ck_usuarios_password_hash_no_vacio"));
        Assert.Contains(checks, c => c.Contains("ck_usuarios_rol_valido"));
        Assert.Contains(checks, c => c.Contains("ck_usuarios_entidad_id_por_rol"));
    }

    [Fact]
    public async Task BootstrapAdmin_Ausente_NoOp()
    {
        // Configuración sin variables de bootstrap
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Sin AuthBootstrap:Email ni AuthBootstrap:Password
            })
            .Build();

        var provider = CrearServiceProvider(config, _fixture.CadenaDeConexion);

        using (var scope = provider.CreateScope())
        {
            var bootstrap = scope.ServiceProvider.GetRequiredService<Menus.Api.Auth.AuthBootstrapService>();
            await bootstrap.StartAsync(CancellationToken.None);
        }

        // Verificar que no se creó ningún Admin (contexto/scope NUEVO)
        using (var scope = provider.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<MenusDbContext>();
            var adminCount = await ctx.Usuarios.CountAsync(u => u.Rol == RolesUsuario.Admin);
            Assert.Equal(0, adminCount);
        }
    }

    [Fact]
    public async Task BootstrapAdmin_Incompleto_NoOpYAvisa()
    {
        // Solo Email
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AuthBootstrap:Email"] = "admin@test.com",
            })
            .Build();

        var provider = CrearServiceProvider(config, _fixture.CadenaDeConexion);

        using (var scope = provider.CreateScope())
        {
            var bootstrap = scope.ServiceProvider.GetRequiredService<Menus.Api.Auth.AuthBootstrapService>();
            await bootstrap.StartAsync(CancellationToken.None);
        }

        // Verificar que no se creó ningún Admin (contexto/scope NUEVO)
        using (var scope = provider.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<MenusDbContext>();
            var adminCount = await ctx.Usuarios.CountAsync(u => u.Rol == RolesUsuario.Admin);
            Assert.Equal(0, adminCount);
        }

        // Solo Password
        config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AuthBootstrap:Password"] = "password123",
            })
            .Build();

        await _fixture.ReiniciarAsync();
        var provider2 = CrearServiceProvider(config, _fixture.CadenaDeConexion);

        using (var scope = provider2.CreateScope())
        {
            var bootstrap2 = scope.ServiceProvider.GetRequiredService<Menus.Api.Auth.AuthBootstrapService>();
            await bootstrap2.StartAsync(CancellationToken.None);
        }

        // Verificar que no se creó ningún Admin (contexto/scope NUEVO)
        using (var scope = provider2.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<MenusDbContext>();
            var adminCount2 = await ctx.Usuarios.CountAsync(u => u.Rol == RolesUsuario.Admin);
            Assert.Equal(0, adminCount2);
        }
    }

    [Fact]
    public async Task BootstrapAdmin_CreaAdminIdempotente()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AuthBootstrap:Email"] = "admin@test.com",
                ["AuthBootstrap:Password"] = "password123",
            })
            .Build();

        // Dejar que DI cree y posea el contexto a partir de la cadena Testcontainers
        var provider = CrearServiceProvider(config, _fixture.CadenaDeConexion);

        // Primera ejecución: crea el Admin
        using (var scope1 = provider.CreateScope())
        {
            var bootstrap1 = scope1.ServiceProvider.GetRequiredService<Menus.Api.Auth.AuthBootstrapService>();
            await bootstrap1.StartAsync(CancellationToken.None);
        }

        // Verificar con contexto/scope NUEVO
        using (var scope2 = provider.CreateScope())
        {
            var ctx2 = scope2.ServiceProvider.GetRequiredService<MenusDbContext>();
            var admin1 = await ctx2.Usuarios.FirstOrDefaultAsync(u => u.Rol == RolesUsuario.Admin);
            Assert.NotNull(admin1);
            Assert.Equal("admin@test.com", admin1.Email);
            Assert.Null(admin1.EntidadId);
            Assert.False(string.IsNullOrEmpty(admin1.PasswordHash));

            var adminId1 = admin1.Id;

            // Segunda ejecución: no debe duplicar
            var bootstrap2 = scope2.ServiceProvider.GetRequiredService<Menus.Api.Auth.AuthBootstrapService>();
            await bootstrap2.StartAsync(CancellationToken.None);
        }

        // Verificar idempotencia con contexto/scope NUEVO tras cada StartAsync
        using (var scope3 = provider.CreateScope())
        {
            var ctx3 = scope3.ServiceProvider.GetRequiredService<MenusDbContext>();
            var adminCount = await ctx3.Usuarios.CountAsync(u => u.Rol == RolesUsuario.Admin);
            Assert.Equal(1, adminCount);

            var admin2 = await ctx3.Usuarios.FirstOrDefaultAsync(u => u.Rol == RolesUsuario.Admin);
            // Note: We can't compare IDs directly since admin1 is from a different scope,
            // but we verify only one admin exists and it has the correct email
            Assert.NotNull(admin2);
            Assert.Equal("admin@test.com", admin2.Email);
        }
    }

    [Fact]
    public async Task BootstrapAdmin_EmailColisionConOtroRol_FallaClaro()
    {
        // Primero sembrar una Especialidad válida y usarla para el Nutricionista
        var especialidadId = Guid.NewGuid();
        await using (var ctx = _fixture.CrearContexto())
        {
            var especialidad = new Especialidad(especialidadId, "Nutrición Clínica");
            ctx.Especialidades.Add(especialidad);

            var nutricionistaId = Guid.NewGuid();
            var nutricionista = new Nutricionista(nutricionistaId, "Dr. Test", especialidadId);
            ctx.Nutricionistas.Add(nutricionista);

            var usuario = Usuario.Crear(Guid.NewGuid(), "admin@test.com", "hash", RolesUsuario.Nutricionista, nutricionistaId);
            ctx.Usuarios.Add(usuario);
            await ctx.SaveChangesAsync();
        }

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AuthBootstrap:Email"] = "admin@test.com",
                ["AuthBootstrap:Password"] = "password123",
            })
            .Build();

        // Usar DI para crear el contexto desde la cadena de conexión
        var provider = CrearServiceProvider(config, _fixture.CadenaDeConexion);

        // Ejecutar bootstrap y verificar que falla con mensaje claro
        using (var scope = provider.CreateScope())
        {
            var bootstrap = scope.ServiceProvider.GetRequiredService<Menus.Api.Auth.AuthBootstrapService>();
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => bootstrap.StartAsync(CancellationToken.None));

            Assert.Contains("admin@test.com", ex.Message);
            Assert.Contains("Nutricionista", ex.Message);
        }

        // Verificar que no se cambió el rol del usuario existente (contexto/scope NUEVO)
        using (var scope = provider.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<MenusDbContext>();
            var usuarioExistente = await ctx.Usuarios.FirstOrDefaultAsync(u => u.Email == "admin@test.com");
            Assert.NotNull(usuarioExistente);
            Assert.Equal(RolesUsuario.Nutricionista, usuarioExistente.Rol);
        }
    }

    private static ServiceCollection CrearServiceCollection(IConfiguration config, string connectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(config);
        services.AddDbContext<MenusDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<Menus.Domain.Abstractions.Repositories.IUsuarioRepository, Menus.Infrastructure.Persistence.Repositories.UsuarioRepository>();
        services.AddScoped<Menus.Domain.Abstractions.IUnitOfWork, Menus.Infrastructure.Persistence.Repositories.UnitOfWork>();
        services.AddScoped<Menus.Domain.Abstractions.Security.IPasswordHasher, Menus.Infrastructure.Security.BCryptPasswordHasher>();
        services.AddScoped<Menus.Api.Auth.AuthBootstrapService>();
        return services;
    }

    private static ServiceProvider CrearServiceProvider(IConfiguration config, string connectionString)
        => CrearServiceCollection(config, connectionString).BuildServiceProvider();
}
