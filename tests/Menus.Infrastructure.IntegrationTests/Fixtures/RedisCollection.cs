using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Fixtures;

/// <summary>
/// Definición de colección xUnit que comparte una única instancia de <see cref="RedisFixture"/> entre las
/// pruebas de Redis. Corre en paralelo con las colecciones de PostgreSQL y de MongoDB.
/// </summary>
[CollectionDefinition("Redis")]
public sealed class RedisCollection : ICollectionFixture<RedisFixture>;
