using Menus.Api.Controladores;
using Menus.Application.Comun;
using Menus.Application.Salas;
using Menus.Application.Salas.ActualizarSala;
using Menus.Application.Salas.EliminarSala;
using Menus.Application.Salas.ListarSalas;
using Menus.Application.Salas.ObtenerSala;
using Menus.Application.Salas.RegistrarSala;
using Microsoft.AspNetCore.Mvc;

namespace Menus.Api.Tests.Controladores;

/// <summary>
/// Pruebas de <see cref="SalasController"/>: qué solicitud arma cada acción y qué código devuelve (ADR-010, §8).
/// Los errores los traduce el manejador global, que tiene sus propias pruebas.
/// </summary>
public sealed class SalasControllerTests
{
    private readonly DespachadorFalso _despachador = new();

    private SalasController Controlador => new(_despachador);

    [Fact]
    public async Task Listar_arma_la_consulta_sin_parametros()
    {
        IReadOnlyList<SalaDto> salas = [new(Guid.NewGuid(), "Sala A")];
        _despachador.Resultado = salas;

        var respuesta = await Controlador.Listar(CancellationToken.None);

        Assert.Same(salas, respuesta);
        Assert.Equal(new ListarSalasQuery(), _despachador.Recibida);
    }

    [Fact]
    public async Task Obtener_arma_la_consulta_con_el_id_de_la_ruta()
    {
        var id = Guid.NewGuid();
        var dto = new SalaDto(id, "Sala A");
        _despachador.Resultado = dto;

        var respuesta = await Controlador.Obtener(id, CancellationToken.None);

        Assert.Same(dto, respuesta);
        Assert.Equal(new ObtenerSalaQuery(id), _despachador.Recibida);
    }

    [Fact]
    public async Task Registrar_arma_el_comando_y_responde_201()
    {
        var creada = new IdCreadoDto(Guid.NewGuid());
        _despachador.Resultado = creada;

        var respuesta = await Controlador.Registrar(new CuerpoDeSala("Sala Nueva"), CancellationToken.None);

        var resultado = Assert.IsType<CreatedResult>(respuesta);
        Assert.Equal($"/api/salas/{creada.Id}", resultado.Location);
        Assert.Same(creada, resultado.Value);
        Assert.Equal(new RegistrarSalaCommand("Sala Nueva"), _despachador.Recibida);
    }

    [Fact]
    public async Task Actualizar_arma_el_comando_con_el_id_de_la_ruta_y_responde_204()
    {
        var id = Guid.NewGuid();
        _despachador.Resultado = Unidad.Valor;

        var respuesta = await Controlador.Actualizar(id, new CuerpoDeSala("Sala A"), CancellationToken.None);

        Assert.IsType<NoContentResult>(respuesta);
        Assert.Equal(new ActualizarSalaCommand(id, "Sala A"), _despachador.Recibida);
    }

    [Fact]
    public async Task Eliminar_arma_el_comando_con_el_id_de_la_ruta_y_responde_204()
    {
        var id = Guid.NewGuid();
        _despachador.Resultado = Unidad.Valor;

        var respuesta = await Controlador.Eliminar(id, CancellationToken.None);

        Assert.IsType<NoContentResult>(respuesta);
        Assert.Equal(new EliminarSalaCommand(id), _despachador.Recibida);
    }

    /// <summary>Guarda la solicitud recibida y devuelve el resultado preparado.</summary>
    private sealed class DespachadorFalso : IDespachador
    {
        public object? Resultado { get; set; }

        public object? Recibida { get; private set; }

        public Task<TResultado> EnviarAsync<TResultado>(ISolicitud<TResultado> solicitud, CancellationToken ct = default)
        {
            Recibida = solicitud;
            return Task.FromResult((TResultado)Resultado!);
        }
    }
}
