using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;

namespace Menus.Domain.Tests.Entidades;

public sealed class ConsumoTests
{
    private readonly Guid _menuId = Guid.NewGuid();
    private readonly Guid _pacienteId = Guid.NewGuid();
    private readonly Guid _alimentoId = Guid.NewGuid();

    // Consumo solo se crea a través de Asignacion.RegistrarConsumo; dos instancias de Menu con el
    // mismo Id representan el mismo menú leído dos veces.
    private Asignacion CrearAsignacion() =>
        new Menu(
            _menuId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(-1),
            4,
            false,
            [(TipoPreparacion.PlatoFuerte.Id, 1m)],
            [Guid.NewGuid()])
        .AsignarPaciente(_pacienteId);

    [Fact]
    public void RegistrarConsumo_ValidArgs_TomaLaClaveDeLaAsignacion()
    {
        var consumo = CrearAsignacion().RegistrarConsumo(_alimentoId, true);

        Assert.Equal(_menuId, consumo.MenuId);
        Assert.Equal(_pacienteId, consumo.PacienteId);
        Assert.Equal(_alimentoId, consumo.AlimentoId);
        Assert.True(consumo.Aceptacion);
    }

    [Fact]
    public void RegistrarConsumo_Rechazo_GuardaAceptacionFalsa()
    {
        var consumo = CrearAsignacion().RegistrarConsumo(_alimentoId, false);

        Assert.False(consumo.Aceptacion);
    }

    [Fact]
    public void Equality_MismaClaveCompuesta_SonIguales()
    {
        var aceptado = CrearAsignacion().RegistrarConsumo(_alimentoId, true);
        var rechazado = CrearAsignacion().RegistrarConsumo(_alimentoId, false);

        Assert.Equal(aceptado, rechazado);
    }

    [Fact]
    public void Equality_DistintoAlimento_NoSonIguales()
    {
        var asignacion = CrearAsignacion();

        var primero = asignacion.RegistrarConsumo(Guid.NewGuid(), true);
        var segundo = asignacion.RegistrarConsumo(Guid.NewGuid(), true);

        Assert.NotEqual(primero, segundo);
    }
}
