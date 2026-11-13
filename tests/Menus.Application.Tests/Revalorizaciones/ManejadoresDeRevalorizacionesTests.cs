using Menus.Application.Comun;
using Menus.Application.Revalorizaciones.ListarPendientes;
using Menus.Application.Revalorizaciones.RegistrarResultadoRevalorizacion;
using Menus.Application.Revalorizaciones.SolicitarRevalorizacion;
using Menus.Application.Tests.Revalorizaciones;
using Menus.Domain.Entidades;
using Menus.Domain.Excepciones;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Tests.Revalorizaciones;

/// <summary>Pruebas de los manejadores del flujo de revalorización (#87) contra el banco en memoria.</summary>
public sealed class ManejadoresDeRevalorizacionesTests
{
    private readonly RevalorizacionesFalso _banco = new();

    // ---- SolicitarRevalorizacion ----

    [Fact]
    public async Task Solicitar_crea_revalorizacion_con_resultado_nulo_y_nutricionista_indicado_y_confirma()
    {
        _banco.Sembrar();
        var comando = new SolicitarRevalorizacionCommand(RevalorizacionesFalso.ValoracionRevisada, RevalorizacionesFalso.NutricionistaEjecutor);
        var handler = new SolicitarRevalorizacionHandler(_banco.RevalorizacionesRepo, _banco.ValoracionesRepo, _banco.NutricionistasRepo, _banco.UnidadDeTrabajo);

        var creado = await handler.HandleAsync(comando, CancellationToken.None);

        var revalorizacion = Assert.Single(_banco.Revalorizaciones);
        Assert.Equal(creado.Id, revalorizacion.Id);
        Assert.Null(revalorizacion.ValoracionGeneradaId);
        Assert.Equal(RevalorizacionesFalso.NutricionistaEjecutor, revalorizacion.NutricionistaId);
        Assert.Equal(RevalorizacionesFalso.ValoracionRevisada, revalorizacion.ValoracionRevisadaId);
        Assert.Equal(DateTimeKind.Utc, revalorizacion.FechaSolicitud.Kind);
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Solicitar_con_valoracion_inexistente_lanza_NoEncontrado_sin_confirmar()
    {
        _banco.Sembrar();
        var comando = new SolicitarRevalorizacionCommand(Guid.NewGuid(), RevalorizacionesFalso.NutricionistaEjecutor);
        var handler = new SolicitarRevalorizacionHandler(_banco.RevalorizacionesRepo, _banco.ValoracionesRepo, _banco.NutricionistasRepo, _banco.UnidadDeTrabajo);

        var ex = await Assert.ThrowsAsync<NoEncontradoException>(() => handler.HandleAsync(comando, CancellationToken.None));

        Assert.Equal("valoración", ex.Recurso);
        Assert.Empty(_banco.Revalorizaciones);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Solicitar_con_nutricionista_inexistente_lanza_NoEncontrado_sin_confirmar()
    {
        _banco.Sembrar();
        var comando = new SolicitarRevalorizacionCommand(RevalorizacionesFalso.ValoracionRevisada, Guid.NewGuid());
        var handler = new SolicitarRevalorizacionHandler(_banco.RevalorizacionesRepo, _banco.ValoracionesRepo, _banco.NutricionistasRepo, _banco.UnidadDeTrabajo);

        var ex = await Assert.ThrowsAsync<NoEncontradoException>(() => handler.HandleAsync(comando, CancellationToken.None));

        Assert.Equal("nutricionista", ex.Recurso);
        Assert.Empty(_banco.Revalorizaciones);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    // ---- RegistrarResultadoRevalorizacion ----

    [Fact]
    public async Task RegistrarResultado_el_ejecutor_crea_valoracion_con_misma_asignacion_y_nutricionista_actor_y_enlaza_e_invalida_cache()
    {
        _banco.Sembrar();

        // Crear una revalorización pendiente
        var revalorizacion = new Revalorizacion(Guid.NewGuid(), DateTime.UtcNow, RevalorizacionesFalso.NutricionistaEjecutor, RevalorizacionesFalso.ValoracionRevisada);
        _banco.Revalorizaciones.Add(revalorizacion);

        var comando = new RegistrarResultadoRevalorizacionCommand(revalorizacion.Id, ResultadoNutricional.Buena.Id);
        var handler = new RegistrarResultadoRevalorizacionHandler(_banco.RevalorizacionesRepo, _banco.ValoracionesRepo, _banco.UnidadDeTrabajo, new ActorFalso(RevalorizacionesFalso.NutricionistaEjecutor), new CacheFalso());

        var creado = await handler.HandleAsync(comando, CancellationToken.None);

        // Se crea la nueva valoración
        var nuevaValoracion = Assert.Single(_banco.Valoraciones, v => v.Id != RevalorizacionesFalso.ValoracionRevisada);
        Assert.Equal(creado.Id, nuevaValoracion.Id);
        Assert.Equal(RevalorizacionesFalso.Menu, nuevaValoracion.MenuId); // RS-10: misma asignación
        Assert.Equal(RevalorizacionesFalso.Paciente, nuevaValoracion.PacienteId); // RS-10: misma asignación
        Assert.Equal(RevalorizacionesFalso.NutricionistaEjecutor, nuevaValoracion.NutricionistaId); // RS-10: emitida por el ejecutor
        Assert.Equal(ResultadoNutricional.Buena.Id, nuevaValoracion.ResultadoNutricionalId);
        Assert.Equal(DateTimeKind.Utc, nuevaValoracion.FechaValoracion.Kind);

        // La revalorización queda enlazada
        Assert.Equal(nuevaValoracion.Id, revalorizacion.ValoracionGeneradaId);

        // Se confirma una vez
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task RegistrarResultado_con_revalorizacion_inexistente_lanza_NoEncontrado()
    {
        _banco.Sembrar();
        var comando = new RegistrarResultadoRevalorizacionCommand(Guid.NewGuid(), ResultadoNutricional.Buena.Id);
        var handler = new RegistrarResultadoRevalorizacionHandler(_banco.RevalorizacionesRepo, _banco.ValoracionesRepo, _banco.UnidadDeTrabajo, new ActorFalso(RevalorizacionesFalso.NutricionistaEjecutor), new CacheFalso());

        await Assert.ThrowsAsync<NoEncontradoException>(() => handler.HandleAsync(comando, CancellationToken.None));
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task RegistrarResultado_por_otro_nutricionista_lanza_RK11_sin_crear_valoracion()
    {
        _banco.Sembrar();
        var revalorizacion = new Revalorizacion(Guid.NewGuid(), DateTime.UtcNow, RevalorizacionesFalso.NutricionistaEjecutor, RevalorizacionesFalso.ValoracionRevisada);
        _banco.Revalorizaciones.Add(revalorizacion);

        var comando = new RegistrarResultadoRevalorizacionCommand(revalorizacion.Id, ResultadoNutricional.Buena.Id);
        var handler = new RegistrarResultadoRevalorizacionHandler(_banco.RevalorizacionesRepo, _banco.ValoracionesRepo, _banco.UnidadDeTrabajo, new ActorFalso(RevalorizacionesFalso.NutricionistaOtro), new CacheFalso());

        var ex = await Assert.ThrowsAsync<ReglaDeNegocioException>(() => handler.HandleAsync(comando, CancellationToken.None));

        Assert.Equal("RK-11", ex.Regla);
        Assert.Single(_banco.Valoraciones); // Solo la valoración revisada original
        Assert.Null(revalorizacion.ValoracionGeneradaId);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task RegistrarResultado_segunda_vez_lanza_ReglaDeNegocioException_sin_crear_valoracion()
    {
        _banco.Sembrar();
        var revalorizacion = new Revalorizacion(Guid.NewGuid(), DateTime.UtcNow, RevalorizacionesFalso.NutricionistaEjecutor, RevalorizacionesFalso.ValoracionRevisada);
        _banco.Revalorizaciones.Add(revalorizacion);

        // Primera vez
        var comando1 = new RegistrarResultadoRevalorizacionCommand(revalorizacion.Id, ResultadoNutricional.Buena.Id);
        var handler1 = new RegistrarResultadoRevalorizacionHandler(_banco.RevalorizacionesRepo, _banco.ValoracionesRepo, _banco.UnidadDeTrabajo, new ActorFalso(RevalorizacionesFalso.NutricionistaEjecutor), new CacheFalso());
        await handler1.HandleAsync(comando1, CancellationToken.None);

        // Segunda vez
        var comando2 = new RegistrarResultadoRevalorizacionCommand(revalorizacion.Id, ResultadoNutricional.Excelente.Id);
        var handler2 = new RegistrarResultadoRevalorizacionHandler(_banco.RevalorizacionesRepo, _banco.ValoracionesRepo, _banco.UnidadDeTrabajo, new ActorFalso(RevalorizacionesFalso.NutricionistaEjecutor), new CacheFalso());

        var ex = await Assert.ThrowsAsync<ReglaDeNegocioException>(() => handler2.HandleAsync(comando2, CancellationToken.None));

        Assert.Equal("La revalorización ya tiene una valoración generada.", ex.Message);
        Assert.Equal(2, _banco.Valoraciones.Count); // La original + la primera generada
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones); // Solo la primera confirmó
    }

    // ---- ListarPendientes ----

    [Fact]
    public async Task ListarPendientes_devuelve_solo_las_del_nutricionista_sin_valoracion_generada_ordenadas_por_fecha()
    {
        _banco.Sembrar();

        var r1 = new Revalorizacion(Guid.NewGuid(), DateTime.UtcNow.AddHours(-2), RevalorizacionesFalso.NutricionistaEjecutor, RevalorizacionesFalso.ValoracionRevisada);
        var r2 = new Revalorizacion(Guid.NewGuid(), DateTime.UtcNow.AddHours(-1), RevalorizacionesFalso.NutricionistaEjecutor, RevalorizacionesFalso.ValoracionRevisada);
        var r3 = new Revalorizacion(Guid.NewGuid(), DateTime.UtcNow, RevalorizacionesFalso.NutricionistaOtro, RevalorizacionesFalso.ValoracionRevisada);
        // r4 ya tiene valoración generada
        var r4 = new Revalorizacion(Guid.NewGuid(), DateTime.UtcNow.AddHours(-3), RevalorizacionesFalso.NutricionistaEjecutor, RevalorizacionesFalso.ValoracionRevisada);
        r4.RegistrarValoracionGenerada(Guid.NewGuid());

        _banco.Revalorizaciones.AddRange([r1, r2, r3, r4]);

        var handler = new ListarRevalorizacionesPendientesHandler(_banco.RevalorizacionesRepo, _banco.NutricionistasRepo);
        var resultado = await handler.HandleAsync(new ListarRevalorizacionesPendientesQuery(RevalorizacionesFalso.NutricionistaEjecutor), CancellationToken.None);

        Assert.Equal(2, resultado.Count);
        Assert.Equal(r1.Id, resultado[0].Id); // Más antigua primero
        Assert.Equal(r2.Id, resultado[1].Id);
        Assert.All(resultado, r => Assert.Null(r.ValoracionGeneradaId));
        Assert.All(resultado, r => Assert.Equal(RevalorizacionesFalso.NutricionistaEjecutor, r.NutricionistaId));
    }

    [Fact]
    public async Task ListarPendientes_con_nutricionista_inexistente_lanza_NoEncontrado()
    {
        _banco.Sembrar();
        var handler = new ListarRevalorizacionesPendientesHandler(_banco.RevalorizacionesRepo, _banco.NutricionistasRepo);

        await Assert.ThrowsAsync<NoEncontradoException>(
            () => handler.HandleAsync(new ListarRevalorizacionesPendientesQuery(Guid.NewGuid()), CancellationToken.None));
    }

    // ---- ObtenerRevalorizacion ----

    [Fact]
    public async Task Obtener_devuelve_el_dto_de_la_revalorizacion()
    {
        _banco.Sembrar();
        var revalorizacion = new Revalorizacion(Guid.NewGuid(), DateTime.UtcNow, RevalorizacionesFalso.NutricionistaEjecutor, RevalorizacionesFalso.ValoracionRevisada);
        _banco.Revalorizaciones.Add(revalorizacion);

        var handler = new ObtenerRevalorizacionHandler(_banco.RevalorizacionesRepo);
        var dto = await handler.HandleAsync(new ObtenerRevalorizacionQuery(revalorizacion.Id), CancellationToken.None);

        Assert.Equal(revalorizacion.Id, dto.Id);
        Assert.Equal(revalorizacion.FechaSolicitud, dto.FechaSolicitud);
        Assert.Equal(revalorizacion.NutricionistaId, dto.NutricionistaId);
        Assert.Equal(revalorizacion.ValoracionRevisadaId, dto.ValoracionRevisadaId);
        Assert.Null(dto.ValoracionGeneradaId);
    }

    [Fact]
    public async Task Obtener_inexistente_lanza_NoEncontrado()
    {
        _banco.Sembrar();
        var handler = new ObtenerRevalorizacionHandler(_banco.RevalorizacionesRepo);

        var ex = await Assert.ThrowsAsync<NoEncontradoException>(
            () => handler.HandleAsync(new ObtenerRevalorizacionQuery(Guid.NewGuid()), CancellationToken.None));

        Assert.Equal("revalorización", ex.Recurso);
    }
}
