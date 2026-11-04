using Menus.Domain.Entidades;
using Menus.Infrastructure.Persistence;
using Menus.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Menus.Infrastructure.Tests.Persistence;

public sealed class RepositoryTests
{
    private static MenusDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MenusDbContext>()
            .UseInMemoryDatabase($"menus-{Guid.NewGuid()}")
            .Options;
        return new MenusDbContext(options);
    }

    [Fact]
    public async Task NutricionistaRepository_GetByDieta_ReturnsOnlyAuthorized()
    {
        await using var context = CreateContext();
        var dietaId = Guid.NewGuid();
        var autorizado = new Nutricionista(Guid.NewGuid(), "Dra. Pérez", Guid.NewGuid());
        autorizado.AutorizarDieta(dietaId);
        var noAutorizado = new Nutricionista(Guid.NewGuid(), "Dr. Gómez", Guid.NewGuid());
        context.Nutricionistas.AddRange(autorizado, noAutorizado);
        await context.SaveChangesAsync();

        var repository = new NutricionistaRepository(context);

        var result = await repository.GetByDietaAsync(dietaId);

        Assert.Single(result);
        Assert.Equal(autorizado.Id, result[0].Id);
    }

    [Fact]
    public async Task NutricionistaRepository_GetById_IncludesDietasAutorizadas()
    {
        await using var context = CreateContext();
        var nutricionista = new Nutricionista(Guid.NewGuid(), "Dra. Pérez", Guid.NewGuid());
        nutricionista.AutorizarDieta(Guid.NewGuid());
        context.Nutricionistas.Add(nutricionista);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repository = new NutricionistaRepository(context);

        var result = await repository.GetByIdAsync(nutricionista.Id);

        Assert.NotNull(result);
        Assert.Single(result.DietasAutorizadas);
    }

    [Fact]
    public async Task PacienteRepository_GetBySala_ReturnsOnlyPatientsOfThatRoom()
    {
        await using var context = CreateContext();
        var salaId = Guid.NewGuid();
        var paciente = new Paciente(Guid.NewGuid(), "Ana", 30, salaId);
        var otro = new Paciente(Guid.NewGuid(), "Luis", 40, Guid.NewGuid());
        context.Pacientes.AddRange(paciente, otro);
        await context.SaveChangesAsync();

        var repository = new PacienteRepository(context);

        var result = await repository.GetBySalaAsync(salaId);

        Assert.Single(result);
        Assert.Equal(paciente.Id, result[0].Id);
    }

    [Fact]
    public async Task PacienteRepository_GetByDieta_ReturnsOnlyInscribedPatients()
    {
        await using var context = CreateContext();
        var dietaId = Guid.NewGuid();
        var inscrito = new Paciente(Guid.NewGuid(), "Ana", 30, Guid.NewGuid());
        inscrito.InscribirDieta(dietaId);
        var noInscrito = new Paciente(Guid.NewGuid(), "Luis", 40, Guid.NewGuid());
        context.Pacientes.AddRange(inscrito, noInscrito);
        await context.SaveChangesAsync();

        var repository = new PacienteRepository(context);

        var result = await repository.GetByDietaAsync(dietaId);

        Assert.Single(result);
        Assert.Equal(inscrito.Id, result[0].Id);
    }

    [Fact]
    public async Task PacienteRepository_GetById_IncludesDietas()
    {
        await using var context = CreateContext();
        var paciente = new Paciente(Guid.NewGuid(), "Ana", 30, Guid.NewGuid());
        paciente.InscribirDieta(Guid.NewGuid());
        context.Pacientes.Add(paciente);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repository = new PacienteRepository(context);

        var result = await repository.GetByIdAsync(paciente.Id);

        Assert.NotNull(result);
        Assert.Single(result.Dietas);
    }

    [Fact]
    public async Task SalaRepository_SupportsGenericOperations()
    {
        await using var context = CreateContext();
        var sala = new Sala(Guid.NewGuid(), "Cardiología");
        var repository = new SalaRepository(context);

        await repository.AddAsync(sala);
        await context.SaveChangesAsync();

        Assert.Equal(sala.Id, (await repository.GetByIdAsync(sala.Id))?.Id);
        Assert.Single(await repository.ListAsync());

        repository.Remove(sala);
        await context.SaveChangesAsync();

        Assert.Empty(await repository.ListAsync());
    }
}
