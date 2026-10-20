using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;

namespace Menus.Domain.Tests.Entidades;

public sealed class ConsumoTests
{
    private readonly Guid _menuId = Guid.NewGuid();
    private readonly Guid _pacienteId = Guid.NewGuid();
    private readonly Guid _itemId = Guid.NewGuid();
    private readonly DateTime _servicio = DateTime.UtcNow.AddHours(-6);

    // Consumo solo se crea a través de Menu.RegistrarConsumo, en un servicio de un menú final asignado al
    // paciente. Dos instancias de Menu con el mismo Id representan el mismo menú leído dos veces.
    private Menu CrearMenuServido(DateTime? fechaServicio = null)
    {
        var menu = new Menu(
            _menuId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(-2),
            4,
            false,
            [(TipoPreparacion.PlatoFuerte.Id, 1m)],
            [Guid.NewGuid()]);
        menu.AgregarItem(_itemId);
        menu.Aprobar(Guid.NewGuid(), DateTime.UtcNow.AddDays(-1));
        menu.AsignarPaciente(_pacienteId);
        menu.RegistrarServicio(_pacienteId, fechaServicio ?? _servicio);
        return menu;
    }

    [Fact]
    public void RegistrarConsumo_ValidArgs_TomaLaClaveDelServicio()
    {
        var consumo = CrearMenuServido().RegistrarConsumo(_pacienteId, _servicio, _itemId, true);

        Assert.Equal(_menuId, consumo.MenuId);
        Assert.Equal(_pacienteId, consumo.PacienteId);
        Assert.Equal(_servicio.Ticks - (_servicio.Ticks % TimeSpan.TicksPerSecond), consumo.FechaServicio.Ticks);
        Assert.Equal(_itemId, consumo.ItemId);
        Assert.True(consumo.Aceptacion);
    }

    [Fact]
    public void RegistrarConsumo_Rechazo_GuardaAceptacionFalsa()
    {
        var consumo = CrearMenuServido().RegistrarConsumo(_pacienteId, _servicio, _itemId, false);

        Assert.False(consumo.Aceptacion);
    }

    [Fact]
    public void Equality_MismaClaveCompuesta_SonIguales()
    {
        var aceptado = CrearMenuServido().RegistrarConsumo(_pacienteId, _servicio, _itemId, true);
        var rechazado = CrearMenuServido().RegistrarConsumo(_pacienteId, _servicio, _itemId, false);

        Assert.Equal(aceptado, rechazado);
    }

    [Fact]
    public void Equality_DistintoServicio_NoSonIguales()
    {
        var otroServicio = _servicio.AddHours(-1);

        var primero = CrearMenuServido().RegistrarConsumo(_pacienteId, _servicio, _itemId, true);
        var segundo = CrearMenuServido(otroServicio).RegistrarConsumo(_pacienteId, otroServicio, _itemId, true);

        Assert.NotEqual(primero, segundo);
    }
}
