namespace Menus.Application.Tests.IA;

using System.Reflection;
using System.Text.Json;
using Menus.Application.IA;
using Moq;
using Xunit;

/// <summary>
/// Pruebas de contrato para los tipos de IA: <see cref="IAsistenteIA"/>, <see cref="PromptIA"/>,
/// <see cref="AsistenteIANoDisponibleException"/>, <see cref="TareaIA"/>, <see cref="SeccionDto"/>,
/// <see cref="RespuestaIADto"/> y <see cref="IAOptions"/>.
/// </summary>
public sealed class ContratosIATests
{
    #region IAsistenteIA

    /// <summary>
    /// Verifica que <see cref="IAsistenteIA.CompletarAsync"/> compila y funciona omitiendo el token,
    /// y que se pasa <see cref="CancellationToken.None"/> cuando no se proporciona.
    /// </summary>
    [Fact]
    public async Task IAsistenteIA_CompletarAsync_CompilaYFunciona_SinTokenExplicito()
    {
        var mock = new Mock<IAsistenteIA>(MockBehavior.Strict);
        var prompt = new PromptIA("sistema", "usuario");

        mock.Setup(x => x.CompletarAsync(prompt, CancellationToken.None))
            .ReturnsAsync("respuesta")
            .Verifiable();

        var resultado = await mock.Object.CompletarAsync(prompt);

        Assert.Equal("respuesta", resultado);
        mock.Verify(x => x.CompletarAsync(prompt, CancellationToken.None), Times.Once);
    }

    #endregion

    #region PromptIA

    /// <summary>
    /// Verifica que <see cref="PromptIA"/> no tiene constructor público, sí tiene el constructor
    /// <c>internal</c> esperado, getters públicos y ningún setter público.
    /// </summary>
    [Fact]
    public void PromptIA_Estructura_ConstructorInternal_GettersPublicos_SinSetters()
    {
        var tipo = typeof(PromptIA);

        // No constructor público
        var constructoresPublicos = tipo.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        Assert.Empty(constructoresPublicos);

        // Constructor internal esperado (string, string)
        var constructorInternal = tipo.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .SingleOrDefault(c => c.GetParameters().Length == 2
                && c.GetParameters()[0].ParameterType == typeof(string)
                && c.GetParameters()[1].ParameterType == typeof(string));
        Assert.NotNull(constructorInternal);
        Assert.True(constructorInternal.IsAssembly); // internal

        // Propiedades públicas de solo lectura (get, sin set público)
        var sistemaProp = tipo.GetProperty("Sistema", BindingFlags.Public | BindingFlags.Instance);
        var usuarioProp = tipo.GetProperty("Usuario", BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(sistemaProp);
        Assert.NotNull(usuarioProp);
        Assert.True(sistemaProp!.CanRead);
        Assert.False(sistemaProp.CanWrite);
        Assert.True(usuarioProp!.CanRead);
        Assert.False(usuarioProp.CanWrite);

        // Construcción exitosa gracias a InternalsVisibleTo
        var prompt = new PromptIA("sistema", "usuario");
        Assert.Equal("sistema", prompt.Sistema);
        Assert.Equal("usuario", prompt.Usuario);
    }

    /// <summary>
    /// Verifica que <see cref="PromptIA.ToString"/> no contiene textos centinela.
    /// </summary>
    [Fact]
    public void PromptIA_ToString_NoContieneTextosCentinela()
    {
        var prompt = new PromptIA("sistema secreto", "usuario secreto");
        var toString = prompt.ToString();

        Assert.DoesNotContain("sistema secreto", toString);
        Assert.DoesNotContain("usuario secreto", toString);
        Assert.Contains("[redacted]", toString);
    }

    #endregion

    #region AsistenteIANoDisponibleException

    /// <summary>
    /// Verifica ambos constructores de <see cref="AsistenteIANoDisponibleException"/>.
    /// </summary>
    [Fact]
    public void AsistenteIANoDisponibleException_Constructores_Funcionan()
    {
        // Constructor con mensaje
        var ex1 = new AsistenteIANoDisponibleException("proveedor caído");
        Assert.Equal("proveedor caído", ex1.Message);
        Assert.Null(ex1.InnerException);

        // Constructor con mensaje y causa
        var causa = new TimeoutException("timeout");
        var ex2 = new AsistenteIANoDisponibleException("timeout", causa);
        Assert.Equal("timeout", ex2.Message);
        Assert.Same(causa, ex2.InnerException);
    }

    #endregion

    #region TareaIA

    /// <summary>
    /// Verifica que <see cref="TareaIA"/> tiene exactamente los nombres y valores esperados.
    /// </summary>
    [Fact]
    public void TareaIA_Valores_NombresYValoresCorrectos()
    {
        var nombres = Enum.GetNames<TareaIA>();
        var valores = Enum.GetValues<TareaIA>();

        Assert.Equal(new[] { "ExplicarAnalisis", "SugerirAcciones", "GuionParaPaciente" }, nombres);
        Assert.Equal(new TareaIA[] { TareaIA.ExplicarAnalisis, TareaIA.SugerirAcciones, TareaIA.GuionParaPaciente }, valores);
        Assert.Equal(0, (int)TareaIA.ExplicarAnalisis);
        Assert.Equal(1, (int)TareaIA.SugerirAcciones);
        Assert.Equal(2, (int)TareaIA.GuionParaPaciente);
    }

    #endregion

    #region SeccionDto

    /// <summary>
    /// Verifica que <see cref="SeccionDto"/> asigna campos, rechaza nulos en textos obligatorios
    /// y su <see cref="ToString"/> no vuelca contenido.
    /// </summary>
    [Fact]
    public void SeccionDto_AsignaCampos_NullLanza_ToStringSeguro()
    {
        var seccion = new SeccionDto("Título único", "Texto único");
        Assert.Equal("Título único", seccion.Titulo);
        Assert.Equal("Texto único", seccion.Texto);

        Assert.Throws<ArgumentNullException>(() => new SeccionDto(null!, "Texto"));
        Assert.Throws<ArgumentNullException>(() => new SeccionDto("Título", null!));

        var toString = seccion.ToString();
        Assert.DoesNotContain("Título único", toString);
        Assert.DoesNotContain("Texto único", toString);
        Assert.Contains("[redacted]", toString);
    }

    #endregion

    #region RespuestaIADto

    /// <summary>
    /// Verifica que <see cref="RespuestaIADto"/> asigna campos, rechaza nulos en textos obligatorios,
    /// normaliza listas null a vacías, copia defensivamente (mutar la original no cambia el DTO)
    /// y su <see cref="ToString"/> no vuelca contenido.
    /// </summary>
    [Fact]
    public void RespuestaIADto_AsignaCampos_NullEnTextosLanza_NullEnListasVacia_CopiaDefensiva_ToStringSeguro()
    {
        var secciones = new List<SeccionDto> { new("S1", "T1") };
        var recomendaciones = new List<string> { "R1" };
        var advertencias = new List<string> { "A1" };
        var fuentes = new List<string> { "F1" };

        var dto = new RespuestaIADto("Resumen único", secciones, recomendaciones, advertencias, fuentes, true);

        Assert.Equal("Resumen único", dto.Resumen);
        Assert.Single(dto.Secciones);
        Assert.Single(dto.Recomendaciones);
        Assert.Single(dto.Advertencias);
        Assert.Single(dto.FuentesUsadas);
        Assert.True(dto.GeneradoPorIA);

        // Null en texto obligatorio lanza
        Assert.Throws<ArgumentNullException>(() => new RespuestaIADto(null!, [], [], [], [], true));

        // Null en listas produce listas vacías
        var dtoVacio = new RespuestaIADto("Resumen", null, null, null, null, false);
        Assert.Empty(dtoVacio.Secciones);
        Assert.Empty(dtoVacio.Recomendaciones);
        Assert.Empty(dtoVacio.Advertencias);
        Assert.Empty(dtoVacio.FuentesUsadas);

        // Mutar listas originales no cambia el DTO (copia defensiva ToArray)
        secciones.Add(new SeccionDto("S2", "T2"));
        recomendaciones.Add("R2");
        advertencias.Add("A2");
        fuentes.Add("F2");

        Assert.Single(dto.Secciones);
        Assert.Single(dto.Recomendaciones);
        Assert.Single(dto.Advertencias);
        Assert.Single(dto.FuentesUsadas);

        // ToString no vuelca contenido (valores únicos)
        var toString = dto.ToString();
        Assert.DoesNotContain("Resumen único", toString);
        Assert.DoesNotContain("S1", toString);
        Assert.DoesNotContain("T1", toString);
        Assert.Contains("Secciones = 1", toString);
        Assert.Contains("GeneradoPorIA = True", toString);
    }

    /// <summary>
    /// Verifica deserialización con <see cref="JsonSerializerDefaults.Web"/> de un
    /// <see cref="RespuestaIADto"/> con las cuatro listas en null y que todas quedan vacías.
    /// </summary>
    [Fact]
    public void RespuestaIADto_Deserializacion_ListasNullQuedanVacias()
    {
        var json = """
        {
            "Resumen": "Resumen de prueba",
            "Secciones": null,
            "Recomendaciones": null,
            "Advertencias": null,
            "FuentesUsadas": null,
            "GeneradoPorIA": true
        }
        """;

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        var dto = JsonSerializer.Deserialize<RespuestaIADto>(json, options);

        Assert.NotNull(dto);
        Assert.Equal("Resumen de prueba", dto!.Resumen);
        Assert.Empty(dto.Secciones);
        Assert.Empty(dto.Recomendaciones);
        Assert.Empty(dto.Advertencias);
        Assert.Empty(dto.FuentesUsadas);
        Assert.True(dto.GeneradoPorIA);
    }

    #endregion

    #region IAOptions

    /// <summary>
    /// Verifica todos los defaults de <see cref="IAOptions"/>, incluido <see cref="IAOptions.SectionName"/>
    /// y la ausencia de propiedades <c>Terminos</c>.
    /// </summary>
    [Fact]
    public void IAOptions_Defaults_Correctos()
    {
        var options = new IAOptions();

        Assert.False(options.Habilitada);
        Assert.Equal("http://localhost:11434/v1", options.BaseUrl);
        Assert.Equal(string.Empty, options.Model);
        Assert.Equal(string.Empty, options.ApiKey);
        Assert.Equal(30, options.TimeoutSegundos);
        Assert.Equal(4, options.MaxBloques);
        Assert.Equal(6000, options.MaxCaracteresDeContexto);
        Assert.Equal("v1", options.VersionDePrompt);
        Assert.Equal(5, options.KMinimo);
        Assert.Equal("IA", IAOptions.SectionName);

        // Verificar que no existen propiedades Terminos*
        var propiedadesTerminos = typeof(IAOptions).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.Name.StartsWith("Terminos", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(propiedadesTerminos);
    }

    /// <summary>
    /// Verifica que <see cref="IAOptions.ToString"/> no expone <see cref="IAOptions.ApiKey"/>.
    /// </summary>
    [Fact]
    public void IAOptions_ToString_NoExponeApiKey()
    {
        var options = new IAOptions { ApiKey = "sk-secreto-123" };
        var toString = options.ToString();

        Assert.DoesNotContain("sk-secreto-123", toString);
        Assert.Contains("[redacted]", toString);
    }

    #endregion
}




