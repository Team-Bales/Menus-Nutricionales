using Menus.Domain.Entidades;
using Menus.Infrastructure.Persistence;
using Menus.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Menus.Infrastructure.Tests.Persistence;

public sealed class UnitOfWorkTests
{
    [Fact]
    public async Task CommitAsync_guarda_lo_que_dejaron_los_repositorios_en_el_mismo_contexto()
    {
        var opciones = new DbContextOptionsBuilder<MenusDbContext>()
            .UseInMemoryDatabase($"menus-{Guid.NewGuid()}")
            .Options;
        var sala = new Sala(Guid.NewGuid(), "Sala A");

        await using (var contexto = new MenusDbContext(opciones))
        {
            await new SalaRepository(contexto).AddAsync(sala);

            var cambios = await new UnitOfWork(contexto).CommitAsync();

            Assert.Equal(1, cambios);
        }

        await using (var contexto = new MenusDbContext(opciones))
        {
            Assert.NotNull(await contexto.Salas.FindAsync(sala.Id));
        }
    }
}
