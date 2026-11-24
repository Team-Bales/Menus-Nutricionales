using FluentValidation;
using Menus.Application.Reportes;

namespace Menus.Application.Tests.Reportes;

/// <summary>Pruebas unitarias del <see cref="OrdenValidator"/> y sus reglas de validación.</summary>
public sealed class OrdenValidatorTests
{
    private static readonly HashSet<string> ColumnasPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        "id", "nombre", "fecha", "valor"
    };

    private sealed record SolicitudConOrden(OrdenDeReporte? Orden);

    private sealed class ValidadorTest : AbstractValidator<SolicitudConOrden>
    {
        public ValidadorTest()
        {
            RuleFor(x => x.Orden).ValidarOrden<SolicitudConOrden>(ColumnasPermitidas, "Orden");
        }
    }

    private readonly ValidadorTest _validador = new();

    // ---- Sin orden (opcional) ----

    [Fact]
    public void OrdenNull_NoAgregaErrores()
    {
        var resultado = _validador.Validate(new SolicitudConOrden(null));
        Assert.True(resultado.IsValid);
    }

    [Fact]
    public void OrdenSinColumna_ValidaDireccionPeroFallaColumna()
    {
        // Cuando se provee un objeto OrdenDeReporte (no null), se valida la dirección
        // y la columna. Columna null no está en la lista permitida.
        var resultado = _validador.Validate(new SolicitudConOrden(new OrdenDeReporte(null, "asc")));

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "Orden.Columna");
    }

    [Fact]
    public void OrdenColumnaVacia_ValidaDireccionPeroFallaColumna()
    {
        var resultado = _validador.Validate(new SolicitudConOrden(new OrdenDeReporte("", "asc")));

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "Orden.Columna");
    }

    [Fact]
    public void OrdenSoloEspacios_ValidaDireccionPeroFallaColumna()
    {
        var resultado = _validador.Validate(new SolicitudConOrden(new OrdenDeReporte("   ", "asc")));

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "Orden.Columna");
    }

    // ---- Dirección inválida ----

    [Fact]
    public void DireccionInvalida_AgregaErrorEnDireccion()
    {
        var resultado = _validador.Validate(new SolicitudConOrden(new OrdenDeReporte("nombre", "invalid")));

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "Orden.Direccion");
        Assert.Contains("dirección debe ser", resultado.Errors.First(e => e.PropertyName == "Orden.Direccion").ErrorMessage);
    }

    [Fact]
    public void DireccionCaseInsensitive_AscValido()
    {
        var resultado = _validador.Validate(new SolicitudConOrden(new OrdenDeReporte("nombre", "ASC")));
        Assert.True(resultado.IsValid);
    }

    [Fact]
    public void DireccionCaseInsensitive_DescValido()
    {
        var resultado = _validador.Validate(new SolicitudConOrden(new OrdenDeReporte("nombre", "DESC")));
        Assert.True(resultado.IsValid);
    }

    [Fact]
    public void DireccionConEspacios_SeNormaliza()
    {
        var resultado = _validador.Validate(new SolicitudConOrden(new OrdenDeReporte("nombre", "  desc  ")));
        Assert.True(resultado.IsValid);
    }

    // ---- Columna inválida ----

    [Fact]
    public void ColumnaInvalida_AgregaErrorEnColumna()
    {
        var resultado = _validador.Validate(new SolicitudConOrden(new OrdenDeReporte("noexiste", "asc")));

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "Orden.Columna");
        Assert.Contains("noexiste", resultado.Errors.First(e => e.PropertyName == "Orden.Columna").ErrorMessage);
    }

    [Fact]
    public void ColumnaCaseInsensitive_Valida()
    {
        var resultado = _validador.Validate(new SolicitudConOrden(new OrdenDeReporte("NOMBRE", "asc")));
        Assert.True(resultado.IsValid);
    }

    // ---- Ambos válidos ----

    [Fact]
    public void ColumnaYDireccionValidas_NoAgregaErrores()
    {
        var resultado = _validador.Validate(new SolicitudConOrden(new OrdenDeReporte("fecha", "desc")));
        Assert.True(resultado.IsValid);
    }

    // ---- Prefijo de campo personalizado ----

    private sealed record SolicitudConOrdenPersonalizado(OrdenDeReporte? OrdenGrupos);

    private sealed class ValidadorConPrefijo : AbstractValidator<SolicitudConOrdenPersonalizado>
    {
        public ValidadorConPrefijo()
        {
            RuleFor(x => x.OrdenGrupos).ValidarOrden<SolicitudConOrdenPersonalizado>(ColumnasPermitidas, "OrdenGrupos");
        }
    }

    [Fact]
    public void PrefijoPersonalizado_SeUsaEnMensajeDeError()
    {
        var validador = new ValidadorConPrefijo();
        var resultado = validador.Validate(new SolicitudConOrdenPersonalizado(new OrdenDeReporte("noexiste", "asc")));

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "OrdenGrupos.Columna");
    }

    // ---- Lista vacía de columnas permitidas ----

    private sealed class ValidadorVacio : AbstractValidator<SolicitudConOrden>
    {
        public ValidadorVacio()
        {
            RuleFor(x => x.Orden).ValidarOrden<SolicitudConOrden>(new HashSet<string>(StringComparer.OrdinalIgnoreCase), "Orden");
        }
    }

    [Fact]
    public void ListaVaciaDeColumnas_TodaColumnaEsInvalida()
    {
        var validadorVacio = new ValidadorVacio();

        var resultado = validadorVacio.Validate(new SolicitudConOrden(new OrdenDeReporte("cualquiera", "asc")));
        Assert.False(resultado.IsValid);
    }

    // ---- Mensaje de error incluye lista de columnas permitidas ----

    [Fact]
    public void ErrorDeColumna_IncluyeListaDeColumnasPermitidas()
    {
        var resultado = _validador.Validate(new SolicitudConOrden(new OrdenDeReporte("invalida", "asc")));

        var mensaje = resultado.Errors.First(e => e.PropertyName == "Orden.Columna").ErrorMessage;
        Assert.Contains("fecha", mensaje);
        Assert.Contains("id", mensaje);
        Assert.Contains("nombre", mensaje);
        Assert.Contains("valor", mensaje);
    }
}
