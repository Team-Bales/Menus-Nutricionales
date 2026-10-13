using FluentAssertions;
using Menus.Domain.Entidades;
using Xunit;

namespace Menus.Domain.UnitTests.Entidades;

public class DietaTests
{
    [Fact]
    public void Constructor_ConParametrosValidos_DeberiaCrearInstanciaCorrectamente()
    {
        // Arrange
        var id = Guid.NewGuid();
        var nombre = "Dieta Hiposódica";
        var programaId = Guid.NewGuid();

        // Act
        var dieta = new Dieta(id, nombre, programaId);

        // Assert
        dieta.Id.Should().Be(id);
        dieta.Nombre.Should().Be(nombre);
        dieta.ProgramaAtencionId.Should().Be(programaId);
        dieta.GruposNutricionales.Should().BeEmpty();
        dieta.Restricciones.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_ConIdVacio_DeberiaLanzarArgumentException()
    {
        // Act
        var act = () => new Dieta(Guid.Empty, "Dieta Normal", Guid.NewGuid());

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("id");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ConNombreInvalido_DeberiaLanzarArgumentException(string? nombreInvalido)
    {
        // Act
        var act = () => new Dieta(Guid.NewGuid(), nombreInvalido!, Guid.NewGuid());

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("nombre");
    }

    [Fact]
    public void Constructor_ConProgramaAtencionIdVacio_DeberiaLanzarArgumentException()
    {
        // Act
        var act = () => new Dieta(Guid.NewGuid(), "Dieta Renal", Guid.Empty);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("programaAtencionId");
    }

    [Fact]
    public void AgregarGrupo_ConGrupoValido_DeberiaAgregarloALaColeccion()
    {
        // Arrange
        var dieta = new Dieta(Guid.NewGuid(), "Dieta Blanda", Guid.NewGuid());
        var grupoId = Guid.NewGuid();

        // Act
        dieta.AgregarGrupo(grupoId);

        // Assert
        dieta.GruposNutricionales.Should().HaveCount(1);
        var item = dieta.GruposNutricionales.First();
        item.DietaId.Should().Be(dieta.Id);
        item.GrupoNutricionalId.Should().Be(grupoId);
    }

    [Fact]
    public void AgregarGrupo_ConGrupoVacio_DeberiaLanzarArgumentException()
    {
        // Arrange
        var dieta = new Dieta(Guid.NewGuid(), "Dieta Blanda", Guid.NewGuid());

        // Act
        var act = () => dieta.AgregarGrupo(Guid.Empty);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("grupoNutricionalId");
    }

    [Fact]
    public void AgregarGrupo_ConGrupoDuplicado_NoDeberiaLanzarNiDuplicar()
    {
        // Arrange
        var dieta = new Dieta(Guid.NewGuid(), "Dieta Blanda", Guid.NewGuid());
        var grupoId = Guid.NewGuid();
        dieta.AgregarGrupo(grupoId);

        // Act
        var act = () => dieta.AgregarGrupo(grupoId);

        // Assert
        act.Should().NotThrow();
        dieta.GruposNutricionales.Should().HaveCount(1);
    }

    [Fact]
    public void AgregarRestriccion_ConRestriccionValida_DeberiaAgregarlaALaColeccion()
    {
        // Arrange
        var dieta = new Dieta(Guid.NewGuid(), "Dieta Sin Gluten", Guid.NewGuid());
        var restriccionId = Guid.NewGuid();

        // Act
        dieta.AgregarRestriccion(restriccionId);

        // Assert
        dieta.Restricciones.Should().HaveCount(1);
        var item = dieta.Restricciones.First();
        item.DietaId.Should().Be(dieta.Id);
        item.RestriccionAlimentariaId.Should().Be(restriccionId);
    }

    [Fact]
    public void AgregarRestriccion_ConRestriccionVacia_DeberiaLanzarArgumentException()
    {
        // Arrange
        var dieta = new Dieta(Guid.NewGuid(), "Dieta Sin Gluten", Guid.NewGuid());

        // Act
        var act = () => dieta.AgregarRestriccion(Guid.Empty);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("restriccionAlimentariaId");
    }

    [Fact]
    public void AgregarRestriccion_ConRestriccionDuplicada_NoDeberiaLanzarNiDuplicar()
    {
        // Arrange
        var dieta = new Dieta(Guid.NewGuid(), "Dieta Sin Gluten", Guid.NewGuid());
        var restriccionId = Guid.NewGuid();
        dieta.AgregarRestriccion(restriccionId);

        // Act
        var act = () => dieta.AgregarRestriccion(restriccionId);

        // Assert
        act.Should().NotThrow();
        dieta.Restricciones.Should().HaveCount(1);
    }

    [Fact]
    public void DietaGrupoNutricional_ConArgumentosInvalidos_DeberiaLanzarArgumentException()
    {
        // Act & Assert
        FluentActions.Invoking(() => new DietaGrupoNutricional(Guid.Empty, Guid.NewGuid()))
            .Should().Throw<ArgumentException>().WithParameterName("dietaId");

        FluentActions.Invoking(() => new DietaGrupoNutricional(Guid.NewGuid(), Guid.Empty()))
            .Should().Throw<ArgumentException>().WithParameterName("grupoNutricionalId");
    }

    [Fact]
    public void DietaRestriccion_ConArgumentosInvalidos_DeberiaLanzarArgumentException()
    {
        // Act & Assert
        FluentActions.Invoking(() => new DietaRestriccion(Guid.Empty, Guid.NewGuid()))
            .Should().Throw<ArgumentException>().WithParameterName("dietaId");

        FluentActions.Invoking(() => new DietaRestriccion(Guid.NewGuid(), Guid.Empty()))
            .Should().Throw<ArgumentException>().WithParameterName("restriccionAlimentariaId");
    }
}
