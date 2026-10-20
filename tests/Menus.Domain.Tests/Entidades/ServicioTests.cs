using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;

namespace Menus.Domain.Tests.Entidades;

public sealed class ServicioTests
{
    private readonly Guid _menuId = Guid.NewGuid();
    private readonly Guid _pacienteId = Guid.NewGuid();

    // Servicio solo se crea a través de Menu.RegistrarServicio, en un menú final asignado al paciente. Dos
    // instancias de Menu con el mismo Id representan el mismo menú leído dos veces.
    private Menu CrearMenuAsignado()
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
        menu.Aprobar(Guid.NewGuid(), DateTime.UtcNow.AddDays(-1));
        menu.AsignarPaciente(_pacienteId);
        return menu;
    }

    [Fact]
    public void RegistrarServicio_TomaLaClaveDeLaAsignacion()
    {
        var servicio = CrearMenuAsignado().RegistrarServicio(_pacienteId, DateTime.UtcNow.AddHours(-3));

        Assert.Equal(_menuId, servicio.MenuId);
        Assert.Equal(_pacienteId, servicio.PacienteId);
        Assert.Equal(DateTimeKind.Utc, servicio.FechaServicio.Kind);
        Assert.Empty(servicio.Consumos);
    }

    [Fact]
    public void FechaServicio_SeTruncaAlSegundo()
    {
        var hace3Horas = DateTime.UtcNow.AddHours(-3);
        var segundoExacto = new DateTime(hace3Horas.Ticks - (hace3Horas.Ticks % TimeSpan.TicksPerSecond), DateTimeKind.Utc);
        var conFraccion = segundoExacto.AddTicks(TimeSpan.TicksPerSecond - 1);

        var servicio = CrearMenuAsignado().RegistrarServicio(_pacienteId, conFraccion);

        Assert.Equal(segundoExacto, servicio.FechaServicio);
    }

    [Fact]
    public void Equality_MismaClaveCompuesta_SonIguales()
    {
        var fecha = DateTime.UtcNow.AddHours(-3);

        var primero = CrearMenuAsignado().RegistrarServicio(_pacienteId, fecha);
        var segundo = CrearMenuAsignado().RegistrarServicio(_pacienteId, fecha);

        Assert.Equal(primero, segundo);
    }

    [Fact]
    public void Equality_DistintaFecha_NoSonIguales()
    {
        var primero = CrearMenuAsignado().RegistrarServicio(_pacienteId, DateTime.UtcNow.AddHours(-3));
        var segundo = CrearMenuAsignado().RegistrarServicio(_pacienteId, DateTime.UtcNow.AddHours(-2));

        Assert.NotEqual(primero, segundo);
    }
}
