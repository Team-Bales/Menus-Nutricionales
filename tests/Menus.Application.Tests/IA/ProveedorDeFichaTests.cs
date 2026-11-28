using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Menus.Application.Catalogo.Fichas;
using Menus.Application.IA;
using Menus.Application.IA.Proveedores;
using Menus.Application.Tests.Catalogo;
using Menus.Application.Tests.Catalogo.Dietas;
using Menus.Application.Tests.Catalogo.Fichas;
using Menus.Application.Tests.Valoraciones;
using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Application.Tests.IA;

/// <summary>
/// Pruebas del proveedor de contexto <c>ficha</c> (#336) con las fichas de ejemplo del documento 1 de
/// <c>docs/Ficha nutricional MongoDB</c>: su contenido, su prueba de privacidad (contrato de IA, §5.5), que cada
/// número del resumen esté en las cifras (§6) y los casos sin ficha o sin MongoDB.
/// </summary>
public sealed partial class ProveedorDeFichaTests
{
    private const string NombreDelPaciente = "Juan Pérez Gómez";
    private const int EdadDelPaciente = 34;

    private static readonly DateTime FechaValoracion = new(2026, 11, 22, 10, 0, 0, DateTimeKind.Utc);

    private readonly BancoDeDietasFalso _banco = new();
    private readonly BancoFalso _catalogo = new();
    private readonly AlmacenDeFichasFalso _fichas = new();
    private readonly ValoracionesFalsas _valoraciones;

    private readonly Guid _pacienteId = Guid.NewGuid();
    private readonly Guid _dietaId;
    private readonly Plato _arrozConPollo;
    private readonly Alimento _manzana;
    private readonly Alimento _pan;
    private readonly ValoracionNutricional _valoracion;

    public ProveedorDeFichaTests()
    {
        _valoraciones = new ValoracionesFalsas(_banco);
        _dietaId = _banco.Agregar("Hiposódica").Id;

        var pollo = _catalogo.Agregar("Pollo");
        _arrozConPollo = _catalogo.AgregarPlato("Arroz con pollo", pollo);
        _manzana = _catalogo.Agregar("Manzana", NivelCalorico.Bajo.Id);
        _pan = _catalogo.Agregar("Pan integral");

        Guardar(FichaDelPlato(_arrozConPollo.Id, pollo.Id));
        Guardar(FichaDeLaManzana(_manzana.Id));

        _valoracion = Valorar(MenuCon(_pan, _manzana, _arrozConPollo));
    }

    [Fact]
    public async Task Describe_la_ficha_de_cada_item_del_menu_y_nombra_los_que_no_tienen()
    {
        var resultado = await Proveedor().ConstruirAsync(Caso(_valoracion), CancellationToken.None);

        var bloque = resultado!.Bloque!;
        Assert.Equal("ficha", bloque.Fuente);
        Assert.Equal(1, bloque.Version);
        Assert.Equal(
            "Ficha nutricional de los ítems del menú valorado. "
            + "Arroz con pollo (plato), por 1 ración (350 g): energía 486 kcal, carbohidratos 58,2 g, proteínas 31,5 g, "
            + "grasas 12,4 g, grasas saturadas 3,1 g, fibra 6,8 g, azúcares 4 g; vitaminas: A 310 µg, C 22 mg, B12 0,6 µg; "
            + "minerales: hierro 3,9 mg, calcio 64 mg, sodio 620 mg, potasio 780 mg; alérgenos: apio; preparación: guiso, 55 min. "
            + "Manzana (alimento), por 100 g: energía 52 kcal, carbohidratos 13,8 g, proteínas 0,3 g, grasas 0,2 g, fibra 2,4 g; "
            + "sin alérgenos. "
            + "Sin ficha registrada: Pan integral.",
            bloque.Resumen);
        Assert.Equal(31.5, bloque.Cifras["ficha.1.macronutrientes.proteinasG"]);
        Assert.Equal(0.6, bloque.Cifras["ficha.1.micronutrientes.vitaminas.b12Ug"]);
        Assert.Equal(1, bloque.Cifras["ficha.1.porcion.cantidad"]);
        Assert.Equal(350, bloque.Cifras["ficha.1.porcion.unidad.texto.1"]);
        Assert.Equal(55, bloque.Cifras["ficha.1.preparacion.tiempoTotalMin"]);
        Assert.Equal(52, bloque.Cifras["ficha.2.macronutrientes.energiaKcal"]);
        Assert.Equal(ProveedorDeFicha.AvisoItemsSinFicha, Assert.Single(resultado.Advertencias));
    }

    [Fact]
    public async Task El_bloque_no_lleva_identificadores_datos_del_paciente_ni_pasos_ni_desglose()
    {
        var bloque = (await Proveedor().ConstruirAsync(Caso(_valoracion), CancellationToken.None))!.Bloque!;
        var json = JsonSerializer.Serialize(bloque);

        Assert.DoesNotMatch(UnGuid(), json);
        Assert.DoesNotContain("Juan", json);
        Assert.DoesNotContain("Pérez", json);
        Assert.DoesNotContain(EdadDelPaciente.ToString(CultureInfo.InvariantCulture), json);
        Assert.DoesNotContain("años", json);
        Assert.DoesNotContain("sala", json, StringComparison.OrdinalIgnoreCase);

        // Ni los pasos de la preparación (texto libre) ni el desglose por ingrediente del plato.
        Assert.DoesNotContain("Cebolla", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Reposar", json);
        Assert.DoesNotContain("ingrediente", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("248", json);
    }

    [Fact]
    public async Task Cada_numero_del_resumen_esta_en_las_cifras()
    {
        Guardar(new FichaNutricionalDto(
            _pan.Id, "Alimento", "simple", new PorcionDeReferenciaDto(2, "rebanadas de 30 g"),
            Bloque("""{ "energiaKcal": 160.04, "proteinasG": 5.55 }"""), null, null, null, null, DateTime.UtcNow));
        var leche = _catalogo.Agregar("Leche 2 %");
        Guardar(new FichaNutricionalDto(
            leche.Id, "Alimento", "simple", null, Bloque("""{ "calcioMg": 120 }"""), null, ["lactosa"], null, null, DateTime.UtcNow));
        var sinFicha = _catalogo.Agregar("Yogur 0 %");
        var valoracion = Valorar(MenuCon(_arrozConPollo, _manzana, _pan, leche, sinFicha));

        var bloque = (await Proveedor().ConstruirAsync(Caso(valoracion), CancellationToken.None))!.Bloque!;
        var numeros = UnNumero().Matches(bloque.Resumen)
            .Select(m => double.Parse(m.Value.Replace(',', '.'), CultureInfo.InvariantCulture))
            .ToList();

        Assert.NotEmpty(numeros);
        Assert.All(numeros, n => Assert.Contains(n, bloque.Cifras.Values));
    }

    [Fact]
    public async Task Redondea_a_un_decimal_con_coma_y_guarda_el_valor_redondeado()
    {
        Guardar(new FichaNutricionalDto(
            _pan.Id, "Alimento", "simple", null,
            Bloque("""{ "energiaKcal": 100.0, "proteinasG": 12.345, "grasasG": 2.25 }"""),
            null, null, null, null, DateTime.UtcNow));
        var valoracion = Valorar(MenuCon(_pan));

        var bloque = (await Proveedor().ConstruirAsync(Caso(valoracion), CancellationToken.None))!.Bloque!;

        Assert.Contains("energía 100 kcal, proteínas 12,3 g, grasas 2,3 g", bloque.Resumen);
        Assert.Equal(12.3, bloque.Cifras["ficha.1.macronutrientes.proteinasG"]);
        Assert.Equal(2.3, bloque.Cifras["ficha.1.macronutrientes.grasasG"]);
    }

    [Fact]
    public async Task Una_ficha_sin_bloques_dice_que_los_alergenos_no_estan_registrados()
    {
        Guardar(new FichaNutricionalDto(_pan.Id, "Alimento", "simple", null, null, null, null, null, null, DateTime.UtcNow));
        var valoracion = Valorar(MenuCon(_pan));

        var resultado = await Proveedor().ConstruirAsync(Caso(valoracion), CancellationToken.None);

        Assert.Equal(
            "Ficha nutricional de los ítems del menú valorado. Pan integral (alimento): alérgenos no registrados.",
            resultado!.Bloque!.Resumen);
        Assert.Empty(resultado.Bloque.Cifras);
        Assert.Empty(resultado.Advertencias);
    }

    [Fact]
    public async Task Omite_lo_que_no_es_numero_y_acorta_el_texto_libre()
    {
        Guardar(new FichaNutricionalDto(
            _pan.Id, "Alimento", "simple", null,
            Bloque("""{ "energiaKcal": 250, "origen": "panadería", "integral": true, "indiceGlucemico": 55 }"""),
            Bloque("""{ "selenioUg": 20, "minerales": { "zincMg": 1.2 } }"""),
            ["  gluten\n", "", "sésamo"],
            Bloque("""{ "tecnica": "horneado   a temperatura media, con vapor al principio y sin abrir el horno", "pasos": [ { "descripcion": "Amasar" } ] }"""),
            null, DateTime.UtcNow));
        var valoracion = Valorar(MenuCon(_pan));

        var resumen = (await Proveedor().ConstruirAsync(Caso(valoracion), CancellationToken.None))!.Bloque!.Resumen;

        Assert.Equal(
            "Ficha nutricional de los ítems del menú valorado. Pan integral (alimento): energía 250 kcal, "
            + "índice glucémico 55; micronutrientes: selenio 20 µg; minerales: zinc 1,2 mg; alérgenos: gluten, sésamo; "
            + "preparación: horneado a temperatura media, con vapor…",
            resumen.TrimEnd('.'));
        Assert.DoesNotContain("panadería", resumen);
        Assert.DoesNotContain("Amasar", resumen);
    }

    [Fact]
    public async Task Sin_ninguna_ficha_en_el_menu_devuelve_null()
    {
        var valoracion = Valorar(MenuCon(_pan));

        Assert.Null(await Proveedor().ConstruirAsync(Caso(valoracion), CancellationToken.None));
    }

    [Fact]
    public async Task Sin_valoracion_en_el_caso_o_inexistente_devuelve_null()
    {
        var sinValoracion = new ContextoDeCaso([Paciente()], DateOnly.FromDateTime(FechaValoracion));
        var inexistente = new ContextoDeCaso([Paciente()], DateOnly.FromDateTime(FechaValoracion), valoracionId: Guid.NewGuid());

        Assert.Null(await Proveedor().ConstruirAsync(sinValoracion, CancellationToken.None));
        Assert.Null(await Proveedor().ConstruirAsync(inexistente, CancellationToken.None));
    }

    [Fact]
    public async Task Sin_MongoDB_devuelve_un_resultado_sin_bloque_y_con_la_advertencia()
    {
        _fichas.Disponible = false;

        var resultado = await Proveedor().ConstruirAsync(Caso(_valoracion), CancellationToken.None);

        Assert.NotNull(resultado);
        Assert.Null(resultado.Bloque);
        Assert.Equal(ProveedorDeFicha.AvisoSinAlmacen, Assert.Single(resultado.Advertencias));
    }

    [Fact]
    public void AddIA_registra_el_proveedor()
    {
        var servicios = new ServiceCollection();
        servicios.AddIA();

        Assert.Contains(
            servicios,
            s => s.ServiceType == typeof(IProveedorDeContexto) && s.ImplementationType == typeof(ProveedorDeFicha));
    }

    [Theory]
    [InlineData("energiaKcal", "energía", "kcal")]
    [InlineData("grasasSaturadasG", "grasas saturadas", "g")]
    [InlineData("sodioMg", "sodio", "mg")]
    [InlineData("aUg", "A", "µg")]
    [InlineData("b12Ug", "B12", "µg")]
    [InlineData("acidoFolicoMcg", "ácido fólico", "µg")]
    [InlineData("tiempoTotalMin", "tiempo total", "min")]
    [InlineData("indiceGlucemico", "índice glucémico", null)]
    [InlineData("vitaminas", "vitaminas", null)]
    public void NombreDeNutriente_separa_las_palabras_y_lee_la_unidad_del_sufijo(string campo, string nombre, string? unidad)
        => Assert.Equal((nombre, unidad), NombreDeNutriente.Leer(campo));

    // ---- ayudas ----

    private ProveedorDeFicha Proveedor() => new(_valoraciones, _banco.MenusRepo, _catalogo.ItemsRepo, _fichas);

    private PacienteSnapshot Paciente() => new()
    {
        Id = _pacienteId,
        Nombre = NombreDelPaciente,
        Edad = EdadDelPaciente,
        SalaId = Guid.NewGuid(),
        Dietas = new Dictionary<string, string> { ["Hiposódica"] = "terapeutica" },
    };

    private ContextoDeCaso Caso(ValoracionNutricional valoracion)
        => new([Paciente()], DateOnly.FromDateTime(FechaValoracion), valoracionId: valoracion.Id, pacienteId: _pacienteId);

    private Menu MenuCon(params ItemAlimentario[] items)
    {
        var menu = new Menu(
            Guid.NewGuid(), _dietaId, Guid.NewGuid(), FechaValoracion.AddDays(-10), Math.Max(items.Length, 1), false,
            [(TipoPreparacion.PlatoFuerte.Id, 1m)], [BancoDeDietasFalso.GrupoCereales]);
        foreach (var item in items)
        {
            menu.AgregarItem(item.Id);
        }

        _banco.Menus.Add(menu);
        return menu;
    }

    private ValoracionNutricional Valorar(Menu menu)
    {
        var valoracion = new ValoracionNutricional(
            Guid.NewGuid(), menu.Id, _pacienteId, Guid.NewGuid(), ResultadoNutricional.Buena.Id, FechaValoracion);
        _valoraciones.Valoraciones.Add(valoracion);
        return valoracion;
    }

    private FichaNutricionalDto Guardar(FichaNutricionalDto ficha)
    {
        _fichas.GuardarAsync(ficha, CancellationToken.None).GetAwaiter().GetResult();
        return ficha;
    }

    private static JsonObject Bloque(string json) => JsonNode.Parse(json)!.AsObject();

    /// <summary>La ficha del plato del documento 1 (§4), con sus pasos y su desglose por ingrediente.</summary>
    private static FichaNutricionalDto FichaDelPlato(Guid platoId, Guid polloId) => new(
        platoId,
        "Plato",
        "compuesto",
        new PorcionDeReferenciaDto(1, "ración (350 g)"),
        Bloque("""
            { "energiaKcal": 486, "carbohidratosG": 58.2, "proteinasG": 31.5, "grasasG": 12.4,
              "grasasSaturadasG": 3.1, "fibraG": 6.8, "azucaresG": 4.0 }
            """),
        Bloque("""
            { "vitaminas": { "aUg": 310, "cMg": 22, "b12Ug": 0.6 },
              "minerales": { "hierroMg": 3.9, "calcioMg": 64, "sodioMg": 620, "potasioMg": 780 } }
            """),
        ["apio"],
        Bloque("""
            { "tecnica": "guiso", "tiempoTotalMin": 55,
              "pasos": [ { "orden": 1, "descripcion": "Cocinar la cebolla y el apio a fuego medio.", "tiempoMin": 8 },
                         { "orden": 2, "descripcion": "Reposar antes de servir.", "tiempoMin": 2 } ] }
            """),
        [new IngredienteDeFichaDto(polloId, "Pollo", Bloque("""{ "energiaKcal": 248, "proteinasG": 27.0 }"""))],
        DateTime.UtcNow);

    /// <summary>La ficha del alimento simple del documento 1 (§3).</summary>
    private static FichaNutricionalDto FichaDeLaManzana(Guid manzanaId) => new(
        manzanaId,
        "Alimento",
        "simple",
        new PorcionDeReferenciaDto(100, "g"),
        Bloque("""{ "energiaKcal": 52, "carbohidratosG": 13.8, "proteinasG": 0.3, "grasasG": 0.2, "fibraG": 2.4 }"""),
        null,
        [],
        null,
        null,
        DateTime.UtcNow);

    [GeneratedRegex(@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")]
    private static partial Regex UnGuid();

    // Como el verificador (#327): enteros, o con coma decimal.
    [GeneratedRegex(@"\d+(?:,\d+)?")]
    private static partial Regex UnNumero();
}
