using Menus.Application.Catalogo;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Menus.Infrastructure.Mongo;
using Menus.Infrastructure.Persistence;
using Menus.Infrastructure.Persistence.Repositories;
using Menus.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Menus.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registra la persistencia, los repositorios y la autenticación JWT.</summary>
    /// <param name="services">Contenedor de servicios.</param>
    /// <param name="configuration">Configuración de la aplicación.</param>
    /// <returns>El mismo contenedor, para encadenar registros.</returns>
    /// <exception cref="InvalidOperationException">
    /// Si la configuración JWT no es utilizable (clave ausente o demasiado corta, emisor vacío o
    /// vigencia no positiva). La clave se toma de variables de entorno o de user secrets.
    /// </exception>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<MenusDbContext>(opt =>
            opt.UseNpgsql(configuration.GetConnectionString("Postgres")));

        services.AddScoped<INutricionistaRepository, NutricionistaRepository>();
        services.AddScoped<IPacienteRepository, PacienteRepository>();
        services.AddScoped<IRepository<Sala, Guid>, SalaRepository>();
        services.AddScoped<IMenuRepository, MenuRepository>();
        services.AddScoped<IValoracionRepository, ValoracionRepository>();
        services.AddScoped<IItemAlimentarioRepository, ItemAlimentarioRepository>();
        services.AddScoped<IAlimentoRepository, AlimentoRepository>();
        services.AddScoped<IPlatoRepository, PlatoRepository>();
        services.AddScoped<IDietaRepository, DietaRepository>();
        services.AddScoped<INomencladorRepository<GrupoNutricional>, GrupoNutricionalRepository>();
        services.AddScoped<INomencladorRepository<RestriccionAlimentaria>, RestriccionAlimentariaRepository>();
        services.AddScoped<INomencladorRepository<ProgramaAtencion>, ProgramaAtencionRepository>();
        services.AddScoped<INomencladorRepository<Especialidad>, EspecialidadRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        JwtBearerConfiguration.Validate(jwtOptions);

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<JwtTokenService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = JwtBearerConfiguration.CreateTokenValidationParameters(jwtOptions);
            });
        services.AddAuthorization();

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

            // Baja coordinada de alimentos y platos (RM-09): solo tiene sentido si hay ficha que borrar.
            services.AddScoped<IFichaNutricionalStore, FichaNutricionalStore>();
            services.AddScoped<BajaCoordinadaDeItems>();

            // Lo que los casos de uso del catálogo mantienen de la ficha, después de confirmar (RM-09, R8-06).
            services.AddScoped<IDesgloseDeIngredientes, FichaNutricionalStore>();
            services.AddScoped<IFichasNutricionales, FichasNutricionalesEnMongo>();
        }
        else
        {
            // Sin MongoDB no hay fichas: los casos de uso siguen funcionando, sin nada que mantener.
            services.AddScoped<IFichasNutricionales, SinFichasNutricionales>();
        }

        return services;
    }
}
