using Menus.Api.Controladores;
using Menus.Application.Comun;
using Menus.Application.Pacientes;
using Menus.Application.Pacientes.ActualizarPaciente;
using Menus.Application.Pacientes.DesinscribirDieta;
using Menus.Application.Pacientes.InscribirDieta;
using Menus.Application.Pacientes.ListarPacientes;
using Menus.Application.Pacientes.ObtenerPaciente;
using Microsoft.AspNetCore.Mvc;

namespace Menus.Api.Tests.Controladores;

/// <summary>
/// Pruebas de <see cref="PacientesController"/>: qué solicitud arma cada acción y qué código devuelve
/// (ADR-010, §8). Los errores los traduce el manejador global, que tiene sus propias pruebas.
/// </summary>
public sealed class PacientesControllerTests
{
    private static readonly Guid Sala = Guid.NewGuid();
    private static readonly Guid Dieta = Guid.NewGuid();

    private readonly DespachadorFalso _despachador = new();

    private PacientesController Controlador => new(_despachador);

    [Fact]
    public async Task Listar_arma_la_consulta_con_los_filtros_y_la_paginacion()
    {
        var pagina = new ResultadoPaginado<PacienteDto>([], 0, 2, 5);
        _despachador.Resultado = pagina;

        var respuesta = await Controlador.Listar(Sala, Dieta, CancellationToken.None, page: 2, size: 5);

        Assert.Same(pagina, respuesta);
        Assert.Equal(new ListarPacientesQuery(Sala, Dieta, 2, 5), _despachador.Recibida);
    }

    [Fact]
    public async Task Listar_sin_filtros_usa_la_primera_pagina_de_20()
    {
        _despachador.Resultado = new ResultadoPaginado<PacienteDto>([], 0, 1, 20);

        await Controlador.Listar(null, null, CancellationToken.None);

        Assert.Equal(new ListarPacientesQuery(null, null, 1, 20), _despachador.Recibida);
    }

    [Fact]
    public async Task Obtener_arma_la_consulta_con_el_id_de_la_ruta()
    {
        var id = Guid.NewGuid();
        var dto = new PacienteDetalleDto(id, "Ana", 30, new SalaDelPacienteDto(Sala, "Sala A"), []);
        _despachador.Resultado = dto;

        var respuesta = await Controlador.Obtener(id, CancellationToken.None);

        Assert.Same(dto, respuesta);
        Assert.Equal(new ObtenerPacienteQuery(id), _despachador.Recibida);
    }

    [Fact]
    public async Task Actualizar_arma_el_comando_con_el_id_de_la_ruta_y_responde_204()
    {
        var id = Guid.NewGuid();
        _despachador.Resultado = Unidad.Valor;

        var respuesta = await Controlador.Actualizar(id, new DatosDePaciente("Ana", 0, Sala), CancellationToken.None);

        Assert.IsType<NoContentResult>(respuesta);
        Assert.Equal(new ActualizarPacienteCommand(id, "Ana", 0, Sala), _despachador.Recibida);
    }

    [Fact]
    public async Task InscribirDieta_arma_el_comando_y_responde_204()
    {
        var id = Guid.NewGuid();
        _despachador.Resultado = Unidad.Valor;

        var respuesta = await Controlador.InscribirDieta(id, new InscripcionEnDieta(Dieta), CancellationToken.None);

        Assert.IsType<NoContentResult>(respuesta);
        Assert.Equal(new InscribirDietaCommand(id, Dieta), _despachador.Recibida);
    }

    [Fact]
    public async Task DesinscribirDieta_arma_el_comando_con_los_dos_ids_de_la_ruta_y_responde_204()
    {
        var id = Guid.NewGuid();
        _despachador.Resultado = Unidad.Valor;

        var respuesta = await Controlador.DesinscribirDieta(id, Dieta, CancellationToken.None);

        Assert.IsType<NoContentResult>(respuesta);
        Assert.Equal(new DesinscribirDietaCommand(id, Dieta), _despachador.Recibida);
    }

    [Fact]
    public void Los_cuerpos_exponen_lo_que_reciben()
    {
        var datos = new DatosDePaciente("Ana", 7, Sala);
        var inscripcion = new InscripcionEnDieta(Dieta);

        Assert.Equal("Ana", datos.Nombre);
        Assert.Equal(7, datos.Edad);
        Assert.Equal(Sala, datos.SalaId);
        Assert.Equal(Dieta, inscripcion.DietaId);
    }
}
