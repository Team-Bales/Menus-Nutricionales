using Menus.Application.Comun;
using Menus.Application.GestionDeMenus;
using Menus.Application.GestionDeMenus.ListarMenus;
using Menus.Application.GestionDeMenus.ObtenerMenu;
using Menus.Application.Tests.Catalogo;
using Menus.Application.Tests.Catalogo.Dietas;
using Menus.Application.Tests.Catalogo.Nomencladores;
using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Tests.GestionDeMenus;

/// <summary>Pruebas de las consultas de menús del #80 contra los datos en memoria.</summary>
public sealed class ConsultasDeMenusTests
{
    private readonly BancoDeDietasFalso _banco = new();
    private readonly NutricionistasFalsos _nutricionistas = new();
    private readonly BancoFalso _items = new();
    private readonly BancoDeNomencladoresFalso _nomencladores = new();
    private readonly Dieta _dieta;
    private readonly Nutricionista _creadora;
    private readonly Nutricionista _jefa;

    public ConsultasDeMenusTests()
    {
        _dieta = _banco.Agregar("Dieta renal");
        _creadora = _nutricionistas.Agregar("Ana", esJefe: false, _dieta.Id);
        _jefa = _nutricionistas.Agregar("Berta", esJefe: true, _dieta.Id);
    }

    private ListarMenusHandler Listar() => new(_banco.MenusRepo, _banco.DietasRepo, _nutricionistas);

    private ObtenerMenuHandler Obtener()
        => new(_banco.MenusRepo, _nutricionistas, _nomencladores.GruposRepo, _items.ItemsRepo);

    private Menu NuevoMenu(int diasAtras = 1, Guid? creador = null, Guid? dietaId = null)
    {
        var menu = new Menu(
            Guid.NewGuid(), dietaId ?? _dieta.Id, creador ?? _creadora.Id, DateTime.UtcNow.AddDays(-diasAtras), 2, true,
            [(TipoPreparacion.Postre.Id, 0.4m), (TipoPreparacion.Entrante.Id, 0.6m)],
            [BancoDeDietasFalso.GrupoFrutas, BancoDeDietasFalso.GrupoCereales]);
        _banco.Menus.Add(menu);
        return menu;
    }

    // ---- Listar ----

    [Fact]
    public async Task Listar_devuelve_la_pagina_del_mas_reciente_al_mas_antiguo_con_el_total()
    {
        var antiguo = NuevoMenu(diasAtras: 3);
        var reciente = NuevoMenu(diasAtras: 1);
        NuevoMenu(diasAtras: 2);
        NuevoMenu(dietaId: Guid.NewGuid());

        var pagina = await Listar().HandleAsync(new ListarMenusQuery(_dieta.Id, null, 1, 2), CancellationToken.None);

        Assert.Equal(3, pagina.Total);
        Assert.Equal(1, pagina.Page);
        Assert.Equal(2, pagina.Size);
        Assert.Equal(reciente.Id, pagina.Data[0].Id);
        Assert.DoesNotContain(pagina.Data, m => m.Id == antiguo.Id);
    }

    [Fact]
    public async Task Listar_un_menu_pendiente_lo_muestra_sin_dictamen_y_con_el_nombre_del_creador()
    {
        var menu = NuevoMenu();

        var dto = (await Listar().HandleAsync(new ListarMenusQuery(_dieta.Id), CancellationToken.None)).Data.Single();

        Assert.Equal(
            new MenuDto(menu.Id, _dieta.Id, menu.FechaCreacion, new(_creadora.Id, "Ana"), true, 2, "Pendiente", null, null),
            dto);
    }

    [Fact]
    public async Task Listar_un_menu_dictaminado_trae_el_dictamen_con_el_nombre_del_revisor()
    {
        var menu = NuevoMenu();
        var fecha = DateTime.UtcNow.AddHours(-1);
        menu.Rechazar(_jefa.Id, fecha, "Faltan frutas");

        var dto = (await Listar().HandleAsync(new ListarMenusQuery(_dieta.Id), CancellationToken.None)).Data.Single();

        Assert.Equal("Rechazado", dto.Estado);
        Assert.Equal(new DictamenDto(new(_jefa.Id, "Berta"), fecha, false, "Faltan frutas"), dto.Dictamen);
    }

    [Theory]
    [InlineData(EstadoDeMenu.Pendiente)]
    [InlineData(EstadoDeMenu.Final)]
    [InlineData(EstadoDeMenu.Rechazado)]
    public async Task Listar_filtra_por_estado(EstadoDeMenu estado)
    {
        var pendiente = NuevoMenu();
        var final = NuevoMenu();
        final.Aprobar(_jefa.Id, DateTime.UtcNow);
        var rechazado = NuevoMenu();
        rechazado.Rechazar(_jefa.Id, DateTime.UtcNow);
        var esperado = estado switch
        {
            EstadoDeMenu.Pendiente => pendiente,
            EstadoDeMenu.Final => final,
            _ => rechazado,
        };

        var pagina = await Listar().HandleAsync(new ListarMenusQuery(_dieta.Id, estado), CancellationToken.None);

        var dto = Assert.Single(pagina.Data);
        Assert.Equal(esperado.Id, dto.Id);
        Assert.Equal(estado.ToString(), dto.Estado);
    }

    [Fact]
    public async Task Listar_incluye_los_menus_de_otros_nutricionistas_autorizados_para_la_dieta()
    {
        var otra = _nutricionistas.Agregar("Carla", esJefe: false, _dieta.Id);
        NuevoMenu();
        NuevoMenu(creador: otra.Id);

        var pagina = await Listar().HandleAsync(new ListarMenusQuery(_dieta.Id), CancellationToken.None);

        Assert.Equal(["Ana", "Carla"], pagina.Data.Select(m => m.Creador.Nombre).Order());
    }

    [Fact]
    public async Task Listar_nombra_al_creador_aunque_ya_no_este_autorizado_para_la_dieta()
    {
        var sinAutorizacion = _nutricionistas.Agregar("Dora", esJefe: false);
        NuevoMenu(creador: sinAutorizacion.Id);

        var dto = (await Listar().HandleAsync(new ListarMenusQuery(_dieta.Id), CancellationToken.None)).Data.Single();

        Assert.Equal(new NutricionistaDelMenuDto(sinAutorizacion.Id, "Dora"), dto.Creador);
    }

    [Fact]
    public async Task Listar_una_dieta_que_no_existe_da_404()
    {
        var id = Guid.NewGuid();

        var error = await Assert.ThrowsAsync<NoEncontradoException>(
            () => Listar().HandleAsync(new ListarMenusQuery(id), CancellationToken.None));

        Assert.Equal($"No existe el dieta {id}.", error.Message);
    }

    [Theory]
    [MemberData(nameof(ListadosInvalidos))]
    public void El_validador_del_listado_rechaza_la_entrada_mal_formada(ListarMenusQuery consulta, string campo)
        => Assert.Equal([campo], new ListarMenusValidator().Validate(consulta).Errors.Select(e => e.PropertyName).Distinct());

    public static TheoryData<ListarMenusQuery, string> ListadosInvalidos() => new()
    {
        { new ListarMenusQuery(Guid.Empty), "DietaId" },
        { new ListarMenusQuery(Guid.NewGuid(), (EstadoDeMenu)7), "Estado" },
        { new ListarMenusQuery(Guid.NewGuid(), null, 0), "Page" },
        { new ListarMenusQuery(Guid.NewGuid(), null, 1, 101), "Size" },
    };

    [Fact]
    public void El_validador_del_listado_acepta_una_consulta_con_estado()
        => Assert.True(new ListarMenusValidator().Validate(new ListarMenusQuery(Guid.NewGuid(), EstadoDeMenu.Final)).IsValid);

    // ---- Obtener ----

    [Fact]
    public async Task Obtener_trae_la_parametrizacion_y_los_items_con_nombre_y_ordenados()
    {
        _nomencladores.Grupos.Add(new GrupoNutricional(BancoDeDietasFalso.GrupoFrutas, "Frutas"));
        _nomencladores.Grupos.Add(new GrupoNutricional(BancoDeDietasFalso.GrupoCereales, "Cereales"));
        _nomencladores.Grupos.Add(new GrupoNutricional(Guid.NewGuid(), "Lácteos"));
        var pera = _items.Agregar("Pera");
        var arroz = _items.Agregar("Arroz");
        _items.Agregar("Leche");
        var menu = NuevoMenu();
        menu.AgregarItem(pera.Id);
        menu.AgregarItem(arroz.Id);

        var dto = await Obtener().HandleAsync(new ObtenerMenuQuery(menu.Id), CancellationToken.None);

        Assert.Equal(
            (menu.Id, _dieta.Id, menu.FechaCreacion, new NutricionistaDelMenuDto(_creadora.Id, "Ana"), true, 2, "Pendiente"),
            (dto.Id, dto.DietaId, dto.FechaCreacion, dto.Creador, dto.EsAutomatico, dto.CantidadTotalAlimentos, dto.Estado));
        Assert.Equal(
            [
                new ProporcionDelMenuDto(TipoPreparacion.Entrante.Id, TipoPreparacion.Entrante.Nombre, 0.6m),
                new ProporcionDelMenuDto(TipoPreparacion.Postre.Id, TipoPreparacion.Postre.Nombre, 0.4m),
            ],
            dto.Distribucion);
        Assert.Equal(["Cereales", "Frutas"], dto.GruposNutricionales.Select(g => g.Nombre));
        Assert.Equal([new ElementoDelMenuDto(arroz.Id, "Arroz"), new ElementoDelMenuDto(pera.Id, "Pera")], dto.Items);
    }

    [Fact]
    public async Task Obtener_un_menu_de_reemplazo_trae_el_rechazado_y_el_dictamen_del_rechazado_su_revisor()
    {
        var rechazado = NuevoMenu(diasAtras: 2);
        rechazado.Rechazar(_jefa.Id, DateTime.UtcNow.AddDays(-1), "Rehacer");
        var nuevo = NuevoMenu();
        nuevo.RegistrarComoReemplazo(rechazado);

        var deRechazado = await Obtener().HandleAsync(new ObtenerMenuQuery(rechazado.Id), CancellationToken.None);
        var deNuevo = await Obtener().HandleAsync(new ObtenerMenuQuery(nuevo.Id), CancellationToken.None);

        Assert.Equal("Rechazado", deRechazado.Estado);
        Assert.Equal("Berta", deRechazado.Dictamen!.Revisor.Nombre);
        Assert.Equal(rechazado.Id, deNuevo.ReemplazaA);
        Assert.Null(deNuevo.Dictamen);
    }

    [Fact]
    public async Task Obtener_un_menu_que_no_existe_da_404()
    {
        var id = Guid.NewGuid();

        var error = await Assert.ThrowsAsync<NoEncontradoException>(
            () => Obtener().HandleAsync(new ObtenerMenuQuery(id), CancellationToken.None));

        Assert.Equal($"No existe el menú {id}.", error.Message);
    }
}
