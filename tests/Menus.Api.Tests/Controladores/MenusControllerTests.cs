using Menus.Api.Controladores;
using Menus.Application.Comun;
using Menus.Application.GeneracionDeMenus.GenerarMenuAutomaticamente;
using Menus.Application.GestionDeMenus;
using Menus.Application.GestionDeMenus.ListarMenus;
using Menus.Application.GestionDeMenus.ObtenerMenu;
using Menus.Application.GestionDeMenus.RegistrarReemplazo;
using Menus.Application.GestionDeMenus.ValidarMenu;
using Menus.Domain.Entidades;
using Microsoft.AspNetCore.Mvc;

namespace Menus.Api.Tests.Controladores;

/// <summary>
/// Pruebas de <see cref="MenusController"/>: qué solicitud arma cada acción y qué código devuelve
/// (ADR-010, §8). Los errores los traduce el manejador global, que tiene sus propias pruebas.
/// </summary>
public sealed class MenusControllerTests
{
    private static readonly Guid Dieta = Guid.NewGuid();

    private readonly DespachadorFalso _despachador = new();

    private MenusController Controlador => new(_despachador);

    [Fact]
    public async Task GenerarAutomaticamente_arma_el_comando_con_la_dieta_de_la_ruta_y_responde_201()
    {
        var dietaId = Guid.NewGuid();
        var tipoId = Guid.NewGuid();
        var grupoId = Guid.NewGuid();
        var creado = new IdCreadoDto(Guid.NewGuid());
        _despachador.Resultado = creado;

        var cuerpo = new CuerpoDeGeneracionAutomatica(10, [new ProporcionPorTipoDePreparacion(tipoId, 1m)], [grupoId]);

        var respuesta = await Controlador.GenerarAutomaticamente(dietaId, cuerpo, CancellationToken.None);

        var resultado = Assert.IsType<CreatedResult>(respuesta);
        Assert.Equal($"/api/menus/{creado.Id}", resultado.Location);
        Assert.Same(creado, resultado.Value);

        // Comparar el récord completo usaría igualdad por referencia en sus listas (no por contenido):
        // se compara campo por campo, y las listas aparte, donde Assert.Equal sí compara elemento a elemento.
        var comando = Assert.IsType<GenerarMenuAutomaticamenteCommand>(_despachador.Recibida);
        Assert.Equal(dietaId, comando.DietaId);
        Assert.Equal(10, comando.CantidadTotalAlimentos);
        Assert.Equal([(tipoId, 1m)], comando.Distribucion);
        Assert.Equal([grupoId], comando.GruposNutricionalesIds);
    }

    [Fact]
    public async Task GenerarAutomaticamente_sin_distribucion_no_lanza_y_arma_el_comando_con_una_lista_vacia()
    {
        // Antes de validar, el binder puede dejar Distribucion en null (Program.cs suprime el [Required]
        // implícito para que sea el validador quien responda 422, no un 500 por NullReferenceException).
        var dietaId = Guid.NewGuid();
        var cuerpo = new CuerpoDeGeneracionAutomatica(10, null!, [Guid.NewGuid()]);
        _despachador.Resultado = new IdCreadoDto(Guid.NewGuid());

        await Controlador.GenerarAutomaticamente(dietaId, cuerpo, CancellationToken.None);

        var comando = Assert.IsType<GenerarMenuAutomaticamenteCommand>(_despachador.Recibida);
        Assert.Empty(comando.Distribucion);
    }

    [Fact]
    public async Task Listar_arma_la_consulta_con_la_dieta_el_estado_y_la_paginacion()
    {
        var pagina = new ResultadoPaginado<MenuDto>([], 0, 2, 5);
        _despachador.Resultado = pagina;

        var respuesta = await Controlador.Listar(Dieta, EstadoDeMenu.Pendiente, CancellationToken.None, page: 2, size: 5);

        Assert.Same(pagina, respuesta);
        Assert.Equal(new ListarMenusQuery(Dieta, EstadoDeMenu.Pendiente, 2, 5), _despachador.Recibida);
    }

    [Fact]
    public async Task Listar_sin_estado_usa_la_primera_pagina_de_20()
    {
        _despachador.Resultado = new ResultadoPaginado<MenuDto>([], 0, 1, 20);

        await Controlador.Listar(Dieta, null, CancellationToken.None);

        Assert.Equal(new ListarMenusQuery(Dieta, null, 1, 20), _despachador.Recibida);
    }

    [Fact]
    public async Task Obtener_arma_la_consulta_con_el_id_de_la_ruta()
    {
        var id = Guid.NewGuid();
        var dto = new MenuDetalleDto(
            id, Dieta, DateTime.UtcNow, new(Guid.NewGuid(), "Ana"), true, 1, "Pendiente", null, null, [], [], []);
        _despachador.Resultado = dto;

        var respuesta = await Controlador.Obtener(id, CancellationToken.None);

        Assert.Same(dto, respuesta);
        Assert.Equal(new ObtenerMenuQuery(id), _despachador.Recibida);
    }

    [Fact]
    public async Task Validar_arma_el_comando_con_el_id_de_la_ruta_y_responde_204()
    {
        var id = Guid.NewGuid();
        _despachador.Resultado = Unidad.Valor;

        var respuesta = await Controlador.Validar(id, new DictamenDeMenu(false, "Faltan frutas"), CancellationToken.None);

        Assert.IsType<NoContentResult>(respuesta);
        Assert.Equal(new ValidarMenuCommand(id, false, "Faltan frutas"), _despachador.Recibida);
    }

    [Fact]
    public async Task RegistrarReemplazo_arma_el_comando_con_los_dos_menus_y_responde_204()
    {
        var id = Guid.NewGuid();
        var rechazado = Guid.NewGuid();
        _despachador.Resultado = Unidad.Valor;

        var respuesta = await Controlador.RegistrarReemplazo(id, new MenuReemplazado(rechazado), CancellationToken.None);

        Assert.IsType<NoContentResult>(respuesta);
        Assert.Equal(new RegistrarReemplazoCommand(id, rechazado), _despachador.Recibida);
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
