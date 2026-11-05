using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Fixtures;

/// <summary>
/// Definición de colección xUnit que comparte una instancia de <see cref="PostgresFixture"/> y una de
/// <see cref="MongoFixture"/> entre las pruebas que necesitan las dos bases a la vez (por ejemplo,
/// <c>BajaCoordinadaDeItemsTests</c>, RM-09).
/// </summary>
[CollectionDefinition("PostgreSQLyMongoDB")]
public sealed class PostgresYMongoCollection : ICollectionFixture<PostgresFixture>, ICollectionFixture<MongoFixture>;
