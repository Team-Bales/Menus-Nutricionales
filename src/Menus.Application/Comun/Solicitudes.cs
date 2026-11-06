namespace Menus.Application.Comun;

/// <summary>
/// Solicitud que el despachador entrega a su manejador: un comando o una consulta que produce
/// <typeparamref name="TResultado"/> (ADR-010, §1).
/// </summary>
/// <remarks>
/// Es la base común de <see cref="ICommand{TResultado}"/> y <see cref="IQuery{TResultado}"/>, para que
/// <see cref="IDespachador.EnviarAsync{TResultado}"/> reciba cualquiera de las dos. No se implementa directamente.
/// </remarks>
/// <typeparam name="TResultado">Lo que devuelve el manejador: un DTO, nunca una entidad (ADR-010, §2).</typeparam>
public interface ISolicitud<TResultado>;

/// <summary>
/// Caso de uso que cambia estado. Su manejador confirma los cambios con una sola llamada a
/// <c>IUnitOfWork.CommitAsync</c> (ADR-010, §5).
/// </summary>
/// <typeparam name="TResultado">Lo que devuelve el manejador (por ejemplo <see cref="IdCreadoDto"/>).</typeparam>
public interface ICommand<TResultado> : ISolicitud<TResultado>;

/// <summary>
/// Caso de uso que solo lee.
/// </summary>
/// <typeparam name="TResultado">Lo que devuelve el manejador (por ejemplo <see cref="ResultadoPaginado{T}"/>).</typeparam>
public interface IQuery<TResultado> : ISolicitud<TResultado>;

/// <summary>
/// Manejador de un caso de uso. Cada comando o consulta tiene exactamente uno.
/// </summary>
/// <typeparam name="TSolicitud">El comando o la consulta que atiende.</typeparam>
/// <typeparam name="TResultado">Lo que devuelve.</typeparam>
public interface IHandler<in TSolicitud, TResultado>
    where TSolicitud : ISolicitud<TResultado>
{
    /// <summary>Ejecuta el caso de uso.</summary>
    /// <param name="solicitud">El comando o la consulta, ya validado por el despachador.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task<TResultado> HandleAsync(TSolicitud solicitud, CancellationToken ct);
}
