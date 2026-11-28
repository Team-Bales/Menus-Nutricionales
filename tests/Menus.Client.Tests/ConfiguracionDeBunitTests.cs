using Bunit;

namespace Menus.Client.Tests;

/// <summary>Comprueba que <see cref="ConfiguracionDeBunit"/> se aplica a las pruebas del cliente (#366).</summary>
public sealed class ConfiguracionDeBunitTests
{
    [Fact]
    public void La_espera_por_defecto_de_bUnit_es_de_cinco_segundos()
        => Assert.Equal(TimeSpan.FromSeconds(5), BunitContext.DefaultWaitTimeout);
}
