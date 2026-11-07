using System.Collections.Concurrent;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Application.Comun;

/// <summary>
/// Punto único por el que los controladores ejecutan los casos de uso (patrón <i>Mediator</i>, ADR-010, §1).
/// </summary>
public interface IDespachador
{
    /// <summary>
    /// Valida la solicitud con sus validadores y, si es válida, la entrega a su manejador.
    /// </summary>
    /// <param name="solicitud">El comando o la consulta.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Lo que devuelve el manejador.</returns>
    /// <exception cref="ValidationException">Si algún validador encuentra errores; el manejador no se ejecuta.</exception>
    /// <exception cref="InvalidOperationException">Si la solicitud no tiene manejador registrado.</exception>
    Task<TResultado> EnviarAsync<TResultado>(ISolicitud<TResultado> solicitud, CancellationToken ct = default);
}

/// <summary>
/// Implementación de <see cref="IDespachador"/> sobre la inyección de dependencias.
/// </summary>
/// <remarks>
/// <see cref="EnviarAsync{TResultado}"/> solo conoce el tipo del resultado; el de la solicitud se sabe en tiempo de
/// ejecución. Para llamar al manejador con sus tipos exactos, sin reflexión en cada llamada ni excepciones envueltas
/// en <c>TargetInvocationException</c>, se crea una vez por tipo de solicitud un <see cref="Ejecutor{TResultado}"/>
/// genérico y se guarda en caché.
/// </remarks>
/// <param name="proveedor">Proveedor del ámbito de la petición, del que salen los validadores y el manejador.</param>
internal sealed class Despachador(IServiceProvider proveedor) : IDespachador
{
    private static readonly ConcurrentDictionary<Type, object> Ejecutores = new();

    public Task<TResultado> EnviarAsync<TResultado>(ISolicitud<TResultado> solicitud, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        var ejecutor = (Ejecutor<TResultado>)Ejecutores.GetOrAdd(
            solicitud.GetType(),
            tipo => Activator.CreateInstance(typeof(Ejecutor<,>).MakeGenericType(tipo, typeof(TResultado)))!);

        return ejecutor.EjecutarAsync(solicitud, proveedor, ct);
    }

    private abstract class Ejecutor<TResultado>
    {
        public abstract Task<TResultado> EjecutarAsync(object solicitud, IServiceProvider proveedor, CancellationToken ct);
    }

    private sealed class Ejecutor<TSolicitud, TResultado> : Ejecutor<TResultado>
        where TSolicitud : ISolicitud<TResultado>
    {
        public override async Task<TResultado> EjecutarAsync(object solicitud, IServiceProvider proveedor, CancellationToken ct)
        {
            var tipada = (TSolicitud)solicitud;

            // Los validadores van uno detrás de otro: si alguno consulta la base, comparten el mismo DbContext.
            var errores = new List<FluentValidation.Results.ValidationFailure>();
            foreach (var validador in proveedor.GetServices<IValidator<TSolicitud>>())
            {
                var resultado = await validador.ValidateAsync(tipada, ct);
                errores.AddRange(resultado.Errors);
            }

            if (errores.Count > 0)
            {
                throw new ValidationException(errores);
            }

            var manejador = proveedor.GetService<IHandler<TSolicitud, TResultado>>()
                ?? throw new InvalidOperationException(
                    $"No hay manejador registrado para {typeof(TSolicitud).Name}. ¿Implementa IHandler<{typeof(TSolicitud).Name}, {typeof(TResultado).Name}>?");

            return await manejador.HandleAsync(tipada, ct);
        }
    }
}
