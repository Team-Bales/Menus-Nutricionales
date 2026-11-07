using Menus.Application.Catalogo;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;
using Menus.Infrastructure.Mongo;
using Menus.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Menus.Infrastructure.Tests;

public sealed class DependencyInjectionTests
{
    private const string Key = "clave-de-prueba-con-mas-de-32-bytes-1234567890";

    private static IConfiguration CreateConfiguration(string? jwtKey = Key, string? mongo = null)
    {
        var valores = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = "Host=localhost;Database=menus;Username=menus;Password=menus",
            ["Jwt:Key"] = jwtKey,
            ["Jwt:Issuer"] = "menus-nutricionales-api",
            ["Jwt:ExpiresMinutes"] = "60",
        };

        if (mongo is not null)
        {
            valores["ConnectionStrings:Mongo"] = mongo;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(valores)
            .Build();
    }

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
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IItemAlimentarioRepository>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IAlimentoRepository>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IPlatoRepository>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IDietaRepository>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IUnitOfWork>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<JwtTokenService>());
    }

    [Fact]
    public void AddInfrastructure_WithoutJwtKey_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(
            () => services.AddInfrastructure(CreateConfiguration(jwtKey: null)));
    }

    [Fact]
    public void AddInfrastructure_ConMongo_RegistraElBorradoCoordinadoYElAlmacenDeFichas()
    {
        var services = new ServiceCollection();
        services.AddLogging(); // la baja coordinada pide un ILogger<BajaCoordinadaDeItems>

        services.AddInfrastructure(CreateConfiguration(mongo: "mongodb://localhost:27017/menus_prueba"));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<FichaNutricionalStore>(scope.ServiceProvider.GetRequiredService<IFichaNutricionalStore>());
        Assert.IsType<BajaCoordinadaDeItems>(scope.ServiceProvider.GetRequiredService<BajaCoordinadaDeItems>());
    }

    [Fact]
    public void AddInfrastructure_SinMongo_NoRegistraLaBajaCoordinada()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddInfrastructure(CreateConfiguration());

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.Throws<InvalidOperationException>(
            () => scope.ServiceProvider.GetRequiredService<IFichaNutricionalStore>());
    }

    [Fact]
    public void AddInfrastructure_ConMongo_LosCasosDeUsoMantienenLasFichasEnMongo()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddInfrastructure(CreateConfiguration(mongo: "mongodb://localhost:27017/menus_prueba"));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<FichasNutricionalesEnMongo>(scope.ServiceProvider.GetRequiredService<IFichasNutricionales>());
        Assert.IsType<FichaNutricionalStore>(scope.ServiceProvider.GetRequiredService<IDesgloseDeIngredientes>());
    }

    [Fact]
    public void AddInfrastructure_SinMongo_LosCasosDeUsoNoTienenFichasQueMantener()
    {
        var services = new ServiceCollection();

        services.AddInfrastructure(CreateConfiguration());

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<SinFichasNutricionales>(scope.ServiceProvider.GetRequiredService<IFichasNutricionales>());
    }
}
