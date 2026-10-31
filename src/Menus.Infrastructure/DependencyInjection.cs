using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;
using Menus.Infrastructure.Persistence;
using Menus.Infrastructure.Persistence.Repositories;
using Menus.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
            opt.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddScoped<INutricionistaRepository, NutricionistaRepository>();
        services.AddScoped<IPacienteRepository, PacienteRepository>();
        services.AddScoped<IRepository<Sala, Guid>, SalaRepository>();

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

        return services;
    }
}
