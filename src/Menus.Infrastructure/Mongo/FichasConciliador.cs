using Menus.Domain.Entidades;
using Menus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Menus.Infrastructure.Mongo;

/// <summary>
/// Implementación de <see cref="IFichasConciliador"/> que recorre <c>fichas_nutricionales</c> por
/// <c>_id</c> ascendente en lotes configurables, compara con PostgreSQL y elimina huérfanas o actualiza
/// copias de nombre desactualizadas (RM-10).
/// </summary>
/// <remarks>
/// <para>
/// Algoritmo por lote:
/// <list type="number">
/// <item>Lee el lote de MongoDB (orden ascendente por <c>_id</c>, filtro <c>_id > último</c>).</item>
/// <item>Parsea <c>origen.id</c> a <see cref="Guid"/>. Si falla, cuenta <see cref="ResultadoConciliacion.FichasInvalidas"/> y no la borra.</item>
/// <item>Consulta PostgreSQL con <c>AsNoTracking</c>: <c>SELECT id, nombre, clase FROM items_alimentarios WHERE id = ANY(@ids)</c>.</item>
/// <item>Una ficha es huérfana si su ítem no existe <b>o</b> su <c>clase</c> no coincide con <c>origen.tabla</c>. Se eliminan con un solo <c>DeleteMany</c>.</item>
/// <item>Para fichas de plato válidas: junta los <c>ingredientes[].alimentoId</c>, consulta sus nombres en PostgreSQL y actualiza solo
/// las copias que difieren ( <c>$set</c> con <c>arrayFilters</c> por <c>alimentoId</c> ), avanzando <c>actualizadoEn</c> a UTC.
/// Una copia ausente no se rellena. Si un <c>alimentoId</c> ya no existe, se registra <c>Warning</c> y no se toca.</item>
/// <item>Si C1 del ADR-009 fue aceptado: compara el conjunto de <c>alimentoId</c> con <c>Compone</c> y cuenta/loguea <see cref="ResultadoConciliacion.DivergenciasRm06"/>; nunca corrige.</item>
/// </list>
/// </para>
/// <para>Idempotente: una segunda ejecución inmediata devuelve ceros en <c>HuerfanasEliminadas</c> y <c>NombresActualizados</c>.</para>
/// </remarks>
/// <param name="contextoMongo">Contexto de MongoDB.</param>
/// <param name="contextoPostgres">Contexto de PostgreSQL (para consultas de solo lectura).</param>
/// <param name="opciones">Opciones de conciliación (lote, etc.).</param>
/// <param name="logger">Logger.</param>
public sealed class FichasConciliador(
    MenusMongoContext contextoMongo,
    MenusDbContext contextoPostgres,
    IOptions<ConciliacionOptions> opciones,
    ILogger<FichasConciliador> logger) : IFichasConciliador
{
    private readonly MenusMongoContext _mongo = contextoMongo;
    private readonly MenusDbContext _postgres = contextoPostgres;
    private readonly ConciliacionOptions _opciones = opciones.Value;
    private readonly ILogger<FichasConciliador> _logger = logger;

    /// <inheritdoc />
    public async Task<ResultadoConciliacion> ConciliarAsync(CancellationToken cancellationToken = default)
    {
        var revisadas = 0;
        var huérfanasEliminadas = 0;
        var nombresActualizados = 0;
        var fichasInvalidas = 0;
        var divergenciasRm06 = 0;

        var tamanoLote = _opciones.TamanoLote;
        ObjectId? ultimoId = null;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Leer lote de MongoDB ordenado por _id ascendente
            var filtro = Builders<FichaNutricionalDocumento>.Filter.Empty;
            if (ultimoId.HasValue)
            {
                filtro = Builders<FichaNutricionalDocumento>.Filter.Gt(f => f.Id, ultimoId.Value);
            }

            var lote = await _mongo.Fichas
                .Find(filtro)
                .SortBy(f => f.Id)
                .Limit(tamanoLote)
                .ToListAsync(cancellationToken);

            if (lote.Count == 0)
            {
                break; // No hay más fichas
            }

            // Actualizar último _id para la siguiente iteración
            ultimoId = lote[^1].Id;

            // Procesar el lote
            var (revisadasLote, huérfanasLote, nombresLote, invalidasLote, divergenciasLote) =
                await ProcesarLoteAsync(lote, cancellationToken);

            revisadas += revisadasLote;
            huérfanasEliminadas += huérfanasLote;
            nombresActualizados += nombresLote;
            fichasInvalidas += invalidasLote;
            divergenciasRm06 += divergenciasLote;
        }

        _logger.LogInformation(
            "Conciliación finalizada: Revisadas={Revisadas}, HuerfanasEliminadas={Huerfanas}, NombresActualizados={Nombres}, FichasInvalidas={Invalidas}, DivergenciasRm06={Divergencias}",
            revisadas, huérfanasEliminadas, nombresActualizados, fichasInvalidas, divergenciasRm06);

        return new ResultadoConciliacion(
            revisadas,
            huérfanasEliminadas,
            nombresActualizados,
            fichasInvalidas,
            divergenciasRm06);
    }

    private async Task<(int Revisadas, int Huerfanas, int Nombres, int Invalidas, int Divergencias)>
        ProcesarLoteAsync(List<FichaNutricionalDocumento> lote, CancellationToken cancellationToken)
    {
        var revisadas = 0;
        var huérfanasIds = new List<ObjectId>();
        var invalidas = 0;
        var nombresActualizados = 0;
        var divergenciasRm06 = 0;

        // Parsear GUIDs y separar válidas de inválidas
        var idsValidas = new Dictionary<ObjectId, (Guid Id, string Tabla, FichaNutricionalDocumento Ficha)>();

        foreach (var ficha in lote)
        {
            revisadas++;

            if (!Guid.TryParseExact(ficha.Origen.Id, "D", out var itemId))
            {
                invalidas++;
                _logger.LogWarning(
                    "Ficha {FichaId} tiene origen.id no parseable: {OrigenId}",
                    ficha.Id, ficha.Origen.Id);
                continue;
            }

            idsValidas[ficha.Id] = (itemId, ficha.Origen.Tabla, ficha);
        }

        if (idsValidas.Count == 0)
        {
            return (revisadas, 0, 0, invalidas, 0);
        }

        // Consultar PostgreSQL: id, nombre, clase (discriminador)
        var ids = idsValidas.Values.Select(v => v.Id).ToList();
        var items = await _postgres.ItemsAlimentarios
            .AsNoTracking()
            .Where(i => ids.Contains(i.Id))
            .Select(i => new { i.Id, i.Nombre, Clase = EF.Property<string>(i, "Clase") })
            .ToDictionaryAsync(i => i.Id, cancellationToken);

        // Separar huérfanas (no existe o clase distinta) de válidas
        var fichasPlatoValidas = new List<(ObjectId FichaId, FichaNutricionalDocumento Ficha, Guid PlatoId)>();

        foreach (var (fichaId, (itemId, tabla, ficha)) in idsValidas)
        {
            if (!items.TryGetValue(itemId, out var item) ||
                !string.Equals(item.Clase, tabla, StringComparison.OrdinalIgnoreCase))
            {
                huérfanasIds.Add(fichaId);
                continue;
            }

            // Ficha válida; si es plato y tiene ingredientes, la procesamos para nombres
            if (string.Equals(tabla, "Plato", StringComparison.OrdinalIgnoreCase) && ficha.Ingredientes?.Count > 0)
            {
                fichasPlatoValidas.Add((fichaId, ficha, itemId));
            }
        }

        // Eliminar huérfanas con un solo DeleteMany
        if (huérfanasIds.Count > 0)
        {
            var filtroHuérfanas = Builders<FichaNutricionalDocumento>.Filter.In(f => f.Id, huérfanasIds);
            await _mongo.Fichas.DeleteManyAsync(filtroHuérfanas, cancellationToken);
        }

        // Procesar fichas de plato: actualizar nombres de ingredientes
        if (fichasPlatoValidas.Count > 0)
        {
            // Recopilar todos los alimentoId únicos
            var alimentoIdsStr = fichasPlatoValidas
                .SelectMany(t => t.Ficha.Ingredientes!.Select(i => i.AlimentoId))
                .Distinct()
                .ToList();

            var alimentoIds = new List<Guid>();
            var alimentoIdParseErrors = new List<string>();

            foreach (var idStr in alimentoIdsStr)
            {
                if (Guid.TryParseExact(idStr, "D", out var g))
                {
                    alimentoIds.Add(g);
                }
                else
                {
                    alimentoIdParseErrors.Add(idStr);
                }
            }

            if (alimentoIdParseErrors.Count > 0)
            {
                _logger.LogWarning(
                    "Ingredientes con alimentoId no parseable en fichas de plato: {Ids}",
                    string.Join(", ", alimentoIdParseErrors));
            }

            // Consultar nombres de alimentos en PostgreSQL
            var nombresAlimentos = new Dictionary<Guid, string>();
            if (alimentoIds.Count > 0)
            {
                nombresAlimentos = await _postgres.ItemsAlimentarios
                    .AsNoTracking()
                    .Where(i => alimentoIds.Contains(i.Id))
                    .Select(i => new { i.Id, i.Nombre })
                    .ToDictionaryAsync(i => i.Id, i => i.Nombre, cancellationToken);
            }

            // Para cada ficha de plato, actualizar solo los nombres que difieren
            foreach (var (fichaId, ficha, platoId) in fichasPlatoValidas)
            {
                var tieneCambios = false;

                for (int i = 0; i < ficha.Ingredientes!.Count; i++)
                {
                    var ingrediente = ficha.Ingredientes[i];
                    if (!Guid.TryParseExact(ingrediente.AlimentoId, "D", out var alimentoId))
                    {
                        continue; // Ya se logueó arriba
                    }

                    if (!nombresAlimentos.TryGetValue(alimentoId, out var nombrePostgres))
                    {
                        // El alimento ya no existe en PostgreSQL (RR-04 lo hace imposible en condiciones normales)
                        _logger.LogWarning(
                            "Alimento {AlimentoId} referenciado en ficha de plato {PlatoId} no existe en PostgreSQL",
                            alimentoId, platoId);
                        continue;
                    }

                    if (ingrediente.Nombre is not null && !string.Equals(ingrediente.Nombre, nombrePostgres, StringComparison.Ordinal))
                    {
                        // El nombre difiere: actualizar solo esta copia
                        var filtroArray = Builders<FichaNutricionalDocumento>.Filter.ElemMatch(
                            f => f.Ingredientes,
                            ing => ing.AlimentoId == ingrediente.AlimentoId);

                        var actualizacion = Builders<FichaNutricionalDocumento>.Update
                            .Set("ingredientes.$[elem].nombre", nombrePostgres)
                            .Set(f => f.ActualizadoEn, DateTime.UtcNow);

                        var opcionesArray = new UpdateOptions
                        {
                            ArrayFilters = new[]
                            {
                                new BsonDocumentArrayFilterDefinition<BsonDocument>(
                                    new BsonDocument("elem.alimentoId", ingrediente.AlimentoId))
                            }
                        };

                        // Ejecutar UpdateOne para esta ficha y este ingrediente
                        await _mongo.Fichas.UpdateOneAsync(
                            Builders<FichaNutricionalDocumento>.Filter.Eq(f => f.Id, fichaId) & filtroArray,
                            actualizacion,
                            opcionesArray,
                            cancellationToken);

                        tieneCambios = true;
                    }
                }

                if (tieneCambios)
                {
                    nombresActualizados++;
                }
            }

            // Verificar divergencias RM-06 si C1 fue aceptado (ADR-009)
            // El ADR-009 dice que C1 fue recomendado y aceptado, así que informamos de divergencias
            if (fichasPlatoValidas.Count > 0)
            {
                var platoIds = fichasPlatoValidas.Select(t => t.PlatoId).Distinct().ToList();

                // Consultar Compone para estos platos
                var composicion = await _postgres.PlatosIngredientes
                    .AsNoTracking()
                    .Where(p => platoIds.Contains(p.PlatoId))
                    .Select(p => new { p.PlatoId, p.AlimentoId })
                    .ToListAsync(cancellationToken);

                var componePorPlato = composicion
                    .GroupBy(c => c.PlatoId)
                    .ToDictionary(g => g.Key, g => g.Select(c => c.AlimentoId).ToHashSet());

                foreach (var (fichaId, ficha, platoId) in fichasPlatoValidas)
                {
                    var alimentosEnFicha = ficha.Ingredientes!
                        .Where(i => Guid.TryParseExact(i.AlimentoId, "D", out _))
                        .Select(i => Guid.ParseExact(i.AlimentoId, "D"))
                        .ToHashSet();

                    if (componePorPlato.TryGetValue(platoId, out var alimentosCompone))
                    {
                        if (!alimentosEnFicha.SetEquals(alimentosCompone))
                        {
                            divergenciasRm06++;
                            _logger.LogWarning(
                                "Divergencia RM-06 en ficha de plato {PlatoId}: ingredientes en ficha={FichaCount}, en Compone={ComponeCount}",
                                platoId, alimentosEnFicha.Count, alimentosCompone.Count);
                        }
                    }
                }
            }
        }

        return (revisadas, huérfanasIds.Count, nombresActualizados, invalidas, divergenciasRm06);
    }
}
