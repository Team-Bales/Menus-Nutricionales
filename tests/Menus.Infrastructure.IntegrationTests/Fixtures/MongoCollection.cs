using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Fixtures;

/// <summary>
/// Definición de colección xUnit que comparte una única instancia de <see cref="MongoFixture"/> entre las pruebas
/// de MongoDB. Corre en paralelo con la colección de PostgreSQL.
/// </summary>
[CollectionDefinition("MongoDB")]
public sealed class MongoCollection : ICollectionFixture<MongoFixture>;
