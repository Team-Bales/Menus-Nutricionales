using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Menus.Api.Errores;
using Menus.Application.Comun;
using Menus.Domain.Excepciones;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace Menus.Api.Tests.Errores;

/// <summary>
/// Pruebas de la tabla de traducción de errores del ADR-010, §4.
/// </summary>
public sealed class ManejadorDeExcepcionesTests
{
    private static PostgresException ErrorDePostgres(string sqlState, string mensaje = "detalle interno")
        => new(mensaje, "ERROR", "ERROR", sqlState);

    [Fact]
    public void ValidationException_es_422_con_los_errores_por_campo()
    {
        var ex = new ValidationException(
        [
            new ValidationFailure("Nombre", "El nombre es obligatorio."),
            new ValidationFailure("Nombre", "El nombre es demasiado largo."),
            new ValidationFailure("Cantidad", "La cantidad debe ser positiva."),
        ]);

        var problema = ManejadorDeExcepciones.Traducir(ex);

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, problema.Status);
        var errores = Assert.IsType<Dictionary<string, string[]>>(problema.Extensions["errores"]);
        Assert.Equal(["El nombre es obligatorio.", "El nombre es demasiado largo."], errores["Nombre"]);
        Assert.Equal(["La cantidad debe ser positiva."], errores["Cantidad"]);
    }

    [Fact]
    public void DatoInvalidoException_es_422_con_el_campo_y_la_regla()
    {
        var problema = ManejadorDeExcepciones.Traducir(new DatoInvalidoException("cantidad", "La cantidad debe ser positiva.", "RD-05"));

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, problema.Status);
        Assert.Equal("La cantidad debe ser positiva.", problema.Detail);
        Assert.Equal("RD-05", problema.Extensions["regla"]);
        var errores = Assert.IsType<Dictionary<string, string[]>>(problema.Extensions["errores"]);
        Assert.Equal(["La cantidad debe ser positiva."], errores["cantidad"]);
    }

    [Fact]
    public void ReglaDeNegocioException_es_409_con_la_regla()
    {
        var problema = ManejadorDeExcepciones.Traducir(new ReglaDeNegocioException("No se quita el único grupo requerido.", "RK-07"));

        Assert.Equal(StatusCodes.Status409Conflict, problema.Status);
        Assert.Equal("No se quita el único grupo requerido.", problema.Detail);
        Assert.Equal("RK-07", problema.Extensions["regla"]);
    }

    [Fact]
    public void ReglaDeNegocioException_sin_codigo_no_lleva_regla()
    {
        var problema = ManejadorDeExcepciones.Traducir(new ReglaDeNegocioException("El menú ya tiene dictamen."));

        Assert.Equal(StatusCodes.Status409Conflict, problema.Status);
        Assert.False(problema.Extensions.ContainsKey("regla"));
    }

    [Fact]
    public void NoEncontradoException_es_404()
    {
        var problema = ManejadorDeExcepciones.Traducir(new NoEncontradoException("alimento", Guid.Empty));

        Assert.Equal(StatusCodes.Status404NotFound, problema.Status);
        Assert.Equal($"No existe el alimento {Guid.Empty}.", problema.Detail);
    }

    [Fact]
    public void ActorNoIdentificadoException_es_401()
    {
        var problema = ManejadorDeExcepciones.Traducir(new ActorNoIdentificadoException("Falta la cabecera."));

        Assert.Equal(StatusCodes.Status401Unauthorized, problema.Status);
    }

    [Fact]
    public void CredencialesInvalidasException_es_401()
    {
        var problema = ManejadorDeExcepciones.Traducir(new CredencialesInvalidasException());

        Assert.Equal(StatusCodes.Status401Unauthorized, problema.Status);
        Assert.Equal("Credenciales inválidas.", problema.Detail);
    }

    [Theory]
    [InlineData(PostgresErrorCodes.UniqueViolation, StatusCodes.Status409Conflict)]
    [InlineData(PostgresErrorCodes.ForeignKeyViolation, StatusCodes.Status409Conflict)]
    [InlineData(PostgresErrorCodes.CheckViolation, StatusCodes.Status422UnprocessableEntity)]
    public void Errores_de_integridad_de_PostgreSQL_dentro_de_DbUpdateException(string sqlState, int status)
    {
        var problema = ManejadorDeExcepciones.Traducir(new DbUpdateException("fallo al guardar", ErrorDePostgres(sqlState)));

        Assert.Equal(status, problema.Status);
        Assert.DoesNotContain("detalle interno", problema.Detail);
    }

    [Fact]
    public void Trigger_con_codigo_de_regla_es_409_con_la_regla_y_el_resto_del_mensaje()
    {
        var problema = ManejadorDeExcepciones.Traducir(
            new DbUpdateException("fallo al guardar", ErrorDePostgres(PostgresErrorCodes.RaiseException, "RS-03: el revisor no es jefe de nutrición")));

        Assert.Equal(StatusCodes.Status409Conflict, problema.Status);
        Assert.Equal("RS-03", problema.Extensions["regla"]);
        Assert.Equal("el revisor no es jefe de nutrición", problema.Detail);
    }

    [Fact]
    public void Trigger_sin_codigo_de_regla_es_409_con_el_mensaje_entero()
    {
        var problema = ManejadorDeExcepciones.Traducir(ErrorDePostgres(PostgresErrorCodes.RaiseException, "algo no se puede"));

        Assert.Equal(StatusCodes.Status409Conflict, problema.Status);
        Assert.Equal("algo no se puede", problema.Detail);
        Assert.False(problema.Extensions.ContainsKey("regla"));
    }

    [Fact]
    public void Otro_error_de_PostgreSQL_es_500()
    {
        var problema = ManejadorDeExcepciones.Traducir(ErrorDePostgres(PostgresErrorCodes.DeadlockDetected));

        Assert.Equal(StatusCodes.Status500InternalServerError, problema.Status);
        Assert.DoesNotContain("detalle interno", problema.Detail);
    }

    [Fact]
    public void Cualquier_otra_excepcion_es_500_sin_su_mensaje()
    {
        var problema = ManejadorDeExcepciones.Traducir(new InvalidOperationException("detalle interno"));

        Assert.Equal(StatusCodes.Status500InternalServerError, problema.Status);
        Assert.DoesNotContain("detalle interno", problema.Detail);
    }

    [Theory]
    [InlineData(false, StatusCodes.Status409Conflict)]
    [InlineData(true, StatusCodes.Status500InternalServerError)]
    public async Task TryHandleAsync_escribe_el_codigo_y_el_cuerpo_ProblemDetails(bool inesperada, int status)
    {
        var services = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider();
        var contexto = new DefaultHttpContext { RequestServices = services };
        contexto.Response.Body = new MemoryStream();
        var manejador = new ManejadorDeExcepciones(
            services.GetRequiredService<IProblemDetailsService>(), NullLogger<ManejadorDeExcepciones>.Instance);
        Exception ex = inesperada ? new InvalidOperationException("detalle interno") : new ReglaDeNegocioException("Regla violada.", "RK-07");

        var atendida = await manejador.TryHandleAsync(contexto, ex, CancellationToken.None);

        Assert.True(atendida);
        Assert.Equal(status, contexto.Response.StatusCode);
        contexto.Response.Body.Position = 0;
        using var cuerpo = await JsonDocument.ParseAsync(contexto.Response.Body);
        Assert.Equal(status, cuerpo.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(!inesperada, cuerpo.RootElement.TryGetProperty("regla", out _));
        Assert.DoesNotContain("detalle interno", cuerpo.RootElement.GetRawText());
    }
}
