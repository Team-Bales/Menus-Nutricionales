using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;
using Menus.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Infrastructure.Tests;

public sealed class DependencyInjectionTests
{
    private const string Key = "clave-de-prueba-con-mas-de-32-bytes-1234567890";

    private static IConfiguration CreateConfiguration(string? jwtKey = Key) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Database=menus;Username=menus;Password=menus",
                ["Jwt:Key"] = jwtKey,
                ["Jwt:Issuer"] = "menus-nutricionales-api",
                ["Jwt:ExpiresMinutes"] = "60",
            })
            .Build();

    [Fact]
    public void AddInfrastructure_RegistersRepositoriesAndJwtTokenService()
    {
        var services = new ServiceCollection();

        services.AddInfrastructure(CreateConfiguration());

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<INutricionistaRepository>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IPacienteRepository>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IRepository<Sala, Guid>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<JwtTokenService>());
    }

    [Fact]
    public void AddInfrastructure_WithoutJwtKey_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(
            () => services.AddInfrastructure(CreateConfiguration(jwtKey: null)));
    }
}
