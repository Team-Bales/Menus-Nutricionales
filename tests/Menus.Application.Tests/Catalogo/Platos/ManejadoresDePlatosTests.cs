using Menus.Application.Catalogo.Platos.ActualizarPlato;
using Menus.Application.Catalogo.Platos.AgregarIngrediente;
using Menus.Application.Catalogo.Platos.EliminarPlato;
using Menus.Application.Catalogo.Platos.ListarPlatos;
using Menus.Application.Catalogo.Platos.ObtenerPlato;
using Menus.Application.Catalogo.Platos.QuitarIngrediente;
using Menus.Application.Catalogo.Platos.RegistrarPlato;
using Menus.Application.Comun;
using Menus.Domain.Entidades;
using Menus.Domain.Excepciones;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Tests.Catalogo.Platos;

/// <summary>Pruebas de los manejadores del #62 contra el banco en memoria.</summary>
public sealed class ManejadoresDePlatosTests
{
    private static readonly Guid Otro = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    private readonly BancoFalso _banco = new();

    private RegistrarPlatoHandler Registrar() => new(
        _banco.ItemsRepo, _banco.AlimentosRepo, _banco.PlatosRepo, _banco.UnidadDeTrabajo, new ActorFalso(BancoFalso.Descriptor));

    private AgregarIngredienteHandler Agregar(Guid? actor = null) => new(
        _banco.ItemsRepo, _banco.AlimentosRepo, _banco.PlatosRepo, _banco.UnidadDeTrabajo, new ActorFalso(actor ?? BancoFalso.Descriptor), _banco.Fichas);

    private QuitarIngredienteHandler Quitar(Guid? actor = null) => new(
        _banco.PlatosRepo, _banco.UnidadDeTrabajo, new ActorFalso(actor ?? BancoFalso.Descriptor), _banco.Fichas);

    private static RegistrarPlatoCommand Comando(string nombre, params IngredienteDelPlato[] ingredientes) => new(
        nombre, BancoFalso.GrupoCereales, TipoPreparacion.PlatoFuerte.Id, NivelCalorico.Medio.Id, ingredientes);

    // ---- Registrar ----

    [Fact]
    public async Task Registrar_guarda_el_plato_con_su_composicion_y_el_actor_como_descriptor()
    {
        var arroz = _banco.Agregar("Arroz");
        var leche = _banco.Agregar("Leche");

        var creado = await Registrar().HandleAsync(
            Comando(" Arroz con leche ", new IngredienteDelPlato(arroz.Id, 200m), new(leche.Id, 250m)), CancellationToken.None);

        var plato = Assert.Single(_banco.Items.OfType<Plato>());
        Assert.Equal(creado.Id, plato.Id);
        Assert.Equal("Arroz con leche", plato.Nombre);
        Assert.Equal(BancoFalso.Descriptor, plato.NutricionistaId);
        Assert.Equal([200m, 250m], plato.Ingredientes.Select(i => i.Cantidad));
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Registrar_con_el_nombre_de_un_alimento_lanza_RC01()
    {
        var arroz = _banco.Agregar("Arroz");

        var ex = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => Registrar().HandleAsync(Comando("ARROZ", new IngredienteDelPlato(arroz.Id, 1m)), CancellationToken.None));

        Assert.Equal("RC-01", ex.Regla);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Registrar_con_un_ingrediente_inexistente_lanza_RR02()
    {
        var ex = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => Registrar().HandleAsync(Comando("Sopa", new IngredienteDelPlato(Guid.NewGuid(), 1m)), CancellationToken.None));

        Assert.Equal("RR-02", ex.Regla);
        Assert.Empty(_banco.Items);
    }

    [Fact]
    public async Task Registrar_con_un_plato_como_ingrediente_lanza_RS12()
    {
        var otroPlato = _banco.AgregarPlato("Sofrito", _banco.Agregar("Cebolla"));

        var ex = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => Registrar().HandleAsync(Comando("Arroz con sofrito", new IngredienteDelPlato(otroPlato.Id, 1m)), CancellationToken.None));

        Assert.Equal("RS-12", ex.Regla);
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    // ---- Composición ----

    [Fact]
    public async Task Agregar_suma_el_ingrediente_y_confirma()
    {
        var plato = _banco.AgregarPlato("Ensalada", _banco.Agregar("Lechuga"));
        var tomate = _banco.Agregar("Tomate");

        await Agregar().HandleAsync(new AgregarIngredienteCommand(plato.Id, tomate.Id, 80m), CancellationToken.None);

        Assert.Contains(plato.Ingredientes, i => i.AlimentoId == tomate.Id && i.Cantidad == 80m);
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
        Assert.Equal([("Agregar", plato.Id, (Guid?)tomate.Id, 1)], _banco.Fichas.Llamadas);
    }

    [Fact]
    public async Task Agregar_por_otro_nutricionista_lanza_RS02()
    {
        var plato = _banco.AgregarPlato("Ensalada", _banco.Agregar("Lechuga"));
        var tomate = _banco.Agregar("Tomate");

        var ex = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => Agregar(Otro).HandleAsync(new AgregarIngredienteCommand(plato.Id, tomate.Id, 80m), CancellationToken.None));

        Assert.Equal("RS-02", ex.Regla);
        Assert.Single(plato.Ingredientes);
        Assert.Empty(_banco.Fichas.Llamadas);
    }

    [Fact]
    public async Task Agregar_a_un_plato_inexistente_lanza_NoEncontrado()
    {
        var tomate = _banco.Agregar("Tomate");

        var ex = await Assert.ThrowsAsync<NoEncontradoException>(
            () => Agregar().HandleAsync(new AgregarIngredienteCommand(Guid.NewGuid(), tomate.Id, 80m), CancellationToken.None));

        Assert.Equal("plato", ex.Recurso);
    }

    [Fact]
    public async Task Agregar_un_ingrediente_que_ya_esta_lanza_regla_de_negocio()
    {
        var lechuga = _banco.Agregar("Lechuga");
        var plato = _banco.AgregarPlato("Ensalada", lechuga);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => Agregar().HandleAsync(new AgregarIngredienteCommand(plato.Id, lechuga.Id, 10m), CancellationToken.None));
        Assert.Equal(0, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Quitar_saca_el_ingrediente_y_confirma()
    {
        var lechuga = _banco.Agregar("Lechuga");
        var tomate = _banco.Agregar("Tomate");
        var plato = _banco.AgregarPlato("Ensalada", lechuga, tomate);

        await Quitar().HandleAsync(new QuitarIngredienteCommand(plato.Id, tomate.Id), CancellationToken.None);

        Assert.Equal([lechuga.Id], plato.Ingredientes.Select(i => i.AlimentoId));
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
        Assert.Equal([("Quitar", plato.Id, (Guid?)tomate.Id, 1)], _banco.Fichas.Llamadas);
    }

    [Fact]
    public async Task Quitar_el_unico_ingrediente_lanza_RK03()
    {
        var lechuga = _banco.Agregar("Lechuga");
        var plato = _banco.AgregarPlato("Ensalada", lechuga);

        var ex = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => Quitar().HandleAsync(new QuitarIngredienteCommand(plato.Id, lechuga.Id), CancellationToken.None));

        Assert.Equal("RK-03", ex.Regla);
        Assert.Single(plato.Ingredientes);
        Assert.Empty(_banco.Fichas.Llamadas);
    }

    [Fact]
    public async Task Quitar_un_alimento_que_no_esta_lanza_NoEncontrado_del_ingrediente()
    {
        var plato = _banco.AgregarPlato("Ensalada", _banco.Agregar("Lechuga"), _banco.Agregar("Tomate"));

        var ex = await Assert.ThrowsAsync<NoEncontradoException>(
            () => Quitar().HandleAsync(new QuitarIngredienteCommand(plato.Id, Guid.NewGuid()), CancellationToken.None));

        Assert.Equal("ingrediente", ex.Recurso);
    }

    [Fact]
    public async Task Quitar_por_otro_nutricionista_lanza_RS02()
    {
        var tomate = _banco.Agregar("Tomate");
        var plato = _banco.AgregarPlato("Ensalada", _banco.Agregar("Lechuga"), tomate);

        var ex = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => Quitar(Otro).HandleAsync(new QuitarIngredienteCommand(plato.Id, tomate.Id), CancellationToken.None));

        Assert.Equal("RS-02", ex.Regla);
        Assert.Equal(2, plato.Ingredientes.Count);
    }

    [Fact]
    public async Task Quitar_de_un_plato_inexistente_lanza_NoEncontrado()
    {
        var ex = await Assert.ThrowsAsync<NoEncontradoException>(
            () => Quitar().HandleAsync(new QuitarIngredienteCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));

        Assert.Equal("plato", ex.Recurso);
    }

    // ---- Actualizar y eliminar ----

    [Fact]
    public async Task Actualizar_reclasifica_el_plato()
    {
        var plato = _banco.AgregarPlato("Ensalada", _banco.Agregar("Lechuga"));
        var handler = new ActualizarPlatoHandler(_banco.PlatosRepo, _banco.UnidadDeTrabajo, new ActorFalso(BancoFalso.Descriptor));

        await handler.HandleAsync(
            new ActualizarPlatoCommand(plato.Id, BancoFalso.GrupoCereales, TipoPreparacion.Entrante.Id, NivelCalorico.Bajo.Id),
            CancellationToken.None);

        Assert.Equal(TipoPreparacion.Entrante.Id, plato.TipoPreparacionId);
        Assert.Equal(NivelCalorico.Bajo.Id, plato.NivelCaloricoId);
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
    }

    [Fact]
    public async Task Actualizar_un_plato_inexistente_lanza_NoEncontrado()
    {
        var handler = new ActualizarPlatoHandler(_banco.PlatosRepo, _banco.UnidadDeTrabajo, new ActorFalso(BancoFalso.Descriptor));

        await Assert.ThrowsAsync<NoEncontradoException>(() => handler.HandleAsync(
            new ActualizarPlatoCommand(Guid.NewGuid(), BancoFalso.GrupoCereales, TipoPreparacion.Entrante.Id, NivelCalorico.Bajo.Id),
            CancellationToken.None));
    }

    [Fact]
    public async Task Eliminar_un_plato_sin_uso_lo_quita_y_confirma()
    {
        var plato = _banco.AgregarPlato("Ensalada", _banco.Agregar("Lechuga"));

        await new EliminarPlatoHandler(_banco.ItemsRepo, _banco.PlatosRepo, _banco.UnidadDeTrabajo, _banco.Fichas)
            .HandleAsync(new EliminarPlatoCommand(plato.Id), CancellationToken.None);

        Assert.DoesNotContain(plato, _banco.Items);
        Assert.Equal(1, _banco.UnidadDeTrabajo.Confirmaciones);
        Assert.Equal([("Eliminar", plato.Id, (Guid?)null, 1)], _banco.Fichas.Llamadas);
    }

    [Fact]
    public async Task Eliminar_un_plato_que_figura_en_un_menu_lanza_RR22()
    {
        var plato = _banco.AgregarPlato("Ensalada", _banco.Agregar("Lechuga"));
        _banco.EnMenus.Add(plato.Id);

        var ex = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => new EliminarPlatoHandler(_banco.ItemsRepo, _banco.PlatosRepo, _banco.UnidadDeTrabajo, _banco.Fichas)
                .HandleAsync(new EliminarPlatoCommand(plato.Id), CancellationToken.None));

        Assert.Equal("RR-22", ex.Regla);
        Assert.Contains(plato, _banco.Items);
        Assert.Empty(_banco.Fichas.Llamadas);
    }

    [Fact]
    public async Task Eliminar_un_plato_inexistente_lanza_NoEncontrado()
    {
        await Assert.ThrowsAsync<NoEncontradoException>(
            () => new EliminarPlatoHandler(_banco.ItemsRepo, _banco.PlatosRepo, _banco.UnidadDeTrabajo, _banco.Fichas)
                .HandleAsync(new EliminarPlatoCommand(Guid.NewGuid()), CancellationToken.None));
    }

    // ---- Consultas ----

    [Fact]
    public async Task Listar_devuelve_solo_platos_paginados()
    {
        var lechuga = _banco.Agregar("Lechuga");
        var huevo = _banco.Agregar("Huevo");
        _banco.AgregarPlato("Ensalada", lechuga);
        _banco.AgregarPlato("Sopa", lechuga);
        var tortilla = _banco.AgregarPlato("Tortilla", lechuga, huevo);

        var resultado = await new ListarPlatosHandler(_banco.PlatosRepo)
            .HandleAsync(new ListarPlatosQuery(Page: 2, Size: 2), CancellationToken.None);

        Assert.Equal(3, resultado.Total);
        Assert.Equal(2, resultado.Page);
        Assert.Equal(2, resultado.Size);
        // Se leen las propiedades una por una: la igualdad de un record compara sus campos y no llega a ellas.
        var dto = Assert.Single(resultado.Data);
        Assert.Equal(tortilla.Id, dto.Id);
        Assert.Equal("Tortilla", dto.Nombre);
        Assert.Equal(BancoFalso.GrupoCereales, dto.GrupoNutricionalId);
        Assert.Equal(TipoPreparacion.PlatoFuerte.Id, dto.TipoPreparacionId);
        Assert.Equal(NivelCalorico.Medio.Id, dto.NivelCaloricoId);
        Assert.Equal(BancoFalso.Descriptor, dto.NutricionistaId);
        Assert.Equal(2, dto.CantidadDeIngredientes);
    }

    [Fact]
    public async Task Obtener_devuelve_los_ingredientes_con_nombre_y_cantidad_ordenados_por_nombre()
    {
        var tomate = _banco.Agregar("Tomate");
        var lechuga = _banco.Agregar("Lechuga");
        var plato = _banco.AgregarPlato("Ensalada", tomate, lechuga);

        var dto = await new ObtenerPlatoHandler(_banco.PlatosRepo, _banco.AlimentosRepo)
            .HandleAsync(new ObtenerPlatoQuery(plato.Id), CancellationToken.None);

        Assert.Equal(plato.Id, dto.Id);
        Assert.Equal("Ensalada", dto.Nombre);
        Assert.Equal(BancoFalso.GrupoCereales, dto.GrupoNutricionalId);
        Assert.Equal(TipoPreparacion.PlatoFuerte.Id, dto.TipoPreparacionId);
        Assert.Equal(NivelCalorico.Medio.Id, dto.NivelCaloricoId);
        Assert.Equal(BancoFalso.Descriptor, dto.NutricionistaId);
        Assert.Equal([lechuga.Id, tomate.Id], dto.Ingredientes.Select(i => i.AlimentoId));
        Assert.Equal(["Lechuga", "Tomate"], dto.Ingredientes.Select(i => i.Nombre));
        Assert.All(dto.Ingredientes, i => Assert.Equal(100m, i.Cantidad));
    }

    [Fact]
    public async Task Obtener_un_plato_inexistente_lanza_NoEncontrado()
    {
        var ex = await Assert.ThrowsAsync<NoEncontradoException>(() => new ObtenerPlatoHandler(_banco.PlatosRepo, _banco.AlimentosRepo)
            .HandleAsync(new ObtenerPlatoQuery(Guid.NewGuid()), CancellationToken.None));

        Assert.Equal("plato", ex.Recurso);
    }
}
