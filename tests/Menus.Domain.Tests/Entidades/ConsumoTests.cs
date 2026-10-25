using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;

namespace Menus.Domain.Tests.Entidades;

public sealed class ConsumoTests
{
    private readonly Guid _menuId = Guid.NewGuid();
    private readonly Guid _pacienteId = Guid.NewGuid();
    private readonly Guid _itemId = Guid.NewGuid();
    private readonly DateOnly _hoy = DateOnly.FromDateTime(DateTime.UtcNow);

    // Consumo solo se crea a través de Menu.RegistrarConsumo, en un servicio de un menú final asignado al
    // paciente. Dos instancias de Menu con el mismo Id representan el mismo menú leído dos veces.
    private Menu CrearMenuServido(DateOnly dia)
    {
        var menu = new Menu(
            _menuId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(-3),
            4,
            false,
            [(TipoPreparacion.PlatoFuerte.Id, 1m)],
            [Guid.NewGuid()]);
        menu.AgregarItem(_itemId);
        menu.Aprobar(Guid.NewGuid(), DateTime.UtcNow.AddDays(-2));
        menu.AsignarPaciente(_pacienteId);
        menu.RegistrarServicio(_pacienteId, dia);
        return menu;
    }

    [Fact]
    public void RegistrarConsumo_ValidArgs_TomaLaClaveDelServicio()
    {
        var consumo = CrearMenuServido(_hoy).RegistrarConsumo(_pacienteId, _hoy, _itemId, true);

        Assert.Equal(_menuId, consumo.MenuId);
        Assert.Equal(_pacienteId, consumo.PacienteId);
        Assert.Equal(_hoy, consumo.FechaServicio);
        Assert.Equal(_itemId, consumo.ItemId);
        Assert.True(consumo.Aceptacion);
    }

    [Fact]
    public void RegistrarConsumo_Rechazo_GuardaAceptacionFalsa()
    {
        var consumo = CrearMenuServido(_hoy).RegistrarConsumo(_pacienteId, _hoy, _itemId, false);

        Assert.False(consumo.Aceptacion);
    }

    [Fact]
    public void Equality_MismaClaveCompuesta_SonIguales()
    {
        var aceptado = CrearMenuServido(_hoy).RegistrarConsumo(_pacienteId, _hoy, _itemId, true);
        var rechazado = CrearMenuServido(_hoy).RegistrarConsumo(_pacienteId, _hoy, _itemId, false);

        Assert.Equal(aceptado, rechazado);
    }

    [Fact]
    public void Equality_DistintoServicio_NoSonIguales()
    {
        var ayer = _hoy.AddDays(-1);

        var primero = CrearMenuServido(_hoy).RegistrarConsumo(_pacienteId, _hoy, _itemId, true);
        var segundo = CrearMenuServido(ayer).RegistrarConsumo(_pacienteId, ayer, _itemId, true);

        Assert.NotEqual(primero, segundo);
    }
}
