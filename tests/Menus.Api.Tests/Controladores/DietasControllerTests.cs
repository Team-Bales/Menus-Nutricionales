using Menus.Api.Controladores;
using Menus.Application.Catalogo.Dietas;
using Menus.Application.Catalogo.Dietas.ActualizarDieta;
using Menus.Application.Catalogo.Dietas.AgregarGrupoDieta;
using Menus.Application.Catalogo.Dietas.AgregarRestriccionDieta;
using Menus.Application.Catalogo.Dietas.DisponibilidadCalorica;
using Menus.Application.Catalogo.Dietas.EliminarDieta;
using Menus.Application.Catalogo.Dietas.ListarDietas;
using Menus.Application.Catalogo.Dietas.ObtenerDieta;
using Menus.Application.Catalogo.Dietas.QuitarGrupoDieta;
using Menus.Application.Catalogo.Dietas.QuitarRestriccionDieta;
using Menus.Application.Catalogo.Dietas.RegistrarDieta;
using Menus.Application.Comun;
using Menus.Domain.Nomenclators;
using Microsoft.AspNetCore.Mvc;

namespace Menus.Api.Tests.Controladores;

/// <summary>
/// Pruebas de <see cref="DietasController"/>: qué solicitud arma cada acción y qué código devuelve
/// (ADR-010, §8). Los errores los traduce el manejador global, que tiene sus propias pruebas.
/// </summary>
public sealed class DietasControllerTests
{
    private static readonly Guid Programa = Guid.NewGuid();
    private static readonly Guid Grupo = Guid.NewGuid();
    private static readonly Guid Restriccion = Guid.NewGuid();

    private readonly DespachadorFalso _despachador = new();

    private DietasController Controlador => new(_despachador);

    [Fact]
    public async Task Registrar_envia_el_comando_y_responde_201_hacia_Obtener()
    {
        var id = Guid.NewGuid();
        _despachador.Resultado = new IdCreadoDto(id);
        var comando = new RegistrarDietaCommand("Dieta renal", Programa, [new GrupoACubrir(Grupo, TipoCobertura.Requerido)], [Restriccion]);

        var respuesta = await Controlador.Registrar(comando, CancellationToken.None);

        var creada = Assert.IsType<CreatedAtActionResult>(respuesta.Result);
        Assert.Equal(nameof(DietasController.Obtener), creada.ActionName);
        Assert.Equal(id, creada.RouteValues!["id"]);
        Assert.Equal(new IdCreadoDto(id), creada.Value);
        Assert.Same(comando, _despachador.Recibida);
    }

    [Fact]
    public async Task Listar_arma_la_consulta_con_el_filtro_y_la_paginacion()
    {
        var pagina = new ResultadoPaginado<DietaResumenDto>([], 0, 2, 5);
        _despachador.Resultado = pagina;

        var respuesta = await Controlador.Listar(Programa, CancellationToken.None, page: 2, size: 5);

        Assert.Same(pagina, respuesta);
        Assert.Equal(new ListarDietasQuery(Programa, 2, 5), _despachador.Recibida);
    }

    [Fact]
    public async Task Obtener_arma_la_consulta_con_el_id_de_la_ruta()
    {
        var id = Guid.NewGuid();
        var dto = new DietaDto(id, "Dieta", Programa, [], []);
        _despachador.Resultado = dto;

        var respuesta = await Controlador.Obtener(id, CancellationToken.None);

        Assert.Same(dto, respuesta);
        Assert.Equal(new ObtenerDietaQuery(id), _despachador.Recibida);
    }

    [Fact]
    public async Task Actualizar_toma_el_id_de_la_ruta_y_los_datos_del_cuerpo_y_responde_204()
    {
        var id = Guid.NewGuid();
        _despachador.Resultado = Unidad.Valor;

        var respuesta = await Controlador.Actualizar(id, new DatosDeDieta("Dieta nueva", Programa), CancellationToken.None);

        Assert.IsType<NoContentResult>(respuesta);
        Assert.Equal(new ActualizarDietaCommand(id, "Dieta nueva", Programa), _despachador.Recibida);
    }

    [Fact]
    public async Task Eliminar_envia_el_comando_y_responde_204()
    {
        var id = Guid.NewGuid();
        _despachador.Resultado = Unidad.Valor;

        var respuesta = await Controlador.Eliminar(id, CancellationToken.None);

        Assert.IsType<NoContentResult>(respuesta);
        Assert.Equal(new EliminarDietaCommand(id), _despachador.Recibida);
    }

    [Fact]
    public async Task AgregarGrupo_toma_el_id_de_la_ruta_y_la_cobertura_del_cuerpo_y_responde_204()
    {
        var id = Guid.NewGuid();
        _despachador.Resultado = Unidad.Valor;

        var respuesta = await Controlador.AgregarGrupo(id, new CoberturaDeGrupo(Grupo, TipoCobertura.Requerido), CancellationToken.None);

        Assert.IsType<NoContentResult>(respuesta);
        Assert.Equal(new AgregarGrupoDietaCommand(id, Grupo, TipoCobertura.Requerido), _despachador.Recibida);
    }

    [Fact]
    public async Task QuitarGrupo_envia_el_comando_con_los_dos_ids_de_la_ruta_y_responde_204()
    {
        var id = Guid.NewGuid();
        _despachador.Resultado = Unidad.Valor;

        var respuesta = await Controlador.QuitarGrupo(id, Grupo, CancellationToken.None);

        Assert.IsType<NoContentResult>(respuesta);
        Assert.Equal(new QuitarGrupoDietaCommand(id, Grupo), _despachador.Recibida);
    }

    [Fact]
    public async Task AgregarRestriccion_toma_el_id_de_la_ruta_y_la_restriccion_del_cuerpo_y_responde_204()
    {
        var id = Guid.NewGuid();
        _despachador.Resultado = Unidad.Valor;

        var respuesta = await Controlador.AgregarRestriccion(id, new RestriccionDeDieta(Restriccion), CancellationToken.None);

        Assert.IsType<NoContentResult>(respuesta);
        Assert.Equal(new AgregarRestriccionDietaCommand(id, Restriccion), _despachador.Recibida);
    }

    [Fact]
    public async Task QuitarRestriccion_envia_el_comando_con_los_dos_ids_de_la_ruta_y_responde_204()
    {
        var id = Guid.NewGuid();
        _despachador.Resultado = Unidad.Valor;

        var respuesta = await Controlador.QuitarRestriccion(id, Restriccion, CancellationToken.None);

        Assert.IsType<NoContentResult>(respuesta);
        Assert.Equal(new QuitarRestriccionDietaCommand(id, Restriccion), _despachador.Recibida);
    }

    [Fact]
    public async Task ObtenerDisponibilidadCalorica_envia_la_consulta_con_el_id_de_la_ruta()
    {
        var id = Guid.NewGuid();
        var dto = new DisponibilidadCaloricaDto(id, []);
        _despachador.Resultado = dto;

        var respuesta = await Controlador.ObtenerDisponibilidadCalorica(id, CancellationToken.None);

        Assert.Same(dto, respuesta);
        Assert.Equal(new DisponibilidadCaloricaQuery(id), _despachador.Recibida);
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
