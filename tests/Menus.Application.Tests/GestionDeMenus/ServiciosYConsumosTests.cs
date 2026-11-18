using Menus.Application.Comun;
using Menus.Application.GestionDeMenus.ListarConsumosDePaciente;
using Menus.Application.GestionDeMenus.RegistrarServicio;
using Menus.Application.Tests.Catalogo;
using Menus.Application.Tests.Catalogo.Dietas;
using Menus.Domain.Entidades;
using Menus.Domain.Excepciones;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Tests.GestionDeMenus;

/// <summary>Pruebas del registro de servicios y consumos, y del histórico de consumo del paciente (#85).</summary>
public sealed class ServiciosYConsumosTests
{
    private static readonly DateOnly Hoy = DateOnly.FromDateTime(DateTime.UtcNow);

    private readonly BancoDeDietasFalso _banco = new();
    private readonly BancoFalso _items = new();
    private readonly Dieta _dieta;
    private readonly Nutricionista _jefa = new(Guid.NewGuid(), "Berta", Guid.NewGuid());
    private readonly Paciente _paciente;
    private readonly Alimento _manzana;
    private readonly Plato _sopa;

    public ServiciosYConsumosTests()
    {
        _dieta = _banco.Agregar("Dieta renal");
        _paciente = NuevoPaciente();
        _manzana = _items.Agregar("Manzana");
        _sopa = _items.AgregarPlato("Sopa", _items.Agregar("Zanahoria"));
    }

    private Paciente NuevoPaciente()
    {
        var paciente = new Paciente(Guid.NewGuid(), "Paciente", 40, Guid.NewGuid());
        paciente.InscribirDieta(_dieta.Id);
        _banco.Pacientes.Add(paciente);
        return paciente;
    }

    /// <summary>Un menú con la manzana y la sopa, aprobado hace dos días y asignado a esos pacientes.</summary>
    private Menu NuevoMenu(bool aprobado = true, params Paciente[] asignados)
    {
        var menu = new Menu(
            Guid.NewGuid(), _dieta.Id, Guid.NewGuid(), DateTime.UtcNow.AddDays(-3), 2, true,
            [(TipoPreparacion.PlatoFuerte.Id, 1m)], [BancoDeDietasFalso.GrupoCereales]);
        menu.AgregarItem(_manzana.Id);
        menu.AgregarItem(_sopa.Id);

        if (aprobado)
        {
            menu.Aprobar(_jefa.Id, DateTime.UtcNow.AddDays(-2));
            foreach (var paciente in asignados)
            {
                menu.AsignarPaciente(paciente.Id);
            }
        }

        _banco.Menus.Add(menu);
        return menu;
    }

    private RegistrarServicioHandler Registrar() => new(_banco.MenusRepo, _banco.PacientesRepo, _banco.UnidadDeTrabajo);

    private ListarConsumosDePacienteHandler Listar() => new(_banco.MenusRepo, _banco.PacientesRepo, _items.ItemsRepo);

    private static RegistrarServicioCommand Servicio(Menu menu, Paciente paciente, DateOnly fecha, params ConsumoDelServicio[] consumos)
        => new(menu.Id, paciente.Id, fecha, consumos);

    private static Servicio ServicioDe(Menu menu, Paciente paciente, DateOnly fecha)
        => menu.Asignaciones.Single(a => a.PacienteId == paciente.Id).Servicios.Single(s => s.FechaServicio == fecha);

    // ---- Registrar ----

    [Fact]
    public async Task Registrar_guarda_el_servicio_con_la_aceptacion_de_cada_item_y_confirma_una_vez()
    {
        var menu = NuevoMenu(asignados: _paciente);

        await Registrar().HandleAsync(
            Servicio(menu, _paciente, Hoy, new ConsumoDelServicio(_manzana.Id, true), new ConsumoDelServicio(_sopa.Id, false)), CancellationToken.None);

        var servicio = ServicioDe(menu, _paciente, Hoy);
        Assert.Equal(
            [(_manzana.Id, true), (_sopa.Id, false)],
            servicio.Consumos.Select(c => (c.ItemId, c.Aceptacion)).OrderBy(c => c.ItemId == _sopa.Id));
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Registrar_sin_consumos_deja_el_servicio_sin_consumo_registrado()
    {
        var menu = NuevoMenu(asignados: _paciente);

        await Registrar().HandleAsync(Servicio(menu, _paciente, Hoy), CancellationToken.None);

        Assert.Empty(ServicioDe(menu, _paciente, Hoy).Consumos);
    }

    [Fact]
    public async Task El_mismo_menu_se_registra_en_dias_distintos_sin_colision()
    {
        var menu = NuevoMenu(asignados: _paciente);

        await Registrar().HandleAsync(Servicio(menu, _paciente, Hoy.AddDays(-1), new ConsumoDelServicio(_manzana.Id, true)), CancellationToken.None);
        await Registrar().HandleAsync(Servicio(menu, _paciente, Hoy, new ConsumoDelServicio(_manzana.Id, false)), CancellationToken.None);

        var servicios = menu.Asignaciones.Single().Servicios;
        Assert.Equal([Hoy.AddDays(-1), Hoy], servicios.Select(s => s.FechaServicio).Order());
        Assert.Equal(2, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Registrar_dos_veces_el_mismo_dia_lanza_RC17_y_no_confirma()
    {
        var menu = NuevoMenu(asignados: _paciente);
        await Registrar().HandleAsync(Servicio(menu, _paciente, Hoy), CancellationToken.None);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => Registrar().HandleAsync(Servicio(menu, _paciente, Hoy), CancellationToken.None));

        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Registrar_un_item_que_no_esta_en_el_menu_lanza_RX01_y_no_confirma()
    {
        var menu = NuevoMenu(asignados: _paciente);
        var ajeno = _items.Agregar("Pera");

        var error = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => Registrar().HandleAsync(Servicio(menu, _paciente, Hoy, new ConsumoDelServicio(ajeno.Id, true)), CancellationToken.None));

        Assert.Contains("no está incluido en este menú", error.Message);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Registrar_a_un_paciente_al_que_no_se_le_asigno_el_menu_lanza_regla_de_negocio()
    {
        var menu = NuevoMenu();
        var sinAsignar = NuevoPaciente();

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => Registrar().HandleAsync(Servicio(menu, sinAsignar, Hoy), CancellationToken.None));

        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Registrar_un_menu_que_no_es_final_lanza_regla_de_negocio()
    {
        var menu = NuevoMenu(aprobado: false);

        var error = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => Registrar().HandleAsync(Servicio(menu, _paciente, Hoy), CancellationToken.None));

        Assert.Equal("Solo se sirve un menú final (aprobado).", error.Message);
    }

    [Fact]
    public async Task Registrar_un_dia_futuro_lanza_dato_invalido()
    {
        var menu = NuevoMenu(asignados: _paciente);

        await Assert.ThrowsAsync<DatoInvalidoException>(
            () => Registrar().HandleAsync(Servicio(menu, _paciente, Hoy.AddDays(1)), CancellationToken.None));
    }

    [Fact]
    public async Task Registrar_un_dia_anterior_a_la_aprobacion_lanza_dato_invalido_RS14()
    {
        var menu = NuevoMenu(asignados: _paciente);

        await Assert.ThrowsAsync<DatoInvalidoException>(
            () => Registrar().HandleAsync(Servicio(menu, _paciente, Hoy.AddDays(-5)), CancellationToken.None));
    }

    [Fact]
    public async Task Registrar_un_menu_que_no_existe_da_404()
    {
        var error = await Assert.ThrowsAsync<NoEncontradoException>(
            () => Registrar().HandleAsync(new RegistrarServicioCommand(Guid.NewGuid(), _paciente.Id, Hoy, []), CancellationToken.None));

        Assert.Contains("menú", error.Message);
    }

    [Fact]
    public async Task Registrar_a_un_paciente_que_no_existe_da_404()
    {
        var menu = NuevoMenu(asignados: _paciente);

        var error = await Assert.ThrowsAsync<NoEncontradoException>(
            () => Registrar().HandleAsync(new RegistrarServicioCommand(menu.Id, Guid.NewGuid(), Hoy, []), CancellationToken.None));

        Assert.Contains("paciente", error.Message);
    }

    // ---- Validador ----

    [Fact]
    public void El_validador_acepta_un_servicio_sin_consumos()
        => Assert.True(new RegistrarServicioValidator().Validate(new RegistrarServicioCommand(Guid.NewGuid(), Guid.NewGuid(), Hoy, [])).IsValid);

    [Fact]
    public void El_validador_exige_el_paciente_el_dia_y_la_lista()
    {
        var resultado = new RegistrarServicioValidator().Validate(new RegistrarServicioCommand(Guid.NewGuid(), Guid.Empty, default, null!));

        Assert.Equal(
            ["Consumos", "Fecha", "PacienteId"],
            resultado.Errors.Select(e => e.PropertyName).Order());
    }

    [Fact]
    public void El_validador_rechaza_un_consumo_sin_item()
    {
        var resultado = new RegistrarServicioValidator().Validate(
            new RegistrarServicioCommand(Guid.NewGuid(), Guid.NewGuid(), Hoy, [new(Guid.Empty, true)]));

        Assert.Equal("Cada consumo indica el ítem.", Assert.Single(resultado.Errors).ErrorMessage);
    }

    [Fact]
    public void El_validador_rechaza_un_item_repetido_RC14()
    {
        var item = Guid.NewGuid();

        var resultado = new RegistrarServicioValidator().Validate(
            new RegistrarServicioCommand(Guid.NewGuid(), Guid.NewGuid(), Hoy, [new(item, true), new(item, false)]));

        Assert.Contains("RC-14", Assert.Single(resultado.Errors).ErrorMessage);
    }

    // ---- Histórico ----

    [Fact]
    public async Task El_historico_trae_los_servicios_del_mas_reciente_al_mas_antiguo_con_los_items_por_nombre()
    {
        var menu = NuevoMenu(asignados: _paciente);
        await Registrar().HandleAsync(Servicio(menu, _paciente, Hoy.AddDays(-1), new ConsumoDelServicio(_manzana.Id, true)), CancellationToken.None);
        await Registrar().HandleAsync(
            Servicio(menu, _paciente, Hoy, new ConsumoDelServicio(_sopa.Id, false), new ConsumoDelServicio(_manzana.Id, true)), CancellationToken.None);

        var historico = await Listar().HandleAsync(new ListarConsumosDePacienteQuery(_paciente.Id), CancellationToken.None);

        Assert.Equal([Hoy, Hoy.AddDays(-1)], historico.Select(s => s.Fecha));
        Assert.All(historico, s => Assert.Equal((menu.Id, _dieta.Id), (s.MenuId, s.DietaId)));
        Assert.Equal(
            [("Manzana", true), ("Sopa", false)],
            historico[0].Consumos.Select(c => (c.Nombre, c.Aceptacion)));
    }

    [Fact]
    public async Task El_historico_trae_solo_los_servicios_del_paciente()
    {
        var otro = NuevoPaciente();
        var menu = NuevoMenu(asignados: [_paciente, otro]);
        await Registrar().HandleAsync(Servicio(menu, otro, Hoy, new ConsumoDelServicio(_manzana.Id, false)), CancellationToken.None);
        await Registrar().HandleAsync(Servicio(menu, _paciente, Hoy.AddDays(-1)), CancellationToken.None);

        var historico = await Listar().HandleAsync(new ListarConsumosDePacienteQuery(_paciente.Id), CancellationToken.None);

        var servicio = Assert.Single(historico);
        Assert.Equal(Hoy.AddDays(-1), servicio.Fecha);
        Assert.Empty(servicio.Consumos);
    }

    [Fact]
    public async Task El_historico_ordena_por_menu_los_servicios_del_mismo_dia()
    {
        var uno = NuevoMenu(asignados: _paciente);
        var otro = NuevoMenu(asignados: _paciente);
        await Registrar().HandleAsync(Servicio(uno, _paciente, Hoy), CancellationToken.None);
        await Registrar().HandleAsync(Servicio(otro, _paciente, Hoy), CancellationToken.None);

        var historico = await Listar().HandleAsync(new ListarConsumosDePacienteQuery(_paciente.Id), CancellationToken.None);

        Assert.Equal(new[] { uno.Id, otro.Id }.Order(), historico.Select(s => s.MenuId));
    }

    [Fact]
    public async Task El_historico_omite_un_consumo_cuyo_item_no_aparece()
    {
        var menu = NuevoMenu(asignados: _paciente);
        await Registrar().HandleAsync(
            Servicio(menu, _paciente, Hoy, new ConsumoDelServicio(_manzana.Id, true), new ConsumoDelServicio(_sopa.Id, true)), CancellationToken.None);
        _items.Items.Remove(_sopa);

        var historico = await Listar().HandleAsync(new ListarConsumosDePacienteQuery(_paciente.Id), CancellationToken.None);

        Assert.Equal(["Manzana"], Assert.Single(historico).Consumos.Select(c => c.Nombre));
    }

    [Fact]
    public async Task El_historico_de_un_paciente_sin_servicios_esta_vacio()
    {
        NuevoMenu(asignados: _paciente);

        Assert.Empty(await Listar().HandleAsync(new ListarConsumosDePacienteQuery(_paciente.Id), CancellationToken.None));
    }

    [Fact]
    public async Task El_historico_de_un_paciente_que_no_existe_da_404()
        => await Assert.ThrowsAsync<NoEncontradoException>(
            () => Listar().HandleAsync(new ListarConsumosDePacienteQuery(Guid.NewGuid()), CancellationToken.None));
}
