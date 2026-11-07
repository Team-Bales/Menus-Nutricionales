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

        await using var ctx = _fixture.CrearContexto();
        var services = CrearServiceProvider(config, ctx);

        var bootstrap = services.GetRequiredService<Menus.Api.Auth.AuthBootstrapService>();
        await bootstrap.StartAsync(CancellationToken.None);

        // Verificar que no se creó ningún Admin
        var adminCount = await ctx.Usuarios.CountAsync(u => u.Rol == RolesUsuario.Admin);
        Assert.Equal(0, adminCount);
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

        await using var ctx = _fixture.CrearContexto();
        var services = CrearServiceProvider(config, ctx);

        var bootstrap = services.GetRequiredService<Menus.Api.Auth.AuthBootstrapService>();
        await bootstrap.StartAsync(CancellationToken.None);

        var adminCount = await ctx.Usuarios.CountAsync(u => u.Rol == RolesUsuario.Admin);
        Assert.Equal(0, adminCount);

        // Solo Password
        config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AuthBootstrap:Password"] = "password123",
            })
            .Build();

        await _fixture.ReiniciarAsync();
        await using var ctx2 = _fixture.CrearContexto();
        var services2 = CrearServiceProvider(config, ctx2);

        var bootstrap2 = services2.GetRequiredService<Menus.Api.Auth.AuthBootstrapService>();
        await bootstrap2.StartAsync(CancellationToken.None);

        var adminCount2 = await ctx2.Usuarios.CountAsync(u => u.Rol == RolesUsuario.Admin);
        Assert.Equal(0, adminCount2);
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

        await using var ctx = _fixture.CrearContexto();
        var services = CrearServiceProvider(config, ctx);

        // Primera ejecución: crea el Admin
        var bootstrap = services.GetRequiredService<Menus.Api.Auth.AuthBootstrapService>();
        await bootstrap.StartAsync(CancellationToken.None);

        var admin1 = await ctx.Usuarios.FirstOrDefaultAsync(u => u.Rol == RolesUsuario.Admin);
        Assert.NotNull(admin1);
        Assert.Equal("admin@test.com", admin1.Email);
        Assert.Null(admin1.EntidadId);
        Assert.False(string.IsNullOrEmpty(admin1.PasswordHash));

        var adminId1 = admin1.Id;

        // Segunda ejecución: no debe duplicar
        await bootstrap.StartAsync(CancellationToken.None);

        var adminCount = await ctx.Usuarios.CountAsync(u => u.Rol == RolesUsuario.Admin);
        Assert.Equal(1, adminCount);

        var admin2 = await ctx.Usuarios.FirstOrDefaultAsync(u => u.Rol == RolesUsuario.Admin);
        Assert.Equal(adminId1, admin2?.Id);
    }

    [Fact]
    public async Task BootstrapAdmin_EmailColisionConOtroRol_FallaClaro()
    {
        // Primero crear un Nutricionista con ese email
        await using (var ctx = _fixture.CrearContexto())
        {
            var nutricionistaId = Guid.NewGuid();
            var nutricionista = new Nutricionista(nutricionistaId, "Dr. Test", Guid.NewGuid());
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

        await using var ctx2 = _fixture.CrearContexto();
        var services = CrearServiceProvider(config, ctx2);

        var bootstrap = services.GetRequiredService<Menus.Api.Auth.AuthBootstrapService>();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => bootstrap.StartAsync(CancellationToken.None));

        Assert.Contains("admin@test.com", ex.Message);
        Assert.Contains("Nutricionista", ex.Message);

        // Verificar que no se cambió el rol del usuario existente
        var usuarioExistente = await ctx2.Usuarios.FirstOrDefaultAsync(u => u.Email == "admin@test.com");
        Assert.NotNull(usuarioExistente);
        Assert.Equal(RolesUsuario.Nutricionista, usuarioExistente.Rol);
    }

    private static ServiceProvider CrearServiceProvider(IConfiguration config, MenusDbContext ctx)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(config);
        services.AddScoped(_ => ctx);
        services.AddScoped<Menus.Domain.Abstractions.Repositories.IUsuarioRepository, Menus.Infrastructure.Persistence.Repositories.UsuarioRepository>();
        services.AddScoped<Menus.Domain.Abstractions.IUnitOfWork, Menus.Infrastructure.Persistence.Repositories.UnitOfWork>();
        services.AddScoped<Menus.Domain.Abstractions.Security.IPasswordHasher, Menus.Infrastructure.Security.BCryptPasswordHasher>();
        services.AddScoped<Menus.Api.Auth.AuthBootstrapService>();
        return services.BuildServiceProvider();
    }
}
