using Menus.Domain.Entidades;

namespace Menus.Domain.Tests.Entidades;

public sealed class DietaTests
{
    private readonly Guid _programaId = Guid.NewGuid();

    [Fact]
    public void Constructor_ValidArgs_SetsProperties()
    {
        var id = Guid.NewGuid();

        var dieta = new Dieta(id, "Dieta Hiposódica", _programaId);

        Assert.Equal(id, dieta.Id);
        Assert.Equal("Dieta Hiposódica", dieta.Nombre);
        Assert.Equal(_programaId, dieta.ProgramaAtencionId);
        Assert.Empty(dieta.GruposNutricionales);
        Assert.Empty(dieta.Restricciones);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NombreVacio_LanzaArgumentException(string nombre)
    {
        Assert.Throws<ArgumentException>(() => new Dieta(Guid.NewGuid(), nombre, _programaId));
    }

    [Fact]
    public void AgregarGrupo_NuevoGrupo_AgregaYRetorna()
    {
        var dieta = new Dieta(Guid.NewGuid(), "Dieta Blanda", _programaId);
        var grupoId = Guid.NewGuid();

        var resultado = dieta.AgregarGrupo(grupoId);

        Assert.Single(dieta.GruposNutricionales);
        Assert.Equal(dieta.Id, resultado.DietaId);
        Assert.Equal(grupoId, resultado.GrupoNutricionalId);
    }

    [Fact]
    public void AgregarGrupo_GrupoDuplicado_LanzaInvalidOperationException()
    {
        var dieta = new Dieta(Guid.NewGuid(), "Dieta Blanda", _programaId);
        var grupoId = Guid.NewGuid();
        dieta.AgregarGrupo(grupoId);

        Assert.Throws<InvalidOperationException>(() => dieta.AgregarGrupo(grupoId));
    }

    [Fact]
    public void AgregarGrupo_VariosGrupos_AgregaTodos()
    {
        var dieta = new Dieta(Guid.NewGuid(), "Dieta Blanda", _programaId);

        dieta.AgregarGrupo(Guid.NewGuid());
        dieta.AgregarGrupo(Guid.NewGuid());
        dieta.AgregarGrupo(Guid.NewGuid());

        Assert.Equal(3, dieta.GruposNutricionales.Count);
    }

    [Fact]
    public void AgregarRestriccion_NuevaRestriccion_AgregaYRetorna()
    {
        var dieta = new Dieta(Guid.NewGuid(), "Dieta Sin Gluten", _programaId);
        var restriccionId = Guid.NewGuid();

        var resultado = dieta.AgregarRestriccion(restriccionId);

        Assert.Single(dieta.Restricciones);
        Assert.Equal(dieta.Id, resultado.DietaId);
        Assert.Equal(restriccionId, resultado.RestriccionAlimentariaId);
    }

    [Fact]
    public void AgregarRestriccion_RestriccionDuplicada_LanzaInvalidOperationException()
    {
        var dieta = new Dieta(Guid.NewGuid(), "Dieta Sin Gluten", _programaId);
        var restriccionId = Guid.NewGuid();
        dieta.AgregarRestriccion(restriccionId);

        Assert.Throws<InvalidOperationException>(() => dieta.AgregarRestriccion(restriccionId));
    }

    [Fact]
    public void GruposNutricionales_EsReadOnly()
    {
        var dieta = new Dieta(Guid.NewGuid(), "Dieta Renal", _programaId);

        Assert.IsAssignableFrom<IReadOnlyCollection<DietaGrupoNutricional>>(dieta.GruposNutricionales);
    }

    [Fact]
    public void Restricciones_EsReadOnly()
    {
        var dieta = new Dieta(Guid.NewGuid(), "Dieta Renal", _programaId);

        Assert.IsAssignableFrom<IReadOnlyCollection<DietaRestriccion>>(dieta.Restricciones);
    }
}
