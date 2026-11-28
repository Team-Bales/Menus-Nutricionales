using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Menus.Application.IA;
using Menus.Application.IA.Proveedores;
using Menus.Application.Tests.Catalogo;
using Menus.Application.Tests.Catalogo.Dietas;
using Menus.Application.Tests.Valoraciones;
using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Menus.Application.Tests.IA;

/// <summary>
/// Pruebas de los proveedores de contexto <c>valoracion</c> y <c>aceptacion</c> (#325) con datos conocidos, su
/// prueba de privacidad (contrato de IA, §5.5) y que cada número del resumen esté en las cifras (§6).
/// </summary>
public sealed partial class ProveedoresDeValoracionYAceptacionTests
{
    private const string NombreDelPaciente = "Juan Pérez Gómez";
    private const int EdadDelPaciente = 34;
    private const string Dieta = "Hiposódica";

    private static readonly DateOnly Dia1 = new(2026, 11, 20);
    private static readonly DateOnly Dia2 = new(2026, 11, 21);
    private static readonly DateOnly DiaPosterior = new(2026, 11, 25);
    private static readonly DateTime FechaValoracion = new(2026, 11, 22, 10, 0, 0, DateTimeKind.Utc);

    private readonly BancoDeDietasFalso _banco = new();
    private readonly BancoFalso _catalogo = new();
    private readonly ValoracionesFalsas _valoraciones;
    private readonly ConteoFalso _conteo = new();

    private readonly Guid _pacienteId = Guid.NewGuid();
    private readonly Guid _salaId = Guid.NewGuid();
    private readonly Menu _menu;
    private readonly ValoracionNutricional _valoracion;
    private readonly Alimento _arroz;
    private readonly Alimento _manzana;
    private readonly Alimento _leche;
    private readonly Alimento _pollo;

    public ProveedoresDeValoracionYAceptacionTests()
    {
        _valoraciones = new ValoracionesFalsas(_banco);
        var dieta = _banco.Agregar(Dieta);

        _arroz = _catalogo.Agregar("Arroz blanco", NivelCalorico.Medio.Id);
        _manzana = _catalogo.Agregar("Manzana", NivelCalorico.Bajo.Id);
        _leche = _catalogo.Agregar("Leche descremada", NivelCalorico.Bajo.Id);
        _pollo = _catalogo.Agregar("Pollo asado", NivelCalorico.Alto.Id);

        _menu = MenuServido(dieta.Id, _arroz, _manzana, _leche, _pollo);
        // Día 1: arroz rechazado; manzana, leche y pollo aceptados.
        Consumir(Dia1, (_arroz, false), (_manzana, true), (_leche, true), (_pollo, true));
        // Día 2: arroz y manzana rechazados; pollo aceptado.
        Consumir(Dia2, (_arroz, false), (_manzana, false), (_pollo, true));
        // Después de la valoración: no cuenta.
        Consumir(DiaPosterior, (_pollo, false));

        _valoracion = new ValoracionNutricional(
            Guid.NewGuid(), _menu.Id, _pacienteId, Guid.NewGuid(), ResultadoNutricional.Buena.Id, FechaValoracion);
        _valoraciones.Valoraciones.Add(_valoracion);
    }

    // ---- valoracion ----

    [Fact]
    public async Task Valoracion_con_una_dieta_frecuente_informa_el_resultado_y_la_dieta()
    {
        _conteo.Pacientes = 12;

        var resultado = await Valoracion().ConstruirAsync(Caso(), CancellationToken.None);

        var bloque = resultado!.Bloque!;
        Assert.Equal("valoracion", bloque.Fuente);
        Assert.Equal(1, bloque.Version);
        Assert.Contains("Buena (4 de 5)", bloque.Resumen);
        Assert.Contains($"Dieta del menú: {Dieta}.", bloque.Resumen);
        Assert.Contains("del paciente", bloque.Resumen);
        Assert.Equal(4, bloque.Cifras["valoracion.valor"]);
        Assert.Equal(5, bloque.Cifras["valoracion.maximo"]);
        Assert.Empty(resultado.Advertencias);
        Assert.Equal((Dieta, 18, (int?)39), Assert.Single(_conteo.Consultas));
    }

    [Fact]
    public async Task Valoracion_con_una_dieta_rara_y_sin_consentimiento_la_generaliza_y_avisa()
    {
        _conteo.Pacientes = 2;

        var resultado = await Valoracion().ConstruirAsync(Caso(), CancellationToken.None);

        Assert.DoesNotContain(Dieta, resultado!.Bloque!.Resumen);
        Assert.Contains("dato clínico no detallado por privacidad", resultado.Bloque.Resumen);
        Assert.Equal("Se generalizó información clínica poco frecuente por privacidad.", Assert.Single(resultado.Advertencias));
    }

    [Fact]
    public async Task Valoracion_con_consentimiento_informa_la_dieta_aunque_sea_rara()
    {
        _conteo.Pacientes = 1;

        var resultado = await Valoracion().ConstruirAsync(
            Caso(consentimiento: EstadoConsentimientoIA.Si), CancellationToken.None);

        Assert.Contains($"Dieta del menú: {Dieta}.", resultado!.Bloque!.Resumen);
        Assert.Empty(resultado.Advertencias);
    }

    [Fact]
    public async Task Valoracion_con_varios_pacientes_usa_el_codigo_opaco()
    {
        _conteo.Pacientes = 12;
        var otro = new PacienteSnapshot { Id = Guid.NewGuid(), Nombre = "Otra Persona", Edad = 50, SalaId = Guid.NewGuid() };
        var caso = new ContextoDeCaso([Paciente(), otro], DateOnly.FromDateTime(FechaValoracion), valoracionId: _valoracion.Id);

        var resultado = await Valoracion().ConstruirAsync(caso, CancellationToken.None);

        Assert.Matches(@"Valoración nutricional de P[12] sobre", resultado!.Bloque!.Resumen);
    }

    [Fact]
    public async Task Valoracion_sin_valoracion_en_el_caso_o_inexistente_devuelve_null()
    {
        var sinValoracion = new ContextoDeCaso([Paciente()], DateOnly.FromDateTime(FechaValoracion));
        var inexistente = new ContextoDeCaso([Paciente()], DateOnly.FromDateTime(FechaValoracion), valoracionId: Guid.NewGuid());

        Assert.Null(await Valoracion().ConstruirAsync(sinValoracion, CancellationToken.None));
        Assert.Null(await Valoracion().ConstruirAsync(inexistente, CancellationToken.None));
    }

    [Fact]
    public async Task Valoracion_cuyo_paciente_no_esta_en_el_caso_lanza()
    {
        var ajeno = new PacienteSnapshot { Id = Guid.NewGuid(), Nombre = "Otra Persona", Edad = 50, SalaId = Guid.NewGuid() };
        var caso = new ContextoDeCaso([ajeno], DateOnly.FromDateTime(FechaValoracion), valoracionId: _valoracion.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Valoracion().ConstruirAsync(caso, CancellationToken.None));
    }

    // ---- aceptacion ----

    [Fact]
    public async Task Aceptacion_informa_la_tasa_global_por_nivel_y_los_mas_rechazados_hasta_la_valoracion()
    {
        var resultado = await Aceptacion().ConstruirAsync(Caso(), CancellationToken.None);

        var bloque = resultado!.Bloque!;
        Assert.Equal("aceptacion", bloque.Fuente);
        // 7 consumos hasta el día de la valoración: 4 aceptados. El rechazo del día posterior no cuenta.
        Assert.Contains("aceptó el 57 % (4 de 7) de los ítems servidos", bloque.Resumen);
        Assert.Contains("Por nivel calórico: Bajo 67 % (2 de 3); Medio 0 % (0 de 2); Alto 100 % (2 de 2).", bloque.Resumen);
        Assert.Contains(
            "Ítems más rechazados: Arroz blanco (nivel Medio), rechazado 2 de 2 veces; Manzana (nivel Bajo), rechazado 1 de 2 veces.",
            bloque.Resumen);
        Assert.Equal(57, bloque.Cifras["aceptacion.global.porcentaje"]);
        Assert.Equal(4, bloque.Cifras["aceptacion.global.aceptados"]);
        Assert.Equal(7, bloque.Cifras["aceptacion.global.total"]);
        Assert.Equal(67, bloque.Cifras["aceptacion.nivel.bajo.porcentaje"]);
        Assert.Equal(2, bloque.Cifras["aceptacion.rechazado.1.rechazos"]);
        Assert.Empty(resultado.Advertencias);
    }

    [Fact]
    public async Task Aceptacion_informa_como_maximo_cinco_rechazados()
    {
        var extras = Enumerable.Range(1, 6).Select(i => _catalogo.Agregar($"Ítem extra {(char)('A' + i)}", NivelCalorico.Medio.Id)).ToArray();
        var menu = MenuServido(_banco.Dietas[0].Id, extras);
        foreach (var item in extras)
        {
            menu.RegistrarConsumo(_pacienteId, Dia1, item.Id, false);
        }

        var valoracion = new ValoracionNutricional(
            Guid.NewGuid(), menu.Id, _pacienteId, Guid.NewGuid(), ResultadoNutricional.Mala.Id, FechaValoracion);
        _valoraciones.Valoraciones.Add(valoracion);
        var caso = new ContextoDeCaso([Paciente()], DateOnly.FromDateTime(FechaValoracion), valoracionId: valoracion.Id);

        var resultado = await Aceptacion().ConstruirAsync(caso, CancellationToken.None);

        Assert.Equal(5, resultado!.Bloque!.Cifras.Keys.Count(k => k.EndsWith(".rechazos", StringComparison.Ordinal)));
        Assert.False(resultado.Bloque.Cifras.ContainsKey("aceptacion.rechazado.6.rechazos"));
    }

    [Fact]
    public async Task Aceptacion_sin_consumos_hasta_la_valoracion_devuelve_null()
    {
        var menu = MenuServido(_banco.Dietas[0].Id, _arroz);
        var valoracion = new ValoracionNutricional(
            Guid.NewGuid(), menu.Id, _pacienteId, Guid.NewGuid(), ResultadoNutricional.Normal.Id, FechaValoracion);
        _valoraciones.Valoraciones.Add(valoracion);
        var caso = new ContextoDeCaso([Paciente()], DateOnly.FromDateTime(FechaValoracion), valoracionId: valoracion.Id);

        Assert.Null(await Aceptacion().ConstruirAsync(caso, CancellationToken.None));
    }

    [Fact]
    public async Task Aceptacion_sin_valoracion_en_el_caso_devuelve_null()
        => Assert.Null(await Aceptacion().ConstruirAsync(
            new ContextoDeCaso([Paciente()], DateOnly.FromDateTime(FechaValoracion)), CancellationToken.None));

    // ---- comunes a los dos ----

    [Fact]
    public async Task Ningun_bloque_lleva_datos_que_identifiquen_al_paciente()
    {
        _conteo.Pacientes = 12;
        var caso = Caso();

        foreach (var proveedor in new IProveedorDeContexto[] { Valoracion(), Aceptacion() })
        {
            var bloque = (await proveedor.ConstruirAsync(caso, CancellationToken.None))!.Bloque!;
            var json = JsonSerializer.Serialize(bloque);

            Assert.DoesNotMatch(UnGuid(), json);
            Assert.DoesNotContain("Juan", json);
            Assert.DoesNotContain("Pérez", json);
            Assert.DoesNotContain(EdadDelPaciente.ToString(CultureInfo.InvariantCulture), json);
            Assert.DoesNotContain("años", json);
            Assert.DoesNotContain("sala", json, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Cada_numero_del_resumen_esta_en_las_cifras()
    {
        _conteo.Pacientes = 12;
        var caso = Caso();

        foreach (var proveedor in new IProveedorDeContexto[] { Valoracion(), Aceptacion() })
        {
            var bloque = (await proveedor.ConstruirAsync(caso, CancellationToken.None))!.Bloque!;
            var numeros = UnNumero().Matches(bloque.Resumen).Select(m => double.Parse(m.Value, CultureInfo.InvariantCulture));

            Assert.All(numeros, n => Assert.Contains(n, bloque.Cifras.Values));
        }
    }

    [Fact]
    public void AddIA_registra_los_dos_proveedores()
    {
        var servicios = new ServiceCollection();
        servicios.AddIA();

        var tipos = servicios.Where(s => s.ServiceType == typeof(IProveedorDeContexto)).Select(s => s.ImplementationType);

        Assert.Contains(typeof(ProveedorDeValoracion), tipos);
        Assert.Contains(typeof(ProveedorDeAceptacion), tipos);
    }

    [Theory]
    [InlineData("0-12", 0, 12)]
    [InlineData("18-39", 18, 39)]
    [InlineData("65+", 65, null)]
    public void RangoDeEdad_lee_los_limites_del_texto_del_transformador(string rango, int minima, int? maxima)
        => Assert.Equal((minima, maxima), RangoDeEdad.Limites(rango));

    [Fact]
    public void RangoDeEdad_rechaza_un_texto_que_no_es_un_rango()
        => Assert.Throws<FormatException>(() => RangoDeEdad.Limites("adulto"));

    [Theory]
    [InlineData(1, 3, 33)]
    [InlineData(2, 3, 67)]
    [InlineData(1, 8, 13)]
    [InlineData(0, 5, 0)]
    public void Porcentaje_redondea_al_entero(int parte, int total, int esperado)
        => Assert.Equal(esperado, TextoDeBloque.Porcentaje(parte, total));

    // ---- ayudas ----

    private ProveedorDeValoracion Valoracion() => new(
        _valoraciones, _banco.MenusRepo, _banco.DietasRepo, _conteo, Options.Create(new IAOptions { KMinimo = 5 }));

    private ProveedorDeAceptacion Aceptacion() => new(_valoraciones, _banco.MenusRepo, _catalogo.ItemsRepo);

    private PacienteSnapshot Paciente(EstadoConsentimientoIA consentimiento = EstadoConsentimientoIA.NoRegistrado) => new()
    {
        Id = _pacienteId,
        Nombre = NombreDelPaciente,
        Edad = EdadDelPaciente,
        SalaId = _salaId,
        ConsentimientoIA = consentimiento,
        Dietas = new Dictionary<string, string> { [Dieta] = "terapeutica" },
    };

    private ContextoDeCaso Caso(EstadoConsentimientoIA consentimiento = EstadoConsentimientoIA.NoRegistrado)
        => new([Paciente(consentimiento)], DateOnly.FromDateTime(FechaValoracion), valoracionId: _valoracion.Id, pacienteId: _pacienteId);

    /// <summary>Un menú final con esos ítems, asignado al paciente y guardado en el banco.</summary>
    private Menu MenuServido(Guid dietaId, params Alimento[] items)
    {
        var menu = new Menu(
            Guid.NewGuid(), dietaId, Guid.NewGuid(), FechaValoracion.AddDays(-10), Math.Max(items.Length, 1), false,
            [(TipoPreparacion.PlatoFuerte.Id, 1m)], [BancoDeDietasFalso.GrupoCereales]);
        foreach (var item in items)
        {
            menu.AgregarItem(item.Id);
        }

        menu.Aprobar(Guid.NewGuid(), FechaValoracion.AddDays(-9));
        menu.AsignarPaciente(_pacienteId);
        foreach (var dia in new[] { Dia1, Dia2, DiaPosterior })
        {
            menu.RegistrarServicio(_pacienteId, dia);
        }

        _banco.Menus.Add(menu);
        return menu;
    }

    private void Consumir(DateOnly dia, params (Alimento Item, bool Aceptado)[] consumos)
    {
        foreach (var (item, aceptado) in consumos)
        {
            _menu.RegistrarConsumo(_pacienteId, dia, item.Id, aceptado);
        }
    }

    [GeneratedRegex(@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")]
    private static partial Regex UnGuid();

    [GeneratedRegex(@"\d+")]
    private static partial Regex UnNumero();

    /// <summary>Conteo de pacientes fijo, que guarda cada consulta.</summary>
    private sealed class ConteoFalso : IConteoDePacientesPorDieta
    {
        public int Pacientes { get; set; } = 10;

        public List<(string Dieta, int Minima, int? Maxima)> Consultas { get; } = [];

        public Task<int> ContarAsync(string nombreDieta, int edadMinima, int? edadMaxima, CancellationToken ct)
        {
            Consultas.Add((nombreDieta, edadMinima, edadMaxima));
            return Task.FromResult(Pacientes);
        }
    }
}
