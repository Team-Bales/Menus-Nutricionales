using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;

namespace Menus.Domain.Tests.Entidades;

public sealed class ValidacionTests
{
    private readonly Guid _menuId = Guid.NewGuid();
    private readonly Guid _creadorId = Guid.NewGuid();
    private readonly DateTime _creacion = DateTime.UtcNow.AddDays(-1);

    // Validacion solo se crea a través de Menu.Aprobar; dos instancias de Menu con el mismo Id
    // representan el mismo menú leído dos veces. Su identidad es el menú (R7-32).
    private Menu CrearMenu(Guid? id = null) =>
        new(
            id ?? _menuId,
            Guid.NewGuid(),
            _creadorId,
            _creacion,
            4,
            false,
            [(TipoPreparacion.PlatoFuerte.Id, 1m)],
            [Guid.NewGuid()]);

    [Fact]
    public void Aprobar_SinObservaciones_DejaObservacionesNulas()
    {
        var nutricionistaId = Guid.NewGuid();
        var fecha = _creacion.AddHours(2);

        var validacion = CrearMenu().Aprobar(nutricionistaId, fecha);

        Assert.Equal(nutricionistaId, validacion.NutricionistaId);
        Assert.Equal(_menuId, validacion.MenuId);
        Assert.Equal(fecha, validacion.FechaValidacion);
        Assert.Null(validacion.Observaciones);
    }

    [Fact]
    public void Aprobar_ConObservaciones_LasGuarda()
    {
        var validacion = CrearMenu().Aprobar(Guid.NewGuid(), _creacion.AddHours(2), "Revisar la porción de postre.");

        Assert.Equal("Revisar la porción de postre.", validacion.Observaciones);
    }

    [Fact]
    public void Aprobado_SiempreEsVerdadero_PorqueElRechazoNoSeRegistra()
    {
        var validacion = CrearMenu().Aprobar(Guid.NewGuid(), _creacion.AddHours(2));

        Assert.True(validacion.Aprobado);
    }

    [Theory]
    [InlineData(DateTimeKind.Local, 60)]
    [InlineData(DateTimeKind.Unspecified, 60)]
    [InlineData(DateTimeKind.Local, -60)]
    [InlineData(DateTimeKind.Unspecified, -60)]
    public void Aprobar_FechaNoUtc_LanzaArgumentException(DateTimeKind kind, int minutosDesdeCreacion)
    {
        // Antes o después de la creación, una fecha que no está en UTC se rechaza por eso mismo.
        var fecha = DateTime.SpecifyKind(_creacion.AddMinutes(minutosDesdeCreacion), kind);
        var menu = CrearMenu();

        Assert.Throws<ArgumentException>(() => menu.Aprobar(Guid.NewGuid(), fecha));
        Assert.Null(menu.Validacion);
    }

    [Fact]
    public void Identidad_EsElMenu()
    {
        var validacion = CrearMenu().Aprobar(Guid.NewGuid(), _creacion.AddHours(1));

        Assert.Equal(_menuId, validacion.Id);
        Assert.Equal(validacion.Id, validacion.MenuId);
    }

    [Fact]
    public void Equality_MismoMenu_SonIguales()
    {
        var nutricionistaId = Guid.NewGuid();

        var primera = CrearMenu().Aprobar(nutricionistaId, _creacion.AddHours(1));
        var segunda = CrearMenu().Aprobar(nutricionistaId, _creacion.AddHours(2), "Otra lectura del mismo registro.");

        Assert.Equal(primera, segunda);
    }

    [Fact]
    public void Equality_DistintoMenu_NoSonIguales()
    {
        var nutricionistaId = Guid.NewGuid();

        var primera = CrearMenu(Guid.NewGuid()).Aprobar(nutricionistaId, _creacion.AddHours(1));
        var segunda = CrearMenu(Guid.NewGuid()).Aprobar(nutricionistaId, _creacion.AddHours(1));

        Assert.NotEqual(primera, segunda);
    }

    [Fact]
    public void Equality_MismoMenuConOtroRevisor_SonIguales_PorqueElMenuEsLaClave()
    {
        // R7-32: un menú tiene a lo sumo una validación, así que dos lecturas del mismo menú son la misma
        // validación aunque el revisor no coincida (en la base no pueden coexistir).
        var primera = CrearMenu().Aprobar(Guid.NewGuid(), _creacion.AddHours(1));
        var segunda = CrearMenu().Aprobar(Guid.NewGuid(), _creacion.AddHours(1));

        Assert.Equal(primera, segunda);
    }
}
