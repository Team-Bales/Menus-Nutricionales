using Menus.Application.Comun;
using Menus.Application.Pacientes;
using Menus.Application.Pacientes.ActualizarPaciente;
using Menus.Application.Pacientes.DesinscribirDieta;
using Menus.Application.Pacientes.InscribirDieta;
using Menus.Application.Pacientes.ListarPacientes;
using Menus.Application.Pacientes.ObtenerPaciente;
using Menus.Application.Tests.Catalogo.Dietas;
using Menus.Domain.Entidades;

namespace Menus.Application.Tests.Pacientes;

/// <summary>Pruebas de los manejadores del #73 contra los datos en memoria.</summary>
public sealed class ManejadoresDePacientesTests
{
    private readonly BancoDeDietasFalso _banco = new();
    private readonly SalasFalsas _salas = new();
    private readonly Sala _salaA;
    private readonly Sala _salaB;

    public ManejadoresDePacientesTests()
    {
        _salaA = _salas.Agregar("Sala A");
        _salaB = _salas.Agregar("Sala B");
    }

    private ListarPacientesHandler Listar() => new(_banco.PacientesRepo);

    private ObtenerPacienteHandler Obtener() => new(_banco.PacientesRepo, _salas, _banco.DietasRepo);

    private ActualizarPacienteHandler Actualizar() => new(_banco.PacientesRepo, _salas, _banco.UnidadDeTrabajo);

    private InscribirDietaHandler Inscribir() => new(_banco.PacientesRepo, _banco.DietasRepo, _banco.UnidadDeTrabajo);

    private DesinscribirDietaHandler Desinscribir() => new(_banco.PacientesRepo, _banco.UnidadDeTrabajo);

    private Paciente NuevoPaciente(string nombre = "Ana", int edad = 30, Sala? sala = null)
    {
        var paciente = new Paciente(Guid.NewGuid(), nombre, edad, (sala ?? _salaA).Id);
        _banco.Pacientes.Add(paciente);
        return paciente;
    }

    // ---- Listar ----

    [Fact]
    public async Task Listar_filtra_por_sala_y_por_dieta_y_ordena_por_nombre()
    {
        var renal = _banco.Agregar("Dieta renal");
        var carla = NuevoPaciente("Carla");
        var ana = NuevoPaciente("Ana");
        var beto = NuevoPaciente("Beto", sala: _salaB);
        carla.InscribirDieta(renal.Id);
        ana.InscribirDieta(renal.Id);
        beto.InscribirDieta(renal.Id);

        var resultado = await Listar().HandleAsync(
            new ListarPacientesQuery(_salaA.Id, renal.Id, 1, 20),
            CancellationToken.None);

        Assert.Equal(["Ana", "Carla"], resultado.Data.Select(p => p.Nombre));
        Assert.Equal(2, resultado.Total);
        Assert.Equal(1, resultado.Page);
        Assert.Equal(20, resultado.Size);

        var primero = resultado.Data[0];
        Assert.Equal(ana.Id, primero.Id);
        Assert.Equal(30, primero.Edad);
        Assert.Equal(_salaA.Id, primero.SalaId);
    }

    [Fact]
    public async Task Listar_pagina_y_da_el_total_sin_paginar()
    {
        NuevoPaciente("Ana");
        NuevoPaciente("Beto");
        NuevoPaciente("Carla");

        var resultado = await Listar().HandleAsync(new ListarPacientesQuery(Page: 2, Size: 2), CancellationToken.None);

        Assert.Equal(["Carla"], resultado.Data.Select(p => p.Nombre));
        Assert.Equal(3, resultado.Total);
    }

    // ---- Obtener ----

    [Fact]
    public async Task Obtener_devuelve_la_sala_y_las_dietas_con_nombre_ordenadas()
    {
        var renal = _banco.Agregar("Dieta renal");
        var blanda = _banco.Agregar("Dieta blanda");
        var paciente = NuevoPaciente("Ana", 0, _salaB);
        paciente.InscribirDieta(renal.Id);
        paciente.InscribirDieta(blanda.Id);

        var detalle = await Obtener().HandleAsync(new ObtenerPacienteQuery(paciente.Id), CancellationToken.None);

        Assert.Equal(paciente.Id, detalle.Id);
        Assert.Equal("Ana", detalle.Nombre);
        Assert.Equal(0, detalle.Edad);
        Assert.Equal(_salaB.Id, detalle.Sala.Id);
        Assert.Equal("Sala B", detalle.Sala.Nombre);
        Assert.Equal(["Dieta blanda", "Dieta renal"], detalle.Dietas.Select(d => d.Nombre));
        Assert.Equal([blanda.Id, renal.Id], detalle.Dietas.Select(d => d.Id));
    }

    [Fact]
    public async Task Obtener_un_paciente_sin_dietas_las_devuelve_vacias()
    {
        var paciente = NuevoPaciente();

        var detalle = await Obtener().HandleAsync(new ObtenerPacienteQuery(paciente.Id), CancellationToken.None);

        Assert.Empty(detalle.Dietas);
    }

    [Fact]
    public async Task Obtener_un_paciente_que_no_existe_es_404()
    {
        var id = Guid.NewGuid();

        var error = await Assert.ThrowsAsync<NoEncontradoException>(
            () => Obtener().HandleAsync(new ObtenerPacienteQuery(id), CancellationToken.None));

        Assert.Equal("paciente", error.Recurso);
        Assert.Equal(id, error.Id);
    }

    // ---- Actualizar ----

    [Fact]
    public async Task Actualizar_cambia_nombre_edad_y_sala_y_confirma_una_vez()
    {
        var paciente = NuevoPaciente("Ana", 30, _salaA);

        await Actualizar().HandleAsync(
            new ActualizarPacienteCommand(paciente.Id, "  Ana María  ", 31, _salaB.Id),
            CancellationToken.None);

        Assert.Equal("Ana María", paciente.Nombre);
        Assert.Equal(31, paciente.Edad);
        Assert.Equal(_salaB.Id, paciente.SalaId);
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Actualizar_acepta_la_edad_cero()
    {
        var paciente = NuevoPaciente(edad: 1);

        await Actualizar().HandleAsync(
            new ActualizarPacienteCommand(paciente.Id, "Ana", 0, _salaA.Id),
            CancellationToken.None);

        Assert.Equal(0, paciente.Edad);
    }

    [Fact]
    public async Task Actualizar_sin_cambiar_de_sala_no_la_busca()
    {
        var paciente = NuevoPaciente(sala: _salaA);
        _salas.Salas.Clear();

        await Actualizar().HandleAsync(
            new ActualizarPacienteCommand(paciente.Id, "Ana", 30, _salaA.Id),
            CancellationToken.None);

        Assert.Equal(_salaA.Id, paciente.SalaId);
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Actualizar_a_una_sala_que_no_existe_es_404_y_no_cambia_nada()
    {
        var paciente = NuevoPaciente("Ana", 30, _salaA);
        var salaInexistente = Guid.NewGuid();

        var error = await Assert.ThrowsAsync<NoEncontradoException>(() => Actualizar().HandleAsync(
            new ActualizarPacienteCommand(paciente.Id, "Otra", 40, salaInexistente),
            CancellationToken.None));

        Assert.Equal("sala", error.Recurso);
        Assert.Equal(salaInexistente, error.Id);
        Assert.Equal("Ana", paciente.Nombre);
        Assert.Equal(_salaA.Id, paciente.SalaId);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Actualizar_un_paciente_que_no_existe_es_404()
    {
        var error = await Assert.ThrowsAsync<NoEncontradoException>(() => Actualizar().HandleAsync(
            new ActualizarPacienteCommand(Guid.NewGuid(), "Ana", 30, _salaA.Id),
            CancellationToken.None));

        Assert.Equal("paciente", error.Recurso);
    }

    // ---- Inscribir ----

    [Fact]
    public async Task Inscribir_agrega_la_inscripcion_y_confirma()
    {
        var renal = _banco.Agregar("Dieta renal");
        var paciente = NuevoPaciente();

        await Inscribir().HandleAsync(new InscribirDietaCommand(paciente.Id, renal.Id), CancellationToken.None);

        Assert.Equal([renal.Id], paciente.Dietas.Select(d => d.DietaId));
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Inscribir_en_una_dieta_ya_inscrita_es_idempotente()
    {
        var renal = _banco.Agregar("Dieta renal");
        var paciente = NuevoPaciente();
        paciente.InscribirDieta(renal.Id);

        await Inscribir().HandleAsync(new InscribirDietaCommand(paciente.Id, renal.Id), CancellationToken.None);

        Assert.Single(paciente.Dietas);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Inscribir_en_una_dieta_que_no_existe_es_404()
    {
        var paciente = NuevoPaciente();
        var dietaInexistente = Guid.NewGuid();

        var error = await Assert.ThrowsAsync<NoEncontradoException>(() => Inscribir().HandleAsync(
            new InscribirDietaCommand(paciente.Id, dietaInexistente),
            CancellationToken.None));

        Assert.Equal("dieta", error.Recurso);
        Assert.Equal(dietaInexistente, error.Id);
        Assert.Empty(paciente.Dietas);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Inscribir_a_un_paciente_que_no_existe_es_404()
    {
        var renal = _banco.Agregar("Dieta renal");

        var error = await Assert.ThrowsAsync<NoEncontradoException>(() => Inscribir().HandleAsync(
            new InscribirDietaCommand(Guid.NewGuid(), renal.Id),
            CancellationToken.None));

        Assert.Equal("paciente", error.Recurso);
    }

    // ---- Desinscribir ----

    [Fact]
    public async Task Desinscribir_quita_la_inscripcion_y_conserva_las_demas()
    {
        var renal = _banco.Agregar("Dieta renal");
        var blanda = _banco.Agregar("Dieta blanda");
        var paciente = NuevoPaciente();
        paciente.InscribirDieta(renal.Id);
        paciente.InscribirDieta(blanda.Id);

        await Desinscribir().HandleAsync(new DesinscribirDietaCommand(paciente.Id, renal.Id), CancellationToken.None);

        Assert.Equal([blanda.Id], paciente.Dietas.Select(d => d.DietaId));
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Desinscribir_de_una_dieta_en_la_que_no_esta_inscrito_es_404()
    {
        var renal = _banco.Agregar("Dieta renal");
        var paciente = NuevoPaciente();

        var error = await Assert.ThrowsAsync<NoEncontradoException>(() => Desinscribir().HandleAsync(
            new DesinscribirDietaCommand(paciente.Id, renal.Id),
            CancellationToken.None));

        Assert.Equal("paciente inscrito en la dieta", error.Recurso);
        Assert.Equal(renal.Id, error.Id);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Desinscribir_a_un_paciente_que_no_existe_es_404()
    {
        var error = await Assert.ThrowsAsync<NoEncontradoException>(() => Desinscribir().HandleAsync(
            new DesinscribirDietaCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None));

        Assert.Equal("paciente", error.Recurso);
    }

    // ---- DTO ----

    [Fact]
    public void Los_DTO_exponen_sus_datos()
    {
        var sala = new SalaDelPacienteDto(_salaA.Id, "Sala A");
        var dieta = new DietaInscritaDto(Guid.NewGuid(), "Dieta renal");
        var detalle = new PacienteDetalleDto(Guid.NewGuid(), "Ana", 30, sala, [dieta]);

        Assert.Equal(_salaA.Id, sala.Id);
        Assert.Equal("Sala A", sala.Nombre);
        Assert.Equal("Dieta renal", dieta.Nombre);
        Assert.NotEqual(Guid.Empty, dieta.Id);
        Assert.Same(sala, detalle.Sala);
        Assert.Equal([dieta], detalle.Dietas);
    }
}
