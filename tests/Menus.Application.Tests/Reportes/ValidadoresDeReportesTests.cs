using FluentValidation;
using Menus.Application.Reportes;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Tests.Reportes;

/// <summary>Pruebas de la forma de la entrada de los reportes (ADR-010, §3: 422 con los errores por campo).</summary>
public sealed class ValidadoresDeReportesTests
{
    private static readonly Guid DietaId = Guid.NewGuid();
    private static readonly Guid RevisorId = Guid.NewGuid();
    private static readonly Guid MenuId = Guid.NewGuid();

    private static string[] Campos<T>(AbstractValidator<T> validador, T solicitud)
        => [.. validador.Validate(solicitud).Errors.Select(e => e.PropertyName).Distinct()];

    // ---- C1: ListarMenusAutomaticos ----

    [Fact]
    public void C1_Valido_SinOrden_NoTieneErrores()
        => Assert.Empty(Campos(new ListarMenusAutomaticosValidator(), new ListarMenusAutomaticosQuery(DietaId)));

    [Fact]
    public void C1_Valido_ConOrdenValido_NoTieneErrores()
        => Assert.Empty(Campos(new ListarMenusAutomaticosValidator(), new ListarMenusAutomaticosQuery(DietaId, new OrdenDeReporte("menuId", "asc"))));

    [Fact]
    public void C1_SinDietaId_FallaEnDietaId()
        => Assert.Equal(["DietaId"], Campos(new ListarMenusAutomaticosValidator(), new ListarMenusAutomaticosQuery(Guid.Empty)));

    [Theory]
    [InlineData("columnaInexistente", "asc", "Orden.Columna")]
    [InlineData("menuId", "invalid", "Orden.Direccion")]
    [InlineData("creador", "DESC", "")]
    public void C1_OrdenInvalido_Falla(string columna, string direccion, string campoEsperado)
    {
        var errores = Campos(new ListarMenusAutomaticosValidator(), new ListarMenusAutomaticosQuery(DietaId, new OrdenDeReporte(columna, direccion)));
        if (string.IsNullOrEmpty(campoEsperado))
        {
            Assert.Empty(errores);
        }
        else
        {
            Assert.Contains(campoEsperado, errores);
        }
    }

    // ---- C2: ListarAlimentosMasUsados ----

    [Fact]
    public void C2_Valido_SinOrden_NoTieneErrores()
        => Assert.Empty(Campos(new ListarAlimentosMasUsadosValidator(), new ListarAlimentosMasUsadosQuery(DietaId)));

    [Fact]
    public void C2_TopFueraDeRango_FallaEnTop()
    {
        Assert.Equal(["Top"], Campos(new ListarAlimentosMasUsadosValidator(), new ListarAlimentosMasUsadosQuery(DietaId, 0)));
        Assert.Equal(["Top"], Campos(new ListarAlimentosMasUsadosValidator(), new ListarAlimentosMasUsadosQuery(DietaId, 21)));
    }

    [Theory]
    [InlineData("columnaInexistente", "asc", "Orden.Columna")]
    [InlineData("alimentoId", "invalid", "Orden.Direccion")]
    [InlineData("alimento", "DESC", "")]
    public void C2_OrdenInvalido_Falla(string columna, string direccion, string campoEsperado)
    {
        var errores = Campos(new ListarAlimentosMasUsadosValidator(), new ListarAlimentosMasUsadosQuery(DietaId, 10, new OrdenDeReporte(columna, direccion)));
        if (string.IsNullOrEmpty(campoEsperado))
        {
            Assert.Empty(errores);
        }
        else
        {
            Assert.Contains(campoEsperado, errores);
        }
    }

    // ---- C3: ListarDictamenes ----

    [Fact]
    public void C3_Valido_SinOrden_NoTieneErrores()
        => Assert.Empty(Campos(new ListarDictamenesValidator(), new ListarDictamenesQuery(RevisorId)));

    [Fact]
    public void C3_SinRevisorId_FallaEnRevisorId()
        => Assert.Equal(["RevisorId"], Campos(new ListarDictamenesValidator(), new ListarDictamenesQuery(Guid.Empty)));

    // ---- C4: ListarDesempenoMenu ----

    [Fact]
    public void C4_Valido_SinOrden_NoTieneErrores()
        => Assert.Empty(Campos(new ListarDesempenoMenuValidator(), new ListarDesempenoMenuQuery(MenuId)));

    [Fact]
    public void C4_SinMenuId_FallaEnMenuId()
        => Assert.Equal(["MenuId"], Campos(new ListarDesempenoMenuValidator(), new ListarDesempenoMenuQuery(Guid.Empty)));

    // ---- C5: CompararDietas ----

    [Fact]
    public void C5_Valido_DosDietas_SinOrden_NoTieneErrores()
    {
        var dietas = new[] { Guid.NewGuid(), Guid.NewGuid() };
        Assert.Empty(Campos(new CompararDietasValidator(), new CompararDietasQuery(dietas)));
    }

    [Fact]
    public void C5_UnaSolaDieta_Falla()
        => Assert.Equal(["Dietas"], Campos(new CompararDietasValidator(), new CompararDietasQuery([Guid.NewGuid()])));

    [Fact]
    public void C5_DietasDuplicadas_Falla()
    {
        var id = Guid.NewGuid();
        Assert.Equal(["Dietas"], Campos(new CompararDietasValidator(), new CompararDietasQuery([id, id])));
    }

    [Theory]
    [InlineData("ordenGrupos", "columnaInexistente", "asc", "OrdenGrupos.Columna")]
    [InlineData("ordenNiveles", "dietaId", "invalid", "OrdenNiveles.Direccion")]
    [InlineData("ordenEquilibrio", "menuId", "DESC", "")]
    public void C5_OrdenInvalido_Falla(string propiedad, string columna, string direccion, string campoEsperado)
    {
        var dietas = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var query = propiedad switch
        {
            "ordenGrupos" => new CompararDietasQuery(dietas, OrdenGrupos: new OrdenDeReporte(columna, direccion)),
            "ordenNiveles" => new CompararDietasQuery(dietas, OrdenNiveles: new OrdenDeReporte(columna, direccion)),
            _ => new CompararDietasQuery(dietas, OrdenEquilibrio: new OrdenDeReporte(columna, direccion))
        };
        var errores = Campos(new CompararDietasValidator(), query);
        if (string.IsNullOrEmpty(campoEsperado))
        {
            Assert.Empty(errores);
        }
        else
        {
            Assert.Contains(campoEsperado, errores);
        }
    }

    // ---- C6: ObtenerCorrelacionNutricional ----

    [Fact]
    public void C6_Valido_SinParametrosOpcionales_NoTieneErrores()
        => Assert.Empty(Campos(new ObtenerCorrelacionNutricionalValidator(), new ObtenerCorrelacionNutricionalQuery(null, null)));

    [Fact]
    public void C6_DesdeMayorQueHasta_Falla()
    {
        var desde = DateTimeOffset.UtcNow.AddHours(1);
        var hasta = DateTimeOffset.UtcNow;
        Assert.Equal(["Desde"], Campos(new ObtenerCorrelacionNutricionalValidator(), new ObtenerCorrelacionNutricionalQuery(desde, hasta)));
    }

    [Fact]
    public void C6_MinimoConsumosCero_Falla()
        => Assert.Equal(["MinimoConsumos"], Campos(new ObtenerCorrelacionNutricionalValidator(), new ObtenerCorrelacionNutricionalQuery(null, null, 0)));

    [Theory]
    [InlineData("ordenCorrelacion", "columnaInexistente", "asc", "OrdenCorrelacion.Columna")]
    [InlineData("ordenRechazos", "itemId", "invalid", "OrdenRechazos.Direccion")]
    [InlineData("ordenRevalorizaciones", "revalorizacionId", "DESC", "")]
    public void C6_OrdenInvalido_Falla(string propiedad, string columna, string direccion, string campoEsperado)
    {
        var query = propiedad switch
        {
            "ordenCorrelacion" => new ObtenerCorrelacionNutricionalQuery(null, null, OrdenCorrelacion: new OrdenDeReporte(columna, direccion)),
            "ordenRechazos" => new ObtenerCorrelacionNutricionalQuery(null, null, OrdenRechazos: new OrdenDeReporte(columna, direccion)),
            _ => new ObtenerCorrelacionNutricionalQuery(null, null, OrdenRevalorizaciones: new OrdenDeReporte(columna, direccion))
        };
        var errores = Campos(new ObtenerCorrelacionNutricionalValidator(), query);
        if (string.IsNullOrEmpty(campoEsperado))
        {
            Assert.Empty(errores);
        }
        else
        {
            Assert.Contains(campoEsperado, errores);
        }
    }
}
