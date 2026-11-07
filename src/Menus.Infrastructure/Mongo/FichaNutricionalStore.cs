using MongoDB.Driver;

namespace Menus.Infrastructure.Mongo;

/// <summary>Implementación de <see cref="IFichaNutricionalStore"/> sobre <see cref="MenusMongoContext.Fichas"/>.</summary>
/// <param name="contexto">El contexto de MongoDB.</param>
public sealed class FichaNutricionalStore(MenusMongoContext contexto) : IFichaNutricionalStore
{
    public async Task<bool> EliminarPorOrigenAsync(string tabla, Guid itemId, CancellationToken cancellationToken = default)
    {
        var filtro = Builders<FichaNutricionalDocumento>.Filter.And(
            Builders<FichaNutricionalDocumento>.Filter.Eq(f => f.Origen.Tabla, tabla),
            Builders<FichaNutricionalDocumento>.Filter.Eq(f => f.Origen.Id, FichaNutricionalEsquema.Identificador(itemId)));

        var resultado = await contexto.Fichas.DeleteOneAsync(filtro, cancellationToken);
        return resultado.DeletedCount > 0;
    }
}
