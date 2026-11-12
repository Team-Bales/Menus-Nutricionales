using Menus.Api.Controladores;
using Menus.Application.Comun;
using Menus.Application.Valoraciones;
using Menus.Application.Valoraciones.ListarValoracionesPorDieta;
using Menus.Application.Valoraciones.ListarValoracionesPorPaciente;
using Menus.Application.Valoraciones.ObtenerValoracion;
using Menus.Application.Valoraciones.RegistrarValoracion;
using Microsoft.AspNetCore.Mvc;

namespace Menus.Api.Tests.Controladores;

/// <summary>
/// Pruebas de <see cref="ValoracionesController"/>: qué solicitud arma cada acción y qué código devuelve
/// (ADR-010, §8). Los errores los traduce el manejador global, que tiene sus propias pruebas.
/// </summary>
public sealed class ValoracionesControllerTests
{
    private static readonly ResultadoNutricionalDto Resultado = new(Guid.NewGuid(), "Buena", 4);

    private readonly DespachadorFalso _despachador = new();

    private ValoracionesController Controlador => new(_despachador);

    [Fact]
    public async Task Registrar_arma_el_comando_y_responde_201_con_la_ubicacion()
    {
        var menuId = Guid.NewGuid();
        var pacienteId = Guid.NewGuid();
        var resultadoId = Guid.NewGuid();
        var creada = new IdCreadoDto(Guid.NewGuid());
        _despachador.Resultado = creada;

        var respuesta = await Controlador.Registrar(new CuerpoDeValoracion(menuId, pacienteId, resultadoId), CancellationToken.None);

        var creado = Assert.IsType<CreatedAtActionResult>(respuesta.Result);
        Assert.Equal(nameof(ValoracionesController.Obtener), creado.ActionName);
        Assert.Equal(creada.Id, creado.RouteValues!["id"]);
        Assert.Same(creada, creado.Value);
        Assert.Equal(new RegistrarValoracionCommand(menuId, pacienteId, resultadoId), _despachador.Recibida);
    }

    [Fact]
    public async Task Obtener_arma_la_consulta_con_el_id_de_la_ruta()
    {
        var id = Guid.NewGuid();
        var dto = new ValoracionDto(id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Resultado, DateTime.UtcNow);
        _despachador.Resultado = dto;

        var respuesta = await Controlador.Obtener(id, CancellationToken.None);

        Assert.Same(dto, respuesta);
        Assert.Equal(new ObtenerValoracionQuery(id), _despachador.Recibida);
    }

    [Fact]
    public async Task ListarPorDieta_arma_la_consulta_con_el_id_de_la_ruta()
    {
        var dietaId = Guid.NewGuid();
        IReadOnlyList<ValoracionDto> lista = [];
        _despachador.Resultado = lista;

        var respuesta = await Controlador.ListarPorDieta(dietaId, CancellationToken.None);

        Assert.Same(lista, respuesta);
        Assert.Equal(new ListarValoracionesPorDietaQuery(dietaId), _despachador.Recibida);
    }

    [Fact]
    public async Task ListarPorPaciente_arma_la_consulta_con_el_id_de_la_ruta()
    {
        var pacienteId = Guid.NewGuid();
        IReadOnlyList<ValoracionDto> lista = [];
        _despachador.Resultado = lista;

        var respuesta = await Controlador.ListarPorPaciente(pacienteId, CancellationToken.None);

        Assert.Same(lista, respuesta);
        Assert.Equal(new ListarValoracionesPorPacienteQuery(pacienteId), _despachador.Recibida);
    }

    [Fact]
    public void El_cuerpo_expone_lo_que_recibe()
    {
        var menuId = Guid.NewGuid();
        var pacienteId = Guid.NewGuid();
        var resultadoId = Guid.NewGuid();

        var cuerpo = new CuerpoDeValoracion(menuId, pacienteId, resultadoId);

        Assert.Equal(menuId, cuerpo.MenuId);
        Assert.Equal(pacienteId, cuerpo.PacienteId);
        Assert.Equal(resultadoId, cuerpo.ResultadoNutricionalId);
    }
}
