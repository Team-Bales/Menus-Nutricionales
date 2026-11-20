using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Fixtures;

/// <summary>
/// Definición de colección xUnit que comparte una instancia de <see cref="PostgresFixture"/> y una de
/// <see cref="RedisFixture"/> entre las pruebas que necesitan las dos a la vez, como el cache-aside de la
/// disponibilidad calórica (#89; informe, «Consultas recurrentes»).
/// </summary>
[CollectionDefinition("PostgreSQLyRedis")]
public sealed class PostgresYRedisCollection : ICollectionFixture<PostgresFixture>, ICollectionFixture<RedisFixture>;
