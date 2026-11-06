using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Menus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Menus.Infrastructure.Tests.Persistence;

public sealed class MenuParametrizacionTests
{
    private static MenusDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MenusDbContext>()
            .UseInMemoryDatabase($"menus-{Guid.NewGuid()}")
            .Options;
        return new MenusDbContext(options);
    }

    [Fact]
    public async Task MenuLeidoSinSuParametrizacion_LosValueObjectsAvisanEnVezDeInventarla()
    {
        await using var context = CreateContext();
        var menu = new Menu(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(-1),
            6,
            true,
            [(TipoPreparacion.Entrante.Id, 0.5m), (TipoPreparacion.PlatoFuerte.Id, 0.5m)],
            [Guid.NewGuid()]);
        context.Menus.Add(menu);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // La parametrización viaja en colecciones propias: al leer el menú sin incluirlas, los value objects
        // no pueden reconstruirse y avisan en vez de devolver una distribución o una cobertura vacías.
        var leido = await context.Menus.SingleAsync(m => m.Id == menu.Id);

        Assert.Throws<InvalidOperationException>(() => leido.Distribucion);
        Assert.Throws<InvalidOperationException>(() => leido.CoberturaGrupos);
    }
}
