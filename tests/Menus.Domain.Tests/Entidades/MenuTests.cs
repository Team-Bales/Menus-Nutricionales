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

    private static readonly (Guid, decimal)[] DistribucionValida =
    [
        (TipoPreparacion.Entrante.Id, 0.25m),
        (TipoPreparacion.PlatoFuerte.Id, 0.5m),
        (TipoPreparacion.Postre.Id, 0.25m),
    ];

    private Menu CrearMenu(
        IEnumerable<(Guid, decimal)>? distribucion = null,
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

    // Un menú aprobado por un revisor distinto del creador: solo los menús finales se asignan.
    private Menu CrearMenuFinal()
    {
        var menu = CrearMenu(fechaCreacion: _ahora.AddDays(-1));
        menu.Aprobar(Guid.NewGuid(), _ahora);
        return menu;
    }

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
        Assert.Empty(menu.Asignaciones);
        Assert.Empty(menu.Validaciones);
    }

    [Fact]
    public void Constructor_ValidArgs_RegistraDistribucionYGrupos()
    {
        var menu = CrearMenu();

        Assert.Equal(3, menu.Distribuciones.Count);
        Assert.All(menu.Distribuciones, d => Assert.Equal(menu.Id, d.MenuId));
        Assert.Contains(menu.Distribuciones, d => d.TipoPreparacionId == TipoPreparacion.PlatoFuerte.Id && d.Proporcion == 0.5m);
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
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Constructor_FechaCreacionNoUtc_LanzaArgumentException(DateTimeKind kind)
    {
        var fecha = DateTime.SpecifyKind(_ahora.AddHours(-1), kind);

        Assert.Throws<ArgumentException>(() => CrearMenu(fechaCreacion: fecha));
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
            CrearMenu(distribucion: [(TipoPreparacion.PlatoFuerte.Id, proporcion), (TipoPreparacion.Bebida.Id, 1m - proporcion)]));
    }

    [Fact]
    public void Constructor_ProporcionUnica_IgualAUno_EsValida()
    {
        var menu = CrearMenu(distribucion: [(TipoPreparacion.PlatoFuerte.Id, 1m)]);

        var distribucion = Assert.Single(menu.Distribuciones);
        Assert.Equal(1m, distribucion.Proporcion);
    }

    [Fact]
    public void Constructor_ProporcionesQueNoSumanUno_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            CrearMenu(distribucion: [(TipoPreparacion.Entrante.Id, 0.3m), (TipoPreparacion.PlatoFuerte.Id, 0.5m)]));
    }

    [Theory]
    [InlineData(0.5, 0.49)]
    [InlineData(0.51, 0.5)]
    [InlineData(0.5, 0.499)]
    public void Constructor_SumaFueraDeLaTolerancia_LanzaArgumentException(decimal primera, decimal segunda)
    {
        Assert.Throws<ArgumentException>(() =>
            CrearMenu(distribucion: [(TipoPreparacion.Entrante.Id, primera), (TipoPreparacion.PlatoFuerte.Id, segunda)]));
    }

    [Fact]
    public void Constructor_TresTerciosCalculados_EsValido()
    {
        var tercio = 1m / 3m;

        var menu = CrearMenu(distribucion:
            [(TipoPreparacion.Entrante.Id, tercio), (TipoPreparacion.PlatoFuerte.Id, tercio), (TipoPreparacion.Postre.Id, tercio)]);

        Assert.Equal(3, menu.Distribuciones.Count);
        Assert.NotEqual(1m, menu.Distribuciones.Sum(d => d.Proporcion));
    }

    [Fact]
    public void Constructor_ProporcionesRedondeadasACuatroDecimales_EsValido()
    {
        var menu = CrearMenu(distribucion:
            [(TipoPreparacion.Entrante.Id, 0.3333m), (TipoPreparacion.PlatoFuerte.Id, 0.3333m), (TipoPreparacion.Postre.Id, 0.3333m)]);

        Assert.Equal(0.9999m, menu.Distribuciones.Sum(d => d.Proporcion));
    }

    [Fact]
    public void Constructor_TipoPreparacionRepetido_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            CrearMenu(distribucion: [(TipoPreparacion.PlatoFuerte.Id, 0.5m), (TipoPreparacion.PlatoFuerte.Id, 0.5m)]));
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
    public void Constructor_TipoPreparacionFueraDelCatalogo_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CrearMenu(distribucion: [(Guid.NewGuid(), 1m)]));
    }

    [Fact]
    public void Estado_MenuNuevo_EstaPendienteYNoEsFinal()
    {
        var menu = CrearMenu();

        Assert.True(menu.EsPendiente);
        Assert.False(menu.EsFinal);
        Assert.Empty(menu.Validaciones);
    }

    [Fact]
    public void AgregarAlimento_MenuConDictamen_LanzaInvalidOperationException()
    {
        var menu = CrearMenuFinal();

        Assert.Throws<InvalidOperationException>(() => menu.AgregarAlimento(_alimentoId));
    }

    [Fact]
    public void AgregarPlato_MenuConDictamen_LanzaInvalidOperationException()
    {
        var menu = CrearMenuFinal();

        Assert.Throws<InvalidOperationException>(() => menu.AgregarPlato(_platoId));
    }

    [Fact]
    public void QuitarAlimento_AlimentoIncluido_LoQuita()
    {
        var menu = CrearMenu();
        var otroAlimentoId = Guid.NewGuid();
        menu.AgregarAlimento(_alimentoId);
        menu.AgregarAlimento(otroAlimentoId);

        menu.QuitarAlimento(_alimentoId);

        Assert.Equal(otroAlimentoId, Assert.Single(menu.Alimentos).AlimentoId);
    }

    [Fact]
    public void QuitarAlimento_AlimentoNoIncluido_LanzaInvalidOperationException()
    {
        var menu = CrearMenu();

        Assert.Throws<InvalidOperationException>(() => menu.QuitarAlimento(_alimentoId));
    }

    [Fact]
    public void QuitarPlato_PlatoIncluido_LoQuita()
    {
        var menu = CrearMenu();
        menu.AgregarPlato(_platoId);

        menu.QuitarPlato(_platoId);

        Assert.Empty(menu.Platos);
    }

    [Fact]
    public void QuitarPlato_PlatoNoIncluido_LanzaInvalidOperationException()
    {
        var menu = CrearMenu();

        Assert.Throws<InvalidOperationException>(() => menu.QuitarPlato(_platoId));
    }

    [Fact]
    public void Quitar_MenuConDictamen_LanzaInvalidOperationException()
    {
        var menu = CrearMenu(fechaCreacion: _ahora.AddDays(-1));
        menu.AgregarAlimento(_alimentoId);
        menu.AgregarPlato(_platoId);
        menu.Aprobar(Guid.NewGuid(), _ahora);

        Assert.Throws<InvalidOperationException>(() => menu.QuitarAlimento(_alimentoId));
        Assert.Throws<InvalidOperationException>(() => menu.QuitarPlato(_platoId));
    }

    [Fact]
    public void Aprobar_ValidArgs_RegistraLaValidacionYElMenuPasaAFinal()
    {
        var menu = CrearMenu(fechaCreacion: _ahora.AddDays(-1));
        var revisorId = Guid.NewGuid();

        var validacion = menu.Aprobar(revisorId, _ahora, "Correcto.");

        Assert.Same(validacion, Assert.Single(menu.Validaciones));
        Assert.Equal(revisorId, validacion.NutricionistaId);
        Assert.Equal(menu.Id, validacion.MenuId);
        Assert.Equal(_ahora, validacion.FechaValidacion);
        Assert.True(validacion.Aprobado);
        Assert.Equal("Correcto.", validacion.Observaciones);
        Assert.True(menu.EsFinal);
        Assert.False(menu.EsPendiente);
    }

    [Fact]
    public void Aprobar_MenuYaAprobado_LanzaInvalidOperationException()
    {
        var menu = CrearMenuFinal();

        Assert.Throws<InvalidOperationException>(() => menu.Aprobar(Guid.NewGuid(), _ahora));
        Assert.Single(menu.Validaciones);
    }

    [Fact]
    public void Aprobar_CreadorDelMenu_LanzaInvalidOperationException()
    {
        var menu = CrearMenu();

        Assert.Throws<InvalidOperationException>(() => menu.Aprobar(_nutricionistaId, _ahora));
    }

    [Fact]
    public void Aprobar_FechaAnteriorACreacion_LanzaArgumentOutOfRangeException()
    {
        var menu = CrearMenu(fechaCreacion: _ahora);

        Assert.Throws<ArgumentOutOfRangeException>(() => menu.Aprobar(Guid.NewGuid(), _ahora.AddMinutes(-1)));
    }

    [Fact]
    public void Rechazar_ValidArgs_NoRegistraNadaYElMenuSigueSinDictamen()
    {
        var menu = CrearMenu(fechaCreacion: _ahora.AddDays(-1));

        menu.Rechazar(Guid.NewGuid(), _ahora);

        Assert.Empty(menu.Validaciones);
        Assert.False(menu.EsFinal);
    }

    [Fact]
    public void Rechazar_MenuYaAprobado_LanzaInvalidOperationException()
    {
        var menu = CrearMenuFinal();

        Assert.Throws<InvalidOperationException>(() => menu.Rechazar(Guid.NewGuid(), _ahora));
    }

    [Fact]
    public void Rechazar_CreadorDelMenu_LanzaInvalidOperationException()
    {
        var menu = CrearMenu();

        Assert.Throws<InvalidOperationException>(() => menu.Rechazar(_nutricionistaId, _ahora));
    }

    [Fact]
    public void Rechazar_FechaAnteriorACreacion_LanzaArgumentOutOfRangeException()
    {
        var menu = CrearMenu(fechaCreacion: _ahora);

        Assert.Throws<ArgumentOutOfRangeException>(() => menu.Rechazar(Guid.NewGuid(), _ahora.AddMinutes(-1)));
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Rechazar_FechaNoUtc_LanzaArgumentException(DateTimeKind kind)
    {
        var menu = CrearMenu(fechaCreacion: _ahora.AddDays(-1));

        Assert.Throws<ArgumentException>(() => menu.Rechazar(Guid.NewGuid(), DateTime.SpecifyKind(_ahora, kind)));
    }

    [Fact]
    public void AsignarPaciente_MenuPendiente_LanzaInvalidOperationException()
    {
        var menu = CrearMenu();

        Assert.Throws<InvalidOperationException>(() => menu.AsignarPaciente(Guid.NewGuid()));
        Assert.Empty(menu.Asignaciones);
    }

    [Fact]
    public void AsignarPaciente_ValidArgs_AgregaYRetornaAsignacion()
    {
        var menu = CrearMenuFinal();
        var pacienteId = Guid.NewGuid();

        var asignacion = menu.AsignarPaciente(pacienteId);

        Assert.Single(menu.Asignaciones);
        Assert.Equal(menu.Id, asignacion.MenuId);
        Assert.Equal(pacienteId, asignacion.PacienteId);
    }

    [Fact]
    public void AsignarPaciente_VariosPacientes_TodosAsignados()
    {
        var menu = CrearMenuFinal();
        var pacientes = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        foreach (var pacienteId in pacientes)
        {
            menu.AsignarPaciente(pacienteId);
        }

        Assert.Equal(3, menu.Asignaciones.Count);
        Assert.All(pacientes, p => Assert.Contains(menu.Asignaciones, a => a.PacienteId == p));
    }

    [Fact]
    public void AsignarPaciente_PacienteDuplicado_LanzaInvalidOperationException()
    {
        var menu = CrearMenuFinal();
        var pacienteId = Guid.NewGuid();
        menu.AsignarPaciente(pacienteId);

        Assert.Throws<InvalidOperationException>(() => menu.AsignarPaciente(pacienteId));
    }

    [Fact]
    public void RegistrarConsumo_ValidArgs_AgregaYRetornaConsumo()
    {
        var asignacion = CrearMenuFinal().AsignarPaciente(Guid.NewGuid());
        var alimentoId = Guid.NewGuid();

        var consumo = asignacion.RegistrarConsumo(alimentoId, true);

        Assert.Single(asignacion.Consumos);
        Assert.Equal(asignacion.MenuId, consumo.MenuId);
        Assert.Equal(asignacion.PacienteId, consumo.PacienteId);
        Assert.Equal(alimentoId, consumo.AlimentoId);
        Assert.True(consumo.Aceptacion);
    }

    [Fact]
    public void RegistrarConsumo_MismoAlimentoDosVeces_LanzaInvalidOperationException()
    {
        var asignacion = CrearMenuFinal().AsignarPaciente(Guid.NewGuid());
        var alimentoId = Guid.NewGuid();
        asignacion.RegistrarConsumo(alimentoId, true);

        Assert.Throws<InvalidOperationException>(() => asignacion.RegistrarConsumo(alimentoId, false));
    }

    [Fact]
    public void Colecciones_SonDeSoloLectura()
    {
        var menu = CrearMenu();

        Assert.IsAssignableFrom<IReadOnlyCollection<MenuAlimento>>(menu.Alimentos);
        Assert.IsAssignableFrom<IReadOnlyCollection<MenuPlato>>(menu.Platos);
        Assert.IsAssignableFrom<IReadOnlyCollection<MenuDistribucion>>(menu.Distribuciones);
        Assert.IsAssignableFrom<IReadOnlyCollection<MenuGrupoNutricional>>(menu.GruposNutricionales);
        Assert.IsAssignableFrom<IReadOnlyCollection<Asignacion>>(menu.Asignaciones);
        Assert.IsAssignableFrom<IReadOnlyCollection<Validacion>>(menu.Validaciones);
        Assert.False(menu.Distribuciones is ICollection<MenuDistribucion> { IsReadOnly: false });
        Assert.False(menu.GruposNutricionales is ICollection<MenuGrupoNutricional> { IsReadOnly: false });
    }
}
