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
/// cambian después (submodelo #7, R7-27), por eso no hay métodos para modificarlos.
/// </para>
/// <para>
/// El menú no guarda un estado, se deriva de su validación (submodelo #7, §4): está pendiente mientras no
/// tiene dictamen (<see cref="EsPendiente"/>) y es final cuando fue aprobado (<see cref="EsFinal"/>). El primer
/// dictamen lo cierra: si es una aprobación queda registrada (<see cref="Aprobar"/>); si es un rechazo no se
/// registra y el menú se elimina (<see cref="Rechazar"/>; decisión del equipo, 16/10). Por eso un menú tiene a
/// lo sumo una validación (R7-32), que después no cambia (R7-33). Solo un menú pendiente se modifica y solo uno
/// final se asigna a pacientes (R7-34).
/// </para>
/// <para>
/// Tampoco guarda un único paciente: un menú puede asignarse a varios pacientes, una sola vez a cada uno
/// (agregación <c>Asignación</c>, R7-05), y servirse a cada uno las veces que haga falta
/// (<see cref="RegistrarServicio"/>). El servicio y el consumo se registran por la raíz, y no por la asignación,
/// porque sus reglas dependen del menú: solo se sirve después de la aprobación (R7-31) y solo se consume lo que el
/// menú incluye (R11-01).
/// </para>
/// <para>
/// Que el paciente esté inscrito en la dieta del menú (R6-23) lo verifica la capa de aplicación, porque la
/// inscripción pertenece al agregado del paciente; lo mismo ocurre con que el nutricionista creador esté
/// autorizado en esa dieta (R6-22).
/// </para>
/// </remarks>
public sealed class Menu : AggregateRoot<Guid>
{
    private readonly List<MenuItem> _items = [];
    private readonly List<MenuDistribucion> _distribuciones = [];
    private readonly List<MenuGrupoNutricional> _gruposNutricionales = [];
    private readonly List<Asignacion> _asignaciones = [];

    /// <summary>
    /// Diferencia máxima admitida entre la suma de las proporciones y 1 (R7-28).
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
    /// <param name="cantidadTotalAlimentos">Cantidad total de alimentos del menú (&gt; 0, R7-15).</param>
    /// <param name="esAutomatico">Indica si el menú fue generado automáticamente (consulta 1, §7-A).</param>
    /// <param name="distribucion">
    /// Proporción de alimentos por tipo de preparación: al menos una (R7-22), cada una en (0, 1] (R7-16),
    /// sin tipos repetidos (R7-03) y con suma igual a 1, con la tolerancia
    /// <see cref="ToleranciaSumaProporciones"/> (R7-28).
    /// </param>
    /// <param name="gruposNutricionalesIds">Grupos nutricionales que cubre el menú: al menos uno (R7-22) y sin repetidos (R7-07).</param>
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

    /// <summary>
    /// Cantidad total de alimentos del menú (&gt; 0). Parámetro de generación. Cuenta ítems del banco: con la
    /// generalización <see cref="ItemAlimentario"/>, un plato es un alimento compuesto.
    /// </summary>
    public int CantidadTotalAlimentos { get; }

    /// <summary>Indica si el menú fue generado automáticamente. Parámetro de generación.</summary>
    public bool EsAutomatico { get; }

    /// <summary>Ítems del banco, alimentos y platos, incluidos en el menú (solo lectura).</summary>
    public IReadOnlyCollection<MenuItem> Items => _items.AsReadOnly();

    /// <summary>Proporción de ítems por tipo de preparación (solo lectura). Parámetro de generación.</summary>
    public IReadOnlyCollection<MenuDistribucion> Distribuciones => _distribuciones.AsReadOnly();

    /// <summary>Grupos nutricionales que cubre el menú (solo lectura). Parámetro de generación.</summary>
    public IReadOnlyCollection<MenuGrupoNutricional> GruposNutricionales => _gruposNutricionales.AsReadOnly();

    /// <summary>Pacientes a los que se asignó el menú, con sus servicios y consumos (solo lectura).</summary>
    public IReadOnlyCollection<Asignacion> Asignaciones => _asignaciones.AsReadOnly();

    /// <summary>
    /// Aprobación del menú, o <see langword="null"/> si todavía no tiene dictamen. Es a lo sumo una (R7-32): el
    /// primer dictamen cierra el menú y el rechazo lo elimina, así que la validación es siempre una aprobación.
    /// </summary>
    /// <remarks>
    /// La persistencia la carga siempre junto con el menú (<c>AutoInclude</c>), porque <see cref="EsPendiente"/> y
    /// <see cref="EsFinal"/> se derivan de ella: un menú leído sin su validación parecería pendiente y admitiría
    /// cambios aunque estuviera aprobado.
    /// </remarks>
    public Validacion? Validacion { get; private set; }

    /// <summary>Indica si el menú está pendiente de revisión: todavía no tiene dictamen.</summary>
    public bool EsPendiente => Validacion is null;

    /// <summary>
    /// Indica si el menú es final: fue aprobado (consulta 2, §7-A). Es el complemento de <see cref="EsPendiente"/>:
    /// como el rechazo elimina el menú, un menú que existe o espera dictamen o fue aprobado.
    /// </summary>
    public bool EsFinal => Validacion is not null;

    /// <summary>Incluye un alimento o un plato en el menú (relación <c>Incluye(Menú, ItemAlimentario)</c>).</summary>
    /// <param name="itemId">Identificador del alimento o plato.</param>
    /// <returns>La inclusión registrada.</returns>
    /// <exception cref="InvalidOperationException">
    /// Si el menú ya tiene dictamen o si el ítem ya está incluido en el menú (R7-07).
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
    /// generado mientras espera revisión; los parámetros de generación no se modifican (R7-27).
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
    /// Si el menú no es final (solo se asignan menús aprobados, R7-34) o si ya está asignado a ese paciente (R7-05).
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
    /// Instante del servicio, en UTC; no puede ser futuro ni anterior a la aprobación del menú (R7-31). Se trunca al
    /// segundo (ver <see cref="Servicio"/>).
    /// </param>
    /// <returns>El servicio registrado.</returns>
    /// <exception cref="InvalidOperationException">
    /// Si el menú no es final, si no está asignado al paciente o si ya hay un servicio en ese instante.
    /// </exception>
    /// <exception cref="ArgumentException">Si la fecha no está en UTC.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Si la fecha es futura o anterior a la aprobación del menú (R7-31).</exception>
    public Servicio RegistrarServicio(Guid pacienteId, DateTime fechaServicio)
    {
        if (Validacion is not { } aprobacion)
        {
            throw new InvalidOperationException("Solo se sirve un menú final (aprobado).");
        }

        var asignacion = BuscarAsignacion(pacienteId);
        var fecha = NormalizarFechaServicio(fechaServicio);

        if (fecha > DateTime.UtcNow)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaServicio), "El instante del servicio no puede ser futuro.");
        }

        if (fecha < aprobacion.FechaValidacion)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaServicio), "El menú no puede servirse antes de su aprobación.");
        }

        return asignacion.RegistrarServicio(fecha);
    }

    /// <summary>
    /// Registra la aceptación o el rechazo de un alimento o un plato del menú en un servicio al paciente
    /// (relación <c>Consume</c>).
    /// </summary>
    /// <param name="pacienteId">Identificador del paciente.</param>
    /// <param name="fechaServicio">Instante del servicio, en UTC, tal como se registró (se trunca al segundo).</param>
    /// <param name="itemId">Identificador del alimento o plato consumido.</param>
    /// <param name="aceptacion"><see langword="true"/> si el paciente lo aceptó; de lo contrario, <see langword="false"/>.</param>
    /// <returns>El consumo registrado.</returns>
    /// <exception cref="InvalidOperationException">
    /// Si el menú no está asignado al paciente, si no hay un servicio en ese instante, si el ítem no está incluido en
    /// el menú (R11-01) o si ya tiene un consumo registrado en ese servicio.
    /// </exception>
    /// <exception cref="ArgumentException">Si la fecha no está en UTC.</exception>
    public Consumo RegistrarConsumo(Guid pacienteId, DateTime fechaServicio, Guid itemId, bool aceptacion)
    {
        var asignacion = BuscarAsignacion(pacienteId);
        var fecha = NormalizarFechaServicio(fechaServicio);

        var servicio = asignacion.Servicios.SingleOrDefault(s => s.FechaServicio == fecha)
            ?? throw new InvalidOperationException($"No hay un servicio de este menú al paciente {pacienteId} en {fecha:O}.");

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
    /// Si el menú ya tiene dictamen (el primero lo cierra) o si el revisor es quien creó el menú (R7-25).
    /// </exception>
    /// <exception cref="ArgumentException">Si la fecha no está en UTC.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Si la fecha es anterior a la creación del menú (R7-26).</exception>
    public Validacion Aprobar(Guid nutricionistaId, DateTime fechaValidacion, string? observaciones = null)
    {
        ComprobarDictamen(nutricionistaId, fechaValidacion);

        Validacion = new Validacion(Id, nutricionistaId, fechaValidacion, observaciones);
        return Validacion;
    }

    /// <summary>
    /// Comprueba que un nutricionista revisor puede rechazar el menú. El rechazo no se registra: el caso de uso
    /// elimina el menú después de llamar a este método (decisión del equipo, 16/10).
    /// </summary>
    /// <param name="nutricionistaId">Identificador del nutricionista revisor.</param>
    /// <param name="fechaRechazo">Instante del rechazo, en UTC.</param>
    /// <exception cref="InvalidOperationException">
    /// Si el menú ya tiene dictamen (el primero lo cierra) o si el revisor es quien creó el menú (R7-25).
    /// </exception>
    /// <exception cref="ArgumentException">Si la fecha no está en UTC.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Si la fecha es anterior a la creación del menú (R7-26).</exception>
    public void Rechazar(Guid nutricionistaId, DateTime fechaRechazo) => ComprobarDictamen(nutricionistaId, fechaRechazo);

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

    /// <summary>Exige UTC antes de comparar o buscar, y trunca al segundo como la clave del servicio.</summary>
    private static DateTime NormalizarFechaServicio(DateTime fechaServicio)
    {
        if (fechaServicio.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("El instante del servicio debe estar en UTC (DateTimeKind.Utc).", nameof(fechaServicio));
        }

        return Servicio.Normalizar(fechaServicio);
    }

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
