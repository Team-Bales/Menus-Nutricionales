using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Menus.Infrastructure.Persistence;

internal sealed class MenusDbContextFactory : IDesignTimeDbContextFactory<MenusDbContext>
{
    public MenusDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MenusDbContext>()
            .UseNpgsql("Host=localhost;Database=menus_dev;Username=postgres;Password=postgres")
            .Options;

        return new MenusDbContext(options);
    }
}
