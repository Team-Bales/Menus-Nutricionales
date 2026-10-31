using Xunit;

namespace Menus.Infrastructure.IntegrationTests.Fixtures;

/// <summary>
/// Definición de colección xUnit que comparte una única instancia de <see cref="PostgresFixture"/>
/// entre todas las pruebas de integración relacionales.
/// </summary>
/// <remarks>
/// Las clases de prueba que necesiten la base de datos PostgreSQL real deben decorarse con
/// <c>[Collection("PostgreSQL")]</c> e implementar <see cref="IAsyncLifetime"/> llamando a
/// <see cref="PostgresFixture.ReiniciarAsync"/> en <see cref="IAsyncLifetime.InitializeAsync"/>.
/// Esto garantiza que cada clase de prueba empieza con las tablas de datos vacías, mientras que
/// los catálogos (sembrados por <c>InitialCreate</c>) y el historial de migraciones se conservan.
/// </remarks>
[CollectionDefinition("PostgreSQL")]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;
