using Menus.Application.IA;

namespace Menus.Application.Tests.IA;

/// <summary>Pruebas de la regla de falla cerrada para proveedores externos de IA (#333).</summary>
public sealed class ValidadorDeConfiguracionIATests
{
    [Fact]
    public void IA_deshabilitada_es_valida_aun_sin_terminos()
    {
        var opciones = new IAOptions { Habilitada = false, BaseUrl = "https://proveedor.test/v1" };

        var valida = ValidadorDeConfiguracionIA.TryValidar(opciones, out var error);

        Assert.True(valida);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("http://localhost:11434/v1")]
    [InlineData("http://127.0.0.1:11434/v1")]
    [InlineData("http://ollama:11434/v1")]
    public void Ollama_local_es_valido_sin_terminos(string baseUrl)
    {
        var opciones = new IAOptions { Habilitada = true, BaseUrl = baseUrl };

        var valida = ValidadorDeConfiguracionIA.TryValidar(opciones, out var error);

        Assert.True(valida);
        Assert.Null(error);
        Assert.False(ValidadorDeConfiguracionIA.UsaProveedorExterno(opciones));
    }

    [Fact]
    public void Proveedor_externo_sin_terminos_no_es_valido()
    {
        var opciones = new IAOptions { Habilitada = true, BaseUrl = "https://api.proveedor.test/v1" };

        var valida = ValidadorDeConfiguracionIA.TryValidar(opciones, out var error);

        Assert.False(valida);
        Assert.Contains("Terminos:Version=1", error);
    }

    [Fact]
    public void Proveedor_externo_con_los_terminos_vigentes_es_valido()
    {
        var opciones = new IAOptions
        {
            Habilitada = true,
            BaseUrl = "https://api.proveedor.test/v1",
            Terminos = new TerminosIAOptions
            {
                Version = ValidadorDeConfiguracionIA.VersionDeTerminosVigente,
                AceptadoPor = "Responsable del despliegue",
                Fecha = new DateOnly(2026, 11, 28),
                RetencionDesactivada = true,
            },
        };

        var valida = ValidadorDeConfiguracionIA.TryValidar(opciones, out var error);

        Assert.True(valida);
        Assert.Null(error);
        Assert.True(ValidadorDeConfiguracionIA.UsaProveedorExterno(opciones));
    }

    [Fact]
    public void BaseUrl_invalida_falla_cerrada_cuando_se_habilita_la_ia()
    {
        var opciones = new IAOptions { Habilitada = true, BaseUrl = "no-es-una-url" };

        var valida = ValidadorDeConfiguracionIA.TryValidar(opciones, out var error);

        Assert.False(valida);
        Assert.Equal("IA:BaseUrl debe ser una URL absoluta cuando IA:Habilitada=true.", error);
    }
}
