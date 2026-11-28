using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Menus.Infrastructure.IntegrationTests.Datos;
using Menus.Infrastructure.IntegrationTests.Fixtures;
using Menus.Infrastructure.Persistence;
using Menus.Infrastructure.Persistence.Consultas;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Consultas;

/// <summary>
/// <see cref="ConteoDePacientesPorDieta"/> contra PostgreSQL real (#325): que EF Core traduzca la consulta (el filtro
/// por nombre de dieta dentro de <c>paciente_dieta</c> y el rango de edad) y que cuente bien.
/// </summary>
[Collection("PostgreSQL")]
public sealed class ConteoDePacientesPorDietaIntegrationTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private MenusDbContext _dbContext = null!;
    private NpgsqlConnection _conexion = null!;
    private Sembrador _s = null!;

    public ConteoDePacientesPorDietaIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ReiniciarAsync();
        _dbContext = _fixture.CrearContexto();
        _conexion = await _fixture.AbrirAsync();
        _s = new Sembrador(_conexion);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _conexion.DisposeAsync();
    }

    [Fact]
    public async Task Cuenta_los_pacientes_inscritos_en_la_dieta_dentro_del_rango()
    {
        var dieta = await _s.DietaAsync();
        var otraDieta = await _s.DietaAsync();
        foreach (var _ in Enumerable.Range(0, 2))
        {
            await _s.InscribirAsync(await _s.PacienteAsync(), dieta);
        }

        await _s.InscribirAsync(await _s.PacienteAsync(), otraDieta);
        var nombre = await _dbContext.Dietas.Where(d => d.Id == dieta).Select(d => d.Nombre).SingleAsync();
        var conteo = new ConteoDePacientesPorDieta(_dbContext);

        // El Sembrador crea los pacientes con 30 años.
        Assert.Equal(2, await conteo.ContarAsync(nombre, 18, 39, CancellationToken.None));
        Assert.Equal(0, await conteo.ContarAsync(nombre, 40, 64, CancellationToken.None));
        Assert.Equal(2, await conteo.ContarAsync(nombre, 18, null, CancellationToken.None));
        Assert.Equal(0, await conteo.ContarAsync("No existe", 0, null, CancellationToken.None));
    }
}
