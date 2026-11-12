using System.Text.RegularExpressions;
using FluentValidation;
using Menus.Application.Comun;
using Menus.Domain.Excepciones;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Menus.Api.Errores;

/// <summary>
/// Traduce cualquier excepción a una respuesta <see cref="ProblemDetails"/> (RFC 9457), en un solo lugar
/// (ADR-010, §4).
/// </summary>
/// <remarks>
/// Además de los campos estándar, el cuerpo puede llevar <c>regla</c> (el código del informe, por ejemplo «RS-03») y
/// <c>errores</c> (los errores por campo). Las respuestas 500 nunca llevan el mensaje de la excepción: se registra
/// en el log y el cliente solo recibe un texto genérico.
/// </remarks>
/// <param name="problemDetails">Servicio que escribe el cuerpo <c>application/problem+json</c>.</param>
/// <param name="logger">Log para los errores no previstos.</param>
public sealed partial class ManejadorDeExcepciones(
    IProblemDetailsService problemDetails,
    ILogger<ManejadorDeExcepciones> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problema = Traducir(exception);
        if (problema.Status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Error no previsto al atender {Metodo} {Ruta}", httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = problema.Status!.Value;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problema,
            Exception = exception,
        });
    }

    /// <summary>
    /// Arma el <see cref="ProblemDetails"/> que corresponde a la excepción, según la tabla del ADR-010, §4.
    /// </summary>
    /// <param name="exception">La excepción que llegó hasta la API.</param>
    internal static ProblemDetails Traducir(Exception exception) => exception switch
    {
        ValidationException e => Problema(StatusCodes.Status422UnprocessableEntity, "Los datos enviados no son válidos.",
            errores: e.Errors
                .GroupBy(f => f.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray())),
        DatoInvalidoException e => Problema(StatusCodes.Status422UnprocessableEntity, e.Message, e.Regla,
            new Dictionary<string, string[]> { [e.Campo] = [e.Message] }),
        ReglaDeNegocioException e => Problema(StatusCodes.Status409Conflict, e.Message, e.Regla),
        NoEncontradoException e => Problema(StatusCodes.Status404NotFound, e.Message),
        ActorNoIdentificadoException e => Problema(StatusCodes.Status401Unauthorized, e.Message),
        CredencialesInvalidasException e => Problema(StatusCodes.Status401Unauthorized, e.Message),
        DbUpdateException { InnerException: PostgresException e } => TraducirPostgres(e),
        PostgresException e => TraducirPostgres(e),
        _ => Problema(StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado al atender la solicitud."),
    };

    /// <summary>
    /// Los errores de la base: unicidad y FK son conflictos con lo que ya hay (409), un <c>CHECK</c> es un dato
    /// inválido (422), y un trigger es una regla violada (409) cuyo código va al principio del mensaje («RS-03: …»).
    /// </summary>
    private static ProblemDetails TraducirPostgres(PostgresException e) => e.SqlState switch
    {
        PostgresErrorCodes.UniqueViolation => Problema(StatusCodes.Status409Conflict, "Ya existe un registro con esos datos."),
        PostgresErrorCodes.ForeignKeyViolation => Problema(StatusCodes.Status409Conflict,
            "La operación hace referencia a un registro que no existe, o el registro está en uso."),
        PostgresErrorCodes.CheckViolation => Problema(StatusCodes.Status422UnprocessableEntity, "Un dato no cumple una restricción del modelo."),
        PostgresErrorCodes.RaiseException => CodigoDeRegla().Match(e.MessageText) is { Success: true } m
            ? Problema(StatusCodes.Status409Conflict, m.Groups["detalle"].Value, m.Groups["regla"].Value)
            : Problema(StatusCodes.Status409Conflict, e.MessageText),
        _ => Problema(StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado al atender la solicitud."),
    };

    private static ProblemDetails Problema(int status, string detalle, string? regla = null, IDictionary<string, string[]>? errores = null)
    {
        var problema = new ProblemDetails { Status = status, Detail = detalle };
        if (regla is not null)
        {
            problema.Extensions["regla"] = regla;
        }

        if (errores is not null)
        {
            problema.Extensions["errores"] = errores;
        }

        return problema;
    }

    /// <summary>Código de regla al principio del mensaje de un trigger: «RS-03: el revisor …».</summary>
    [GeneratedRegex(@"^(?<regla>R[A-Z]-\d+[a-z]?):\s*(?<detalle>.+)$", RegexOptions.Singleline)]
    private static partial Regex CodigoDeRegla();
}
