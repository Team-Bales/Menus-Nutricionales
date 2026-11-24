using Menus.Application.Reportes;
using Menus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Menus.Infrastructure.Persistence.Consultas;

/// <summary>
/// Implementación del puerto <see cref="IConsultasInformacionales"/> usando <see cref="MenusDbContext.Database.SqlQuery{T}"/>.
/// Cada método ejecuta la función SQL correspondiente con parámetros parametrizados y mapea las columnas
/// snake_case a PascalCase mediante alias explícitos (sin SELECT *).
/// </summary>
internal sealed class LectorDeConsultasInformacionales(MenusDbContext dbContext) : IConsultasInformacionales
{
    private readonly MenusDbContext _dbContext = dbContext;

    // ------------------------------------------------------------
    // Consulta 1: Menús automáticos por dieta
    // ------------------------------------------------------------
    public async Task<IReadOnlyList<MenuAutomaticoDto>> ConsultarMenusAutomaticosAsync(Guid dietaId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                menu_id AS "MenuId",
                creador AS "Creador",
                fecha_creacion AS "FechaCreacion",
                cantidad_total_alimentos AS "CantidadTotalAlimentos",
                distribucion AS "Distribucion",
                grupos_cubiertos AS "GruposCubiertos"
            FROM consulta_1_menus_automaticos_por_dieta(@dietaId)
            """;

        return await _dbContext.Database.SqlQueryRaw<MenuAutomaticoDto>(sql, new NpgsqlParameter("dietaId", dietaId))
            .ToListAsync(ct);
    }

    // ------------------------------------------------------------
    // Consulta 2: Alimentos más usados
    // ------------------------------------------------------------
    public async Task<IReadOnlyList<AlimentoMasUsadoDto>> ConsultarAlimentosMasUsadosAsync(Guid dietaId, int? limite, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                alimento_id AS "AlimentoId",
                alimento AS "Alimento",
                nivel_calorico AS "NivelCalorico",
                grupo_nutricional AS "GrupoNutricional",
                menus_finales AS "MenusFinales"
            FROM consulta_2_alimentos_mas_usados(@dietaId, @limite)
            """;

        var parametros = new[]
        {
            new NpgsqlParameter("dietaId", dietaId),
            new NpgsqlParameter("limite", (object?)limite ?? DBNull.Value)
        };

        return await _dbContext.Database.SqlQueryRaw<AlimentoMasUsadoDto>(sql, parametros)
            .ToListAsync(ct);
    }

    // ------------------------------------------------------------
    // Consulta 3: Dictámenes de un revisor
    // ------------------------------------------------------------
    public async Task<IReadOnlyList<DictamenDto>> ConsultarDictamenesAsync(Guid revisorId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                menu_id AS "MenuId",
                dieta AS "Dieta",
                creador AS "Creador",
                fecha_creacion AS "FechaCreacion",
                fecha_validacion AS "FechaValidacion",
                aprobado AS "Aprobado",
                observaciones AS "Observaciones",
                menu_reemplazo_id AS "MenuReemplazoId"
            FROM consulta_3_dictamenes_de_un_revisor(@revisorId)
            """;

        return await _dbContext.Database.SqlQueryRaw<DictamenDto>(sql, new NpgsqlParameter("revisorId", revisorId))
            .ToListAsync(ct);
    }

    // ------------------------------------------------------------
    // Consulta 4: Desempeño ante un menú
    // ------------------------------------------------------------
    public async Task<IReadOnlyList<DesempenoItemDto>> ConsultarDesempenoMenuAsync(Guid menuId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                item_id AS "ItemId",
                item AS "Item",
                clase AS "Clase",
                nivel_calorico AS "NivelCalorico",
                servicios_con_consumo AS "ServiciosConConsumo",
                aceptaciones AS "Aceptaciones",
                rechazos AS "Rechazos",
                tasa_aceptacion AS "TasaAceptacion"
            FROM consulta_4_desempeno_ante_un_menu(@menuId)
            """;

        return await _dbContext.Database.SqlQueryRaw<DesempenoItemDto>(sql, new NpgsqlParameter("menuId", menuId))
            .ToListAsync(ct);
    }

    // ------------------------------------------------------------
    // Consulta 5.1: Distribución por grupo nutricional
    // ------------------------------------------------------------
    public async Task<IReadOnlyList<ComparacionPorGrupoDto>> ConsultarComparacionPorGrupoAsync(IReadOnlyList<Guid> dietas, bool soloFinales, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                dieta_id AS "DietaId",
                dieta AS "Dieta",
                grupo_nutricional AS "GrupoNutricional",
                items AS "Items",
                porcentaje AS "Porcentaje"
            FROM consulta_5_distribucion_por_grupo(@dietas, @soloFinales)
            """;

        var parametros = new[]
        {
            new NpgsqlParameter("dietas", dietas.ToArray()),
            new NpgsqlParameter("soloFinales", soloFinales)
        };

        return await _dbContext.Database.SqlQueryRaw<ComparacionPorGrupoDto>(sql, parametros)
            .ToListAsync(ct);
    }

    // ------------------------------------------------------------
    // Consulta 5.2: Distribución por nivel calórico
    // ------------------------------------------------------------
    public async Task<IReadOnlyList<ComparacionPorNivelDto>> ConsultarComparacionPorNivelAsync(IReadOnlyList<Guid> dietas, bool soloFinales, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                dieta_id AS "DietaId",
                dieta AS "Dieta",
                nivel_calorico AS "NivelCalorico",
                items AS "Items",
                porcentaje AS "Porcentaje"
            FROM consulta_5_distribucion_por_nivel(@dietas, @soloFinales)
            """;

        var parametros = new[]
        {
            new NpgsqlParameter("dietas", dietas.ToArray()),
            new NpgsqlParameter("soloFinales", soloFinales)
        };

        return await _dbContext.Database.SqlQueryRaw<ComparacionPorNivelDto>(sql, parametros)
            .ToListAsync(ct);
    }

    // ------------------------------------------------------------
    // Consulta 5.3: Criterios de equilibrio
    // ------------------------------------------------------------
    public async Task<IReadOnlyList<CriteriosEquilibrioDto>> ConsultarCriteriosEquilibrioAsync(IReadOnlyList<Guid> dietas, bool soloFinales, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                dieta_id AS "DietaId",
                dieta AS "Dieta",
                menu_id AS "MenuId",
                estado AS "Estado",
                items AS "Items",
                cantidad_declarada AS "CantidadDeclarada",
                cantidad_cumple AS "CantidadCumple",
                proporciones_cumplen AS "ProporcionesCumplen",
                tipos_fuera_de_proporcion AS "TiposFueraDeProporcion",
                requeridos_cubiertos AS "RequeridosCubiertos",
                requeridos_faltantes AS "RequeridosFaltantes",
                prohibidos_ausentes AS "ProhibidosAusentes",
                prohibidos_presentes AS "ProhibidosPresentes"
            FROM consulta_5_criterios_de_equilibrio(@dietas, @soloFinales)
            """;

        var parametros = new[]
        {
            new NpgsqlParameter("dietas", dietas.ToArray()),
            new NpgsqlParameter("soloFinales", soloFinales)
        };

        return await _dbContext.Database.SqlQueryRaw<CriteriosEquilibrioDto>(sql, parametros)
            .ToListAsync(ct);
    }

    // ------------------------------------------------------------
    // Consulta 6A: Correlación de Spearman por dieta
    // ------------------------------------------------------------
    public async Task<IReadOnlyList<CorrelacionPorDietaDto>> ConsultarCorrelacionPorDietaAsync(DateTimeOffset? desde, DateTimeOffset? hasta, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                dieta_id AS "DietaId",
                dieta AS "Dieta",
                menus_valorados AS "MenusValorados",
                coeficiente_spearman AS "CoeficienteSpearman"
            FROM consulta_6a_correlacion_por_dieta(@desde, @hasta)
            """;

        var parametros = new[]
        {
            new NpgsqlParameter("desde", (object?)desde?.UtcDateTime ?? DBNull.Value),
            new NpgsqlParameter("hasta", (object?)hasta?.UtcDateTime ?? DBNull.Value)
        };

        return await _dbContext.Database.SqlQueryRaw<CorrelacionPorDietaDto>(sql, parametros)
            .ToListAsync(ct);
    }

    // ------------------------------------------------------------
    // Consulta 6B: Top-N ítems más rechazados
    // ------------------------------------------------------------
    public async Task<IReadOnlyList<ItemMasRechazadoDto>> ConsultarItemsMasRechazadosAsync(int limite, int minimoConsumos, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                item_id AS "ItemId",
                item AS "Item",
                clase AS "Clase",
                descriptor AS "Descriptor",
                consumos AS "Consumos",
                rechazos AS "Rechazos",
                tasa_rechazo AS "TasaRechazo",
                dietas AS "Dietas"
            FROM consulta_6b_items_mas_rechazados(@limite, @minimoConsumos)
            """;

        var parametros = new[]
        {
            new NpgsqlParameter("limite", limite),
            new NpgsqlParameter("minimoConsumos", minimoConsumos)
        };

        return await _dbContext.Database.SqlQueryRaw<ItemMasRechazadoDto>(sql, parametros)
            .ToListAsync(ct);
    }

    // ------------------------------------------------------------
    // Consulta 6C: Revalorizados frente al promedio de su sala
    // ------------------------------------------------------------
    public async Task<IReadOnlyList<RevalorizadoDto>> ConsultarRevalorizadosAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                revalorizacion_id AS "RevalorizacionId",
                paciente_id AS "PacienteId",
                paciente AS "Paciente",
                sala AS "Sala",
                dieta AS "Dieta",
                valor_revalorizacion AS "ValorRevalorizacion",
                promedio_sala AS "PromedioSala",
                diferencia AS "Diferencia"
            FROM consulta_6c_revalorizados_frente_a_su_sala()
            """;

        return await _dbContext.Database.SqlQueryRaw<RevalorizadoDto>(sql)
            .ToListAsync(ct);
    }
}
