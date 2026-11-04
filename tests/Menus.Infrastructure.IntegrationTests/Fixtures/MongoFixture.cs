using System;
using System.Threading.Tasks;
using Menus.Infrastructure.Mongo;
using MongoDB.Driver;
using Testcontainers.MongoDb;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Fixtures;

/// <summary>
/// Fixture de xUnit que levanta un contenedor MongoDB 7 (la imagen de <c>docker-compose.yml</c>) con Testcontainers.
/// </summary>
/// <remarks>
/// Cada prueba pide su propio contexto con <see cref="NuevoContextoAsync"/>. El contexto usa una base con nombre
/// único y ya inicializada, así que no hace falta limpiar entre pruebas. El contenedor se comparte por colección
/// (ver <see cref="MongoCollection"/>).
/// </remarks>
public sealed class MongoFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _contenedor = new MongoDbBuilder().WithImage("mongo:7").Build();

    /// <summary>El cliente de MongoDB conectado al contenedor.</summary>
    public IMongoClient Cliente { get; private set; } = null!;

    /// <summary>Arranca el contenedor y crea el cliente.</summary>
    public async Task InitializeAsync()
    {
        await _contenedor.StartAsync();
        Cliente = new MongoClient(_contenedor.GetConnectionString());
    }

    /// <summary>
    /// Crea un contexto sobre una base nueva, con la colección de fichas ya inicializada.
    /// </summary>
    /// <returns>El contexto y la base, para insertar documentos que no pasan por el tipo de la ficha.</returns>
    public async Task<(MenusMongoContext Contexto, IMongoDatabase BaseDeDatos)> NuevoContextoAsync()
    {
        var nombre = $"menus_{Guid.NewGuid():N}";
        var contexto = new MenusMongoContext(Cliente, nombre);
        await contexto.InicializarAsync();
        return (contexto, Cliente.GetDatabase(nombre));
    }

    /// <summary>Detiene y elimina el contenedor.</summary>
    public async Task DisposeAsync() => await _contenedor.DisposeAsync();
}
