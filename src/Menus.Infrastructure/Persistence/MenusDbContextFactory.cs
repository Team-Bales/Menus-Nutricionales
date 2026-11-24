using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Menus.Infrastructure.Persistence;

/// <summary>
/// Crea el contexto para las herramientas de diseño de EF Core (<c>dotnet ef migrations</c>), que no arrancan la
/// API. Solo la usan esas herramientas: en ejecución, el contexto se registra en <c>DependencyInjection</c> con
/// la cadena <c>ConnectionStrings:Postgres</c>. La cadena de abajo es la de la base local de desarrollo; para
/// generar una migración no hace falta que la base esté levantada.
/// </summary>
internal sealed class MenusDbContextFactory : IDesignTimeDbContextFactory<MenusDbContext>
{
    /// <inheritdoc />
    public MenusDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MenusDbContext>()
            .UseNpgsql("Host=localhost;Database=menus_dev;Username=postgres;Password=postgres")
            .Options;

        return new MenusDbContext(options);
    }
}
