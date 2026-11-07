using Menus.Api.Controladores;
using Menus.Application.Catalogo.Nomencladores;
using Menus.Application.Catalogo.Nomencladores.Especialidades;
using Menus.Application.Catalogo.Nomencladores.GruposNutricionales;
using Menus.Application.Catalogo.Nomencladores.ProgramasAtencion;
using Menus.Application.Catalogo.Nomencladores.RestriccionesAlimentarias;
using Menus.Application.Comun;
using Microsoft.AspNetCore.Mvc;

namespace Menus.Api.Tests.Controladores;

/// <summary>
/// Pruebas de <see cref="NomencladoresController"/>: qué solicitud arma cada acción y qué código devuelve
/// (ADR-010, §8). Los errores los traduce el manejador global, que tiene sus propias pruebas.
/// </summary>
public sealed class NomencladoresControllerTests
{
    private readonly DespachadorFalso _despachador = new();

    private NomencladoresController Controlador => new(_despachador);

    // ---- Grupos nutricionales ----

    [Fact]
    public async Task ListarGruposNutricionales_envia_la_consulta()
    {
        var lista = new List<NomencladorDto> { new(Guid.NewGuid(), "Cereales") };
        _despachador.Resultado = lista;

        var respuesta = await Controlador.ListarGruposNutricionales(CancellationToken.None);

        Assert.Same(lista, respuesta);
        Assert.Equal(new ListarGruposNutricionalesQuery(), _despachador.Recibida);
    }

    [Fact]
    public async Task RegistrarGrupoNutricional_envia_el_comando_y_responde_201_con_la_ubicacion()
    {
        var id = Guid.NewGuid();
        _despachador.Resultado = new IdCreadoDto(id);
        var comando = new RegistrarGrupoNutricionalCommand("Cereales");

        var respuesta = await Controlador.RegistrarGrupoNutricional(comando, CancellationToken.None);

        var creado = Assert.IsType<CreatedResult>(respuesta.Result);
        Assert.Equal($"/api/nomencladores/grupos-nutricionales/{id}", creado.Location);
        Assert.Same(comando, _despachador.Recibida);
    }

    [Fact]
    public async Task EliminarGrupoNutricional_envia_el_comando_y_responde_204()
    {
        var id = Guid.NewGuid();
        _despachador.Resultado = Unidad.Valor;

        var respuesta = await Controlador.EliminarGrupoNutricional(id, CancellationToken.None);

        Assert.IsType<NoContentResult>(respuesta);
        Assert.Equal(new EliminarGrupoNutricionalCommand(id), _despachador.Recibida);
    }

    // ---- Restricciones alimentarias ----

    [Fact]
    public async Task ListarRestriccionesAlimentarias_envia_la_consulta()
    {
        _despachador.Resultado = new List<NomencladorDto>();

        await Controlador.ListarRestriccionesAlimentarias(CancellationToken.None);

        Assert.Equal(new ListarRestriccionesAlimentariasQuery(), _despachador.Recibida);
    }

    [Fact]
    public async Task RegistrarRestriccionAlimentaria_envia_el_comando_y_responde_201_con_la_ubicacion()
    {
        var id = Guid.NewGuid();
        _despachador.Resultado = new IdCreadoDto(id);
        var comando = new RegistrarRestriccionAlimentariaCommand("Sin gluten");

        var respuesta = await Controlador.RegistrarRestriccionAlimentaria(comando, CancellationToken.None);

        var creada = Assert.IsType<CreatedResult>(respuesta.Result);
        Assert.Equal($"/api/nomencladores/restricciones-alimentarias/{id}", creada.Location);
        Assert.Same(comando, _despachador.Recibida);
    }

    [Fact]
    public async Task EliminarRestriccionAlimentaria_envia_el_comando_y_responde_204()
    {
        var id = Guid.NewGuid();
        _despachador.Resultado = Unidad.Valor;

        var respuesta = await Controlador.EliminarRestriccionAlimentaria(id, CancellationToken.None);

        Assert.IsType<NoContentResult>(respuesta);
        Assert.Equal(new EliminarRestriccionAlimentariaCommand(id), _despachador.Recibida);
    }

    // ---- Programas de atención ----

    [Fact]
    public async Task ListarProgramasAtencion_envia_la_consulta()
    {
        _despachador.Resultado = new List<NomencladorDto>();

        await Controlador.ListarProgramasAtencion(CancellationToken.None);

        Assert.Equal(new ListarProgramasAtencionQuery(), _despachador.Recibida);
    }

    [Fact]
    public async Task RegistrarProgramaAtencion_envia_el_comando_y_responde_201_con_la_ubicacion()
    {
        var id = Guid.NewGuid();
        _despachador.Resultado = new IdCreadoDto(id);
        var comando = new RegistrarProgramaAtencionCommand("Oncología");

        var respuesta = await Controlador.RegistrarProgramaAtencion(comando, CancellationToken.None);

        var creado = Assert.IsType<CreatedResult>(respuesta.Result);
        Assert.Equal($"/api/nomencladores/programas-atencion/{id}", creado.Location);
        Assert.Same(comando, _despachador.Recibida);
    }

    [Fact]
    public async Task EliminarProgramaAtencion_envia_el_comando_y_responde_204()
    {
        var id = Guid.NewGuid();
        _despachador.Resultado = Unidad.Valor;

        var respuesta = await Controlador.EliminarProgramaAtencion(id, CancellationToken.None);

        Assert.IsType<NoContentResult>(respuesta);
        Assert.Equal(new EliminarProgramaAtencionCommand(id), _despachador.Recibida);
    }

    // ---- Especialidades ----

    [Fact]
    public async Task ListarEspecialidades_envia_la_consulta()
    {
        _despachador.Resultado = new List<NomencladorDto>();

        await Controlador.ListarEspecialidades(CancellationToken.None);

        Assert.Equal(new ListarEspecialidadesQuery(), _despachador.Recibida);
    }

    [Fact]
    public async Task RegistrarEspecialidad_envia_el_comando_y_responde_201_con_la_ubicacion()
    {
        var id = Guid.NewGuid();
        _despachador.Resultado = new IdCreadoDto(id);
        var comando = new RegistrarEspecialidadCommand("Nutrición Clínica");

        var respuesta = await Controlador.RegistrarEspecialidad(comando, CancellationToken.None);

        var creada = Assert.IsType<CreatedResult>(respuesta.Result);
        Assert.Equal($"/api/nomencladores/especialidades/{id}", creada.Location);
        Assert.Same(comando, _despachador.Recibida);
    }

    [Fact]
    public async Task EliminarEspecialidad_envia_el_comando_y_responde_204()
    {
        var id = Guid.NewGuid();
        _despachador.Resultado = Unidad.Valor;

        var respuesta = await Controlador.EliminarEspecialidad(id, CancellationToken.None);

        Assert.IsType<NoContentResult>(respuesta);
        Assert.Equal(new EliminarEspecialidadCommand(id), _despachador.Recibida);
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
