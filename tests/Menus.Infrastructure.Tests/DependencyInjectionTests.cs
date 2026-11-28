using Menus.Application.Catalogo;
using Menus.Application.Catalogo.Fichas;
using Menus.Application.Comun;
using Menus.Application.IA;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;
using Menus.Infrastructure.Cache;
using Menus.Infrastructure.IA;
using Menus.Infrastructure.Mongo;
using Menus.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Menus.Infrastructure.Tests;

public sealed class DependencyInjectionTests
{
    private const string Key = "clave-de-prueba-con-mas-de-32-bytes-1234567890";

    private static IConfiguration CreateConfiguration(
        string? jwtKey = Key,
        string? mongo = null,
        string? redis = null,
        bool? conciliacionHabilitada = null,
        bool? iaHabilitada = null,
        string? iaModel = null,
        string? iaBaseUrl = null,
        int? iaTimeoutSegundos = null)
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

        if (redis is not null)
        {
            valores["ConnectionStrings:Redis"] = redis;
        }

        if (conciliacionHabilitada is not null)
        {
            valores["Conciliacion:Habilitada"] = conciliacionHabilitada.Value ? "true" : "false";
        }

        if (iaHabilitada is not null)
        {
            valores["IA:Habilitada"] = iaHabilitada.Value ? "true" : "false";
        }

        if (iaModel is not null)
        {
            valores["IA:Model"] = iaModel;
        }

        if (iaBaseUrl is not null)
        {
            valores["IA:BaseUrl"] = iaBaseUrl;
        }

        if (iaTimeoutSegundos is not null)
        {
            valores["IA:TimeoutSegundos"] = iaTimeoutSegundos.Value.ToString();
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
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ISalaRepository>());
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

    [Fact]
    public void AddInfrastructure_ConMongo_LaFichaSeLeeYSeEscribeEnMongo_YLaColeccionSeInicializaAlArrancar()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddInfrastructure(CreateConfiguration(mongo: "mongodb://localhost:27017/menus_prueba"));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<AlmacenDeFichasEnMongo>(scope.ServiceProvider.GetRequiredService<IAlmacenDeFichas>());
        Assert.Contains(provider.GetServices<IHostedService>(), s => s is InicializacionDeFichasHostedService);
    }

    [Fact]
    public void AddInfrastructure_SinMongo_LaFichaNoEstaDisponible_YNoSeInicializaNada()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddInfrastructure(CreateConfiguration());

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<SinAlmacenDeFichas>(scope.ServiceProvider.GetRequiredService<IAlmacenDeFichas>());
        Assert.DoesNotContain(provider.GetServices<IHostedService>(), s => s is InicializacionDeFichasHostedService);
    }

    [Fact]
    public void AddInfrastructure_ConRedis_RegistraRedisCache()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddInfrastructure(CreateConfiguration(redis: "localhost:6379"));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<RedisCache>(scope.ServiceProvider.GetRequiredService<ICache>());
    }

    [Fact]
    public void AddInfrastructure_SinRedis_RegistraCacheNula()
    {
        var services = new ServiceCollection();

        services.AddInfrastructure(CreateConfiguration());

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<CacheNula>(scope.ServiceProvider.GetRequiredService<ICache>());
    }

    [Fact]
    public void AddInfrastructure_ConMongo_YConciliacionHabilitada_RegistraElServicioHospedado()
    {
        var services = new ServiceCollection();
        services.AddLogging(); // el servicio hospedado pide un ILogger<ConciliacionHostedService>

        services.AddInfrastructure(CreateConfiguration(
            mongo: "mongodb://localhost:27017/menus_prueba",
            conciliacionHabilitada: true));

        using var provider = services.BuildServiceProvider();

        Assert.Contains(
            provider.GetServices<IHostedService>(),
            servicio => servicio is ConciliacionHostedService);
    }

    [Fact]
    public void AddInfrastructure_ConIaDeshabilitada_RegistraElAsistenteDeshabilitado()
    {
        var services = new ServiceCollection();

        services.AddInfrastructure(CreateConfiguration(iaHabilitada: false));

        using var provider = services.BuildServiceProvider();

        Assert.IsType<AsistenteIADeshabilitado>(provider.GetRequiredService<IAsistenteIA>());
        Assert.Null(provider.GetService<IHttpClientFactory>());
    }

    [Fact]
    public void AddInfrastructure_ConIaHabilitada_RegistraElAdaptador()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddInfrastructure(CreateConfiguration(iaHabilitada: true, iaModel: "llama3"));

        using var provider = services.BuildServiceProvider();

        Assert.IsType<AsistenteIAOpenAICompatible>(provider.GetRequiredService<IAsistenteIA>());
    }

    [Fact]
    public void AddInfrastructure_ConProveedorExterno_RegistraElAdaptador()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddInfrastructure(CreateConfiguration(
            iaHabilitada: true,
            iaModel: "gpt-4o-mini",
            iaBaseUrl: "https://api.openai.com/v1"));

        using var provider = services.BuildServiceProvider();

        Assert.IsType<AsistenteIAOpenAICompatible>(provider.GetRequiredService<IAsistenteIA>());
    }

    [Fact]
    public void AddInfrastructure_ConIaHabilitadaSinModelo_Lanza()
    {
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(
            () => services.AddInfrastructure(CreateConfiguration(iaHabilitada: true)));
    }

    [Theory]
    [InlineData("ftp://localhost/v1", 30)]
    [InlineData("no-es-una-url", 30)]
    [InlineData("http://localhost:11434/v1", 0)]
    public void AddInfrastructure_ConIaHabilitadaYConfiguracionInvalida_Lanza(string baseUrl, int timeoutSegundos)
    {
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(() => services.AddInfrastructure(CreateConfiguration(
            iaHabilitada: true,
            iaModel: "llama3",
            iaBaseUrl: baseUrl,
            iaTimeoutSegundos: timeoutSegundos)));
    }
}
