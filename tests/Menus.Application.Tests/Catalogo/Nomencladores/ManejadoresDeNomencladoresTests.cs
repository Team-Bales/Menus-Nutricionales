using Menus.Application.Catalogo.Nomencladores.Especialidades;
using Menus.Application.Catalogo.Nomencladores.GruposNutricionales;
using Menus.Application.Catalogo.Nomencladores.ProgramasAtencion;
using Menus.Application.Catalogo.Nomencladores.RestriccionesAlimentarias;
using Menus.Application.Comun;
using Menus.Domain.Excepciones;

namespace Menus.Application.Tests.Catalogo.Nomencladores;

/// <summary>Pruebas de los manejadores del #64 contra el catálogo en memoria.</summary>
public sealed class ManejadoresDeNomencladoresTests
{
    private readonly BancoDeNomencladoresFalso _banco = new();

    // ---- Grupos nutricionales (profundidad completa; los otros tres comparten la misma lógica) ----

    [Fact]
    public async Task RegistrarGrupo_guarda_el_grupo_y_confirma_una_vez()
    {
        var comando = new RegistrarGrupoNutricionalCommand("  Legumbres  ");

        var creado = await new RegistrarGrupoNutricionalHandler(_banco.GruposRepo, _banco.UnidadDeTrabajo)
            .HandleAsync(comando, CancellationToken.None);

        var grupo = Assert.Single(_banco.Grupos);
        Assert.Equal(creado.Id, grupo.Id);
        Assert.Equal("Legumbres", grupo.Nombre);
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task RegistrarGrupo_con_un_nombre_que_ya_existe_lanza_RD08_sin_confirmar()
    {
        _banco.AgregarGrupo("Cereales");
        var comando = new RegistrarGrupoNutricionalCommand("Cereales");

        var ex = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => new RegistrarGrupoNutricionalHandler(_banco.GruposRepo, _banco.UnidadDeTrabajo).HandleAsync(comando, CancellationToken.None));

        Assert.Equal("RD-08", ex.Regla);
        Assert.Single(_banco.Grupos);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task EliminarGrupo_sin_uso_lo_quita_y_confirma()
    {
        var grupo = _banco.AgregarGrupo("Cereales");

        await new EliminarGrupoNutricionalHandler(_banco.GruposRepo, _banco.UnidadDeTrabajo)
            .HandleAsync(new EliminarGrupoNutricionalCommand(grupo.Id), CancellationToken.None);

        Assert.Empty(_banco.Grupos);
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task EliminarGrupo_que_no_existe_lanza_NoEncontrado()
    {
        await Assert.ThrowsAsync<NoEncontradoException>(() => new EliminarGrupoNutricionalHandler(_banco.GruposRepo, _banco.UnidadDeTrabajo)
            .HandleAsync(new EliminarGrupoNutricionalCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task EliminarGrupo_en_uso_lanza_ReglaDeNegocio_sin_quitarlo()
    {
        var grupo = _banco.AgregarGrupo("Cereales");
        _banco.EnUso.Add(grupo.Id);

        var ex = await Assert.ThrowsAsync<ReglaDeNegocioException>(() => new EliminarGrupoNutricionalHandler(_banco.GruposRepo, _banco.UnidadDeTrabajo)
            .HandleAsync(new EliminarGrupoNutricionalCommand(grupo.Id), CancellationToken.None));

        Assert.Contains("Cereales", ex.Message);
        Assert.Single(_banco.Grupos);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task ListarGrupos_devuelve_todos_ordenados_por_nombre()
    {
        _banco.AgregarGrupo("Verduras");
        _banco.AgregarGrupo("Cereales");
        _banco.AgregarGrupo("Lácteos");

        var resultado = await new ListarGruposNutricionalesHandler(_banco.GruposRepo)
            .HandleAsync(new ListarGruposNutricionalesQuery(), CancellationToken.None);

        Assert.Equal(["Cereales", "Lácteos", "Verduras"], resultado.Select(g => g.Nombre));
    }

    // ---- Restricciones alimentarias ----

    [Fact]
    public async Task RegistrarRestriccion_guarda_la_restriccion_y_confirma_una_vez()
    {
        var creada = await new RegistrarRestriccionAlimentariaHandler(_banco.RestriccionesRepo, _banco.UnidadDeTrabajo)
            .HandleAsync(new RegistrarRestriccionAlimentariaCommand("  Sin gluten  "), CancellationToken.None);

        var restriccion = Assert.Single(_banco.Restricciones);
        Assert.Equal(creada.Id, restriccion.Id);
        Assert.Equal("Sin gluten", restriccion.Nombre);
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task RegistrarRestriccion_con_un_nombre_que_ya_existe_lanza_RD08_sin_confirmar()
    {
        _banco.AgregarRestriccion("Sin gluten");
        var comando = new RegistrarRestriccionAlimentariaCommand("Sin gluten");

        var ex = await Assert.ThrowsAsync<ReglaDeNegocioException>(() =>
            new RegistrarRestriccionAlimentariaHandler(_banco.RestriccionesRepo, _banco.UnidadDeTrabajo).HandleAsync(comando, CancellationToken.None));

        Assert.Equal("RD-08", ex.Regla);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task EliminarRestriccion_sin_uso_la_quita_y_confirma()
    {
        var restriccion = _banco.AgregarRestriccion("Sin gluten");

        await new EliminarRestriccionAlimentariaHandler(_banco.RestriccionesRepo, _banco.UnidadDeTrabajo)
            .HandleAsync(new EliminarRestriccionAlimentariaCommand(restriccion.Id), CancellationToken.None);

        Assert.Empty(_banco.Restricciones);
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task EliminarRestriccion_que_no_existe_lanza_NoEncontrado()
    {
        await Assert.ThrowsAsync<NoEncontradoException>(() => new EliminarRestriccionAlimentariaHandler(_banco.RestriccionesRepo, _banco.UnidadDeTrabajo)
            .HandleAsync(new EliminarRestriccionAlimentariaCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task EliminarRestriccion_en_uso_lanza_ReglaDeNegocio_sin_quitarla()
    {
        var restriccion = _banco.AgregarRestriccion("Sin gluten");
        _banco.EnUso.Add(restriccion.Id);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => new EliminarRestriccionAlimentariaHandler(_banco.RestriccionesRepo, _banco.UnidadDeTrabajo)
            .HandleAsync(new EliminarRestriccionAlimentariaCommand(restriccion.Id), CancellationToken.None));

        Assert.Single(_banco.Restricciones);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task ListarRestricciones_devuelve_todas_ordenadas_por_nombre()
    {
        _banco.AgregarRestriccion("Vegana");
        _banco.AgregarRestriccion("Sin gluten");

        var resultado = await new ListarRestriccionesAlimentariasHandler(_banco.RestriccionesRepo)
            .HandleAsync(new ListarRestriccionesAlimentariasQuery(), CancellationToken.None);

        Assert.Equal(["Sin gluten", "Vegana"], resultado.Select(r => r.Nombre));
    }

    // ---- Programas de atención ----

    [Fact]
    public async Task RegistrarPrograma_guarda_el_programa_y_confirma_una_vez()
    {
        var creado = await new RegistrarProgramaAtencionHandler(_banco.ProgramasRepo, _banco.UnidadDeTrabajo)
            .HandleAsync(new RegistrarProgramaAtencionCommand("  Oncología  "), CancellationToken.None);

        var programa = Assert.Single(_banco.Programas);
        Assert.Equal(creado.Id, programa.Id);
        Assert.Equal("Oncología", programa.Nombre);
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task RegistrarPrograma_con_un_nombre_que_ya_existe_lanza_RD08_sin_confirmar()
    {
        _banco.AgregarPrograma("Oncología");
        var comando = new RegistrarProgramaAtencionCommand("Oncología");

        var ex = await Assert.ThrowsAsync<ReglaDeNegocioException>(() =>
            new RegistrarProgramaAtencionHandler(_banco.ProgramasRepo, _banco.UnidadDeTrabajo).HandleAsync(comando, CancellationToken.None));

        Assert.Equal("RD-08", ex.Regla);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task EliminarPrograma_sin_uso_lo_quita_y_confirma()
    {
        var programa = _banco.AgregarPrograma("Oncología");

        await new EliminarProgramaAtencionHandler(_banco.ProgramasRepo, _banco.UnidadDeTrabajo)
            .HandleAsync(new EliminarProgramaAtencionCommand(programa.Id), CancellationToken.None);

        Assert.Empty(_banco.Programas);
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task EliminarPrograma_que_no_existe_lanza_NoEncontrado()
    {
        await Assert.ThrowsAsync<NoEncontradoException>(() => new EliminarProgramaAtencionHandler(_banco.ProgramasRepo, _banco.UnidadDeTrabajo)
            .HandleAsync(new EliminarProgramaAtencionCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task EliminarPrograma_en_uso_lanza_RR12_sin_quitarlo()
    {
        var programa = _banco.AgregarPrograma("Oncología");
        _banco.EnUso.Add(programa.Id);

        var ex = await Assert.ThrowsAsync<ReglaDeNegocioException>(() => new EliminarProgramaAtencionHandler(_banco.ProgramasRepo, _banco.UnidadDeTrabajo)
            .HandleAsync(new EliminarProgramaAtencionCommand(programa.Id), CancellationToken.None));

        Assert.Equal("RR-12", ex.Regla);
        Assert.Single(_banco.Programas);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task ListarProgramas_devuelve_todos_ordenados_por_nombre()
    {
        _banco.AgregarPrograma("Oncología");
        _banco.AgregarPrograma("Geriatría");

        var resultado = await new ListarProgramasAtencionHandler(_banco.ProgramasRepo)
            .HandleAsync(new ListarProgramasAtencionQuery(), CancellationToken.None);

        Assert.Equal(["Geriatría", "Oncología"], resultado.Select(p => p.Nombre));
    }

    // ---- Especialidades ----

    [Fact]
    public async Task RegistrarEspecialidad_guarda_la_especialidad_y_confirma_una_vez()
    {
        var creada = await new RegistrarEspecialidadHandler(_banco.EspecialidadesRepo, _banco.UnidadDeTrabajo)
            .HandleAsync(new RegistrarEspecialidadCommand("  Nutrición Clínica  "), CancellationToken.None);

        var especialidad = Assert.Single(_banco.Especialidades);
        Assert.Equal(creada.Id, especialidad.Id);
        Assert.Equal("Nutrición Clínica", especialidad.Nombre);
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task RegistrarEspecialidad_con_un_nombre_que_ya_existe_lanza_RD08_sin_confirmar()
    {
        _banco.AgregarEspecialidad("Nutrición Clínica");
        var comando = new RegistrarEspecialidadCommand("Nutrición Clínica");

        var ex = await Assert.ThrowsAsync<ReglaDeNegocioException>(() =>
            new RegistrarEspecialidadHandler(_banco.EspecialidadesRepo, _banco.UnidadDeTrabajo).HandleAsync(comando, CancellationToken.None));

        Assert.Equal("RD-08", ex.Regla);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task EliminarEspecialidad_sin_uso_la_quita_y_confirma()
    {
        var especialidad = _banco.AgregarEspecialidad("Nutrición Clínica");

        await new EliminarEspecialidadHandler(_banco.EspecialidadesRepo, _banco.UnidadDeTrabajo)
            .HandleAsync(new EliminarEspecialidadCommand(especialidad.Id), CancellationToken.None);

        Assert.Empty(_banco.Especialidades);
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task EliminarEspecialidad_que_no_existe_lanza_NoEncontrado()
    {
        await Assert.ThrowsAsync<NoEncontradoException>(() => new EliminarEspecialidadHandler(_banco.EspecialidadesRepo, _banco.UnidadDeTrabajo)
            .HandleAsync(new EliminarEspecialidadCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task EliminarEspecialidad_en_uso_lanza_RR12_sin_quitarla()
    {
        var especialidad = _banco.AgregarEspecialidad("Nutrición Clínica");
        _banco.EnUso.Add(especialidad.Id);

        var ex = await Assert.ThrowsAsync<ReglaDeNegocioException>(() => new EliminarEspecialidadHandler(_banco.EspecialidadesRepo, _banco.UnidadDeTrabajo)
            .HandleAsync(new EliminarEspecialidadCommand(especialidad.Id), CancellationToken.None));

        Assert.Equal("RR-12", ex.Regla);
        Assert.Single(_banco.Especialidades);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task ListarEspecialidades_devuelve_todas_ordenadas_por_nombre()
    {
        _banco.AgregarEspecialidad("Nutrición Deportiva");
        _banco.AgregarEspecialidad("Dietética Hospitalaria");

        var resultado = await new ListarEspecialidadesHandler(_banco.EspecialidadesRepo)
            .HandleAsync(new ListarEspecialidadesQuery(), CancellationToken.None);

        Assert.Equal(["Dietética Hospitalaria", "Nutrición Deportiva"], resultado.Select(e => e.Nombre));
    }
}
