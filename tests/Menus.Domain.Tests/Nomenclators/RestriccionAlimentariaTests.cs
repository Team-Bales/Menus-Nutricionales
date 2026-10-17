using Menus.Domain.Nomenclators;

namespace Menus.Domain.Tests.Nomenclators;

public sealed class RestriccionAlimentariaTests
{
    [Fact]
    public void Constructor_ValidArgs_SetsProperties()
    {
        var id = Guid.NewGuid();

        var restriccion = new RestriccionAlimentaria(id, "Sin gluten");

        Assert.Equal(id, restriccion.Id);
        Assert.Equal("Sin gluten", restriccion.Nombre);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NombreVacio_LanzaArgumentException(string nombre)
    {
        Assert.Throws<ArgumentException>(() => new RestriccionAlimentaria(Guid.NewGuid(), nombre));
    }

    [Fact]
    public void Constructor_SinExclusiones_EmpiezaVacia()
    {
        var restriccion = new RestriccionAlimentaria(Guid.NewGuid(), "Sin lactosa");

        Assert.Empty(restriccion.GruposExcluidos);
    }

    [Fact]
    public void ExcluirGrupo_ValidArgs_AgregaYRetornaExclusion()
    {
        var restriccion = new RestriccionAlimentaria(Guid.NewGuid(), "Sin lactosa");
        var lacteosId = Guid.NewGuid();

        var exclusion = restriccion.ExcluirGrupo(lacteosId);

        Assert.Single(restriccion.GruposExcluidos);
        Assert.Equal(restriccion.Id, exclusion.RestriccionAlimentariaId);
        Assert.Equal(lacteosId, exclusion.GrupoNutricionalId);
    }

    [Fact]
    public void ExcluirGrupo_GruposDistintos_RegistraTodos()
    {
        var restriccion = new RestriccionAlimentaria(Guid.NewGuid(), "Vegana");

        restriccion.ExcluirGrupo(Guid.NewGuid());
        restriccion.ExcluirGrupo(Guid.NewGuid());

        Assert.Equal(2, restriccion.GruposExcluidos.Count);
    }

    [Fact]
    public void ExcluirGrupo_GrupoRepetido_LanzaInvalidOperationException()
    {
        var restriccion = new RestriccionAlimentaria(Guid.NewGuid(), "Sin lactosa");
        var lacteosId = Guid.NewGuid();
        restriccion.ExcluirGrupo(lacteosId);

        Assert.Throws<InvalidOperationException>(() => restriccion.ExcluirGrupo(lacteosId));
        Assert.Single(restriccion.GruposExcluidos);
    }

    [Fact]
    public void QuitarExclusion_GrupoExcluido_LoQuita()
    {
        var restriccion = new RestriccionAlimentaria(Guid.NewGuid(), "Vegana");
        var lacteosId = Guid.NewGuid();
        var carnesId = Guid.NewGuid();
        restriccion.ExcluirGrupo(lacteosId);
        restriccion.ExcluirGrupo(carnesId);

        restriccion.QuitarExclusion(lacteosId);

        Assert.Equal(carnesId, Assert.Single(restriccion.GruposExcluidos).GrupoNutricionalId);
    }

    [Fact]
    public void QuitarExclusion_GrupoNoExcluido_LanzaInvalidOperationException()
    {
        var restriccion = new RestriccionAlimentaria(Guid.NewGuid(), "Sin lactosa");

        Assert.Throws<InvalidOperationException>(() => restriccion.QuitarExclusion(Guid.NewGuid()));
    }

    [Fact]
    public void QuitarExclusion_DespuesDeQuitar_SePuedeVolverAExcluir()
    {
        var restriccion = new RestriccionAlimentaria(Guid.NewGuid(), "Sin lactosa");
        var lacteosId = Guid.NewGuid();
        restriccion.ExcluirGrupo(lacteosId);
        restriccion.QuitarExclusion(lacteosId);

        restriccion.ExcluirGrupo(lacteosId);

        Assert.Single(restriccion.GruposExcluidos);
    }

    [Fact]
    public void GruposExcluidos_EsDeSoloLectura()
    {
        var restriccion = new RestriccionAlimentaria(Guid.NewGuid(), "Sin lactosa");

        Assert.False(restriccion.GruposExcluidos is ICollection<RestriccionGrupoExcluido> { IsReadOnly: false });
    }

    [Fact]
    public void Exclusion_MismaClaveCompuesta_SonIguales()
    {
        var id = Guid.NewGuid();
        var lacteosId = Guid.NewGuid();

        var primera = new RestriccionAlimentaria(id, "Sin lactosa").ExcluirGrupo(lacteosId);
        var segunda = new RestriccionAlimentaria(id, "Sin lactosa").ExcluirGrupo(lacteosId);

        Assert.Equal(primera, segunda);
    }

    [Fact]
    public void Exclusion_DistintoGrupo_NoSonIguales()
    {
        var restriccion = new RestriccionAlimentaria(Guid.NewGuid(), "Vegana");

        var primera = restriccion.ExcluirGrupo(Guid.NewGuid());
        var segunda = restriccion.ExcluirGrupo(Guid.NewGuid());

        Assert.NotEqual(primera, segunda);
    }
}
