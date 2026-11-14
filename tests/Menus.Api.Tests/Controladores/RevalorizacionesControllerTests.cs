using Menus.Api.Controladores;
using Menus.Application.Comun;
using Menus.Application.Revalorizaciones.ListarPendientes;
using Menus.Application.Revalorizaciones.RegistrarResultadoRevalorizacion;
using Menus.Application.Revalorizaciones.SolicitarRevalorizacion;
using Menus.Domain.Nomenclators;
using Microsoft.AspNetCore.Mvc;

namespace Menus.Api.Tests.Controladores;

/// <summary>Pruebas de <see cref="RevalorizacionesController"/> con despachador falso.</summary>
public sealed class RevalorizacionesControllerTests
{
    private readonly DespachadorFalso _despachador = new();
    private readonly RevalorizacionesController _controller;

    public RevalorizacionesControllerTests()
    {
        _controller = new RevalorizacionesController(_despachador);
    }

    [Fact]
    public async Task Solicitar_Valido_Retorna201ConLocationEnObtener()
    {
        var cuerpo = new CuerpoDeSolicitudDeRevalorizacion(Guid.NewGuid(), Guid.NewGuid());
        var revalorizacionId = Guid.NewGuid();
        _despachador.Resultado = new IdCreadoDto(revalorizacionId);

        var resultado = await _controller.Solicitar(cuerpo, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(resultado.Result);
        Assert.Equal(nameof(RevalorizacionesController.Obtener), created.ActionName);
        Assert.Equal(revalorizacionId, ((IdCreadoDto)created.Value!).Id);

        var comandoEnviado = Assert.IsType<SolicitarRevalorizacionCommand>(_despachador.Recibida);
        Assert.Equal(cuerpo.ValoracionRevisadaId, comandoEnviado.ValoracionRevisadaId);
        Assert.Equal(cuerpo.NutricionistaId, comandoEnviado.NutricionistaId);
    }

    [Fact]
    public async Task Obtener_Valido_Retorna200ConDto()
    {
        var id = Guid.NewGuid();
        var dto = new RevalorizacionDto(id, DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(), null);
        _despachador.Resultado = dto;

        var resultado = await _controller.Obtener(id, CancellationToken.None);

        Assert.Equal(dto, resultado);
        var queryEnviada = Assert.IsType<ObtenerRevalorizacionQuery>(_despachador.Recibida);
        Assert.Equal(id, queryEnviada.Id);
    }

    [Fact]
    public async Task RegistrarResultado_Valido_Retorna201ConLocationEnValoracionesController()
    {
        var id = Guid.NewGuid();
        var cuerpo = new CuerpoDeResultadoDeRevalorizacion(ResultadoNutricional.Buena.Id);
        var valoracionGeneradaId = Guid.NewGuid();
        _despachador.Resultado = new IdCreadoDto(valoracionGeneradaId);

        var resultado = await _controller.RegistrarResultado(id, cuerpo, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(resultado.Result);
        Assert.Equal(nameof(ValoracionesController.Obtener), created.ActionName);
        Assert.Equal(valoracionGeneradaId, created.RouteValues!["id"]);
        Assert.Equal(new IdCreadoDto(valoracionGeneradaId), created.Value);

        var comandoEnviado = Assert.IsType<RegistrarResultadoRevalorizacionCommand>(_despachador.Recibida);
        Assert.Equal(id, comandoEnviado.RevalorizacionId);
        Assert.Equal(cuerpo.ResultadoNutricionalId, comandoEnviado.ResultadoNutricionalId);
    }

    [Fact]
    public async Task ListarPendientes_Valido_Retorna200ConLista()
    {
        var nutricionistaId = Guid.NewGuid();
        var lista = new List<RevalorizacionDto>
        {
            new(Guid.NewGuid(), DateTime.UtcNow, nutricionistaId, Guid.NewGuid(), null),
            new(Guid.NewGuid(), DateTime.UtcNow.AddHours(1), nutricionistaId, Guid.NewGuid(), null)
        };
        _despachador.Resultado = lista;

        var resultado = await _controller.ListarPendientes(nutricionistaId, CancellationToken.None);

        Assert.Equal(lista, resultado);
        var queryEnviada = Assert.IsType<ListarRevalorizacionesPendientesQuery>(_despachador.Recibida);
        Assert.Equal(nutricionistaId, queryEnviada.NutricionistaId);
    }
}
