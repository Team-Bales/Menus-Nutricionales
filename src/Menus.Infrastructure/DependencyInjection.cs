using Menus.Infrastructure.Mongo;
using Menus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Menus.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<MenusDbContext>(opt =>
            opt.UseNpgsql(configuration.GetConnectionString("Default")));

        // MongoDB (ficha nutricional, ADR-005), solo si hay cadena de conexión: sin ella, la API arranca como antes.
        // La colección no se inicializa aquí; MenusMongoContext.InicializarAsync lo hace quien aloje el proceso.
        var cadenaMongo = configuration.GetConnectionString("Mongo");
        if (!string.IsNullOrWhiteSpace(cadenaMongo))
        {
            var url = MongoUrl.Create(cadenaMongo);
            services.AddSingleton<IMongoClient>(_ => new MongoClient(url));
            services.AddSingleton(sp => new MenusMongoContext(
                sp.GetRequiredService<IMongoClient>(),
                url.DatabaseName ?? throw new InvalidOperationException(
                    "La cadena de conexión de MongoDB (ConnectionStrings:Mongo) no indica la base de datos.")));
        }

        return services;
    }
}
