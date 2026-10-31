using Menus.Domain.Entidades;

namespace Menus.Domain.Tests.Entidades;

public sealed class NutricionistaTests
{
    private readonly Guid _especialidadId = Guid.NewGuid();
    private readonly Guid _dietaId1 = Guid.NewGuid();
    private readonly Guid _dietaId2 = Guid.NewGuid();

    [Fact]
    public void Constructor_ValidArgs_SetsProperties()
    {
        var id = Guid.NewGuid();

        var nutricionista = new Nutricionista(id, "Dra. Pérez", _especialidadId);

        Assert.Equal(id, nutricionista.Id);
        Assert.Equal("Dra. Pérez", nutricionista.Nombre);
        Assert.Equal(_especialidadId, nutricionista.EspecialidadId);
        Assert.Empty(nutricionista.DietasAutorizadas);
        Assert.False(nutricionista.EsJefe); // RD-18: falso por defecto
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NombreVacio_LanzaArgumentException(string nombre)
    {
        Assert.Throws<ArgumentException>(() => new Nutricionista(Guid.NewGuid(), nombre, _especialidadId));
    }

    [Fact]
    public void AutorizarDieta_ValidArgs_AgregaYRetornaAutorizacion()
    {
        var nutricionista = new Nutricionista(Guid.NewGuid(), "Dra. Pérez", _especialidadId);

        var autorizacion = nutricionista.AutorizarDieta(_dietaId1);

        Assert.Single(nutricionista.DietasAutorizadas);
        Assert.Equal(nutricionista.Id, autorizacion.NutricionistaId);
        Assert.Equal(_dietaId1, autorizacion.DietaId);
    }

    [Fact]
    public void AutorizarDieta_DietaDuplicada_LanzaInvalidOperationException()
    {
        var nutricionista = new Nutricionista(Guid.NewGuid(), "Dra. Pérez", _especialidadId);
        nutricionista.AutorizarDieta(_dietaId1);

        Assert.Throws<InvalidOperationException>(() => nutricionista.AutorizarDieta(_dietaId1));
    }

    [Fact]
    public void AutorizarDieta_MultiplesDietas_TodasPresentes()
    {
        var nutricionista = new Nutricionista(Guid.NewGuid(), "Dra. Pérez", _especialidadId);

        nutricionista.AutorizarDieta(_dietaId1);
        nutricionista.AutorizarDieta(_dietaId2);

        Assert.Equal(2, nutricionista.DietasAutorizadas.Count);
        Assert.Contains(nutricionista.DietasAutorizadas, a => a.DietaId == _dietaId1);
        Assert.Contains(nutricionista.DietasAutorizadas, a => a.DietaId == _dietaId2);
    }

    [Fact]
    public void DietasAutorizadas_ReturnsReadOnlyCollection_NoPermiteModificacionExterna()
    {
        var nutricionista = new Nutricionista(Guid.NewGuid(), "Dra. Pérez", _especialidadId);

        var collection = nutricionista.DietasAutorizadas;

        Assert.IsAssignableFrom<IReadOnlyCollection<NutricionistasDieta>>(collection);
        Assert.NotSame(nutricionista.DietasAutorizadas, collection); // nueva instancia cada acceso (AsReadOnly crea wrapper)
    }

    [Fact]
    public void DesignarJefe_NoEraJefe_PasaASerJefe()
    {
        var nutricionista = new Nutricionista(Guid.NewGuid(), "Dra. Pérez", _especialidadId);

        nutricionista.DesignarJefe();

        Assert.True(nutricionista.EsJefe);
    }

    [Fact]
    public void DesignarJefe_YaEraJefe_SigueSiendoJefe()
    {
        var nutricionista = new Nutricionista(Guid.NewGuid(), "Dra. Pérez", _especialidadId);
        nutricionista.DesignarJefe();

        nutricionista.DesignarJefe();

        Assert.True(nutricionista.EsJefe);
    }

    [Fact]
    public void RetirarJefe_EraJefe_DejaDeSerJefe()
    {
        var nutricionista = new Nutricionista(Guid.NewGuid(), "Dra. Pérez", _especialidadId);
        nutricionista.DesignarJefe();

        nutricionista.RetirarJefe();

        Assert.False(nutricionista.EsJefe);
    }

    [Fact]
    public void RetirarJefe_NoEraJefe_SigueSinSerJefe()
    {
        var nutricionista = new Nutricionista(Guid.NewGuid(), "Dra. Pérez", _especialidadId);

        nutricionista.RetirarJefe();

        Assert.False(nutricionista.EsJefe);
    }

    [Fact]
    public void DesignarJefe_NoAfectaLasAutorizaciones()
    {
        var nutricionista = new Nutricionista(Guid.NewGuid(), "Dra. Pérez", _especialidadId);
        nutricionista.AutorizarDieta(_dietaId1);

        nutricionista.DesignarJefe();

        Assert.Single(nutricionista.DietasAutorizadas, a => a.DietaId == _dietaId1);
    }

    [Fact]
    public void RevocarDieta_Autorizada_LaQuitaYConservaLasDemas()
    {
        var nutricionista = new Nutricionista(Guid.NewGuid(), "Dra. Pérez", _especialidadId);
        nutricionista.AutorizarDieta(_dietaId1);
        nutricionista.AutorizarDieta(_dietaId2);

        nutricionista.RevocarDieta(_dietaId1);

        var restante = Assert.Single(nutricionista.DietasAutorizadas);
        Assert.Equal(_dietaId2, restante.DietaId);
    }

    [Fact]
    public void RevocarDieta_NoAutorizada_LanzaInvalidOperationException()
    {
        var nutricionista = new Nutricionista(Guid.NewGuid(), "Dra. Pérez", _especialidadId);
        nutricionista.AutorizarDieta(_dietaId1);

        Assert.Throws<InvalidOperationException>(() => nutricionista.RevocarDieta(_dietaId2));
        Assert.Single(nutricionista.DietasAutorizadas);
    }

    [Fact]
    public void RevocarDieta_DespuesDeRevocarla_SePuedeVolverAAutorizar()
    {
        var nutricionista = new Nutricionista(Guid.NewGuid(), "Dra. Pérez", _especialidadId);
        nutricionista.AutorizarDieta(_dietaId1);
        nutricionista.RevocarDieta(_dietaId1);

        nutricionista.AutorizarDieta(_dietaId1);

        Assert.Single(nutricionista.DietasAutorizadas);
    }

    [Fact]
    public void RevocarDieta_NoAfectaElCargoDeJefe()
    {
        var nutricionista = new Nutricionista(Guid.NewGuid(), "Dra. Pérez", _especialidadId);
        nutricionista.AutorizarDieta(_dietaId1);
        nutricionista.DesignarJefe();

        nutricionista.RevocarDieta(_dietaId1);

        Assert.True(nutricionista.EsJefe);
    }
}
