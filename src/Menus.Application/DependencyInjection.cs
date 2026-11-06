using System.Reflection;
using FluentValidation;
using Menus.Application.Comun;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registra el despachador y, por reflexión, todos los manejadores y validadores de este ensamblado
    /// (ADR-010, §1): un caso de uso nuevo no exige tocar la configuración.
    /// </summary>
    /// <param name="services">Contenedor de servicios.</param>
    /// <returns>El mismo contenedor, para encadenar registros.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
        => services.AddApplication(typeof(DependencyInjection).Assembly);

    /// <summary>
    /// Igual que <see cref="AddApplication(IServiceCollection)"/>, pero busca los manejadores y validadores en
    /// <paramref name="ensamblado"/>. Lo usan las pruebas, con manejadores propios.
    /// </summary>
    /// <param name="services">Contenedor de servicios.</param>
    /// <param name="ensamblado">Ensamblado donde buscar.</param>
    /// <returns>El mismo contenedor, para encadenar registros.</returns>
    internal static IServiceCollection AddApplication(this IServiceCollection services, Assembly ensamblado)
    {
        services.AddScoped<IDespachador, Despachador>();

        var manejadores = ensamblado.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandler<,>))
                .Select(i => (Servicio: i, Implementacion: t)));

        foreach (var (servicio, implementacion) in manejadores)
        {
            services.AddScoped(servicio, implementacion);
        }

        services.AddValidatorsFromAssembly(ensamblado, ServiceLifetime.Scoped, includeInternalTypes: true);

        return services;
    }
}
