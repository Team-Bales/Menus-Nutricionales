using Menus.Application.Comun;
using Menus.Application.GestionDeMenus.RegistrarReemplazo;
using Menus.Application.GestionDeMenus.ValidarMenu;
using Menus.Application.Tests.Catalogo;
using Menus.Application.Tests.Catalogo.Dietas;
using Menus.Domain.Entidades;
using Menus.Domain.Excepciones;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Tests.GestionDeMenus;

/// <summary>Pruebas de los manejadores del #99 contra los datos en memoria.</summary>
public sealed class ManejadoresDeValidacionTests
{
    private readonly BancoDeDietasFalso _banco = new();
    private readonly NutricionistasFalsos _nutricionistas = new();
    private readonly Dieta _dieta;
    private readonly Guid _creador = Guid.NewGuid();

    public ManejadoresDeValidacionTests()
    {
        _dieta = _banco.Agregar("Dieta renal");
    }

    private ValidarMenuHandler Validar(Guid actor)
        => new(_banco.MenusRepo, _nutricionistas, _banco.UnidadDeTrabajo, new ActorFalso(actor));

    private RegistrarReemplazoHandler Reemplazar() => new(_banco.MenusRepo, _banco.UnidadDeTrabajo);

    private Menu NuevoMenu(Guid? dietaId = null)
    {
        var menu = new Menu(
            Guid.NewGuid(), dietaId ?? _dieta.Id, _creador, DateTime.UtcNow.AddDays(-1), 1, true,
            [(TipoPreparacion.PlatoFuerte.Id, 1m)], [BancoDeDietasFalso.GrupoCereales]);
        _banco.Menus.Add(menu);
        return menu;
    }

    private Guid JefeAutorizado() => _nutricionistas.Agregar(esJefe: true, _dieta.Id).Id;

    // ---- Validar ----

    [Fact]
    public async Task Aprobar_registra_el_dictamen_y_el_menu_pasa_a_ser_final()
    {
        var menu = NuevoMenu();
        var jefe = JefeAutorizado();
        var antes = DateTime.UtcNow;

        await Validar(jefe).HandleAsync(new ValidarMenuCommand(menu.Id, true, "  Bien balanceado  "), CancellationToken.None);

        Assert.True(menu.EsFinal);
        var validacion = menu.Validacion!;
        Assert.Equal(jefe, validacion.NutricionistaId);
        Assert.True(validacion.Aprobado);
        Assert.Equal("Bien balanceado", validacion.Observaciones);
        Assert.InRange(validacion.FechaValidacion, antes, DateTime.UtcNow);
        Assert.Equal(DateTimeKind.Utc, validacion.FechaValidacion.Kind);
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Rechazar_registra_el_dictamen_negativo_sin_borrar_el_menu()
    {
        var menu = NuevoMenu();

        await Validar(JefeAutorizado()).HandleAsync(
            new ValidarMenuCommand(menu.Id, false, "Faltan frutas"),
            CancellationToken.None);

        Assert.True(menu.EsRechazado);
        Assert.Contains(menu, _banco.Menus);
        Assert.False(menu.Validacion!.Aprobado);
        Assert.Equal("Faltan frutas", menu.Validacion.Observaciones);
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Sin_observaciones_se_registran_como_nulas(string? observaciones)
    {
        var menu = NuevoMenu();

        await Validar(JefeAutorizado()).HandleAsync(
            new ValidarMenuCommand(menu.Id, true, observaciones),
            CancellationToken.None);

        Assert.Null(menu.Validacion!.Observaciones);
    }

    [Fact]
    public async Task Un_nutricionista_que_no_es_jefe_no_dictamina_RS03()
    {
        var menu = NuevoMenu();
        var noJefe = _nutricionistas.Agregar(esJefe: false, _dieta.Id).Id;

        var error = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => Validar(noJefe).HandleAsync(new ValidarMenuCommand(menu.Id, true), CancellationToken.None));

        Assert.Equal("RS-03", error.Regla);
        Assert.True(menu.EsPendiente);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Un_actor_que_no_es_nutricionista_no_dictamina_RS03()
    {
        var menu = NuevoMenu();

        var error = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => Validar(Guid.NewGuid()).HandleAsync(new ValidarMenuCommand(menu.Id, true), CancellationToken.None));

        Assert.Equal("RS-03", error.Regla);
        Assert.True(menu.EsPendiente);
    }

    [Fact]
    public async Task Un_jefe_no_autorizado_para_la_dieta_no_dictamina_RS03()
    {
        var menu = NuevoMenu();
        var otraDieta = _banco.Agregar("Dieta blanda");
        var jefeDeOtraDieta = _nutricionistas.Agregar(esJefe: true, otraDieta.Id).Id;

        var error = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => Validar(jefeDeOtraDieta).HandleAsync(new ValidarMenuCommand(menu.Id, false), CancellationToken.None));

        Assert.Equal("RS-03", error.Regla);
        Assert.Contains("autorizado", error.Message);
        Assert.True(menu.EsPendiente);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Quien_creo_el_menu_no_lo_dictamina_RS05()
    {
        var jefe = _nutricionistas.Agregar(esJefe: true, _dieta.Id);
        var menu = new Menu(
            Guid.NewGuid(), _dieta.Id, jefe.Id, DateTime.UtcNow.AddDays(-1), 1, true,
            [(TipoPreparacion.PlatoFuerte.Id, 1m)], [BancoDeDietasFalso.GrupoCereales]);
        _banco.Menus.Add(menu);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => Validar(jefe.Id).HandleAsync(new ValidarMenuCommand(menu.Id, true), CancellationToken.None));

        Assert.True(menu.EsPendiente);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Un_menu_ya_dictaminado_no_se_vuelve_a_dictaminar_RS15()
    {
        var menu = NuevoMenu();
        var jefe = JefeAutorizado();
        await Validar(jefe).HandleAsync(new ValidarMenuCommand(menu.Id, false, "Rehacer"), CancellationToken.None);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => Validar(jefe).HandleAsync(new ValidarMenuCommand(menu.Id, true), CancellationToken.None));

        Assert.True(menu.EsRechazado);
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Validar_un_menu_que_no_existe_es_404()
    {
        var id = Guid.NewGuid();

        var error = await Assert.ThrowsAsync<NoEncontradoException>(
            () => Validar(JefeAutorizado()).HandleAsync(new ValidarMenuCommand(id, true), CancellationToken.None));

        Assert.Equal("menú", error.Recurso);
        Assert.Equal(id, error.Id);
    }

    // ---- Registrar reemplazo ----

    private async Task<Menu> RechazadoAsync()
    {
        var menu = NuevoMenu();
        await Validar(JefeAutorizado()).HandleAsync(new ValidarMenuCommand(menu.Id, false, "Rehacer"), CancellationToken.None);
        return menu;
    }

    [Fact]
    public async Task Reemplazar_vincula_el_menu_nuevo_con_el_rechazado()
    {
        var rechazado = await RechazadoAsync();
        var nuevo = NuevoMenu();

        await Reemplazar().HandleAsync(new RegistrarReemplazoCommand(nuevo.Id, rechazado.Id), CancellationToken.None);

        Assert.Equal(rechazado.Id, nuevo.ValidacionOrigenId);
        Assert.True(nuevo.EsPendiente);
        Assert.Equal(2, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Un_menu_aprobado_no_se_reemplaza_RS17()
    {
        var aprobado = NuevoMenu();
        await Validar(JefeAutorizado()).HandleAsync(new ValidarMenuCommand(aprobado.Id, true), CancellationToken.None);
        var nuevo = NuevoMenu();

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => Reemplazar().HandleAsync(new RegistrarReemplazoCommand(nuevo.Id, aprobado.Id), CancellationToken.None));

        Assert.Null(nuevo.ValidacionOrigenId);
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task El_reemplazo_tiene_que_ser_de_la_misma_dieta_RS18()
    {
        var rechazado = await RechazadoAsync();
        var deOtraDieta = NuevoMenu(_banco.Agregar("Dieta blanda").Id);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => Reemplazar().HandleAsync(new RegistrarReemplazoCommand(deOtraDieta.Id, rechazado.Id), CancellationToken.None));

        Assert.Null(deOtraDieta.ValidacionOrigenId);
    }

    [Fact]
    public async Task Reemplazar_con_un_menu_que_no_existe_es_404()
    {
        var rechazado = await RechazadoAsync();
        var inexistente = Guid.NewGuid();

        var sinNuevo = await Assert.ThrowsAsync<NoEncontradoException>(
            () => Reemplazar().HandleAsync(new RegistrarReemplazoCommand(inexistente, rechazado.Id), CancellationToken.None));
        var sinRechazado = await Assert.ThrowsAsync<NoEncontradoException>(
            () => Reemplazar().HandleAsync(new RegistrarReemplazoCommand(NuevoMenu().Id, inexistente), CancellationToken.None));

        Assert.Equal(inexistente, sinNuevo.Id);
        Assert.Equal(inexistente, sinRechazado.Id);
        Assert.Equal("menú", sinRechazado.Recurso);
    }

    [Fact]
    public void El_validador_de_reemplazo_exige_el_menu_rechazado()
    {
        var validador = new RegistrarReemplazoValidator();

        Assert.True(validador.Validate(new RegistrarReemplazoCommand(Guid.NewGuid(), Guid.NewGuid())).IsValid);
        Assert.Equal(
            ["MenuRechazadoId"],
            validador.Validate(new RegistrarReemplazoCommand(Guid.NewGuid(), Guid.Empty)).Errors.Select(e => e.PropertyName));
    }
}
