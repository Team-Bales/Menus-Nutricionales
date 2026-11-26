using Menus.Application.Reportes;

namespace Menus.Application.Tests.Reportes;

/// <summary>Pruebas de construcción y propiedades de los DTOs de reportes.</summary>
public sealed class ReportesDtosTests
{
    // ---- C1: MenuAutomaticoDto ----

    [Fact]
    public void MenuAutomaticoDto_Constructor_AsignaTodasLasPropiedades()
    {
        var id = Guid.NewGuid();
        var fecha = DateTimeOffset.UtcNow;
        var dto = new MenuAutomaticoDto(id, "Nutri", fecha, 10, "50% PlatoFuerte", "Cereales,Proteinas");

        Assert.Equal(id, dto.MenuId);
        Assert.Equal("Nutri", dto.Creador);
        Assert.Equal(fecha, dto.FechaCreacion);
        Assert.Equal(10, dto.CantidadTotalAlimentos);
        Assert.Equal("50% PlatoFuerte", dto.Distribucion);
        Assert.Equal("Cereales,Proteinas", dto.GruposCubiertos);
    }

    [Fact]
    public void MenuAutomaticoDto_With_CreaCopiaConCambios()
    {
        var original = new MenuAutomaticoDto(Guid.NewGuid(), "A", DateTimeOffset.UtcNow, 5, "d", "g");
        var modificado = original with { Creador = "B", CantidadTotalAlimentos = 7 };

        Assert.Equal("B", modificado.Creador);
        Assert.Equal(7, modificado.CantidadTotalAlimentos);
        Assert.Equal(original.MenuId, modificado.MenuId);
        Assert.Equal(original.FechaCreacion, modificado.FechaCreacion);
    }

    // ---- C2: AlimentoMasUsadoDto ----

    [Fact]
    public void AlimentoMasUsadoDto_Constructor_AsignaTodasLasPropiedades()
    {
        var id = Guid.NewGuid();
        var dto = new AlimentoMasUsadoDto(id, "Arroz", "Medio", "Cereales", 15L);

        Assert.Equal(id, dto.AlimentoId);
        Assert.Equal("Arroz", dto.Alimento);
        Assert.Equal("Medio", dto.NivelCalorico);
        Assert.Equal("Cereales", dto.GrupoNutricional);
        Assert.Equal(15L, dto.MenusFinales);
    }

    // ---- C3: DictamenDto ----

    [Fact]
    public void DictamenDto_Constructor_AsignaTodasLasPropiedades()
    {
        var menuId = Guid.NewGuid();
        var reemplazoId = Guid.NewGuid();
        var fechaCre = DateTimeOffset.UtcNow.AddDays(-5);
        var fechaVal = DateTimeOffset.UtcNow;
        var dto = new DictamenDto(menuId, "DietaX", "CreadorY", fechaCre, fechaVal, false, "Rechazado por X", reemplazoId);

        Assert.Equal(menuId, dto.MenuId);
        Assert.Equal("DietaX", dto.Dieta);
        Assert.Equal("CreadorY", dto.Creador);
        Assert.Equal(fechaCre, dto.FechaCreacion);
        Assert.Equal(fechaVal, dto.FechaValidacion);
        Assert.False(dto.Aprobado);
        Assert.Equal("Rechazado por X", dto.Observaciones);
        Assert.Equal(reemplazoId, dto.MenuReemplazoId);
    }

    [Fact]
    public void DictamenDto_ObservacionesNull_SePermite()
    {
        var dto = new DictamenDto(Guid.NewGuid(), "D", "C", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, true, null, null);
        Assert.Null(dto.Observaciones);
        Assert.Null(dto.MenuReemplazoId);
    }

    // ---- C4: DesempenoItemDto ----

    [Fact]
    public void DesempenoItemDto_Constructor_AsignaTodasLasPropiedades()
    {
        var itemId = Guid.NewGuid();
        var dto = new DesempenoItemDto(itemId, "Pollo", "Plato", "Alto", 20L, 18L, 2L, 0.9m);

        Assert.Equal(itemId, dto.ItemId);
        Assert.Equal("Pollo", dto.Item);
        Assert.Equal("Plato", dto.Clase);
        Assert.Equal("Alto", dto.NivelCalorico);
        Assert.Equal(20L, dto.ServiciosConConsumo);
        Assert.Equal(18L, dto.Aceptaciones);
        Assert.Equal(2L, dto.Rechazos);
        Assert.Equal(0.9m, dto.TasaAceptacion);
    }

    [Fact]
    public void DesempenoItemDto_TasaAceptacionNull_CuandoSinConsumos()
    {
        var dto = new DesempenoItemDto(Guid.NewGuid(), "X", "Alimento", "Bajo", 0L, 0L, 0L, null);
        Assert.Null(dto.TasaAceptacion);
    }

    // ---- C5: ComparacionPorGrupoDto ----

    [Fact]
    public void ComparacionPorGrupoDto_Constructor_AsignaTodasLasPropiedades()
    {
        var dietaId = Guid.NewGuid();
        var dto = new ComparacionPorGrupoDto(dietaId, "Dieta", "Proteinas", 8L, 40m);
        Assert.Equal(dietaId, dto.DietaId);
        Assert.Equal("Dieta", dto.Dieta);
        Assert.Equal("Proteinas", dto.GrupoNutricional);
        Assert.Equal(8L, dto.Items);
        Assert.Equal(40m, dto.Porcentaje);
    }

    // ---- C5: ComparacionPorNivelDto ----

    [Fact]
    public void ComparacionPorNivelDto_Constructor_AsignaTodasLasPropiedades()
    {
        var dietaId = Guid.NewGuid();
        var dto = new ComparacionPorNivelDto(dietaId, "Dieta", "Medio", 12L, 60m);
        Assert.Equal(dietaId, dto.DietaId);
        Assert.Equal("Dieta", dto.Dieta);
        Assert.Equal("Medio", dto.NivelCalorico);
        Assert.Equal(12L, dto.Items);
        Assert.Equal(60m, dto.Porcentaje);
    }

    // ---- C5: CriteriosEquilibrioDto ----

    [Fact]
    public void CriteriosEquilibrioDto_Constructor_AsignaTodasLasPropiedades()
    {
        var dietaId = Guid.NewGuid();
        var menuId = Guid.NewGuid();
        var dto = new CriteriosEquilibrioDto(
            dietaId, "Dieta", menuId, "Final", 10L, 10, true, true, null,
            true, null, true, null);

        Assert.Equal(dietaId, dto.DietaId);
        Assert.Equal("Dieta", dto.Dieta);
        Assert.Equal(menuId, dto.MenuId);
        Assert.Equal("Final", dto.Estado);
        Assert.Equal(10L, dto.Items);
        Assert.Equal(10, dto.CantidadDeclarada);
        Assert.True(dto.CantidadCumple);
        Assert.True(dto.ProporcionesCumplen);
        Assert.Null(dto.TiposFueraDeProporcion);
        Assert.True(dto.RequeridosCubiertos);
        Assert.Null(dto.RequeridosFaltantes);
        Assert.True(dto.ProhibidosAusentes);
        Assert.Null(dto.ProhibidosPresentes);
    }

    [Fact]
    public void CriteriosEquilibrioDto_CamposOpcionalesNull_SePermiten()
    {
        var dietaId = Guid.NewGuid();
        var menuId = Guid.NewGuid();
        var dto = new CriteriosEquilibrioDto(
            dietaId, "D", menuId, "Pendiente", 5L, 5, false, false, "PlatoFuerte",
            false, "Proteinas", false, null);

        Assert.Equal(dietaId, dto.DietaId);
        Assert.Equal("D", dto.Dieta);
        Assert.Equal(menuId, dto.MenuId);
        Assert.Equal("PlatoFuerte", dto.TiposFueraDeProporcion);
        Assert.Equal("Proteinas", dto.RequeridosFaltantes);
        Assert.Null(dto.ProhibidosPresentes);
        Assert.False(dto.CantidadCumple);
        Assert.False(dto.ProporcionesCumplen);
        Assert.False(dto.RequeridosCubiertos);
        Assert.False(dto.ProhibidosAusentes);
    }

    // ---- C5: ComparacionDietasDto ----

    [Fact]
    public void ComparacionDietasDto_Constructor_AgrupaTresListas()
    {
        var grupos = new List<ComparacionPorGrupoDto> { new(Guid.NewGuid(), "D1", "G1", 5L, 50m) };
        var niveles = new List<ComparacionPorNivelDto> { new(Guid.NewGuid(), "D1", "N1", 3L, 30m) };
        var criterios = new List<CriteriosEquilibrioDto> { new(Guid.NewGuid(), "D1", Guid.NewGuid(), "Final", 2L, 2, true, true, null, true, null, true, null) };

        var dto = new ComparacionDietasDto(grupos, niveles, criterios);

        Assert.Single(dto.PorGrupo);
        Assert.Single(dto.PorNivel);
        Assert.Single(dto.Criterios);
    }

    // ---- C6: CorrelacionPorDietaDto ----

    [Fact]
    public void CorrelacionPorDietaDto_Constructor_AsignaTodasLasPropiedades()
    {
        var dietaId = Guid.NewGuid();
        var dto = new CorrelacionPorDietaDto(dietaId, "Dieta", 8L, 0.75m);
        Assert.Equal(dietaId, dto.DietaId);
        Assert.Equal("Dieta", dto.Dieta);
        Assert.Equal(8L, dto.MenusValorados);
        Assert.Equal(0.75m, dto.CoeficienteSpearman);
    }

    [Fact]
    public void CorrelacionPorDietaDto_CoeficienteNull_CuandoNoCalculable()
    {
        var dietaId = Guid.NewGuid();
        var dto = new CorrelacionPorDietaDto(dietaId, "Dieta", 0L, null);
        Assert.Equal(dietaId, dto.DietaId);
        Assert.Null(dto.CoeficienteSpearman);
    }

    // ---- C6: ItemMasRechazadoDto ----

    [Fact]
    public void ItemMasRechazadoDto_Constructor_AsignaTodasLasPropiedades()
    {
        var itemId = Guid.NewGuid();
        var dto = new ItemMasRechazadoDto(itemId, "Arroz", "Alimento", "Nutri", 50L, 10L, 0.2m, "Dieta1,Dieta2");
        Assert.Equal(itemId, dto.ItemId);
        Assert.Equal("Arroz", dto.Item);
        Assert.Equal("Alimento", dto.Clase);
        Assert.Equal("Nutri", dto.Descriptor);
        Assert.Equal(50L, dto.Consumos);
        Assert.Equal(10L, dto.Rechazos);
        Assert.Equal(0.2m, dto.TasaRechazo);
        Assert.Equal("Dieta1,Dieta2", dto.Dietas);
    }

    // ---- C6: RevalorizadoDto ----

    [Fact]
    public void RevalorizadoDto_Constructor_AsignaTodasLasPropiedades()
    {
        var pId = Guid.NewGuid();
        var rId = Guid.NewGuid();
        var dto = new RevalorizadoDto(rId, pId, "Paciente", "SalaA", "DietaX", 8, 5.5m, 2.5m);
        Assert.Equal(rId, dto.RevalorizacionId);
        Assert.Equal(pId, dto.PacienteId);
        Assert.Equal("Paciente", dto.Paciente);
        Assert.Equal("SalaA", dto.Sala);
        Assert.Equal("DietaX", dto.Dieta);
        Assert.Equal(8, dto.ValorRevalorizacion);
        Assert.Equal(5.5m, dto.PromedioSala);
        Assert.Equal(2.5m, dto.Diferencia);
    }

    [Fact]
    public void RevalorizadoDto_PromedioNull_CuandoSinDatos()
    {
        var dto = new RevalorizadoDto(Guid.NewGuid(), Guid.NewGuid(), "P", "S", "D", 5, null, null);
        Assert.Null(dto.PromedioSala);
        Assert.Null(dto.Diferencia);
    }

    // ---- C6: CorrelacionNutricionalDto ----

    [Fact]
    public void CorrelacionNutricionalDto_Constructor_AgrupaTresListas()
    {
        var correlaciones = new List<CorrelacionPorDietaDto> { new(Guid.NewGuid(), "D1", 5L, 0.6m) };
        var rechazados = new List<ItemMasRechazadoDto> { new(Guid.NewGuid(), "X", "Alimento", "N", 10L, 2L, 0.2m, "D1") };
        var revalorizaciones = new List<RevalorizadoDto> { new(Guid.NewGuid(), Guid.NewGuid(), "P", "S", "D", 7, 5.0m, 2.0m) };

        var dto = new CorrelacionNutricionalDto(correlaciones, rechazados, revalorizaciones);

        Assert.Single(dto.Correlaciones);
        Assert.Single(dto.ItemsMasRechazados);
        Assert.Single(dto.Revalorizaciones);
    }

    // ---- OrdenDeReporte ----

    [Fact]
    public void OrdenDeReporte_TieneOrden_FalseCuandoColumnaNull()
    {
        var orden = new OrdenDeReporte(null, "asc");
        Assert.False(orden.TieneOrden);
    }

    [Fact]
    public void OrdenDeReporte_TieneOrden_FalseCuandoColumnaVacia()
    {
        var orden = new OrdenDeReporte("", "asc");
        Assert.False(orden.TieneOrden);
    }

    [Fact]
    public void OrdenDeReporte_TieneOrden_FalseCuandoColumnaSoloEspacios()
    {
        var orden = new OrdenDeReporte("   ", "asc");
        Assert.False(orden.TieneOrden);
    }

    [Fact]
    public void OrdenDeReporte_TieneOrden_TrueCuandoColumnaValida()
    {
        var orden = new OrdenDeReporte("nombre", "asc");
        Assert.True(orden.TieneOrden);
    }

    [Fact]
    public void OrdenDeReporte_DireccionNormalizada_DefaultAsc()
    {
        var orden = new OrdenDeReporte("nombre", null);
        Assert.Equal("asc", orden.DireccionNormalizada);
    }

    [Fact]
    public void OrdenDeReporte_DireccionNormalizada_LowerCase()
    {
        var orden = new OrdenDeReporte("nombre", "DESC");
        Assert.Equal("desc", orden.DireccionNormalizada);
    }

    [Fact]
    public void OrdenDeReporte_DireccionNormalizada_Trim()
    {
        var orden = new OrdenDeReporte("nombre", "  asc  ");
        Assert.Equal("asc", orden.DireccionNormalizada);
    }

    [Fact]
    public void OrdenDeReporte_DireccionValida_TrueParaAsc()
    {
        var orden = new OrdenDeReporte("nombre", "asc");
        Assert.True(orden.DireccionValida);
    }

    [Fact]
    public void OrdenDeReporte_DireccionValida_TrueParaDesc()
    {
        var orden = new OrdenDeReporte("nombre", "desc");
        Assert.True(orden.DireccionValida);
    }

    [Fact]
    public void OrdenDeReporte_DireccionValida_TrueParaNull()
    {
        var orden = new OrdenDeReporte("nombre", null);
        Assert.True(orden.DireccionValida);
    }

    [Fact]
    public void OrdenDeReporte_DireccionValida_TrueParaVacio()
    {
        var orden = new OrdenDeReporte("nombre", "");
        Assert.True(orden.DireccionValida);
    }

    [Fact]
    public void OrdenDeReporte_DireccionValida_FalseParaInvalida()
    {
        var orden = new OrdenDeReporte("nombre", "invalid");
        Assert.False(orden.DireccionValida);
    }

    [Fact]
    public void OrdenDeReporte_DireccionValida_FalseCaseInsensitive()
    {
        var orden = new OrdenDeReporte("nombre", "ASC");
        Assert.True(orden.DireccionValida);
    }
}
