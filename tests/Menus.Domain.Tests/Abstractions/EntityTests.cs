using Menus.Domain.Abstractions;
using Menus.Domain.Nomenclators;

namespace Menus.Domain.Tests.Abstractions;

public sealed class EntityTests
{
    private sealed class DummyEntityA : Entity<Guid>
    {
        public DummyEntityA(Guid id) : base(id) { }
    }

    [Fact]
    public void Entity_MismoId_SonIguales()
    {
        var id = Guid.NewGuid();
        var entity1 = new DummyEntityA(id);
        var entity2 = new DummyEntityA(id);

        Assert.True(entity1.Equals(entity2));
        Assert.Equal(entity1.GetHashCode(), entity2.GetHashCode());
    }

    [Fact]
    public void Entity_DiferenteId_NoSonIguales()
    {
        var entity1 = new DummyEntityA(Guid.NewGuid());
        var entity2 = new DummyEntityA(Guid.NewGuid());

        Assert.False(entity1.Equals(entity2));
        Assert.NotEqual(entity1, entity2);
    }

    [Fact]
    public void Entity_DiferentesTipos_MismoId_NoSonIguales()
    {
        var id = Guid.NewGuid();
        var grupo = new GrupoNutricional(id, "Proteínas");
        var restriccion = new RestriccionAlimentaria(id, "Celiaquía");

        Assert.False(grupo.Equals(restriccion));
        Assert.False(restriccion.Equals(grupo));
    }

    [Fact]
    public void Entity_OperadorIgualdad_FuncionaCorrectamente()
    {
        var id = Guid.NewGuid();
        var entity1 = new DummyEntityA(id);
        var entity2 = new DummyEntityA(id);
        var entity3 = new DummyEntityA(Guid.NewGuid());
        DummyEntityA? nullEntity1 = null;
        DummyEntityA? nullEntity2 = null;

        Assert.True(entity1 == entity2);
        Assert.False(entity1 == entity3);
        Assert.False(entity1 == nullEntity1);
        Assert.False(nullEntity1 == entity1);
        Assert.True(nullEntity1 == nullEntity2);

        Assert.False(entity1 != entity2);
        Assert.True(entity1 != entity3);
        Assert.True(entity1 != nullEntity1);
        Assert.False(nullEntity1 != nullEntity2);
    }
}
