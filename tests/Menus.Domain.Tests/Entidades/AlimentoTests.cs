using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;

namespace Menus.Domain.Tests.Entidades;

public sealed class AlimentoTests
{
    private readonly Guid _grupoId = Guid.NewGuid();
    private readonly Guid _nutricionistaId = Guid.NewGuid();

    [Fact]
    public void Constructor_ValidArgs_SetsProperties()
    {
        var id = Guid.NewGuid();

        var alimento = new Alimento(id, "Manzana", _grupoId, TipoPreparacion.Postre.Id, NivelCalorico.Bajo.Id, _nutricionistaId);

        Assert.Equal(id, alimento.Id);
        Assert.Equal("Manzana", alimento.Nombre);
        Assert.Equal(_grupoId, alimento.GrupoNutricionalId);
        Assert.Equal(TipoPreparacion.Postre.Id, alimento.TipoPreparacionId);
        Assert.Equal(NivelCalorico.Bajo.Id, alimento.NivelCaloricoId);
        Assert.Equal(_nutricionistaId, alimento.NutricionistaId);
        Assert.IsAssignableFrom<ItemAlimentario>(alimento);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NombreVacio_LanzaArgumentException(string nombre)
    {
        Assert.Throws<ArgumentException>(() => new Alimento(Guid.NewGuid(), nombre, _grupoId, TipoPreparacion.Entrante.Id, NivelCalorico.Medio.Id, _nutricionistaId));
    }

    [Fact]
    public void Constructor_TipoPreparacionFueraDelCatalogo_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Alimento(Guid.NewGuid(), "Nombre", _grupoId, Guid.NewGuid(), NivelCalorico.Medio.Id, _nutricionistaId));
    }

    [Fact]
    public void Constructor_NivelCaloricoFueraDelCatalogo_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Alimento(Guid.NewGuid(), "Nombre", _grupoId, TipoPreparacion.Entrante.Id, Guid.NewGuid(), _nutricionistaId));
    }

    [Fact]
    public void Reclasificar_ElDescriptor_CambiaGrupoTipoYNivel()
    {
        var alimento = new Alimento(Guid.NewGuid(), "Manzana", _grupoId, TipoPreparacion.Postre.Id, NivelCalorico.Bajo.Id, _nutricionistaId);
        var otroGrupo = Guid.NewGuid();

        alimento.Reclasificar(_nutricionistaId, otroGrupo, TipoPreparacion.Entrante.Id, NivelCalorico.Medio.Id);

        Assert.Equal(otroGrupo, alimento.GrupoNutricionalId);
        Assert.Equal(TipoPreparacion.Entrante.Id, alimento.TipoPreparacionId);
        Assert.Equal(NivelCalorico.Medio.Id, alimento.NivelCaloricoId);
    }

    [Fact]
    public void Reclasificar_ElDescriptor_NoCambiaNombreNiDescriptor()
    {
        var alimento = new Alimento(Guid.NewGuid(), "Manzana", _grupoId, TipoPreparacion.Postre.Id, NivelCalorico.Bajo.Id, _nutricionistaId);

        alimento.Reclasificar(_nutricionistaId, Guid.NewGuid(), TipoPreparacion.Entrante.Id, NivelCalorico.Alto.Id);

        Assert.Equal("Manzana", alimento.Nombre);
        Assert.Equal(_nutricionistaId, alimento.NutricionistaId);
    }

    [Fact]
    public void Reclasificar_OtroNutricionista_LanzaInvalidOperationExceptionYNoCambiaNada()
    {
        // RS-02: solo el nutricionista que describió el ítem lo clasifica.
        var alimento = new Alimento(Guid.NewGuid(), "Manzana", _grupoId, TipoPreparacion.Postre.Id, NivelCalorico.Bajo.Id, _nutricionistaId);

        Assert.Throws<InvalidOperationException>(() =>
            alimento.Reclasificar(Guid.NewGuid(), Guid.NewGuid(), TipoPreparacion.Entrante.Id, NivelCalorico.Medio.Id));

        Assert.Equal(_grupoId, alimento.GrupoNutricionalId);
        Assert.Equal(TipoPreparacion.Postre.Id, alimento.TipoPreparacionId);
        Assert.Equal(NivelCalorico.Bajo.Id, alimento.NivelCaloricoId);
    }

    [Fact]
    public void Reclasificar_TipoFueraDelCatalogo_LanzaArgumentExceptionYNoCambiaNada()
    {
        var alimento = new Alimento(Guid.NewGuid(), "Manzana", _grupoId, TipoPreparacion.Postre.Id, NivelCalorico.Bajo.Id, _nutricionistaId);

        Assert.Throws<ArgumentException>(() =>
            alimento.Reclasificar(_nutricionistaId, Guid.NewGuid(), Guid.NewGuid(), NivelCalorico.Medio.Id));

        Assert.Equal(_grupoId, alimento.GrupoNutricionalId);
        Assert.Equal(NivelCalorico.Bajo.Id, alimento.NivelCaloricoId);
    }

    [Fact]
    public void Reclasificar_NivelFueraDelCatalogo_LanzaArgumentException()
    {
        var alimento = new Alimento(Guid.NewGuid(), "Manzana", _grupoId, TipoPreparacion.Postre.Id, NivelCalorico.Bajo.Id, _nutricionistaId);

        Assert.Throws<ArgumentException>(() =>
            alimento.Reclasificar(_nutricionistaId, _grupoId, TipoPreparacion.Postre.Id, Guid.NewGuid()));
    }
}
