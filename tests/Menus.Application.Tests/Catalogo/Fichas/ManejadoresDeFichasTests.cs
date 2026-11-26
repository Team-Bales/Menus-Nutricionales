using System.Text.Json.Nodes;
using Menus.Application.Catalogo.Fichas;
using Menus.Application.Catalogo.Fichas.GuardarFicha;
using Menus.Application.Catalogo.Fichas.ObtenerFicha;
using Menus.Application.Comun;
using Menus.Domain.Excepciones;

namespace Menus.Application.Tests.Catalogo.Fichas;

/// <summary>Pruebas de los manejadores de la ficha nutricional (#283) contra el banco en memoria.</summary>
public sealed class ManejadoresDeFichasTests
{
    private static readonly Guid Otro = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    private readonly BancoFalso _banco = new();
    private readonly AlmacenDeFichasFalso _almacen = new();

    private GuardarFichaHandler Guardar(Guid? actor = null)
        => new(_banco.AlimentosRepo, _banco.PlatosRepo, new ActorFalso(actor ?? BancoFalso.Descriptor), _almacen);

    private ObtenerFichaHandler Obtener() => new(_banco.AlimentosRepo, _banco.PlatosRepo, _almacen);

    private static GuardarFichaCommand Comando(
        ClaseDeItem clase,
        Guid itemId,
        IReadOnlyList<string>? alergenos = null,
        IReadOnlyList<IngredienteDeFichaEntrada>? ingredientes = null,
        PorcionDeReferenciaDto? porcion = null)
        => new(clase, itemId, porcion, new JsonObject { ["energiaKcal"] = 52 }, null, alergenos, null, ingredientes);

    // ---- Guardar ----

    [Fact]
    public async Task Guardar_la_ficha_de_un_alimento_fija_origen_tipo_y_fecha_y_recorta_los_textos()
    {
        var manzana = _banco.Agregar("Manzana");
        var antes = DateTime.UtcNow.AddSeconds(-1);

        var ficha = await Guardar().HandleAsync(
            Comando(ClaseDeItem.Alimento, manzana.Id, [" gluten "], porcion: new PorcionDeReferenciaDto(100, " g ")),
            CancellationToken.None);

        var guardada = Assert.Single(_almacen.Guardadas);
        Assert.Same(guardada, ficha);
        Assert.Equal(manzana.Id, ficha.ItemId);
        Assert.Equal("Alimento", ficha.Clase);
        Assert.Equal("simple", ficha.Tipo);
        Assert.Equal(new PorcionDeReferenciaDto(100, "g"), ficha.PorcionReferencia);
        Assert.Equal(["gluten"], ficha.Alergenos!);
        Assert.Equal(52, (int)ficha.Macronutrientes!["energiaKcal"]!);
        Assert.Null(ficha.Ingredientes);
        Assert.Equal(DateTimeKind.Utc, ficha.ActualizadoEn.Kind);
        Assert.InRange(ficha.ActualizadoEn, antes, DateTime.UtcNow);
        Assert.Equal(0, ficha.ActualizadoEn.Ticks % TimeSpan.TicksPerMillisecond);
    }

    [Fact]
    public async Task Guardar_la_ficha_de_un_alimento_que_no_existe_lanza_404_y_no_guarda()
    {
        var ex = await Assert.ThrowsAsync<NoEncontradoException>(
            () => Guardar().HandleAsync(Comando(ClaseDeItem.Alimento, Guid.NewGuid()), CancellationToken.None));

        Assert.Equal("alimento", ex.Recurso);
        Assert.Empty(_almacen.Guardadas);
    }

    [Fact]
    public async Task Guardar_por_la_ruta_de_alimento_la_ficha_de_un_plato_lanza_404()
    {
        var plato = _banco.AgregarPlato("Arroz con leche", _banco.Agregar("Arroz"));

        await Assert.ThrowsAsync<NoEncontradoException>(
            () => Guardar().HandleAsync(Comando(ClaseDeItem.Alimento, plato.Id), CancellationToken.None));
        Assert.Empty(_almacen.Guardadas);
    }

    [Fact]
    public async Task Guardar_la_ficha_de_un_alimento_ajeno_lanza_RS02_y_no_guarda()
    {
        var manzana = _banco.Agregar("Manzana");

        var ex = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => Guardar(Otro).HandleAsync(Comando(ClaseDeItem.Alimento, manzana.Id), CancellationToken.None));

        Assert.Equal("RS-02", ex.Regla);
        Assert.Empty(_almacen.Guardadas);
    }

    [Fact]
    public async Task Guardar_la_ficha_de_un_plato_ajeno_lanza_RS02_y_no_guarda()
    {
        var plato = _banco.AgregarPlato("Arroz con leche", _banco.Agregar("Arroz"));

        var ex = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => Guardar(Otro).HandleAsync(Comando(ClaseDeItem.Plato, plato.Id), CancellationToken.None));

        Assert.Equal("RS-02", ex.Regla);
        Assert.Empty(_almacen.Guardadas);
    }

    [Fact]
    public async Task Guardar_la_ficha_de_un_alimento_con_desglose_lanza_RM03()
    {
        var manzana = _banco.Agregar("Manzana");

        var ex = await Assert.ThrowsAsync<DatoInvalidoException>(
            () => Guardar().HandleAsync(
                Comando(ClaseDeItem.Alimento, manzana.Id, ingredientes: [new(Guid.NewGuid(), null)]),
                CancellationToken.None));

        Assert.Equal("RM-03", ex.Regla);
        Assert.Equal("ingredientes", ex.Campo);
        Assert.Empty(_almacen.Guardadas);
    }

    [Fact]
    public async Task Guardar_la_ficha_de_un_plato_sin_desglose_es_compuesta_y_sin_ingredientes()
    {
        var plato = _banco.AgregarPlato("Arroz con leche", _banco.Agregar("Arroz"), _banco.Agregar("Leche"));

        var ficha = await Guardar().HandleAsync(Comando(ClaseDeItem.Plato, plato.Id), CancellationToken.None);

        Assert.Equal("Plato", ficha.Clase);
        Assert.Equal("compuesto", ficha.Tipo);
        Assert.Null(ficha.Ingredientes);
        Assert.Single(_almacen.Guardadas);
    }

    [Fact]
    public async Task Guardar_la_ficha_de_un_plato_con_su_composicion_copia_los_nombres_en_el_orden_recibido()
    {
        var arroz = _banco.Agregar("Arroz");
        var leche = _banco.Agregar("Leche");
        var plato = _banco.AgregarPlato("Arroz con leche", arroz, leche);
        var aporte = new JsonObject { ["energiaKcal"] = 120 };

        var ficha = await Guardar().HandleAsync(
            Comando(ClaseDeItem.Plato, plato.Id, ingredientes: [new(leche.Id, aporte), new(arroz.Id, null)]),
            CancellationToken.None);

        Assert.Equal(
            new[] { (leche.Id, "Leche"), (arroz.Id, "Arroz") },
            ficha.Ingredientes!.Select(i => (i.AlimentoId, i.Nombre!)));
        Assert.Same(aporte, ficha.Ingredientes![0].Aporte);
        Assert.Null(ficha.Ingredientes[1].Aporte);
    }

    [Fact]
    public async Task Guardar_un_desglose_al_que_le_falta_un_ingrediente_lanza_RM06_y_no_guarda()
    {
        var arroz = _banco.Agregar("Arroz");
        var leche = _banco.Agregar("Leche");
        var plato = _banco.AgregarPlato("Arroz con leche", arroz, leche);

        var ex = await Assert.ThrowsAsync<DatoInvalidoException>(
            () => Guardar().HandleAsync(
                Comando(ClaseDeItem.Plato, plato.Id, ingredientes: [new(arroz.Id, null)]),
                CancellationToken.None));

        Assert.Equal("RM-06", ex.Regla);
        Assert.Contains(leche.Id.ToString(), ex.Message);
        Assert.Empty(_almacen.Guardadas);
    }

    [Fact]
    public async Task Guardar_un_desglose_con_un_alimento_fuera_de_la_composicion_lanza_RM06()
    {
        var arroz = _banco.Agregar("Arroz");
        var azucar = _banco.Agregar("Azúcar");
        var plato = _banco.AgregarPlato("Arroz blanco", arroz);

        var ex = await Assert.ThrowsAsync<DatoInvalidoException>(
            () => Guardar().HandleAsync(
                Comando(ClaseDeItem.Plato, plato.Id, ingredientes: [new(arroz.Id, null), new(azucar.Id, null)]),
                CancellationToken.None));

        Assert.Equal("RM-06", ex.Regla);
        Assert.Contains(azucar.Id.ToString(), ex.Message);
        Assert.Empty(_almacen.Guardadas);
    }

    [Fact]
    public async Task Guardar_la_ficha_de_un_plato_que_no_existe_lanza_404()
    {
        var ex = await Assert.ThrowsAsync<NoEncontradoException>(
            () => Guardar().HandleAsync(Comando(ClaseDeItem.Plato, Guid.NewGuid()), CancellationToken.None));

        Assert.Equal("plato", ex.Recurso);
    }

    // ---- Obtener ----

    [Fact]
    public async Task Obtener_la_ficha_de_un_alimento_que_no_existe_lanza_404_de_alimento()
    {
        var id = Guid.NewGuid();

        var ex = await Assert.ThrowsAsync<NoEncontradoException>(
            () => Obtener().HandleAsync(new ObtenerFichaQuery(ClaseDeItem.Alimento, id), CancellationToken.None));

        Assert.Equal($"No existe el alimento {id}.", ex.Message);
    }

    [Fact]
    public async Task Obtener_la_ficha_de_un_plato_sin_ficha_lanza_404_que_lo_dice()
    {
        var plato = _banco.AgregarPlato("Arroz con leche", _banco.Agregar("Arroz"));

        var ex = await Assert.ThrowsAsync<NoEncontradoException>(
            () => Obtener().HandleAsync(new ObtenerFichaQuery(ClaseDeItem.Plato, plato.Id), CancellationToken.None));

        Assert.Equal($"El plato {plato.Id} no tiene ficha nutricional.", ex.Message);
    }

    [Fact]
    public async Task Obtener_devuelve_la_ficha_guardada()
    {
        var manzana = _banco.Agregar("Manzana");
        var guardada = await Guardar().HandleAsync(Comando(ClaseDeItem.Alimento, manzana.Id, []), CancellationToken.None);

        var ficha = await Obtener().HandleAsync(new ObtenerFichaQuery(ClaseDeItem.Alimento, manzana.Id), CancellationToken.None);

        Assert.Same(guardada, ficha);
    }

    [Fact]
    public async Task Obtener_por_la_ruta_de_plato_la_ficha_de_un_alimento_lanza_404()
    {
        var manzana = _banco.Agregar("Manzana");

        await Assert.ThrowsAsync<NoEncontradoException>(
            () => Obtener().HandleAsync(new ObtenerFichaQuery(ClaseDeItem.Plato, manzana.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Sin_almacen_disponible_el_error_llega_al_cliente()
    {
        var manzana = _banco.Agregar("Manzana");
        _almacen.Disponible = false;

        await Assert.ThrowsAsync<ServicioNoDisponibleException>(
            () => Obtener().HandleAsync(new ObtenerFichaQuery(ClaseDeItem.Alimento, manzana.Id), CancellationToken.None));
        await Assert.ThrowsAsync<ServicioNoDisponibleException>(
            () => Guardar().HandleAsync(Comando(ClaseDeItem.Alimento, manzana.Id), CancellationToken.None));
    }
}

/// <summary>Almacén de fichas en memoria, con una ficha por clase e ítem (RM-08).</summary>
internal sealed class AlmacenDeFichasFalso : IAlmacenDeFichas
{
    private readonly Dictionary<(string Clase, Guid ItemId), FichaNutricionalDto> _fichas = [];

    public List<FichaNutricionalDto> Guardadas { get; } = [];

    public bool Disponible { get; set; } = true;

    public Task<FichaNutricionalDto?> ObtenerAsync(ClaseDeItem clase, Guid itemId, CancellationToken ct)
    {
        ExigirDisponible();
        var tabla = clase == ClaseDeItem.Plato ? "Plato" : "Alimento";
        return Task.FromResult(_fichas.GetValueOrDefault((tabla, itemId)));
    }

    public Task GuardarAsync(FichaNutricionalDto ficha, CancellationToken ct)
    {
        ExigirDisponible();
        _fichas[(ficha.Clase, ficha.ItemId)] = ficha;
        Guardadas.Add(ficha);
        return Task.CompletedTask;
    }

    private void ExigirDisponible()
    {
        if (!Disponible)
        {
            throw new ServicioNoDisponibleException("Sin MongoDB.");
        }
    }
}
