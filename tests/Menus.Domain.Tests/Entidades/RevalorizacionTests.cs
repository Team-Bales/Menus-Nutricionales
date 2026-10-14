using Menus.Domain.Entidades;

namespace Menus.Domain.Tests.Entidades;

public sealed class RevalorizacionTests
{
    [Fact]
    public void Constructor_ValidArgs_CreatesPendingRevalorizacion()
    {
        var id = Guid.NewGuid();
        var fecha = DateTime.UtcNow;
        var pacienteId = Guid.NewGuid();
        var nutricionistaId = Guid.NewGuid();
        var valoracionRevisadaId = Guid.NewGuid();

        var revalorizacion = new Revalorizacion(id, fecha, pacienteId, nutricionistaId, valoracionRevisadaId);

        Assert.Equal(id, revalorizacion.Id);
        Assert.Equal(fecha, revalorizacion.FechaSolicitud);
        Assert.Equal(pacienteId, revalorizacion.PacienteId);
        Assert.Equal(nutricionistaId, revalorizacion.NutricionistaId);
        Assert.Equal(valoracionRevisadaId, revalorizacion.ValoracionRevisadaId);
        Assert.Null(revalorizacion.ValoracionGeneradaId);
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Constructor_FechaSolicitudNoUtc_ThrowsArgumentException(DateTimeKind kind)
    {
        var fecha = DateTime.SpecifyKind(DateTime.UtcNow, kind);

        Assert.Throws<ArgumentException>(() =>
            new Revalorizacion(Guid.NewGuid(), fecha, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public void RegistrarValoracionGenerada_SetsGeneratedValoracion()
    {
        var revalorizacion = CrearRevalorizacion();
        var valoracionGeneradaId = Guid.NewGuid();

        revalorizacion.RegistrarValoracionGenerada(valoracionGeneradaId);

        Assert.Equal(valoracionGeneradaId, revalorizacion.ValoracionGeneradaId);
    }

    [Fact]
    public void RegistrarValoracionGenerada_WhenAlreadyCompleted_ThrowsInvalidOperationException()
    {
        var revalorizacion = CrearRevalorizacion();
        revalorizacion.RegistrarValoracionGenerada(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => revalorizacion.RegistrarValoracionGenerada(Guid.NewGuid()));
    }

    [Fact]
    public void Equality_SameId_ReturnsTrue()
    {
        var id = Guid.NewGuid();

        var primera = new Revalorizacion(id, DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var segunda = new Revalorizacion(id, DateTime.UtcNow.AddMinutes(1), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(primera, segunda);
    }

    private static Revalorizacion CrearRevalorizacion() =>
        new(Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
}
