using Menus.Application.Comun;
using Menus.Application.Salas.ActualizarSala;
using Menus.Application.Salas.EliminarSala;
using Menus.Application.Salas.ListarSalas;
using Menus.Application.Salas.ObtenerSala;
using Menus.Application.Salas.RegistrarSala;
using Menus.Domain.Entidades;
using Menus.Domain.Excepciones;

namespace Menus.Application.Tests.Salas;

/// <summary>Pruebas de los manejadores de salas del #72 contra los datos en memoria.</summary>
public sealed class ManejadoresDeSalasTests
{
    private readonly BancoDeSalasFalso _banco = new();

    // ---- Listar ----

    [Fact]
    public async Task Listar_devuelve_todas_ordenadas_por_nombre()
    {
        _banco.Agregar("Sala C");
        _banco.Agregar("Sala A");
        _banco.Agregar("Sala B");

        var handler = new ListarSalasHandler(_banco.SalasRepo);
        var salas = await handler.HandleAsync(new ListarSalasQuery(), CancellationToken.None);

        Assert.Equal(["Sala A", "Sala B", "Sala C"], salas.Select(s => s.Nombre));
    }

    [Fact]
    public async Task Listar_sin_salas_devuelve_vacio()
    {
        var handler = new ListarSalasHandler(_banco.SalasRepo);

        var salas = await handler.HandleAsync(new ListarSalasQuery(), CancellationToken.None);

        Assert.Empty(salas);
    }

    // ---- Obtener ----

    [Fact]
    public async Task Obtener_devuelve_la_sala()
    {
        var sala = _banco.Agregar("Sala A");
        var handler = new ObtenerSalaHandler(_banco.SalasRepo);

        var dto = await handler.HandleAsync(new ObtenerSalaQuery(sala.Id), CancellationToken.None);

        Assert.Equal(sala.Id, dto.Id);
        Assert.Equal("Sala A", dto.Nombre);
    }

    [Fact]
    public async Task Obtener_una_sala_que_no_existe_da_404()
    {
        var id = Guid.NewGuid();
        var handler = new ObtenerSalaHandler(_banco.SalasRepo);

        var error = await Assert.ThrowsAsync<NoEncontradoException>(
            () => handler.HandleAsync(new ObtenerSalaQuery(id), CancellationToken.None));

        Assert.Equal("sala", error.Recurso);
        Assert.Equal(id, error.Id);
    }

    // ---- Registrar ----

    [Fact]
    public async Task Registrar_crea_la_sala()
    {
        var handler = new RegistrarSalaHandler(_banco.SalasRepo, _banco.UnidadDeTrabajo);

        var creada = await handler.HandleAsync(new RegistrarSalaCommand("  Sala Nueva  "), CancellationToken.None);

        var sala = Assert.Single(_banco.Salas);
        Assert.Equal(creada.Id, sala.Id);
        Assert.Equal("Sala Nueva", sala.Nombre);
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Registrar_con_nombre_repetido_lanza_ReglaDeNegocioException()
    {
        _banco.Agregar("Sala A");
        var handler = new RegistrarSalaHandler(_banco.SalasRepo, _banco.UnidadDeTrabajo);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => handler.HandleAsync(new RegistrarSalaCommand("sala a"), CancellationToken.None));

        Assert.Single(_banco.Salas);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public void El_validador_de_registrar_exige_el_nombre()
        => Assert.Equal(
            ["Nombre"],
            new RegistrarSalaValidator().Validate(new RegistrarSalaCommand("")).Errors.Select(e => e.PropertyName));

    // ---- Actualizar ----

    [Fact]
    public async Task Actualizar_cambia_el_nombre()
    {
        var sala = _banco.Agregar("Sala A");
        var handler = new ActualizarSalaHandler(_banco.SalasRepo, _banco.UnidadDeTrabajo);

        await handler.HandleAsync(new ActualizarSalaCommand(sala.Id, "Sala A - Pediatría"), CancellationToken.None);

        Assert.Equal("Sala A - Pediatría", sala.Nombre);
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Actualizar_con_el_mismo_nombre_que_ya_tiene_no_falla()
    {
        var sala = _banco.Agregar("Sala A");
        var handler = new ActualizarSalaHandler(_banco.SalasRepo, _banco.UnidadDeTrabajo);

        await handler.HandleAsync(new ActualizarSalaCommand(sala.Id, "Sala A"), CancellationToken.None);

        Assert.Equal("Sala A", sala.Nombre);
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Actualizar_con_el_nombre_de_otra_sala_lanza_ReglaDeNegocioException()
    {
        _banco.Agregar("Sala A");
        var otra = _banco.Agregar("Sala B");
        var handler = new ActualizarSalaHandler(_banco.SalasRepo, _banco.UnidadDeTrabajo);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => handler.HandleAsync(new ActualizarSalaCommand(otra.Id, "Sala A"), CancellationToken.None));

        Assert.Equal("Sala B", otra.Nombre);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Actualizar_una_sala_que_no_existe_da_404()
    {
        var handler = new ActualizarSalaHandler(_banco.SalasRepo, _banco.UnidadDeTrabajo);

        await Assert.ThrowsAsync<NoEncontradoException>(
            () => handler.HandleAsync(new ActualizarSalaCommand(Guid.NewGuid(), "Sala A"), CancellationToken.None));
    }

    [Fact]
    public void El_validador_de_actualizar_exige_el_nombre()
        => Assert.Equal(
            ["Nombre"],
            new ActualizarSalaValidator().Validate(new ActualizarSalaCommand(Guid.NewGuid(), ""))
                .Errors.Select(e => e.PropertyName));

    [Fact]
    public void El_validador_de_actualizar_rechaza_un_nombre_demasiado_largo()
        => Assert.Equal(
            ["Nombre"],
            new ActualizarSalaValidator().Validate(new ActualizarSalaCommand(Guid.NewGuid(), new string('a', 101)))
                .Errors.Select(e => e.PropertyName));

    // ---- Eliminar ----

    [Fact]
    public async Task Eliminar_sin_pacientes_la_quita()
    {
        var sala = _banco.Agregar("Sala A");
        var handler = new EliminarSalaHandler(_banco.SalasRepo, _banco.PacientesRepo, _banco.UnidadDeTrabajo);

        await handler.HandleAsync(new EliminarSalaCommand(sala.Id), CancellationToken.None);

        Assert.Empty(_banco.Salas);
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Eliminar_con_pacientes_asignados_lanza_ReglaDeNegocioException()
    {
        var sala = _banco.Agregar("Sala A");
        _banco.Pacientes.Add(new Paciente(Guid.NewGuid(), "Juan", 30, sala.Id));
        var handler = new EliminarSalaHandler(_banco.SalasRepo, _banco.PacientesRepo, _banco.UnidadDeTrabajo);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => handler.HandleAsync(new EliminarSalaCommand(sala.Id), CancellationToken.None));

        Assert.Single(_banco.Salas);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Eliminar_una_sala_que_no_existe_da_404()
    {
        var handler = new EliminarSalaHandler(_banco.SalasRepo, _banco.PacientesRepo, _banco.UnidadDeTrabajo);

        await Assert.ThrowsAsync<NoEncontradoException>(
            () => handler.HandleAsync(new EliminarSalaCommand(Guid.NewGuid()), CancellationToken.None));
    }
}
