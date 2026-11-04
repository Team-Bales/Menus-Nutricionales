using MongoDB.Bson;
using MongoDB.Driver;

namespace Menus.Infrastructure.Mongo;

/// <summary>
/// Acceso a la base de MongoDB del proyecto: hoy, la colección de fichas nutricionales (RF-04, ADR-005).
/// </summary>
/// <remarks>
/// <see cref="InicializarAsync"/> deja la colección con su validador y su índice único, y se puede llamar las veces
/// que haga falta. No se llama al registrar el contexto en la inyección de dependencias: lo hace quien aloja el
/// proceso (la API al arrancar, o las pruebas), cuando esté definido.
/// </remarks>
public sealed class MenusMongoContext
{
    /// <summary>Código de MongoDB cuando la colección ya existe (<c>NamespaceExists</c>).</summary>
    private const int CodigoColeccionExistente = 48;

    private readonly IMongoDatabase _baseDeDatos;

    /// <summary>Crea el contexto sobre una base de MongoDB.</summary>
    /// <param name="cliente">El cliente de MongoDB, que se registra como singleton.</param>
    /// <param name="nombreBaseDeDatos">El nombre de la base (el de <c>MONGO_DB</c> en el <c>.env</c>).</param>
    /// <exception cref="ArgumentNullException">Si <paramref name="cliente"/> es nulo.</exception>
    /// <exception cref="ArgumentException">Si <paramref name="nombreBaseDeDatos"/> está vacío.</exception>
    public MenusMongoContext(IMongoClient cliente, string nombreBaseDeDatos)
    {
        ArgumentNullException.ThrowIfNull(cliente);
        ArgumentException.ThrowIfNullOrWhiteSpace(nombreBaseDeDatos);
        _baseDeDatos = cliente.GetDatabase(nombreBaseDeDatos);
    }

    /// <summary>La colección de fichas nutricionales.</summary>
    public IMongoCollection<FichaNutricionalDocumento> Fichas
        => _baseDeDatos.GetCollection<FichaNutricionalDocumento>(FichaNutricionalEsquema.NombreColeccion);

    /// <summary>
    /// Deja la colección de fichas con el validador vigente y el índice único de RM-08. Es idempotente.
    /// </summary>
    /// <remarks>
    /// Si la colección no existe, la crea con el validador, <c>validationLevel = strict</c> y
    /// <c>validationAction = error</c>. Si ya existe, le aplica el mismo validador con <c>collMod</c>, así que una
    /// versión nueva del esquema se aplica sin borrar datos. En los dos casos crea el índice único, y crearlo de nuevo
    /// con la misma definición no hace nada.
    /// </remarks>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Una tarea que termina cuando la colección y el índice están listos.</returns>
    public async Task InicializarAsync(CancellationToken cancellationToken = default)
    {
        if (await ExisteColeccionAsync(cancellationToken))
        {
            await AplicarValidadorAsync("collMod", cancellationToken);
        }
        else
        {
            try
            {
                await AplicarValidadorAsync("create", cancellationToken);
            }
            catch (MongoCommandException ex) when (ex.Code == CodigoColeccionExistente)
            {
                // Otro proceso la creó entre la comprobación y la creación: basta con aplicarle el validador.
                await AplicarValidadorAsync("collMod", cancellationToken);
            }
        }

        var clave = Builders<FichaNutricionalDocumento>.IndexKeys
            .Ascending("origen.tabla")
            .Ascending("origen.id");
        var opciones = new CreateIndexOptions { Unique = true, Name = FichaNutricionalEsquema.NombreIndiceOrigen };
        await Fichas.Indexes.CreateOneAsync(
            new CreateIndexModel<FichaNutricionalDocumento>(clave, opciones),
            cancellationToken: cancellationToken);
    }

    private async Task<bool> ExisteColeccionAsync(CancellationToken cancellationToken)
    {
        var filtro = new BsonDocument("name", FichaNutricionalEsquema.NombreColeccion);
        using var nombres = await _baseDeDatos.ListCollectionNamesAsync(
            new ListCollectionNamesOptions { Filter = filtro },
            cancellationToken);
        return await nombres.AnyAsync(cancellationToken);
    }

    private Task AplicarValidadorAsync(string comando, CancellationToken cancellationToken)
    {
        var orden = new BsonDocument
        {
            { comando, FichaNutricionalEsquema.NombreColeccion },
            { "validator", FichaNutricionalEsquema.Validador() },
            { "validationLevel", "strict" },
            { "validationAction", "error" },
        };
        return _baseDeDatos.RunCommandAsync<BsonDocument>(orden, cancellationToken: cancellationToken);
    }
}
