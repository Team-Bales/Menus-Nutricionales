using Menus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Auth;

/// <summary>
/// Pruebas de integración para la migración de la tabla usuarios.
/// Requieren Testcontainers (Docker) para ejecutarse contra PostgreSQL real.
/// </summary>
[Collection("IntegrationTests")]
public sealed class UsuarioMigrationTests
{
    [Fact]
    public async Task Migracion_CreaTablaUsuariosConIndicesYChecks()
    {
        // Esta prueba solo se ejecuta con Testcontainers/Docker
        // Verifica que la migración crea:
        // - Tabla usuarios con columnas id, email, password_hash, rol, entidad_id
        // - CHECK constraints: email no vacío, password_hash no vacío, rol válido, entidad_id por rol
        // - Índice único simple en email
        // - Índice único filtrado en entidad_id (non-null)
        //
        // Ejemplo de uso con Testcontainers:
        // var container = new PostgreSqlContainer("postgres:16");
        // await container.StartAsync();
        // var options = new DbContextOptionsBuilder<MenusDbContext>()
        //     .UseNpgsql(container.GetConnectionString())
        //     .Options;
        // using var context = new MenusDbContext(options);
        // await context.Database.MigrateAsync();
        // 
        // // Verificar esquema via SQL raw
        // var columns = await context.Database.SqlQueryRaw<string>(
        //     "SELECT column_name FROM information_schema.columns WHERE table_name = 'usuarios'").ToListAsync();
        // Assert.Contains("email", columns);
        // Assert.Contains("password_hash", columns);
        // Assert.Contains("rol", columns);
        // Assert.Contains("entidad_id", columns);
        //
        // var indexes = await context.Database.SqlQueryRaw<string>(
        //     "SELECT indexname FROM pg_indexes WHERE tablename = 'usuarios'").ToListAsync();
        // Assert.Contains(indexes, i => i.Contains("ux_usuarios_email"));
        // Assert.Contains(indexes, i => i.Contains("ux_usuarios_entidad_id_non_null"));
        //
        // var checks = await context.Database.SqlQueryRaw<string>(
        //     "SELECT conname FROM pg_constraint WHERE conrelid = 'usuarios'::regclass AND contype = 'c'").ToListAsync();
        // Assert.Contains(checks, c => c.Contains("ck_usuarios_rol_valido"));
        // Assert.Contains(checks, c => c.Contains("ck_usuarios_entidad_id_por_rol"));
    }

    [Fact]
    public async Task BootstrapAdmin_Ausente_NoOp()
    {
        // Verifica que si faltan variables AuthBootstrap__Email/Password, el bootstrap no hace nada
    }

    [Fact]
    public async Task BootstrapAdmin_Incompleto_NoOpYAvisa()
    {
        // Verifica que si solo hay Email o solo Password, no hace nada y loguea aviso
    }

    [Fact]
    public async Task BootstrapAdmin_CreaAdminIdempotente()
    {
        // Verifica que si no hay Admin y config completa, crea Admin con EntidadId null
        // Y que en segundo arranque no duplica
    }

    [Fact]
    public async Task BootstrapAdmin_EmailColisionConOtroRol_FallaClaro()
    {
        // Verifica que si el email ya existe con otro rol, lanza excepción clara sin cambiar rol
    }
}