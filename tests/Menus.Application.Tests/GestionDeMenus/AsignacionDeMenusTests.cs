using Menus.Application.Comun;
using Menus.Application.GestionDeMenus.AsignarMenu;
using Menus.Application.GestionDeMenus.ListarMenusDePaciente;
using Menus.Application.Tests.Catalogo;
using Menus.Application.Tests.Catalogo.Dietas;
using Menus.Application.Tests.Catalogo.Nomencladores;
using Menus.Domain.Entidades;
using Menus.Domain.Excepciones;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Tests.GestionDeMenus;

/// <summary>Pruebas de la asignación de menús y de los menús de un paciente (#84) contra los datos en memoria.</summary>
public sealed class AsignacionDeMenusTests
{
    private readonly BancoDeDietasFalso _banco = new();
    private readonly NutricionistasFalsos _nutricionistas = new();
    private readonly BancoFalso _items = new();
    private readonly BancoDeNomencladoresFalso _nomencladores = new();
    private readonly Dieta _dieta;
    private readonly Nutricionista _creadora;
    private readonly Nutricionista _jefa;
    private readonly Paciente _paciente;

    public AsignacionDeMenusTests()
    {
        _dieta = _banco.Agregar("Dieta renal");
        _creadora = _nutricionistas.Agregar("Ana", esJefe: false, _dieta.Id);
        _jefa = _nutricionistas.Agregar("Berta", esJefe: true, _dieta.Id);
        _paciente = NuevoPaciente(_dieta.Id);
    }

    private Paciente NuevoPaciente(params Guid[] dietas)
    {
        var paciente = new Paciente(Guid.NewGuid(), "Paciente", 40, Guid.NewGuid());
        foreach (var dieta in dietas)
        {
            paciente.InscribirDieta(dieta);
        }

        _banco.Pacientes.Add(paciente);
        return paciente;
    }

    /// <summary>Un menú de la dieta, aprobado por la jefa salvo que se pida otra cosa, con ese ítem si se da.</summary>
    private Menu NuevoMenu(bool aprobado = true, int diasAtras = 1, Guid? item = null)
    {
        var menu = new Menu(
            Guid.NewGuid(), _dieta.Id, _creadora.Id, DateTime.UtcNow.AddDays(-diasAtras), 1, true,
            [(TipoPreparacion.PlatoFuerte.Id, 1m)], [BancoDeDietasFalso.GrupoCereales]);

        // Los ítems se agregan mientras el menú está pendiente: después del dictamen no cambian.
        if (item is { } itemId)
        {
            menu.AgregarItem(itemId);
        }

        if (aprobado)
        {
            menu.Aprobar(_jefa.Id, DateTime.UtcNow);
        }

        _banco.Menus.Add(menu);
        return menu;
    }

    private AsignarMenuHandler Asignar(Guid? actor = null)
        => new(_banco.MenusRepo, _banco.PacientesRepo, _nutricionistas, _banco.UnidadDeTrabajo, new ActorFalso(actor ?? _creadora.Id));

    private ListarMenusDePacienteHandler Listar()
        => new(_banco.MenusRepo, _banco.PacientesRepo, _nutricionistas, _nomencladores.GruposRepo, _items.ItemsRepo);

    // ---- Asignar ----

    [Fact]
    public async Task Asignar_un_menu_final_a_un_paciente_inscrito_registra_la_asignacion_y_confirma()
    {
        var menu = NuevoMenu();

        await Asignar().HandleAsync(new AsignarMenuCommand(menu.Id, _paciente.Id), CancellationToken.None);

        Assert.Equal([_paciente.Id], menu.Asignaciones.Select(a => a.PacienteId));
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Un_menu_se_asigna_a_varios_pacientes()
    {
        var menu = NuevoMenu();
        var otro = NuevoPaciente(_dieta.Id);

        await Asignar().HandleAsync(new AsignarMenuCommand(menu.Id, _paciente.Id), CancellationToken.None);
        await Asignar().HandleAsync(new AsignarMenuCommand(menu.Id, otro.Id), CancellationToken.None);

        Assert.Equal(2, menu.Asignaciones.Count);
    }

    [Fact]
    public async Task Asignar_dos_veces_al_mismo_paciente_lanza_regla_de_negocio_RC13()
    {
        var menu = NuevoMenu();
        await Asignar().HandleAsync(new AsignarMenuCommand(menu.Id, _paciente.Id), CancellationToken.None);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => Asignar().HandleAsync(new AsignarMenuCommand(menu.Id, _paciente.Id), CancellationToken.None));

        Assert.Single(menu.Asignaciones);
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Asignar_un_menu_que_no_es_final_lanza_regla_de_negocio_RS16()
    {
        var menu = NuevoMenu(aprobado: false);

        var error = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => Asignar().HandleAsync(new AsignarMenuCommand(menu.Id, _paciente.Id), CancellationToken.None));

        Assert.Equal("Solo se asigna un menú final (aprobado).", error.Message);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Asignar_a_un_paciente_que_no_esta_inscrito_en_la_dieta_lanza_RS04()
    {
        var menu = NuevoMenu();
        var deOtraDieta = NuevoPaciente(Guid.NewGuid());

        var error = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => Asignar().HandleAsync(new AsignarMenuCommand(menu.Id, deOtraDieta.Id), CancellationToken.None));

        Assert.Equal("RS-04", error.Regla);
        Assert.Empty(menu.Asignaciones);
    }

    [Fact]
    public async Task Asignar_sin_estar_autorizado_para_la_dieta_lanza_regla_de_negocio()
    {
        var menu = NuevoMenu();
        var deOtraDieta = _nutricionistas.Agregar("Carla", esJefe: false, Guid.NewGuid());

        var error = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => Asignar(deOtraDieta.Id).HandleAsync(new AsignarMenuCommand(menu.Id, _paciente.Id), CancellationToken.None));

        Assert.Contains("no está autorizado para la dieta del menú", error.Message);
        Assert.Empty(menu.Asignaciones);
    }

    [Fact]
    public async Task Asignar_con_un_actor_que_no_es_nutricionista_lanza_regla_de_negocio()
    {
        var menu = NuevoMenu();

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => Asignar(Guid.NewGuid()).HandleAsync(new AsignarMenuCommand(menu.Id, _paciente.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Asignar_un_menu_que_no_existe_da_404()
    {
        var id = Guid.NewGuid();

        var error = await Assert.ThrowsAsync<NoEncontradoException>(
            () => Asignar().HandleAsync(new AsignarMenuCommand(id, _paciente.Id), CancellationToken.None));

        Assert.Equal($"No existe el menú {id}.", error.Message);
    }

    [Fact]
    public async Task Asignar_a_un_paciente_que_no_existe_da_404()
    {
        var menu = NuevoMenu();
        var id = Guid.NewGuid();

        var error = await Assert.ThrowsAsync<NoEncontradoException>(
            () => Asignar().HandleAsync(new AsignarMenuCommand(menu.Id, id), CancellationToken.None));

        Assert.Equal($"No existe el paciente {id}.", error.Message);
    }

    [Fact]
    public void El_validador_exige_el_paciente()
    {
        var validador = new AsignarMenuValidator();

        Assert.True(validador.Validate(new AsignarMenuCommand(Guid.NewGuid(), Guid.NewGuid())).IsValid);
        Assert.Equal(
            ["PacienteId"],
            validador.Validate(new AsignarMenuCommand(Guid.NewGuid(), Guid.Empty)).Errors.Select(e => e.PropertyName));
    }

    // ---- Menús del paciente ----

    [Fact]
    public async Task Los_menus_del_paciente_son_solo_los_suyos_del_mas_reciente_al_mas_antiguo_con_sus_items()
    {
        _nomencladores.Grupos.Add(new GrupoNutricional(BancoDeDietasFalso.GrupoCereales, "Cereales"));
        var arroz = _items.Agregar("Arroz");
        var antiguo = NuevoMenu(diasAtras: 3);
        var reciente = NuevoMenu(diasAtras: 1, item: arroz.Id);
        var deOtro = NuevoMenu();
        antiguo.AsignarPaciente(_paciente.Id);
        reciente.AsignarPaciente(_paciente.Id);
        deOtro.AsignarPaciente(NuevoPaciente(_dieta.Id).Id);

        var menus = await Listar().HandleAsync(new ListarMenusDePacienteQuery(_paciente.Id), CancellationToken.None);

        Assert.Equal([reciente.Id, antiguo.Id], menus.Select(m => m.Id));
        Assert.Equal(["Arroz"], menus[0].Items.Select(i => i.Nombre));
        Assert.Equal(["Cereales"], menus[0].GruposNutricionales.Select(g => g.Nombre));
        Assert.Equal("Final", menus[0].Estado);
        Assert.Equal("Berta", menus[0].Dictamen!.Revisor.Nombre);
    }

    [Fact]
    public async Task Un_paciente_sin_menus_asignados_recibe_una_lista_vacia()
    {
        NuevoMenu();

        Assert.Empty(await Listar().HandleAsync(new ListarMenusDePacienteQuery(_paciente.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Los_menus_de_un_paciente_que_no_existe_dan_404()
    {
        var id = Guid.NewGuid();

        var error = await Assert.ThrowsAsync<NoEncontradoException>(
            () => Listar().HandleAsync(new ListarMenusDePacienteQuery(id), CancellationToken.None));

        Assert.Equal($"No existe el paciente {id}.", error.Message);
    }
}
