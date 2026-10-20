using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;

namespace Menus.Domain.Tests.Entidades;

public sealed class ServicioTests
{
    private readonly Guid _menuId = Guid.NewGuid();
    private readonly Guid _pacienteId = Guid.NewGuid();
    private readonly DateOnly _hoy = DateOnly.FromDateTime(DateTime.UtcNow);

    // Servicio solo se crea a través de Menu.RegistrarServicio, en un menú final asignado al paciente. Dos
    // instancias de Menu con el mismo Id representan el mismo menú leído dos veces.
    private Menu CrearMenuAsignado()
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
        menu.Aprobar(Guid.NewGuid(), DateTime.UtcNow.AddDays(-2));
        menu.AsignarPaciente(_pacienteId);
        return menu;
    }

    [Fact]
    public void RegistrarServicio_TomaLaClaveDeLaAsignacionYElDia()
    {
        var servicio = CrearMenuAsignado().RegistrarServicio(_pacienteId, _hoy);

        Assert.Equal(_menuId, servicio.MenuId);
        Assert.Equal(_pacienteId, servicio.PacienteId);
        Assert.Equal(_hoy, servicio.FechaServicio);
        Assert.Empty(servicio.Consumos);
    }

    [Fact]
    public void Equality_MismaClaveCompuesta_SonIguales()
    {
        var primero = CrearMenuAsignado().RegistrarServicio(_pacienteId, _hoy);
        var segundo = CrearMenuAsignado().RegistrarServicio(_pacienteId, _hoy);

        Assert.Equal(primero, segundo);
    }

    [Fact]
    public void Equality_DistintoDia_NoSonIguales()
    {
        var primero = CrearMenuAsignado().RegistrarServicio(_pacienteId, _hoy.AddDays(-1));
        var segundo = CrearMenuAsignado().RegistrarServicio(_pacienteId, _hoy);

        Assert.NotEqual(primero, segundo);
    }
}
