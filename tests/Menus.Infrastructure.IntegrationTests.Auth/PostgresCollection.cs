using Menus.Infrastructure.IntegrationTests.Fixtures;
using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Auth;

/// <summary>
/// Definición de colección xUnit para las pruebas de integración de autenticación.
/// Comparte la misma instancia de <see cref="PostgresFixture"/> que el resto de pruebas de integración.
/// </summary>
[CollectionDefinition("PostgreSQL")]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;
