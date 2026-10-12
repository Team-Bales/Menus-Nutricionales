using Menus.Domain.Abstractions;
using Menus.Domain.Entities;
using Xunit;

namespace Menus.Domain.Tests.Abstractions;

public class EntityTests
{
    private class DummyEntityA : Entity<Guid>
    {
        public DummyEntityA(Guid id) : base(id) { }
    }

    private class DummyEntityB : Entity<Guid>
    {
        public DummyEntityB(Guid id) : base(id) { }
    }

    [Fact]
    public void Entity_MismoId_SonIguales()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new DummyEntityA(id);
        var entity2 = new DummyEntityA(id);

        // Act & Assert
        Assert.True(entity1.Equals(entity2));
        Assert.Equal(entity1.GetHashCode(), entity2.GetHashCode());
    }

    [Fact]
    public void Entity_DiferenteId_NoSonIguales()
    {
        // Arrange
        var entity1 = new DummyEntityA(Guid.NewGuid());
        var entity2 = new DummyEntityA(Guid.NewGuid());

        // Act & Assert
        Assert.False(entity1.Equals(entity2));
        Assert.NotEqual(entity1, entity2);
    }

    [Fact]
    public void Entity_DiferentesTipos_MismoId_NoSonIguales()
    {
        // Arrange: mismo Guid pero diferentes entidades del dominio
        var id = Guid.NewGuid();
        var grupo = new GrupoNutricional(id, "Proteínas");
        var restriccion = new RestriccionAlimentaria(id, "Celiaquía");

        // Act & Assert
        Assert.False(grupo.Equals(restriccion));
        Assert.False(restriccion.Equals(grupo));
    }

    [Fact]
    public void Entity_OperadorIgualdad_FuncionaCorrectamente()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new DummyEntityA(id);
        var entity2 = new DummyEntityA(id);
        var entity3 = new DummyEntityA(Guid.NewGuid());
        DummyEntityA? nullEntity1 = null;
        DummyEntityA? nullEntity2 = null;

        // Assert - Operador ==
        Assert.True(entity1 == entity2);
        Assert.False(entity1 == entity3);
        Assert.False(entity1 == nullEntity1);
        Assert.False(nullEntity1 == entity1);
        Assert.True(nullEntity1 == nullEntity2);

        // Assert - Operador !=
        Assert.False(entity1 != entity2);
        Assert.True(entity1 != entity3);
        Assert.True(entity1 != nullEntity1);
        Assert.False(nullEntity1 != nullEntity2);
    }
}