using Menus.Domain.Entidades;

namespace Menus.Domain.Tests.Entidades;

public sealed class PacienteTests
{
    private readonly Guid _salaId = Guid.NewGuid();

    [Fact]
    public void Constructor_ValidArgs_SetsProperties()
    {
        var id = Guid.NewGuid();

        var paciente = new Paciente(id, "Juan Pérez", 30, _salaId);

        Assert.Equal(id, paciente.Id);
        Assert.Equal("Juan Pérez", paciente.Nombre);
        Assert.Equal(30, paciente.Edad);
        Assert.Equal(_salaId, paciente.SalaId);
        Assert.Empty(paciente.Dietas);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NombreVacio_LanzaArgumentException(string nombre)
    {
        Assert.Throws<ArgumentException>(() =>
            new Paciente(Guid.NewGuid(), nombre, 25, _salaId));
    }

    [Fact]
    public void Constructor_EdadCero_SeAcepta()
    {
        // R6-14: la edad es mayor o igual que cero (un recién nacido tiene 0 años).
        var paciente = new Paciente(Guid.NewGuid(), "Recién nacido", 0, _salaId);

        Assert.Equal(0, paciente.Edad);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_EdadNegativa_LanzaArgumentOutOfRangeException(int edad)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Paciente(Guid.NewGuid(), "Maria López", edad, _salaId));
    }

    [Fact]
    public void InscribirDieta_DietaNueva_AgregaInscripcion()
    {
        var paciente = new Paciente(Guid.NewGuid(), "Ana García", 45, _salaId);
        var dietaId = Guid.NewGuid();

        var inscripcion = paciente.InscribirDieta(dietaId);

        Assert.Single(paciente.Dietas);
        Assert.Equal(paciente.Id, inscripcion.PacienteId);
        Assert.Equal(dietaId, inscripcion.DietaId);
    }

    [Fact]
    public void InscribirDieta_DietaDuplicada_LanzaInvalidOperationException()
    {
        var paciente = new Paciente(Guid.NewGuid(), "Carlos Ruiz", 60, _salaId);
        var dietaId = Guid.NewGuid();
        paciente.InscribirDieta(dietaId);

        Assert.Throws<InvalidOperationException>(() =>
            paciente.InscribirDieta(dietaId));
    }

    [Fact]
    public void InscribirDieta_VariasDietasDistintas_AgregaTodasSinDuplicados()
    {
        var paciente = new Paciente(Guid.NewGuid(), "Luis Martínez", 35, _salaId);
        var dieta1 = Guid.NewGuid();
        var dieta2 = Guid.NewGuid();

        paciente.InscribirDieta(dieta1);
        paciente.InscribirDieta(dieta2);

        Assert.Equal(2, paciente.Dietas.Count);
        Assert.Contains(paciente.Dietas, d => d.DietaId == dieta1);
        Assert.Contains(paciente.Dietas, d => d.DietaId == dieta2);
    }

    [Fact]
    public void Dietas_ReturnsReadOnlyCollection_NoPermiteModificacionExterna()
    {
        var paciente = new Paciente(Guid.NewGuid(), "Rosa Díaz", 28, _salaId);

        var collection = paciente.Dietas;

        Assert.IsAssignableFrom<IReadOnlyCollection<PacienteDieta>>(collection);
        Assert.NotSame(paciente.Dietas, collection);
    }
}
