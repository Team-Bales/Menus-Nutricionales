using System.Reflection;
using FluentValidation;
using Menus.Application.Comun;
using Menus.Application.GeneracionDeMenus;
using Menus.Application.IA;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Menus.Application;

/// <summary>Registro de la capa de aplicación en el contenedor de servicios.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra el despachador y, por reflexión, todos los manejadores y validadores de este ensamblado
    /// (ADR-010, §1): un caso de uso nuevo no exige tocar la configuración.
    /// </summary>
    /// <param name="services">Contenedor de servicios.</param>
    /// <returns>El mismo contenedor, para encadenar registros.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddApplication(typeof(DependencyInjection).Assembly);
        services.AddIA();
        return services;
    }

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

        // Estrategia de generación automática de menús (patrón Strategy, #98; checklist #96). Se registra aparte
        // porque no implementa IHandler<,>: el caso de uso la recibe inyectada, no la crea.
        services.AddScoped<IEstrategiaGeneracionMenu, EstrategiaGeneracionAleatoria>();

        return services;
    }

    /// <summary>
    /// Registra los proveedores de contexto <see cref="IProveedorDeContexto"/> por reflexión desde
    /// <see cref="Menus.Application"/> (solo este ensamblado). Usa <see cref="ServiceCollectionDescriptorExtensions.TryAddEnumerable"/>
    /// con lifetime <see cref="ServiceLifetime.Scoped"/> por tipo, de modo que dos llamadas no duplican registros.
    /// No valida unicidad de <see cref="IProveedorDeContexto.Fuente"/> instanciando proveedores durante el registro.
    /// </summary>
    /// <param name="services">Contenedor de servicios.</param>
    /// <returns>El mismo contenedor, para encadenar registros.</returns>
    public static IServiceCollection AddIA(this IServiceCollection services)
        => services.AddIA(typeof(DependencyInjection).Assembly);

    /// <summary>
    /// Sobrecarga interna para pruebas: registra proveedores desde el ensamblado indicado.
    /// </summary>
    /// <param name="services">Contenedor de servicios.</param>
    /// <param name="ensamblado">Ensamblado donde buscar proveedores.</param>
    /// <returns>El mismo contenedor, para encadenar registros.</returns>
    internal static IServiceCollection AddIA(this IServiceCollection services, Assembly ensamblado)
    {
        var proveedores = ensamblado.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false }
                && typeof(IProveedorDeContexto).IsAssignableFrom(t));

        foreach (var proveedor in proveedores)
        {
            services.TryAddEnumerable(ServiceDescriptor.Scoped(typeof(IProveedorDeContexto), proveedor));
        }

        return services;
    }
}
