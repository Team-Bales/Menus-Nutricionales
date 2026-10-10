using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;

namespace Menus.Domain.Tests.Entidades;

public sealed class MenuTests
{
    private readonly Guid _dietaId = Guid.NewGuid();
    private readonly Guid _nutricionistaId = Guid.NewGuid();
    private readonly Guid _grupoId1 = Guid.NewGuid();
    private readonly Guid _grupoId2 = Guid.NewGuid();
    private readonly Guid _alimentoId = Guid.NewGuid();
    private readonly Guid _platoId = Guid.NewGuid();
    private readonly DateTime _ahora = DateTime.UtcNow;

    private static readonly (TipoPreparacion, decimal)[] DistribucionValida =
    [
        (TipoPreparacion.Entrante, 0.25m),
        (TipoPreparacion.PlatoFuerte, 0.5m),
        (TipoPreparacion.Postre, 0.25m),
    ];

    private Menu CrearMenu(
        IEnumerable<(TipoPreparacion, decimal)>? distribucion = null,
        IEnumerable<Guid>? grupos = null,
        int cantidadTotalAlimentos = 6,
        DateTime? fechaCreacion = null) =>
        new(
            Guid.NewGuid(),
            _dietaId,
            _nutricionistaId,
            fechaCreacion ?? _ahora,
            cantidadTotalAlimentos,
            true,
            distribucion ?? DistribucionValida,
            grupos ?? [_grupoId1, _grupoId2]);

    [Fact]
    public void Constructor_ValidArgs_SetsProperties()
    {
        var id = Guid.NewGuid();
        var fecha = _ahora.AddDays(-3);

        var menu = new Menu(id, _dietaId, _nutricionistaId, fecha, 8, false, DistribucionValida, [_grupoId1]);

        Assert.Equal(id, menu.Id);
        Assert.Equal(_dietaId, menu.DietaId);
        Assert.Equal(_nutricionistaId, menu.NutricionistaCreadorId);
        Assert.Equal(fecha, menu.FechaCreacion);
        Assert.Equal(8, menu.CantidadTotalAlimentos);
        Assert.False(menu.EsAutomatico);
        Assert.Empty(menu.Alimentos);
        Assert.Empty(menu.Platos);
    }

    [Fact]
    public void Constructor_ValidArgs_RegistraDistribucionYGrupos()
    {
        var menu = CrearMenu();

        Assert.Equal(3, menu.Distribuciones.Count);
        Assert.All(menu.Distribuciones, d => Assert.Equal(menu.Id, d.MenuId));
        Assert.Contains(menu.Distribuciones, d => d.TipoPreparacion == TipoPreparacion.PlatoFuerte && d.Proporcion == 0.5m);
        Assert.Equal(2, menu.GruposNutricionales.Count);
        Assert.All(menu.GruposNutricionales, g => Assert.Equal(menu.Id, g.MenuId));
        Assert.Contains(menu.GruposNutricionales, g => g.GrupoNutricionalId == _grupoId2);
    }

    [Fact]
    public void Constructor_FechaCreacionFutura_LanzaArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CrearMenu(fechaCreacion: _ahora.AddDays(1)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_CantidadTotalAlimentosNoPositiva_LanzaArgumentOutOfRangeException(int cantidad)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CrearMenu(cantidadTotalAlimentos: cantidad));
    }

    [Fact]
    public void Constructor_DistribucionNula_LanzaArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Menu(Guid.NewGuid(), _dietaId, _nutricionistaId, _ahora, 6, true, null!, [_grupoId1]));
    }

    [Fact]
    public void Constructor_GruposNulos_LanzaArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Menu(Guid.NewGuid(), _dietaId, _nutricionistaId, _ahora, 6, true, DistribucionValida, null!));
    }

    [Fact]
    public void Constructor_DistribucionVacia_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CrearMenu(distribucion: []));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.5)]
    [InlineData(1.5)]
    public void Constructor_ProporcionFueraDeRango_LanzaArgumentOutOfRangeException(decimal proporcion)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CrearMenu(distribucion: [(TipoPreparacion.PlatoFuerte, proporcion), (TipoPreparacion.Bebida, 1m - proporcion)]));
    }

    [Fact]
    public void Constructor_ProporcionUnica_IgualAUno_EsValida()
    {
        var menu = CrearMenu(distribucion: [(TipoPreparacion.PlatoFuerte, 1m)]);

        var distribucion = Assert.Single(menu.Distribuciones);
        Assert.Equal(1m, distribucion.Proporcion);
    }

    [Fact]
    public void Constructor_ProporcionesQueNoSumanUno_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            CrearMenu(distribucion: [(TipoPreparacion.Entrante, 0.3m), (TipoPreparacion.PlatoFuerte, 0.5m)]));
    }

    [Fact]
    public void Constructor_TipoPreparacionRepetido_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            CrearMenu(distribucion: [(TipoPreparacion.PlatoFuerte, 0.5m), (TipoPreparacion.PlatoFuerte, 0.5m)]));
    }

    [Fact]
    public void Constructor_SinGruposNutricionales_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CrearMenu(grupos: []));
    }

    [Fact]
    public void Constructor_GrupoNutricionalRepetido_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CrearMenu(grupos: [_grupoId1, _grupoId1]));
    }

    [Fact]
    public void AgregarAlimento_ValidArgs_AgregaYRetornaInclusion()
    {
        var menu = CrearMenu();

        var inclusion = menu.AgregarAlimento(_alimentoId);

        Assert.Single(menu.Alimentos);
        Assert.Equal(menu.Id, inclusion.MenuId);
        Assert.Equal(_alimentoId, inclusion.AlimentoId);
    }

    [Fact]
    public void AgregarAlimento_AlimentoDuplicado_LanzaInvalidOperationException()
    {
        var menu = CrearMenu();
        menu.AgregarAlimento(_alimentoId);

        Assert.Throws<InvalidOperationException>(() => menu.AgregarAlimento(_alimentoId));
    }

    [Fact]
    public void AgregarPlato_ValidArgs_AgregaYRetornaInclusion()
    {
        var menu = CrearMenu();

        var inclusion = menu.AgregarPlato(_platoId);

        Assert.Single(menu.Platos);
        Assert.Equal(menu.Id, inclusion.MenuId);
        Assert.Equal(_platoId, inclusion.PlatoId);
    }

    [Fact]
    public void AgregarPlato_PlatoDuplicado_LanzaInvalidOperationException()
    {
        var menu = CrearMenu();
        menu.AgregarPlato(_platoId);

        Assert.Throws<InvalidOperationException>(() => menu.AgregarPlato(_platoId));
    }

    [Fact]
    public void Colecciones_SonDeSoloLectura()
    {
        var menu = CrearMenu();

        Assert.IsAssignableFrom<IReadOnlyCollection<MenuAlimento>>(menu.Alimentos);
        Assert.IsAssignableFrom<IReadOnlyCollection<MenuPlato>>(menu.Platos);
        Assert.IsAssignableFrom<IReadOnlyCollection<MenuDistribucion>>(menu.Distribuciones);
        Assert.IsAssignableFrom<IReadOnlyCollection<MenuGrupoNutricional>>(menu.GruposNutricionales);
        Assert.False(menu.Distribuciones is ICollection<MenuDistribucion> { IsReadOnly: false });
        Assert.False(menu.GruposNutricionales is ICollection<MenuGrupoNutricional> { IsReadOnly: false });
    }
}
