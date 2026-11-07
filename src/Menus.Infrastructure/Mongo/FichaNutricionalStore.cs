using MongoDB.Driver;

namespace Menus.Infrastructure.Mongo;

/// <summary>
/// Implementación de <see cref="IFichaNutricionalStore"/> y de <see cref="IDesgloseDeIngredientes"/> sobre
/// <see cref="MenusMongoContext.Fichas"/>.
/// </summary>
/// <param name="contexto">El contexto de MongoDB.</param>
public sealed class FichaNutricionalStore(MenusMongoContext contexto) : IFichaNutricionalStore, IDesgloseDeIngredientes
{
    private static FilterDefinitionBuilder<FichaNutricionalDocumento> Filtro => Builders<FichaNutricionalDocumento>.Filter;

    public async Task<bool> EliminarPorOrigenAsync(string tabla, Guid itemId, CancellationToken cancellationToken = default)
    {
        var resultado = await contexto.Fichas.DeleteOneAsync(PorOrigen(tabla, itemId), cancellationToken);
        return resultado.DeletedCount > 0;
    }

    public async Task<bool> AgregarIngredienteAsync(
        Guid platoId,
        Guid alimentoId,
        string nombre,
        CancellationToken cancellationToken = default)
    {
        var id = FichaNutricionalEsquema.Identificador(alimentoId);

        // Con desglose y sin ese alimento: si la ficha no tiene desglose, uno con un solo ingrediente no coincidiría
        // con la composición (R8-06); y la condición sobre el alimento hace la operación idempotente.
        var filtro = Filtro.And(
            PorOrigen("Plato", platoId),
            Filtro.Exists(f => f.Ingredientes),
            Filtro.Not(Filtro.ElemMatch(f => f.Ingredientes, i => i.AlimentoId == id)));

        var cambios = Builders<FichaNutricionalDocumento>.Update
            .Push(f => f.Ingredientes, new IngredienteFicha { AlimentoId = id, Nombre = nombre })
            .Set(f => f.ActualizadoEn, DateTime.UtcNow);

        var resultado = await contexto.Fichas.UpdateOneAsync(filtro, cambios, cancellationToken: cancellationToken);
        return resultado.ModifiedCount > 0;
    }

    public async Task<bool> QuitarIngredienteAsync(Guid platoId, Guid alimentoId, CancellationToken cancellationToken = default)
    {
        var id = FichaNutricionalEsquema.Identificador(alimentoId);

        var filtro = Filtro.And(
            PorOrigen("Plato", platoId),
            Filtro.ElemMatch(f => f.Ingredientes, i => i.AlimentoId == id));

        var cambios = Builders<FichaNutricionalDocumento>.Update
            .PullFilter(f => f.Ingredientes, i => i.AlimentoId == id)
            .Set(f => f.ActualizadoEn, DateTime.UtcNow);

        var resultado = await contexto.Fichas.UpdateOneAsync(filtro, cambios, cancellationToken: cancellationToken);
        return resultado.ModifiedCount > 0;
    }

    /// <summary>La ficha de un ítem, por su origen en PostgreSQL (RM-08: hay a lo sumo una).</summary>
    private static FilterDefinition<FichaNutricionalDocumento> PorOrigen(string tabla, Guid itemId) => Filtro.And(
        Filtro.Eq(f => f.Origen.Tabla, tabla),
        Filtro.Eq(f => f.Origen.Id, FichaNutricionalEsquema.Identificador(itemId)));
}
