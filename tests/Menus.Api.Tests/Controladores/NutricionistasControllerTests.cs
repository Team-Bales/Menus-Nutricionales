using Menus.Api.Controladores;
using Menus.Application.Comun;
using Menus.Application.Nutricionistas;
using Menus.Application.Nutricionistas.ActualizarNutricionista;
using Menus.Application.Nutricionistas.AsignarRolJefe;
using Menus.Application.Nutricionistas.AutorizarDieta;
using Menus.Application.Nutricionistas.ListarNutricionistas;
using Menus.Application.Nutricionistas.ObtenerNutricionista;
using Menus.Application.Nutricionistas.RevocarDieta;
using Microsoft.AspNetCore.Mvc;

namespace Menus.Api.Tests.Controladores;

/// <summary>
/// Pruebas de <see cref="NutricionistasController"/>: qué solicitud arma cada acción y qué código devuelve
/// (ADR-010, §8). Los errores los traduce el manejador global, que tiene sus propias pruebas.
/// </summary>
public sealed class NutricionistasControllerTests
{
    private readonly DespachadorFalso _despachador = new();

    private NutricionistasController Controlador => new(_despachador);

    [Fact]
    public async Task Listar_arma_la_consulta_con_la_especialidad_y_la_paginacion()
    {
        var especialidadId = Guid.NewGuid();
        var pagina = new ResultadoPaginado<NutricionistaDto>([], 0, 2, 5);
        _despachador.Resultado = pagina;

        var respuesta = await Controlador.Listar(especialidadId, CancellationToken.None, page: 2, size: 5);

        Assert.Same(pagina, respuesta);
        Assert.Equal(new ListarNutricionistasQuery(especialidadId, 2, 5), _despachador.Recibida);
    }

    [Fact]
    public async Task Listar_sin_especialidad_usa_la_primera_pagina_de_20()
    {
        _despachador.Resultado = new ResultadoPaginado<NutricionistaDto>([], 0, 1, 20);

        await Controlador.Listar(null, CancellationToken.None);

        Assert.Equal(new ListarNutricionistasQuery(null, 1, 20), _despachador.Recibida);
    }

    [Fact]
    public async Task Obtener_arma_la_consulta_con_el_id_de_la_ruta()
    {
        var id = Guid.NewGuid();
        var dto = new NutricionistaDetalleDto(id, "Ana", Guid.NewGuid(), false, []);
        _despachador.Resultado = dto;

        var respuesta = await Controlador.Obtener(id, CancellationToken.None);

        Assert.Same(dto, respuesta);
        Assert.Equal(new ObtenerNutricionistaQuery(id), _despachador.Recibida);
    }

    [Fact]
    public async Task Actualizar_arma_el_comando_con_el_id_de_la_ruta_y_responde_204()
    {
        var id = Guid.NewGuid();
        var especialidadId = Guid.NewGuid();
        _despachador.Resultado = Unidad.Valor;

        var respuesta = await Controlador.Actualizar(id, new CuerpoDeActualizacionDeNutricionista("Ana", especialidadId), CancellationToken.None);

        Assert.IsType<NoContentResult>(respuesta);
        Assert.Equal(new ActualizarNutricionistaCommand(id, "Ana", especialidadId), _despachador.Recibida);
    }

    [Fact]
    public async Task AutorizarDieta_arma_el_comando_con_el_id_de_la_ruta_y_responde_204()
    {
        var id = Guid.NewGuid();
        var dietaId = Guid.NewGuid();
        _despachador.Resultado = Unidad.Valor;

        var respuesta = await Controlador.AutorizarDieta(id, new CuerpoDeAutorizacionDeDieta(dietaId), CancellationToken.None);

        Assert.IsType<NoContentResult>(respuesta);
        Assert.Equal(new AutorizarDietaCommand(id, dietaId), _despachador.Recibida);
    }

    [Fact]
    public async Task RevocarDieta_arma_el_comando_con_los_dos_ids_de_la_ruta_y_responde_204()
    {
        var id = Guid.NewGuid();
        var dietaId = Guid.NewGuid();
        _despachador.Resultado = Unidad.Valor;

        var respuesta = await Controlador.RevocarDieta(id, dietaId, CancellationToken.None);

        Assert.IsType<NoContentResult>(respuesta);
        Assert.Equal(new RevocarDietaCommand(id, dietaId), _despachador.Recibida);
    }

    [Fact]
    public async Task AsignarRolJefe_arma_el_comando_con_el_id_de_la_ruta_y_responde_204()
    {
        var id = Guid.NewGuid();
        _despachador.Resultado = Unidad.Valor;

        var respuesta = await Controlador.AsignarRolJefe(id, CancellationToken.None);

        Assert.IsType<NoContentResult>(respuesta);
        Assert.Equal(new AsignarRolJefeCommand(id), _despachador.Recibida);
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
