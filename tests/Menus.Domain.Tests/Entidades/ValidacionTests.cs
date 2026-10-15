using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;

namespace Menus.Domain.Tests.Entidades;

public sealed class ValidacionTests
{
    private readonly Guid _menuId = Guid.NewGuid();
    private readonly Guid _creadorId = Guid.NewGuid();
    private readonly DateTime _creacion = DateTime.UtcNow.AddDays(-1);

    // Validacion solo se crea a través de Menu.Validar; dos instancias de Menu con el mismo Id
    // representan el mismo menú leído dos veces.
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
    public void Validar_SinObservaciones_DejaObservacionesNulas()
    {
        var nutricionistaId = Guid.NewGuid();
        var fecha = _creacion.AddHours(2);

        var validacion = CrearMenu().Validar(nutricionistaId, fecha, true);

        Assert.Equal(nutricionistaId, validacion.NutricionistaId);
        Assert.Equal(_menuId, validacion.MenuId);
        Assert.Equal(fecha, validacion.FechaValidacion);
        Assert.True(validacion.Aprobado);
        Assert.Null(validacion.Observaciones);
    }

    [Fact]
    public void Validar_ConObservaciones_GuardaTodasLasPropiedades()
    {
        var validacion = CrearMenu().Validar(Guid.NewGuid(), _creacion.AddHours(2), false, "Ajustar la distribución.");

        Assert.False(validacion.Aprobado);
        Assert.Equal("Ajustar la distribución.", validacion.Observaciones);
    }

    [Theory]
    [InlineData(DateTimeKind.Local, 60)]
    [InlineData(DateTimeKind.Unspecified, 60)]
    [InlineData(DateTimeKind.Local, -60)]
    [InlineData(DateTimeKind.Unspecified, -60)]
    public void Validar_FechaNoUtc_LanzaArgumentException(DateTimeKind kind, int minutosDesdeCreacion)
    {
        // Antes o después de la creación, una fecha que no está en UTC se rechaza por eso mismo.
        var fecha = DateTime.SpecifyKind(_creacion.AddMinutes(minutosDesdeCreacion), kind);
        var menu = CrearMenu();

        Assert.Throws<ArgumentException>(() => menu.Validar(Guid.NewGuid(), fecha, true));
        Assert.Empty(menu.Validaciones);
    }

    [Fact]
    public void Equality_MismaClaveCompuesta_SonIguales()
    {
        var nutricionistaId = Guid.NewGuid();

        var primera = CrearMenu().Validar(nutricionistaId, _creacion.AddHours(1), true);
        var segunda = CrearMenu().Validar(nutricionistaId, _creacion.AddHours(2), false);

        Assert.Equal(primera, segunda);
    }

    [Fact]
    public void Equality_DistintoMenu_NoSonIguales()
    {
        var nutricionistaId = Guid.NewGuid();

        var primera = CrearMenu(Guid.NewGuid()).Validar(nutricionistaId, _creacion.AddHours(1), true);
        var segunda = CrearMenu(Guid.NewGuid()).Validar(nutricionistaId, _creacion.AddHours(1), true);

        Assert.NotEqual(primera, segunda);
    }

    [Fact]
    public void Equality_DistintoRevisor_NoSonIguales()
    {
        var menu = CrearMenu();

        var primera = menu.Validar(Guid.NewGuid(), _creacion.AddHours(1), true);
        var segunda = menu.Validar(Guid.NewGuid(), _creacion.AddHours(1), true);

        Assert.NotEqual(primera, segunda);
    }
}
