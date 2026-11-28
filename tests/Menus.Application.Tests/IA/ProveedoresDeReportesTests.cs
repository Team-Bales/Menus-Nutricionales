using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Menus.Application.IA;
using Menus.Application.IA.Proveedores;
using Menus.Application.Reportes;
using Menus.Application.Tests.Catalogo;
using Menus.Application.Tests.Catalogo.Dietas;
using Menus.Application.Tests.Valoraciones;
using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Application.Tests.IA;

/// <summary>
/// Pruebas de los proveedores de contexto <c>reporte-1</c> a <c>reporte-6</c> (#335) con datos conocidos de las
/// consultas informacionales, su prueba de privacidad (contrato de IA, §5.4) y que cada número del resumen esté en las
/// cifras (§6).
/// </summary>
/// <remarks>
/// Los datos llevan centinelas que no deben llegar a ningún bloque: el nombre, la edad y la sala del paciente, el
/// nombre de la dieta del caso, los nombres del personal y los de otros pacientes.
/// </remarks>
public sealed partial class ProveedoresDeReportesTests
{
    private const string NombreDelPaciente = "Juan Pérez Gómez";
    private const int EdadDelPaciente = 83;
    private const string Sala = "Sala Norte";
    private const string Dieta = "Hiposódica Rarísima";
    private const string OtraDieta = "Blanda";
    private const string Creador = "Ana Creadora";
    private const string Descriptor = "Luis Descriptor";
    private const string OtroPaciente = "María Otra";

    private static readonly DateOnly Hoy = new(2026, 11, 22);
    private static readonly DateTime FechaDeAprobacion = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

    private readonly BancoDeDietasFalso _banco = new();
    private readonly BancoFalso _catalogo = new();
    private readonly ValoracionesFalsas _valoraciones;
    private readonly ConsultasFalsas _consultas = new();

    private readonly Guid _pacienteId = Guid.NewGuid();
    private readonly Guid _revisorId = Guid.NewGuid();
    private readonly Dieta _dieta;
    private readonly Alimento _arroz;
    private readonly Alimento _manzana;
    private readonly Menu _menu;
    private readonly ValoracionNutricional _valoracion;

    public ProveedoresDeReportesTests()
    {
        _valoraciones = new ValoracionesFalsas(_banco);
        _dieta = _banco.Agregar(Dieta);
        _arroz = _catalogo.Agregar("Arroz blanco");
        _manzana = _catalogo.Agregar("Manzana", NivelCalorico.Bajo.Id);
        _menu = MenuAprobado(_arroz, _manzana);
        _valoracion = new ValoracionNutricional(
            Guid.NewGuid(), _menu.Id, _pacienteId, Guid.NewGuid(), ResultadoNutricional.Buena.Id, FechaDeAprobacion.AddDays(10));
        _valoraciones.Valoraciones.Add(_valoracion);
        SembrarConsultas();
    }

    // ---- reporte-1 ----

    [Fact]
    public async Task Reporte1_cuenta_los_menus_automaticos_y_describe_los_cinco_mas_recientes()
    {
        var bloque = await BloqueAsync(Proveedores().MenusAutomaticos);

        Assert.Equal("reporte-1", bloque.Fuente);
        Assert.StartsWith(
            "Menús generados automáticamente en la dieta del menú: 6 menús. El menú valorado es uno de ellos. Los 5 más recientes: "
            + "creado hace 2 días, 4 alimentos, distribución Plato fuerte 50 %, Postre 50 %, grupos cubiertos: Cereales, Frutas; "
            + "creado hace 12 días (el menú valorado), 2 alimentos, distribución Plato fuerte 100 %, grupos cubiertos: Cereales;",
            bloque.Resumen);
        Assert.Equal(6, bloque.Cifras["reporte-1.total"]);
        Assert.Equal(50, bloque.Cifras["reporte-1.1.distribucion.2.porcentaje"]);
        Assert.Equal(12, bloque.Cifras["reporte-1.2.dias"]);
        Assert.Equal(5, Regex.Count(bloque.Resumen, "creado "));
        Assert.Equal(_dieta.Id, _consultas.DietaPedida);
    }

    [Fact]
    public async Task Reporte1_omite_una_distribucion_que_no_tiene_la_forma_esperada()
    {
        _consultas.MenusAutomaticos = [Automatico(_menu.Id, 12, distribucion: "sin datos")];

        var bloque = await BloqueAsync(Proveedores().MenusAutomaticos);

        Assert.DoesNotContain("distribución", bloque.Resumen);
        Assert.Contains("Por fecha: creado hace 12 días (el menú valorado), 2 alimentos, grupos cubiertos: Cereales.", bloque.Resumen);
    }

    // ---- reporte-2 ----

    [Fact]
    public async Task Reporte2_lista_los_alimentos_mas_usados_y_marca_los_del_menu()
    {
        var bloque = await BloqueAsync(Proveedores().AlimentosMasUsados);

        Assert.Equal(
            "Alimentos simples más usados en los menús finales de la dieta del menú: "
            + "Arroz blanco (nivel Medio, grupo Cereales), en 12 menús finales, está en el menú valorado; "
            + "Pan (nivel Medio, grupo Cereales), en 1 menú final.",
            bloque.Resumen);
        Assert.Equal(12, bloque.Cifras["reporte-2.1.menusFinales"]);
        Assert.Equal(5, _consultas.LimitePedido);
    }

    // ---- reporte-3 ----

    [Fact]
    public async Task Reporte3_describe_el_dictamen_del_menu_y_los_conteos_del_revisor()
    {
        var bloque = await BloqueAsync(Proveedores().Dictamenes);

        Assert.StartsWith("Dictamen del menú valorado: aprobado hace 12 días, con las observaciones «Buen equilibrio; revisar ", bloque.Resumen);
        Assert.Contains("…».", bloque.Resumen);
        Assert.EndsWith("El revisor que lo dictaminó registró 3 dictámenes y aprobó el 67 % (2 de 3).", bloque.Resumen);
        Assert.Equal(12, bloque.Cifras["reporte-3.dictamen.dias"]);
        Assert.Equal(67, bloque.Cifras["reporte-3.revisor.aprobados.porcentaje"]);
        Assert.Equal(_revisorId, _consultas.RevisorPedido);

        var observaciones = Regex.Match(bloque.Resumen, "«(.*)»").Groups[1].Value;
        Assert.Equal(ProveedorDeDictamenes.LargoDeObservaciones + 1, observaciones.Length);
        Assert.DoesNotContain("\n", observaciones);
    }

    [Fact]
    public async Task Reporte3_de_un_menu_sin_dictamen_devuelve_null()
    {
        var pendiente = new Menu(
            Guid.NewGuid(), _dieta.Id, Guid.NewGuid(), FechaDeAprobacion, 1, true,
            [(TipoPreparacion.PlatoFuerte.Id, 1m)], [BancoDeDietasFalso.GrupoCereales]);
        _banco.Menus.Add(pendiente);
        var valoracion = new ValoracionNutricional(
            Guid.NewGuid(), pendiente.Id, _pacienteId, Guid.NewGuid(), ResultadoNutricional.Buena.Id, FechaDeAprobacion);
        _valoraciones.Valoraciones.Add(valoracion);

        Assert.Null(await Proveedores().Dictamenes.ConstruirAsync(Caso(valoracion.Id), CancellationToken.None));
    }

    // ---- reporte-4 ----

    [Fact]
    public async Task Reporte4_da_la_aceptacion_global_por_nivel_los_menos_aceptados_y_los_sin_consumos()
    {
        var bloque = await BloqueAsync(Proveedores().DesempenoDelMenu);

        Assert.Equal(
            "Desempeño de todos los pacientes que recibieron el menú valorado: aceptaron el 60 % (9 de 15) de los ítems servidos. "
            + "Por nivel calórico: Bajo 100 % (5 de 5); Medio 40 % (4 de 10). "
            + "Ítems con menor aceptación: Arroz blanco (nivel Medio), 40 % (4 de 10). "
            + "Sin consumos registrados: Pollo asado.",
            bloque.Resumen);
        Assert.Equal(60, bloque.Cifras["reporte-4.global.porcentaje"]);
        Assert.Equal(_menu.Id, _consultas.MenuPedido);
    }

    [Fact]
    public async Task Reporte4_sin_consumos_devuelve_null()
    {
        _consultas.Desempeno = [new DesempenoItemDto(Guid.NewGuid(), "Pollo asado", "Plato", "Alto", 0, 0, 0, null)];

        Assert.Null(await Proveedores().DesempenoDelMenu.ConstruirAsync(Caso(), CancellationToken.None));
    }

    // ---- reporte-5 ----

    [Fact]
    public async Task Reporte5_resume_la_composicion_de_la_dieta_y_los_criterios_del_menu()
    {
        var bloque = await BloqueAsync(Proveedores().ComparacionDeDietas);

        Assert.Equal(
            "Composición de los menús finales de la dieta del menú. "
            + "Por grupo nutricional: Cereales 33 % (12 ítems); Frutas 22 % (8 ítems); Lácteos 17 % (6 ítems); "
            + "Carnes 14 % (5 ítems); Verduras 11 % (4 ítems); y 1 grupo más. "
            + "Por nivel calórico: Bajo 40 % (14 ítems); Medio 34 % (12 ítems); Alto 26 % (9 ítems). "
            + "Criterios de equilibrio del menú valorado: 2 ítems de 2 declarados (cumple); "
            + "proporciones por tipo de preparación: no cumplen (fuera de proporción: Postre); "
            + "grupos requeridos: no cubiertos (faltan: Lácteos); grupos prohibidos: ausentes.",
            bloque.Resumen);
        Assert.Equal([_dieta.Id], _consultas.DietasComparadas);
        Assert.True(_consultas.SoloFinales);
    }

    // ---- reporte-6 ----

    [Fact]
    public async Task Reporte6_da_la_correlacion_los_rechazados_de_la_dieta_y_las_revalorizaciones_del_paciente()
    {
        var bloque = await BloqueAsync(Proveedores().Correlacion);

        Assert.Equal(
            "Correlación entre el nivel calórico de los ítems servidos y el resultado nutricional, en la dieta del menú: "
            + "coeficiente de Spearman -0,45, sobre 12 menús valorados. "
            + "De los 10 ítems con mayor tasa de rechazo en todo el sistema, tuvieron rechazos en la dieta del menú: "
            + "Arroz blanco (alimento), rechazado el 60 % (6 de 10) de sus consumos. "
            + "Revalorizaciones del paciente en la dieta del menú: valor 2 frente a un promedio de 3,45 de los demás pacientes "
            + "de su sala en esa dieta (diferencia -1,45).",
            bloque.Resumen);
        Assert.Equal(-0.45, bloque.Cifras["reporte-6.correlacion.spearman"]);
        Assert.Equal(-1.45, bloque.Cifras["reporte-6.revalorizacion.1.diferencia"]);
    }

    [Fact]
    public async Task Reporte6_sin_coeficiente_dice_que_no_se_puede_calcular_y_sin_nada_devuelve_null()
    {
        _consultas.Correlaciones = [new CorrelacionPorDietaDto(_dieta.Id, Dieta, 1, null)];
        _consultas.Rechazados = [];
        _consultas.Revalorizados = [];

        var bloque = await BloqueAsync(Proveedores().Correlacion);
        Assert.Contains("en la dieta del menú: no se puede calcular con 1 menú valorado.", bloque.Resumen);

        _consultas.Correlaciones = [];
        Assert.Null(await Proveedores().Correlacion.ConstruirAsync(Caso(), CancellationToken.None));
    }

    // ---- comunes a los seis ----

    [Fact]
    public async Task Ningun_bloque_lleva_datos_de_personas_ni_el_nombre_de_la_dieta()
    {
        foreach (var proveedor in Proveedores().Todos)
        {
            var json = JsonSerializer.Serialize(await BloqueAsync(proveedor), new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

            Assert.DoesNotMatch(UnGuid(), json);
            foreach (var centinela in new[] { "Juan", "Pérez", Sala, Dieta, OtraDieta, Creador, Descriptor, OtroPaciente, "años" })
            {
                Assert.DoesNotContain(centinela, json);
            }

            Assert.DoesNotContain(EdadDelPaciente.ToString(CultureInfo.InvariantCulture), json);
        }
    }

    [Fact]
    public async Task Cada_numero_del_resumen_esta_en_las_cifras()
    {
        foreach (var proveedor in Proveedores().Todos)
        {
            var bloque = await BloqueAsync(proveedor);
            var numeros = UnNumero().Matches(bloque.Resumen)
                .Select(m => double.Parse(m.Value.Replace(',', '.'), CultureInfo.InvariantCulture))
                .ToList();

            Assert.NotEmpty(numeros);
            Assert.All(numeros, n => Assert.Contains(n, bloque.Cifras.Values));
        }
    }

    [Fact]
    public async Task Sin_valoracion_en_el_caso_o_inexistente_ninguno_devuelve_bloque()
    {
        var sinValoracion = new ContextoDeCaso([Paciente()], Hoy);
        var inexistente = new ContextoDeCaso([Paciente()], Hoy, valoracionId: Guid.NewGuid());

        foreach (var proveedor in Proveedores().Todos)
        {
            Assert.Null(await proveedor.ConstruirAsync(sinValoracion, CancellationToken.None));
            Assert.Null(await proveedor.ConstruirAsync(inexistente, CancellationToken.None));
        }
    }

    [Fact]
    public async Task Sin_filas_en_la_consulta_ninguno_devuelve_bloque()
    {
        _consultas.Vaciar();
        var sinDictamenes = Proveedores().Todos.Where(p => p.Fuente != "reporte-3");

        foreach (var proveedor in sinDictamenes)
        {
            Assert.Null(await proveedor.ConstruirAsync(Caso(), CancellationToken.None));
        }

        // El dictamen del menú está en el menú; sin filas del revisor, el bloque queda sin sus conteos.
        var dictamen = await BloqueAsync(Proveedores().Dictamenes);
        Assert.DoesNotContain("revisor", dictamen.Resumen);
    }

    [Fact]
    public void AddIA_registra_los_seis_proveedores()
    {
        var servicios = new ServiceCollection();
        servicios.AddIA();

        var tipos = servicios.Where(s => s.ServiceType == typeof(IProveedorDeContexto)).Select(s => s.ImplementationType).ToList();

        foreach (var proveedor in Proveedores().Todos)
        {
            Assert.Contains(proveedor.GetType(), tipos);
        }

        Assert.Equal(
            ["reporte-1", "reporte-2", "reporte-3", "reporte-4", "reporte-5", "reporte-6"],
            Proveedores().Todos.Select(p => p.Fuente));
    }

    [Theory]
    [InlineData(-0.4537, 2, "-0,45")]
    [InlineData(3.4, 2, "3,4")]
    [InlineData(12.0, 2, "12")]
    [InlineData(-0.001, 2, "0")]
    [InlineData(2.0, 0, "2")]
    public void Numero_redondea_con_coma_y_sin_ceros_de_mas(double valor, int decimales, string esperado)
    {
        var cifras = new Dictionary<string, double>();

        Assert.Equal(esperado, TextoDeBloque.Numero(valor, decimales, "x", cifras));
        Assert.Equal(double.Parse(esperado.Replace(',', '.'), CultureInfo.InvariantCulture), cifras["x"]);
    }

    [Theory]
    [InlineData(0, "hoy")]
    [InlineData(-1, "hace 1 día")]
    [InlineData(-12, "hace 12 días")]
    [InlineData(3, "dentro de 3 días")]
    public void FechaRelativa_cuenta_los_dias_desde_la_fecha_del_caso(int dias, string esperado)
        => Assert.Equal(esperado, TextoDeBloque.FechaRelativa(Hoy.AddDays(dias), Hoy, "x", new Dictionary<string, double>()));

    // ---- ayudas ----

    private sealed record LosSeis(
        ProveedorDeMenusAutomaticos MenusAutomaticos,
        ProveedorDeAlimentosMasUsados AlimentosMasUsados,
        ProveedorDeDictamenes Dictamenes,
        ProveedorDeDesempenoDelMenu DesempenoDelMenu,
        ProveedorDeComparacionDeDietas ComparacionDeDietas,
        ProveedorDeCorrelacion Correlacion)
    {
        public IReadOnlyList<IProveedorDeContexto> Todos =>
            [MenusAutomaticos, AlimentosMasUsados, Dictamenes, DesempenoDelMenu, ComparacionDeDietas, Correlacion];
    }

    private LosSeis Proveedores()
    {
        var menus = _banco.MenusRepo;
        return new LosSeis(
            new ProveedorDeMenusAutomaticos(_valoraciones, menus, _consultas),
            new ProveedorDeAlimentosMasUsados(_valoraciones, menus, _consultas),
            new ProveedorDeDictamenes(_valoraciones, menus, _consultas),
            new ProveedorDeDesempenoDelMenu(_valoraciones, menus, _consultas),
            new ProveedorDeComparacionDeDietas(_valoraciones, menus, _consultas),
            new ProveedorDeCorrelacion(_valoraciones, menus, _banco.DietasRepo, _consultas));
    }

    private async Task<BloqueDeContexto> BloqueAsync(IProveedorDeContexto proveedor)
    {
        var resultado = await proveedor.ConstruirAsync(Caso(), CancellationToken.None);
        Assert.NotNull(resultado);
        Assert.Empty(resultado.Advertencias);
        return Assert.IsType<BloqueDeContexto>(resultado.Bloque);
    }

    private PacienteSnapshot Paciente() => new()
    {
        Id = _pacienteId,
        Nombre = NombreDelPaciente,
        Edad = EdadDelPaciente,
        SalaId = Guid.NewGuid(),
        Dietas = new Dictionary<string, string> { [Dieta] = "terapeutica" },
    };

    private ContextoDeCaso Caso(Guid? valoracionId = null)
        => new([Paciente()], Hoy, valoracionId: valoracionId ?? _valoracion.Id, pacienteId: _pacienteId);

    private Menu MenuAprobado(params Alimento[] items)
    {
        var menu = new Menu(
            Guid.NewGuid(), _dieta.Id, Guid.NewGuid(), FechaDeAprobacion.AddDays(-1), items.Length, true,
            [(TipoPreparacion.PlatoFuerte.Id, 1m)], [BancoDeDietasFalso.GrupoCereales]);
        foreach (var item in items)
        {
            menu.AgregarItem(item.Id);
        }

        menu.Aprobar(
            _revisorId,
            FechaDeAprobacion,
            "Buen equilibrio; revisar\nla porción de arroz en la cena " + new string('x', ProveedorDeDictamenes.LargoDeObservaciones));
        _banco.Menus.Add(menu);
        return menu;
    }

    private MenuAutomaticoDto Automatico(Guid menuId, int haceDias, int alimentos = 2, string distribucion = "Plato fuerte: 1.00")
        => new(menuId, Creador, new DateTimeOffset(Hoy.AddDays(-haceDias).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero), alimentos, distribucion, "Cereales");

    private void SembrarConsultas()
    {
        _consultas.MenusAutomaticos =
        [
            Automatico(_menu.Id, 12),
            Automatico(Guid.NewGuid(), 2, 4, "Plato fuerte: 0.50, Postre: 0.50") with { GruposCubiertos = "Cereales, Frutas" },
            Automatico(Guid.NewGuid(), 20),
            Automatico(Guid.NewGuid(), 30),
            Automatico(Guid.NewGuid(), 40),
            Automatico(Guid.NewGuid(), 50),
        ];
        _consultas.AlimentosMasUsados =
        [
            new AlimentoMasUsadoDto(_arroz.Id, "Arroz blanco", "Medio", "Cereales", 12),
            new AlimentoMasUsadoDto(Guid.NewGuid(), "Pan", "Medio", "Cereales", 1),
        ];
        var fecha = new DateTimeOffset(FechaDeAprobacion);
        _consultas.Dictamenes =
        [
            new DictamenDto(_menu.Id, Dieta, Creador, fecha, fecha, true, "Buen equilibrio", null),
            new DictamenDto(Guid.NewGuid(), OtraDieta, Creador, fecha, fecha, true, null, null),
            new DictamenDto(Guid.NewGuid(), OtraDieta, Creador, fecha, fecha, false, "Falta fruta", Guid.NewGuid()),
        ];
        _consultas.Desempeno =
        [
            new DesempenoItemDto(_manzana.Id, "Manzana", "Alimento", "Bajo", 5, 5, 0, 1m),
            new DesempenoItemDto(_arroz.Id, "Arroz blanco", "Alimento", "Medio", 10, 4, 6, 0.4m),
            new DesempenoItemDto(Guid.NewGuid(), "Pollo asado", "Plato", "Alto", 0, 0, 0, null),
        ];
        _consultas.PorGrupo =
        [
            Grupo("Frutas", 8, 22.22m), Grupo("Cereales", 12, 33.33m), Grupo("Lácteos", 6, 16.67m),
            Grupo("Carnes", 5, 13.89m), Grupo("Verduras", 4, 11.11m), Grupo("Grasas", 1, 2.78m),
        ];
        _consultas.PorNivel =
        [
            new ComparacionPorNivelDto(_dieta.Id, Dieta, "Alto", 9, 25.71m),
            new ComparacionPorNivelDto(_dieta.Id, Dieta, "Bajo", 14, 40m),
            new ComparacionPorNivelDto(_dieta.Id, Dieta, "Medio", 12, 34.29m),
        ];
        _consultas.Criterios =
        [
            new CriteriosEquilibrioDto(_dieta.Id, Dieta, Guid.NewGuid(), "Final", 3, 3, true, true, null, true, null, true, null),
            new CriteriosEquilibrioDto(_dieta.Id, Dieta, _menu.Id, "Final", 2, 2, true, false, "Postre", false, "Lácteos", true, null),
        ];
        _consultas.Correlaciones =
        [
            new CorrelacionPorDietaDto(Guid.NewGuid(), OtraDieta, 30, 0.8m),
            new CorrelacionPorDietaDto(_dieta.Id, Dieta, 12, -0.4537m),
        ];
        _consultas.Rechazados =
        [
            new ItemMasRechazadoDto(_arroz.Id, "Arroz blanco", "Alimento", Descriptor, 10, 6, 0.6m, $"{OtraDieta}, {Dieta}"),
            new ItemMasRechazadoDto(Guid.NewGuid(), "Sopa", "Plato", Descriptor, 4, 3, 0.75m, OtraDieta),
        ];
        _consultas.Revalorizados =
        [
            new RevalorizadoDto(Guid.NewGuid(), _pacienteId, NombreDelPaciente, Sala, Dieta, 2, 3.45m, -1.45m),
            new RevalorizadoDto(Guid.NewGuid(), _pacienteId, NombreDelPaciente, Sala, OtraDieta, 4, 3m, 1m),
            new RevalorizadoDto(Guid.NewGuid(), Guid.NewGuid(), OtroPaciente, Sala, Dieta, 1, 2m, -1m),
        ];
    }

    private ComparacionPorGrupoDto Grupo(string nombre, long items, decimal porcentaje)
        => new(_dieta.Id, Dieta, nombre, items, porcentaje);

    [GeneratedRegex(@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")]
    private static partial Regex UnGuid();

    // Como el verificador (#327): enteros o con coma decimal, con su signo si va separado de la palabra anterior.
    [GeneratedRegex(@"(?<![\w-])-?\d+(?:,\d+)?")]
    private static partial Regex UnNumero();

    /// <summary>Consultas informacionales en memoria, que guardan los parámetros con los que se las llamó.</summary>
    private sealed class ConsultasFalsas : IConsultasInformacionales
    {
        public IReadOnlyList<MenuAutomaticoDto> MenusAutomaticos { get; set; } = [];

        public IReadOnlyList<AlimentoMasUsadoDto> AlimentosMasUsados { get; set; } = [];

        public IReadOnlyList<DictamenDto> Dictamenes { get; set; } = [];

        public IReadOnlyList<DesempenoItemDto> Desempeno { get; set; } = [];

        public IReadOnlyList<ComparacionPorGrupoDto> PorGrupo { get; set; } = [];

        public IReadOnlyList<ComparacionPorNivelDto> PorNivel { get; set; } = [];

        public IReadOnlyList<CriteriosEquilibrioDto> Criterios { get; set; } = [];

        public IReadOnlyList<CorrelacionPorDietaDto> Correlaciones { get; set; } = [];

        public IReadOnlyList<ItemMasRechazadoDto> Rechazados { get; set; } = [];

        public IReadOnlyList<RevalorizadoDto> Revalorizados { get; set; } = [];

        public Guid? DietaPedida { get; private set; }

        public int? LimitePedido { get; private set; }

        public Guid? RevisorPedido { get; private set; }

        public Guid? MenuPedido { get; private set; }

        public IReadOnlyList<Guid>? DietasComparadas { get; private set; }

        public bool? SoloFinales { get; private set; }

        public void Vaciar()
        {
            MenusAutomaticos = [];
            AlimentosMasUsados = [];
            Dictamenes = [];
            Desempeno = [];
            PorGrupo = [];
            PorNivel = [];
            Criterios = [];
            Correlaciones = [];
            Rechazados = [];
            Revalorizados = [];
        }

        public Task<IReadOnlyList<MenuAutomaticoDto>> ConsultarMenusAutomaticosAsync(Guid dietaId, CancellationToken ct = default)
        {
            DietaPedida = dietaId;
            return Task.FromResult(MenusAutomaticos);
        }

        public Task<IReadOnlyList<AlimentoMasUsadoDto>> ConsultarAlimentosMasUsadosAsync(Guid dietaId, int? limite, CancellationToken ct = default)
        {
            LimitePedido = limite;
            return Task.FromResult(AlimentosMasUsados);
        }

        public Task<IReadOnlyList<DictamenDto>> ConsultarDictamenesAsync(Guid revisorId, CancellationToken ct = default)
        {
            RevisorPedido = revisorId;
            return Task.FromResult(Dictamenes);
        }

        public Task<IReadOnlyList<DesempenoItemDto>> ConsultarDesempenoMenuAsync(Guid menuId, CancellationToken ct = default)
        {
            MenuPedido = menuId;
            return Task.FromResult(Desempeno);
        }

        public Task<IReadOnlyList<ComparacionPorGrupoDto>> ConsultarComparacionPorGrupoAsync(IReadOnlyList<Guid> dietas, bool soloFinales, CancellationToken ct = default)
        {
            DietasComparadas = dietas;
            SoloFinales = soloFinales;
            return Task.FromResult(PorGrupo);
        }

        public Task<IReadOnlyList<ComparacionPorNivelDto>> ConsultarComparacionPorNivelAsync(IReadOnlyList<Guid> dietas, bool soloFinales, CancellationToken ct = default)
            => Task.FromResult(PorNivel);

        public Task<IReadOnlyList<CriteriosEquilibrioDto>> ConsultarCriteriosEquilibrioAsync(IReadOnlyList<Guid> dietas, bool soloFinales, CancellationToken ct = default)
            => Task.FromResult(Criterios);

        public Task<IReadOnlyList<CorrelacionPorDietaDto>> ConsultarCorrelacionPorDietaAsync(DateTimeOffset? desde, DateTimeOffset? hasta, CancellationToken ct = default)
            => Task.FromResult(Correlaciones);

        public Task<IReadOnlyList<ItemMasRechazadoDto>> ConsultarItemsMasRechazadosAsync(int limite, int minimoConsumos, CancellationToken ct = default)
            => Task.FromResult(Rechazados);

        public Task<IReadOnlyList<RevalorizadoDto>> ConsultarRevalorizadosAsync(CancellationToken ct = default)
            => Task.FromResult(Revalorizados);
    }
}
