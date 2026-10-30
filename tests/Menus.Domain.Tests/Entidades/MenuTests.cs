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

    // Un menú de la misma dieta, rechazado por un revisor distinto del creador.
    private Menu CrearMenuRechazado()
    {
        var menu = CrearMenu(fechaCreacion: _ahora.AddDays(-1));
        menu.Rechazar(Guid.NewGuid(), _ahora, "Rehacer con más frutas.");
        return menu;
    }

    // Un menú con un alimento y un plato, aprobado ayer y asignado a un paciente: se puede servir desde el día de
    // la aprobación hasta hoy.
    private Menu CrearMenuServido(out Guid pacienteId)
    {
        var menu = CrearMenu(fechaCreacion: _ahora.AddDays(-3));
        menu.AgregarItem(_alimentoId);
        menu.AgregarItem(_platoId);
        menu.Aprobar(Guid.NewGuid(), _ahora.AddDays(-1));
        pacienteId = Guid.NewGuid();
        menu.AsignarPaciente(pacienteId);
        return menu;
    }

    // Un día del calendario UTC relativo a hoy.
    private DateOnly Dia(int desplazamiento) => DateOnly.FromDateTime(_ahora).AddDays(desplazamiento);

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
        Assert.Empty(menu.Items);
        Assert.Empty(menu.Asignaciones);
        Assert.Null(menu.Validacion);
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
    public void AgregarItem_Alimento_AgregaYRetornaInclusion()
    {
        var menu = CrearMenu();

        var inclusion = menu.AgregarItem(_alimentoId);

        Assert.Same(inclusion, Assert.Single(menu.Items));
        Assert.Equal(menu.Id, inclusion.MenuId);
        Assert.Equal(_alimentoId, inclusion.ItemId);
    }

    [Fact]
    public void AgregarItem_AlimentoYPlato_QuedanEnLaMismaColeccion()
    {
        var menu = CrearMenu();

        menu.AgregarItem(_alimentoId);
        menu.AgregarItem(_platoId);

        Assert.Equal(2, menu.Items.Count);
        Assert.Contains(menu.Items, i => i.ItemId == _alimentoId);
        Assert.Contains(menu.Items, i => i.ItemId == _platoId);
    }

    [Fact]
    public void AgregarItem_ItemDuplicado_LanzaInvalidOperationException()
    {
        var menu = CrearMenu();
        menu.AgregarItem(_platoId);

        Assert.Throws<InvalidOperationException>(() => menu.AgregarItem(_platoId));
        Assert.Single(menu.Items);
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
        Assert.False(menu.EsRechazado);
        Assert.Null(menu.Validacion);
        Assert.Null(menu.ValidacionOrigenId);
    }

    [Fact]
    public void AgregarItem_MenuConDictamen_LanzaInvalidOperationException()
    {
        var menu = CrearMenuFinal();

        Assert.Throws<InvalidOperationException>(() => menu.AgregarItem(_alimentoId));
    }

    [Fact]
    public void QuitarItem_ItemIncluido_LoQuita()
    {
        var menu = CrearMenu();
        menu.AgregarItem(_alimentoId);
        menu.AgregarItem(_platoId);

        menu.QuitarItem(_alimentoId);

        Assert.Equal(_platoId, Assert.Single(menu.Items).ItemId);
    }

    [Fact]
    public void QuitarItem_ItemNoIncluido_LanzaInvalidOperationException()
    {
        var menu = CrearMenu();

        Assert.Throws<InvalidOperationException>(() => menu.QuitarItem(_alimentoId));
    }

    [Fact]
    public void QuitarItem_MenuConDictamen_LanzaInvalidOperationException()
    {
        var menu = CrearMenu(fechaCreacion: _ahora.AddDays(-1));
        menu.AgregarItem(_alimentoId);
        menu.Aprobar(Guid.NewGuid(), _ahora);

        Assert.Throws<InvalidOperationException>(() => menu.QuitarItem(_alimentoId));
        Assert.Single(menu.Items);
    }

    [Fact]
    public void Aprobar_ValidArgs_RegistraLaValidacionYElMenuPasaAFinal()
    {
        var menu = CrearMenu(fechaCreacion: _ahora.AddDays(-1));
        var revisorId = Guid.NewGuid();

        var validacion = menu.Aprobar(revisorId, _ahora, "Correcto.");

        Assert.Same(validacion, menu.Validacion);
        Assert.Equal(revisorId, validacion.NutricionistaId);
        Assert.Equal(menu.Id, validacion.MenuId);
        Assert.Equal(_ahora, validacion.FechaValidacion);
        Assert.Equal("Correcto.", validacion.Observaciones);
        Assert.True(validacion.Aprobado);
        Assert.True(menu.EsFinal);
        Assert.False(menu.EsPendiente);
        Assert.False(menu.EsRechazado);
    }

    [Fact]
    public void Aprobar_MenuYaAprobado_LanzaInvalidOperationException()
    {
        var menu = CrearMenuFinal();
        var aprobacion = menu.Validacion;

        Assert.Throws<InvalidOperationException>(() => menu.Aprobar(Guid.NewGuid(), _ahora));
        Assert.Same(aprobacion, menu.Validacion);
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
    public void Rechazar_ValidArgs_RegistraElRechazoYElMenuQuedaRechazado()
    {
        // El rechazo se registra (decisión del 24/10, #155): el menú no se borra.
        var menu = CrearMenu(fechaCreacion: _ahora.AddDays(-1));
        var revisorId = Guid.NewGuid();

        var validacion = menu.Rechazar(revisorId, _ahora, "Falta un grupo requerido.");

        Assert.Same(validacion, menu.Validacion);
        Assert.False(validacion.Aprobado);
        Assert.Equal(revisorId, validacion.NutricionistaId);
        Assert.Equal("Falta un grupo requerido.", validacion.Observaciones);
        Assert.True(menu.EsRechazado);
        Assert.False(menu.EsPendiente);
        Assert.False(menu.EsFinal);
    }

    [Fact]
    public void MenuRechazado_NoSeVuelveARevisarNiSeModificaNiSeAsigna()
    {
        var menu = CrearMenu(fechaCreacion: _ahora.AddDays(-1));
        menu.AgregarItem(_alimentoId);
        menu.Rechazar(Guid.NewGuid(), _ahora);

        Assert.Throws<InvalidOperationException>(() => menu.Aprobar(Guid.NewGuid(), _ahora));
        Assert.Throws<InvalidOperationException>(() => menu.Rechazar(Guid.NewGuid(), _ahora));
        Assert.Throws<InvalidOperationException>(() => menu.AgregarItem(_platoId));
        Assert.Throws<InvalidOperationException>(() => menu.QuitarItem(_alimentoId));
        Assert.Throws<InvalidOperationException>(() => menu.AsignarPaciente(Guid.NewGuid()));
        Assert.Throws<InvalidOperationException>(() => menu.RegistrarServicio(Guid.NewGuid(), Dia(0)));
    }

    [Fact]
    public void RegistrarComoReemplazo_DeUnMenuRechazadoDeLaMismaDieta_RegistraElOrigen()
    {
        var rechazado = CrearMenuRechazado();
        var reemplazo = CrearMenu();

        reemplazo.RegistrarComoReemplazo(rechazado);

        Assert.Equal(rechazado.Id, reemplazo.ValidacionOrigenId);
        Assert.True(reemplazo.EsPendiente);
    }

    [Fact]
    public void RegistrarComoReemplazo_ElMenuDeReemplazoSePuedeAprobar()
    {
        var rechazado = CrearMenuRechazado();
        var reemplazo = CrearMenu(fechaCreacion: _ahora.AddDays(-1));
        reemplazo.RegistrarComoReemplazo(rechazado);

        reemplazo.Aprobar(Guid.NewGuid(), _ahora);

        Assert.True(reemplazo.EsFinal);
        Assert.Equal(rechazado.Id, reemplazo.ValidacionOrigenId);
    }

    [Fact]
    public void RegistrarComoReemplazo_DeUnMenuAprobado_LanzaInvalidOperationException()
    {
        // RS-17: solo un rechazo origina un menú de reemplazo.
        var reemplazo = CrearMenu();

        Assert.Throws<InvalidOperationException>(() => reemplazo.RegistrarComoReemplazo(CrearMenuFinal()));
        Assert.Null(reemplazo.ValidacionOrigenId);
    }

    [Fact]
    public void RegistrarComoReemplazo_DeUnMenuPendiente_LanzaInvalidOperationException()
    {
        var reemplazo = CrearMenu();

        Assert.Throws<InvalidOperationException>(() => reemplazo.RegistrarComoReemplazo(CrearMenu()));
    }

    [Fact]
    public void RegistrarComoReemplazo_DeOtraDieta_LanzaInvalidOperationException()
    {
        // RS-18: el menú de reemplazo es de la misma dieta que el rechazado.
        var rechazado = new Menu(Guid.NewGuid(), Guid.NewGuid(), _nutricionistaId, _ahora.AddDays(-1), 6, true,
            DistribucionValida, [_grupoId1]);
        rechazado.Rechazar(Guid.NewGuid(), _ahora);

        Assert.Throws<InvalidOperationException>(() => CrearMenu().RegistrarComoReemplazo(rechazado));
    }

    [Fact]
    public void RegistrarComoReemplazo_DeSiMismo_LanzaInvalidOperationException()
    {
        // RS-19: un menú no es reemplazo de sí mismo.
        var menu = CrearMenu();

        Assert.Throws<InvalidOperationException>(() => menu.RegistrarComoReemplazo(menu));
    }

    [Fact]
    public void RegistrarComoReemplazo_DosVeces_LanzaInvalidOperationException()
    {
        var reemplazo = CrearMenu();
        reemplazo.RegistrarComoReemplazo(CrearMenuRechazado());

        Assert.Throws<InvalidOperationException>(() => reemplazo.RegistrarComoReemplazo(CrearMenuRechazado()));
    }

    [Fact]
    public void RegistrarComoReemplazo_MenuConDictamen_LanzaInvalidOperationException()
    {
        // El vínculo se registra mientras el menú de reemplazo espera revisión.
        var reemplazo = CrearMenuFinal();

        Assert.Throws<InvalidOperationException>(() => reemplazo.RegistrarComoReemplazo(CrearMenuRechazado()));
    }

    [Fact]
    public void RegistrarComoReemplazo_Nulo_LanzaArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => CrearMenu().RegistrarComoReemplazo(null!));
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
    public void RegistrarServicio_ValidArgs_LoRegistraEnLaAsignacion()
    {
        var menu = CrearMenuServido(out var pacienteId);

        var servicio = menu.RegistrarServicio(pacienteId, Dia(0));

        var asignacion = Assert.Single(menu.Asignaciones);
        Assert.Same(servicio, Assert.Single(asignacion.Servicios));
        Assert.Equal(menu.Id, servicio.MenuId);
        Assert.Equal(pacienteId, servicio.PacienteId);
        Assert.Equal(Dia(0), servicio.FechaServicio);
        Assert.Empty(servicio.Consumos);
    }

    [Fact]
    public void RegistrarServicio_VariosDias_TodosRegistrados()
    {
        var menu = CrearMenuServido(out var pacienteId);

        menu.RegistrarServicio(pacienteId, Dia(-1));
        menu.RegistrarServicio(pacienteId, Dia(0));

        Assert.Equal(2, Assert.Single(menu.Asignaciones).Servicios.Count);
    }

    [Fact]
    public void RegistrarServicio_ElDiaDeLaAprobacion_EsValido()
    {
        // RS-14 compara días: el menú aprobado ayer se puede servir ayer mismo.
        var menu = CrearMenuServido(out var pacienteId);

        var servicio = menu.RegistrarServicio(pacienteId, Dia(-1));

        Assert.Equal(Dia(-1), servicio.FechaServicio);
    }

    [Fact]
    public void RegistrarServicio_MismoDia_LanzaInvalidOperationException()
    {
        // Un servicio es un día de la asignación: el mismo día no se registra dos veces.
        var menu = CrearMenuServido(out var pacienteId);
        menu.RegistrarServicio(pacienteId, Dia(0));

        Assert.Throws<InvalidOperationException>(() => menu.RegistrarServicio(pacienteId, Dia(0)));
    }

    [Fact]
    public void RegistrarServicio_MenuPendiente_LanzaInvalidOperationException()
    {
        var menu = CrearMenu();

        Assert.Throws<InvalidOperationException>(() => menu.RegistrarServicio(Guid.NewGuid(), Dia(0)));
    }

    [Fact]
    public void RegistrarServicio_PacienteSinAsignacion_LanzaInvalidOperationException()
    {
        var menu = CrearMenuServido(out _);

        Assert.Throws<InvalidOperationException>(() => menu.RegistrarServicio(Guid.NewGuid(), Dia(0)));
    }

    [Fact]
    public void RegistrarServicio_DiaFuturo_LanzaArgumentOutOfRangeException()
    {
        var menu = CrearMenuServido(out var pacienteId);

        Assert.Throws<ArgumentOutOfRangeException>(() => menu.RegistrarServicio(pacienteId, Dia(1)));
    }

    [Fact]
    public void RegistrarServicio_AnteriorALaAprobacion_LanzaArgumentOutOfRangeException()
    {
        var menu = CrearMenuServido(out var pacienteId);

        Assert.Throws<ArgumentOutOfRangeException>(() => menu.RegistrarServicio(pacienteId, Dia(-2)));
        Assert.Empty(Assert.Single(menu.Asignaciones).Servicios);
    }

    [Fact]
    public void RegistrarConsumo_ValidArgs_LoRegistraEnElServicio()
    {
        var menu = CrearMenuServido(out var pacienteId);
        var servicio = menu.RegistrarServicio(pacienteId, Dia(0));

        var consumo = menu.RegistrarConsumo(pacienteId, Dia(0), _alimentoId, true);

        Assert.Same(consumo, Assert.Single(servicio.Consumos));
        Assert.Equal(menu.Id, consumo.MenuId);
        Assert.Equal(pacienteId, consumo.PacienteId);
        Assert.Equal(Dia(0), consumo.FechaServicio);
        Assert.Equal(_alimentoId, consumo.ItemId);
        Assert.True(consumo.Aceptacion);
    }

    [Fact]
    public void RegistrarConsumo_PlatoEntero_SeRegistraComoUnItem()
    {
        var menu = CrearMenuServido(out var pacienteId);
        menu.RegistrarServicio(pacienteId, Dia(0));

        var consumo = menu.RegistrarConsumo(pacienteId, Dia(0), _platoId, false);

        Assert.Equal(_platoId, consumo.ItemId);
        Assert.False(consumo.Aceptacion);
    }

    [Fact]
    public void RegistrarConsumo_MismoItemEnDosServicios_SeRegistraEnCadaUno()
    {
        // El caso que el modelo anterior no admitía: el mismo menú servido dos días.
        var menu = CrearMenuServido(out var pacienteId);
        var primero = menu.RegistrarServicio(pacienteId, Dia(-1));
        var segundo = menu.RegistrarServicio(pacienteId, Dia(0));

        menu.RegistrarConsumo(pacienteId, Dia(-1), _alimentoId, true);
        menu.RegistrarConsumo(pacienteId, Dia(0), _alimentoId, false);

        Assert.True(Assert.Single(primero.Consumos).Aceptacion);
        Assert.False(Assert.Single(segundo.Consumos).Aceptacion);
    }

    [Fact]
    public void RegistrarConsumo_MismoItemDosVecesEnUnServicio_LanzaInvalidOperationException()
    {
        var menu = CrearMenuServido(out var pacienteId);
        menu.RegistrarServicio(pacienteId, Dia(0));
        menu.RegistrarConsumo(pacienteId, Dia(0), _alimentoId, true);

        Assert.Throws<InvalidOperationException>(() => menu.RegistrarConsumo(pacienteId, Dia(0), _alimentoId, false));
    }

    [Fact]
    public void RegistrarConsumo_ItemNoIncluidoEnElMenu_LanzaInvalidOperationException()
    {
        var menu = CrearMenuServido(out var pacienteId);
        var servicio = menu.RegistrarServicio(pacienteId, Dia(0));

        Assert.Throws<InvalidOperationException>(() => menu.RegistrarConsumo(pacienteId, Dia(0), Guid.NewGuid(), true));
        Assert.Empty(servicio.Consumos);
    }

    [Fact]
    public void RegistrarConsumo_SinServicioEseDia_LanzaInvalidOperationException()
    {
        var menu = CrearMenuServido(out var pacienteId);
        menu.RegistrarServicio(pacienteId, Dia(0));

        Assert.Throws<InvalidOperationException>(() => menu.RegistrarConsumo(pacienteId, Dia(-1), _alimentoId, true));
    }

    [Fact]
    public void RegistrarConsumo_PacienteSinAsignacion_LanzaInvalidOperationException()
    {
        var menu = CrearMenuServido(out _);

        Assert.Throws<InvalidOperationException>(() => menu.RegistrarConsumo(Guid.NewGuid(), Dia(0), _alimentoId, true));
    }

    [Fact]
    public void Colecciones_SonDeSoloLectura()
    {
        var menu = CrearMenu();

        Assert.IsAssignableFrom<IReadOnlyCollection<MenuItem>>(menu.Items);
        Assert.IsAssignableFrom<IReadOnlyCollection<MenuDistribucion>>(menu.Distribuciones);
        Assert.IsAssignableFrom<IReadOnlyCollection<MenuGrupoNutricional>>(menu.GruposNutricionales);
        Assert.IsAssignableFrom<IReadOnlyCollection<Asignacion>>(menu.Asignaciones);
        Assert.False(menu.Items is ICollection<MenuItem> { IsReadOnly: false });
        Assert.False(menu.Distribuciones is ICollection<MenuDistribucion> { IsReadOnly: false });
        Assert.False(menu.GruposNutricionales is ICollection<MenuGrupoNutricional> { IsReadOnly: false });
    }
}
