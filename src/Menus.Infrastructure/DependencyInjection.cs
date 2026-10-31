using Menus.Domain.Abstractions.Repositories;
using Menus.Infrastructure.Persistence;
using Menus.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<MenusDbContext>(opt =>
            opt.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddScoped<IMenuRepository, MenuRepository>();

        return services;
    }
}
