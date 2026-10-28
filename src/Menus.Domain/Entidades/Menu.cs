using Menus.Domain.Abstractions;

namespace Menus.Domain.Entidades;

/// <summary>
/// Menú creado por un nutricionista para una dieta, de forma automática o manual,
/// junto con los parámetros con que se generó.
/// Es la raíz del agregado: los ítems incluidos (alimentos y platos), la distribución por tipo de
/// preparación, los grupos nutricionales cubiertos, la validación, las asignaciones a pacientes y sus
/// servicios y consumos solo se registran a través de él.
/// </summary>
/// <remarks>
/// <para>
/// Los parámetros de generación (<see cref="CantidadTotalAlimentos"/>, <see cref="EsAutomatico"/>,
/// <see cref="Distribuciones"/> y <see cref="GruposNutricionales"/>) se fijan al crear el menú y no
/// cambian después (submodelo #7, RS-07), por eso no hay métodos para modificarlos.
/// </para>
/// <para>
/// El menú no guarda un estado, se deriva de su validación (submodelo #7, §4): está pendiente mientras no
/// tiene dictamen (<see cref="EsPendiente"/>), es final cuando fue aprobado (<see cref="EsFinal"/>) y está
/// rechazado cuando el dictamen lo rechazó (<see cref="EsRechazado"/>). El primer dictamen lo cierra, y los dos se
/// registran (<see cref="Aprobar"/> y <see cref="Rechazar"/>; decisión del equipo del 24/10, que revierte la del
/// 16/10 de borrar el menú rechazado). Por eso un menú tiene a lo sumo una validación (RC-12, RK-13), que después no
/// cambia (RS-15). Solo un menú pendiente se modifica y solo uno final se asigna a pacientes (RS-16). Un menú
/// rechazado no se vuelve a revisar: se confecciona otro, que lo referencia (<see cref="RegistrarComoReemplazo"/>,
/// relación <c>Origina</c>).
/// </para>
/// <para>
/// Tampoco guarda un único paciente: un menú puede asignarse a varios pacientes, una sola vez a cada uno
/// (agregación <c>Asignación</c>, RC-13), y servirse a cada uno las veces que haga falta
/// (<see cref="RegistrarServicio"/>). El servicio y el consumo se registran por la raíz, y no por la asignación,
/// porque sus reglas dependen del menú: solo se sirve después de la aprobación (RS-14) y solo se consume lo que el
/// menú incluye (RX-01).
/// </para>
/// <para>
/// Que el paciente esté inscrito en la dieta del menú (RS-04) lo verifica la capa de aplicación, porque la
/// inscripción pertenece al agregado del paciente; lo mismo ocurre con que el nutricionista creador esté
/// autorizado en esa dieta (RS-03).
/// </para>
/// </remarks>
public sealed class Menu : AggregateRoot<Guid>
{
    private readonly List<MenuItem> _items = [];
    private readonly List<MenuDistribucion> _distribuciones = [];
    private readonly List<MenuGrupoNutricional> _gruposNutricionales = [];
    private readonly List<Asignacion> _asignaciones = [];

    /// <summary>
    /// Diferencia máxima admitida entre la suma de las proporciones y 1 (RS-08).
    /// </summary>
    /// <remarks>
    /// La suma no se compara por igualdad exacta por dos motivos: hay fracciones sin representación
    /// decimal finita (tres veces <c>1m / 3m</c> suma 0,999…9) y las proporciones se redondean al
    /// persistirse. Con cuatro tipos de preparación guardados con cuatro decimales, el error acumulado
    /// es como máximo 0,0002, de modo que esta tolerancia lo absorbe y sigue rechazando sumas como 0,99.
    /// </remarks>
    public static readonly decimal ToleranciaSumaProporciones = 0.001m;

    /// <summary>Crea un menú con sus parámetros de generación.</summary>
    /// <param name="id">Identificador único del menú.</param>
    /// <param name="dietaId">Dieta a la que pertenece el menú (relación <c>Pertenece</c>).</param>
    /// <param name="nutricionistaCreadorId">Nutricionista que creó el menú (relación <c>Crea</c>).</param>
    /// <param name="fechaCreacion">
    /// Instante de creación del menú. Debe estar en UTC (<see cref="DateTimeKind.Utc"/>), porque así se
    /// persiste, y no puede ser posterior al instante actual.
    /// </param>
    /// <param name="cantidadTotalAlimentos">Cantidad total de alimentos del menú (&gt; 0, RD-10).</param>
    /// <param name="esAutomatico">Indica si el menú fue generado automáticamente (consulta 1, §7-A).</param>
    /// <param name="distribucion">
    /// Proporción de alimentos por tipo de preparación: al menos una (RK-09), cada una en (0, 1] (RD-11),
    /// sin tipos repetidos (RC-11) y con suma igual a 1, con la tolerancia
    /// <see cref="ToleranciaSumaProporciones"/> (RS-08).
    /// </param>
    /// <param name="gruposNutricionalesIds">Grupos nutricionales que cubre el menú: al menos uno (RK-09) y sin repetidos (RC-15).</param>
    /// <exception cref="ArgumentNullException">Si <paramref name="distribucion"/> o <paramref name="gruposNutricionalesIds"/> es nulo.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Si <paramref name="fechaCreacion"/> es futura, si <paramref name="cantidadTotalAlimentos"/> no es positiva
    /// o si alguna proporción está fuera de (0, 1].
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Si <paramref name="fechaCreacion"/> no está en UTC, si la distribución o los grupos están vacíos
    /// o tienen repetidos, o si las proporciones no suman 1.
    /// </exception>
    public Menu(
        Guid id,
        Guid dietaId,
        Guid nutricionistaCreadorId,
        DateTime fechaCreacion,
        int cantidadTotalAlimentos,
        bool esAutomatico,
        IEnumerable<(Guid TipoPreparacionId, decimal Proporcion)> distribucion,
        IEnumerable<Guid> gruposNutricionalesIds)
        : this(id, dietaId, nutricionistaCreadorId, fechaCreacion, cantidadTotalAlimentos, esAutomatico)
    {
        ArgumentNullException.ThrowIfNull(distribucion);
        ArgumentNullException.ThrowIfNull(gruposNutricionalesIds);

        FijarDistribucion(distribucion);
        FijarGruposNutricionales(gruposNutricionalesIds);
    }

    /// <summary>
    /// Valida y asigna los datos propios del menú. Lo usa el constructor público y, al leer
    /// un menú existente, la persistencia, que carga las colecciones por separado.
    /// </summary>
    private Menu(
        Guid id,
        Guid dietaId,
        Guid nutricionistaCreadorId,
        DateTime fechaCreacion,
        int cantidadTotalAlimentos,
        bool esAutomatico) : base(id)
    {
        if (fechaCreacion.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("El instante de creación debe estar en UTC (DateTimeKind.Utc).", nameof(fechaCreacion));
        }

        if (fechaCreacion > DateTime.UtcNow)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaCreacion), "El instante de creación no puede ser futuro.");
        }

        if (cantidadTotalAlimentos <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cantidadTotalAlimentos), "La cantidad total de alimentos debe ser estrictamente positiva.");
        }

        DietaId = dietaId;
        NutricionistaCreadorId = nutricionistaCreadorId;
        FechaCreacion = fechaCreacion;
        CantidadTotalAlimentos = cantidadTotalAlimentos;
        EsAutomatico = esAutomatico;
    }

    /// <summary>FK → Dieta a la que pertenece el menú (relación <c>Pertenece</c>).</summary>
    public Guid DietaId { get; }

    /// <summary>FK → Nutricionista que creó el menú (relación <c>Crea</c>).</summary>
    public Guid NutricionistaCreadorId { get; }

    /// <summary>
    /// Instante de creación del menú, en UTC (consulta 1, §2). Los servicios y las valoraciones tienen su propia
    /// fecha (<see cref="Servicio.FechaServicio"/> y <see cref="ValoracionNutricional.FechaValoracion"/>).
    /// </summary>
    public DateTime FechaCreacion { get; }

    /// <summary>Cantidad total de alimentos del menú (&gt; 0). Parámetro de generación.</summary>
    public int CantidadTotalAlimentos { get; }

    /// <summary>Indica si el menú fue generado automáticamente. Parámetro de generación.</summary>
    public bool EsAutomatico { get; }

    /// <summary>Ítems del banco, alimentos y platos, incluidos en el menú (solo lectura).</summary>
    public IReadOnlyCollection<MenuItem> Items => _items.AsReadOnly();

    /// <summary>Proporción de alimentos por tipo de preparación (solo lectura). Parámetro de generación.</summary>
    public IReadOnlyCollection<MenuDistribucion> Distribuciones => _distribuciones.AsReadOnly();

    /// <summary>Grupos nutricionales que cubre el menú (solo lectura). Parámetro de generación.</summary>
    public IReadOnlyCollection<MenuGrupoNutricional> GruposNutricionales => _gruposNutricionales.AsReadOnly();

    /// <summary>Pacientes a los que se asignó el menú, con sus servicios y consumos (solo lectura).</summary>
    public IReadOnlyCollection<Asignacion> Asignaciones => _asignaciones.AsReadOnly();

    /// <summary>
    /// Dictamen del menú (aprobación o rechazo), o <see langword="null"/> si todavía no lo tiene. Es a lo sumo uno
    /// (RC-12, RK-13): el primer dictamen cierra el menú.
    /// </summary>
    /// <remarks>
    /// La persistencia la carga siempre junto con el menú (<c>AutoInclude</c>), porque <see cref="EsPendiente"/>,
    /// <see cref="EsFinal"/> y <see cref="EsRechazado"/> se derivan de ella: un menú leído sin su validación
    /// parecería pendiente y admitiría cambios aunque ya tuviera dictamen.
    /// </remarks>
    public Validacion? Validacion { get; private set; }

    /// <summary>
    /// FK → Validación rechazada que originó este menú (relación <c>Origina</c>), o <see langword="null"/> si el
    /// menú no reemplaza a otro. Su valor es el identificador del menú rechazado, que es la clave de su validación.
    /// </summary>
    public Guid? ValidacionOrigenId { get; private set; }

    /// <summary>Indica si el menú está pendiente de revisión: todavía no tiene dictamen.</summary>
    public bool EsPendiente => Validacion is null;

    /// <summary>Indica si el menú es final: fue aprobado (consulta 2, §7-A).</summary>
    public bool EsFinal => Validacion is { Aprobado: true };

    /// <summary>Indica si el menú fue rechazado. No se modifica, no se asigna y no se vuelve a revisar.</summary>
    public bool EsRechazado => Validacion is { Aprobado: false };

    /// <summary>Incluye un alimento o un plato en el menú (relación <c>Incluye(Menú, ItemAlimentario)</c>).</summary>
    /// <param name="itemId">Identificador del alimento o plato.</param>
    /// <returns>La inclusión registrada.</returns>
    /// <exception cref="InvalidOperationException">
    /// Si el menú ya tiene dictamen o si el ítem ya está incluido en el menú (RC-15).
    /// </exception>
    public MenuItem AgregarItem(Guid itemId)
    {
        ExigirPendiente();

        if (_items.Any(i => i.ItemId == itemId))
        {
            throw new InvalidOperationException($"El ítem {itemId} ya está incluido en este menú.");
        }

        var item = new MenuItem(Id, itemId);
        _items.Add(item);
        return item;
    }

    /// <summary>
    /// Quita un alimento o un plato incluido en el menú. Es parte de la modificación manual que admite un menú
    /// generado mientras espera revisión; los parámetros de generación no se modifican (RS-07).
    /// </summary>
    /// <param name="itemId">Identificador del alimento o plato.</param>
    /// <exception cref="InvalidOperationException">Si el menú ya tiene dictamen o si el ítem no está incluido.</exception>
    public void QuitarItem(Guid itemId)
    {
        ExigirPendiente();

        var item = _items.SingleOrDefault(i => i.ItemId == itemId)
            ?? throw new InvalidOperationException($"El ítem {itemId} no está incluido en este menú.");

        _items.Remove(item);
    }

    /// <summary>Asigna el menú a un paciente (agregación <c>Asignación</c>).</summary>
    /// <param name="pacienteId">Identificador del paciente que recibe el menú.</param>
    /// <returns>La asignación registrada.</returns>
    /// <exception cref="InvalidOperationException">
    /// Si el menú no es final (solo se asignan menús aprobados, RS-16; un menú pendiente o rechazado no) o si ya
    /// está asignado a ese paciente (RC-13).
    /// </exception>
    public Asignacion AsignarPaciente(Guid pacienteId)
    {
        if (!EsFinal)
        {
            throw new InvalidOperationException("Solo se asigna un menú final (aprobado).");
        }

        if (_asignaciones.Any(a => a.PacienteId == pacienteId))
        {
            throw new InvalidOperationException($"El menú ya está asignado al paciente {pacienteId}.");
        }

        var asignacion = new Asignacion(Id, pacienteId);
        _asignaciones.Add(asignacion);
        return asignacion;
    }

    /// <summary>Registra que el menú se le sirvió a un paciente al que está asignado (entidad débil <c>Servicio</c>).</summary>
    /// <param name="pacienteId">Identificador del paciente.</param>
    /// <param name="fechaServicio">
    /// Día del servicio, en el calendario UTC. No puede ser futuro, y tampoco anterior al día de la aprobación del
    /// menú (RS-14).
    /// </param>
    /// <returns>El servicio registrado.</returns>
    /// <exception cref="InvalidOperationException">
    /// Si el menú no es final, si no está asignado al paciente o si ya se le sirvió ese día.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Si el día es futuro o anterior al de la aprobación del menú.</exception>
    public Servicio RegistrarServicio(Guid pacienteId, DateOnly fechaServicio)
    {
        if (Validacion is not { Aprobado: true } aprobacion)
        {
            throw new InvalidOperationException("Solo se sirve un menú final (aprobado).");
        }

        var asignacion = BuscarAsignacion(pacienteId);

        if (fechaServicio > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new ArgumentOutOfRangeException(nameof(fechaServicio), "El día del servicio no puede ser futuro.");
        }

        if (fechaServicio < DateOnly.FromDateTime(aprobacion.FechaValidacion))
        {
            throw new ArgumentOutOfRangeException(nameof(fechaServicio), "El menú no puede servirse antes de su aprobación.");
        }

        return asignacion.RegistrarServicio(fechaServicio);
    }

    /// <summary>
    /// Registra la aceptación o el rechazo de un alimento o un plato del menú en un servicio al paciente
    /// (relación <c>Consume</c>).
    /// </summary>
    /// <param name="pacienteId">Identificador del paciente.</param>
    /// <param name="fechaServicio">Día del servicio, en el calendario UTC.</param>
    /// <param name="itemId">Identificador del alimento o plato consumido.</param>
    /// <param name="aceptacion"><see langword="true"/> si el paciente lo aceptó; de lo contrario, <see langword="false"/>.</param>
    /// <returns>El consumo registrado.</returns>
    /// <exception cref="InvalidOperationException">
    /// Si el menú no está asignado al paciente, si no se le sirvió ese día, si el ítem no está incluido en el menú
    /// (RX-01) o si ya tiene un consumo registrado en ese servicio.
    /// </exception>
    public Consumo RegistrarConsumo(Guid pacienteId, DateOnly fechaServicio, Guid itemId, bool aceptacion)
    {
        var asignacion = BuscarAsignacion(pacienteId);

        var servicio = asignacion.Servicios.SingleOrDefault(s => s.FechaServicio == fechaServicio)
            ?? throw new InvalidOperationException($"El menú no se le sirvió al paciente {pacienteId} el {fechaServicio:yyyy-MM-dd}.");

        if (_items.All(i => i.ItemId != itemId))
        {
            throw new InvalidOperationException($"El ítem {itemId} no está incluido en este menú: solo se consume lo que el menú incluye.");
        }

        return servicio.RegistrarConsumo(itemId, aceptacion);
    }

    /// <summary>Registra que un nutricionista revisor aprobó el menú, que pasa a ser final.</summary>
    /// <param name="nutricionistaId">Identificador del nutricionista revisor.</param>
    /// <param name="fechaValidacion">Instante de la aprobación, en UTC.</param>
    /// <param name="observaciones">Señalamientos del revisor, si existen.</param>
    /// <returns>La validación registrada.</returns>
    /// <exception cref="InvalidOperationException">
    /// Si el menú ya tiene dictamen (el primero lo cierra) o si el revisor es quien creó el menú (RS-05).
    /// </exception>
    /// <exception cref="ArgumentException">Si la fecha no está en UTC.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Si la fecha es anterior a la creación del menú (RS-06).</exception>
    public Validacion Aprobar(Guid nutricionistaId, DateTime fechaValidacion, string? observaciones = null)
    {
        ComprobarDictamen(nutricionistaId, fechaValidacion);

        Validacion = new Validacion(Id, nutricionistaId, fechaValidacion, aprobado: true, observaciones);
        return Validacion;
    }

    /// <summary>
    /// Registra que un nutricionista revisor rechazó el menú, que queda rechazado: no se modifica, no se asigna y
    /// no se vuelve a revisar. El menú no se borra; si el jefe indica confeccionar otro, el nuevo lo referencia
    /// (<see cref="RegistrarComoReemplazo"/>). Decisión del equipo del 24/10 (#155), que revierte la del 16/10.
    /// </summary>
    /// <param name="nutricionistaId">Identificador del nutricionista revisor.</param>
    /// <param name="fechaRechazo">Instante del rechazo, en UTC.</param>
    /// <param name="observaciones">
    /// Señalamientos del revisor, si existen. Son los criterios con los que se pide confeccionar otro menú, y los
    /// que lista la consulta 3.
    /// </param>
    /// <returns>La validación registrada, con <see cref="Validacion.Aprobado"/> en <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Si el menú ya tiene dictamen (el primero lo cierra) o si el revisor es quien creó el menú (RS-05).
    /// </exception>
    /// <exception cref="ArgumentException">Si la fecha no está en UTC.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Si la fecha es anterior a la creación del menú (RS-06).</exception>
    public Validacion Rechazar(Guid nutricionistaId, DateTime fechaRechazo, string? observaciones = null)
    {
        ComprobarDictamen(nutricionistaId, fechaRechazo);

        Validacion = new Validacion(Id, nutricionistaId, fechaRechazo, aprobado: false, observaciones);
        return Validacion;
    }

    /// <summary>
    /// Registra que este menú se confeccionó en lugar de un menú rechazado (relación <c>Origina</c>, desde el
    /// dictamen rechazado hacia el menú de reemplazo). Es como queda modelado que el jefe de nutrición «puede indicar
    /// la confección de otro [menú] bajo sus criterios» (P1-5).
    /// </summary>
    /// <remarks>
    /// El menú de reemplazo se crea como cualquier otro, generado o manual, con sus parámetros; el vínculo se registra
    /// mientras espera revisión. Que una validación rechazada origine a lo sumo un menú (RK-15) no lo puede comprobar
    /// el agregado, porque abarca a otros menús: lo garantiza la base con una clave única.
    /// </remarks>
    /// <param name="menuRechazado">El menú rechazado al que este reemplaza.</param>
    /// <exception cref="ArgumentNullException">Si <paramref name="menuRechazado"/> es nulo.</exception>
    /// <exception cref="InvalidOperationException">
    /// Si este menú ya tiene dictamen o ya reemplaza a otro, si se indica a sí mismo (RS-19), si el otro menú no fue
    /// rechazado (RS-17) o si es de otra dieta (RS-18).
    /// </exception>
    public void RegistrarComoReemplazo(Menu menuRechazado)
    {
        ArgumentNullException.ThrowIfNull(menuRechazado);
        ExigirPendiente();

        if (ValidacionOrigenId is not null)
        {
            throw new InvalidOperationException("Este menú ya está registrado como reemplazo de otro.");
        }

        if (menuRechazado.Id == Id)
        {
            throw new InvalidOperationException("Un menú no puede ser reemplazo de sí mismo.");
        }

        if (!menuRechazado.EsRechazado)
        {
            throw new InvalidOperationException("Solo un menú rechazado origina un menú de reemplazo.");
        }

        if (menuRechazado.DietaId != DietaId)
        {
            throw new InvalidOperationException("El menú de reemplazo tiene que ser de la misma dieta que el rechazado.");
        }

        ValidacionOrigenId = menuRechazado.Id;
    }

    /// <summary>Reglas comunes a la aprobación y al rechazo.</summary>
    private void ComprobarDictamen(Guid nutricionistaId, DateTime fecha)
    {
        ExigirPendiente();

        if (nutricionistaId == NutricionistaCreadorId)
        {
            throw new InvalidOperationException("El nutricionista que creó el menú no puede dictaminarlo.");
        }

        // Antes de comparar con FechaCreacion (UTC): una fecha local daría una comparación sin sentido.
        if (fecha.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("El instante del dictamen debe estar en UTC (DateTimeKind.Utc).", nameof(fecha));
        }

        if (fecha < FechaCreacion)
        {
            throw new ArgumentOutOfRangeException(nameof(fecha), "El dictamen no puede ser anterior a la creación del menú.");
        }
    }

    /// <summary>Solo un menú pendiente se modifica o se dictamina.</summary>
    private void ExigirPendiente()
    {
        if (!EsPendiente)
        {
            throw new InvalidOperationException("El menú ya tiene dictamen: solo se modifica o se dictamina mientras está pendiente.");
        }
    }

    private Asignacion BuscarAsignacion(Guid pacienteId)
        => _asignaciones.SingleOrDefault(a => a.PacienteId == pacienteId)
            ?? throw new InvalidOperationException($"El menú no está asignado al paciente {pacienteId}.");

    private void FijarDistribucion(IEnumerable<(Guid TipoPreparacionId, decimal Proporcion)> distribucion)
    {
        foreach (var (tipoPreparacionId, proporcion) in distribucion)
        {
            if (_distribuciones.Any(d => d.TipoPreparacionId == tipoPreparacionId))
            {
                throw new ArgumentException($"El tipo de preparación {tipoPreparacionId} aparece más de una vez en la distribución.", nameof(distribucion));
            }

            _distribuciones.Add(new MenuDistribucion(Id, tipoPreparacionId, proporcion));
        }

        if (_distribuciones.Count == 0)
        {
            throw new ArgumentException("El menú debe tener al menos una proporción por tipo de preparación.", nameof(distribucion));
        }

        if (Math.Abs(_distribuciones.Sum(d => d.Proporcion) - 1m) >= ToleranciaSumaProporciones)
        {
            throw new ArgumentException("Las proporciones de la distribución deben sumar 1.", nameof(distribucion));
        }
    }

    private void FijarGruposNutricionales(IEnumerable<Guid> gruposNutricionalesIds)
    {
        foreach (var grupoNutricionalId in gruposNutricionalesIds)
        {
            if (_gruposNutricionales.Any(g => g.GrupoNutricionalId == grupoNutricionalId))
            {
                throw new ArgumentException($"El grupo nutricional {grupoNutricionalId} aparece más de una vez.", nameof(gruposNutricionalesIds));
            }

            _gruposNutricionales.Add(new MenuGrupoNutricional(Id, grupoNutricionalId));
        }

        if (_gruposNutricionales.Count == 0)
        {
            throw new ArgumentException("El menú debe cubrir al menos un grupo nutricional.", nameof(gruposNutricionalesIds));
        }
    }
}
