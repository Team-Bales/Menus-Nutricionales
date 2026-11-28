using Menus.Domain.Entidades;
using Menus.Infrastructure.Persistence;
using Menus.Infrastructure.Persistence.Consultas;
using Microsoft.EntityFrameworkCore;

namespace Menus.Infrastructure.Tests.Persistence;

/// <summary>
/// Pruebas de <see cref="ConteoDePacientesPorDieta"/> (#325): cuenta los pacientes de una dieta, por nombre, dentro de
/// un rango de edad con los dos extremos incluidos.
/// </summary>
public sealed class ConteoDePacientesPorDietaTests
{
    private static readonly Guid Programa = Guid.Parse("33333333-0000-0000-0000-000000000001");

    private static MenusDbContext CrearContexto() => new(
        new DbContextOptionsBuilder<MenusDbContext>().UseInMemoryDatabase($"conteo-{Guid.NewGuid()}").Options);

    private static async Task<MenusDbContext> SembrarAsync()
    {
        var contexto = CrearContexto();
        var hiposodica = new Dieta(Guid.NewGuid(), "Hiposódica", Programa);
        var otraConElMismoNombre = new Dieta(Guid.NewGuid(), "Hiposódica", Programa);
        var blanda = new Dieta(Guid.NewGuid(), "Blanda", Programa);
        contexto.Dietas.AddRange(hiposodica, otraConElMismoNombre, blanda);

        // Edades en el borde del rango 18-39 y fuera de él.
        foreach (var (edad, dieta) in new[] { (18, hiposodica), (39, otraConElMismoNombre), (25, blanda), (17, hiposodica), (40, hiposodica) })
        {
            var paciente = new Paciente(Guid.NewGuid(), $"Paciente de {edad}", edad, Guid.NewGuid());
            paciente.InscribirDieta(dieta.Id);
            contexto.Pacientes.Add(paciente);
        }

        // Un paciente en las dos dietas cuenta una sola vez.
        var enAmbas = new Paciente(Guid.NewGuid(), "En ambas", 30, Guid.NewGuid());
        enAmbas.InscribirDieta(hiposodica.Id);
        enAmbas.InscribirDieta(otraConElMismoNombre.Id);
        contexto.Pacientes.Add(enAmbas);

        await contexto.SaveChangesAsync();
        return contexto;
    }

    [Fact]
    public async Task Cuenta_los_pacientes_de_la_dieta_dentro_del_rango_con_los_extremos_incluidos()
    {
        await using var contexto = await SembrarAsync();

        var cantidad = await new ConteoDePacientesPorDieta(contexto).ContarAsync("Hiposódica", 18, 39, CancellationToken.None);

        // 18, 39 y el de 30 que está en las dos dietas con ese nombre (una sola vez); no el de 17 ni el de 40.
        Assert.Equal(3, cantidad);
    }

    [Fact]
    public async Task Sin_tope_cuenta_desde_la_edad_minima()
    {
        await using var contexto = await SembrarAsync();

        var cantidad = await new ConteoDePacientesPorDieta(contexto).ContarAsync("Hiposódica", 40, null, CancellationToken.None);

        Assert.Equal(1, cantidad);
    }

    [Fact]
    public async Task Una_dieta_que_no_existe_cuenta_cero()
    {
        await using var contexto = await SembrarAsync();

        Assert.Equal(0, await new ConteoDePacientesPorDieta(contexto).ContarAsync("Inexistente", 0, null, CancellationToken.None));
    }
}
