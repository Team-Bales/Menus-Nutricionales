using Menus.Domain.Abstractions;

namespace Menus.Domain.Entidades;

/// <summary>
/// Menú creado por un nutricionista para una dieta, de forma automática o manual,
/// junto con los parámetros con que se generó.
/// Es la raíz del agregado: los alimentos y platos incluidos, la distribución por tipo de
/// preparación, los grupos nutricionales cubiertos y las asignaciones a pacientes solo se
/// registran a través de él.
/// </summary>
/// <remarks>
/// Los parámetros de generación (<see cref="CantidadTotalAlimentos"/>, <see cref="EsAutomatico"/>,
/// <see cref="Distribuciones"/> y <see cref="GruposNutricionales"/>) se fijan al crear el menú y no
/// cambian después (submodelo #7, R7-27), por eso no hay métodos para modificarlos.
/// El menú no guarda un estado: es final si su validación más reciente está aprobada (submodelo #7, §4).
/// Tampoco guarda un único paciente: un menú puede asignarse a varios pacientes, una sola vez a cada uno
/// (agregación <c>Asignación</c>, R7-05). Que el paciente esté inscrito en la dieta del menú (R6-23)
/// lo verifica la capa de aplicación, porque la inscripción pertenece al agregado del paciente; lo mismo
/// ocurre con que el nutricionista creador esté autorizado en esa dieta (R6-22).
/// </remarks>
public sealed class Menu : AggregateRoot<Guid>
{
    private readonly List<MenuAlimento> _alimentos = [];
    private readonly List<MenuPlato> _platos = [];
    private readonly List<MenuDistribucion> _distribuciones = [];
    private readonly List<MenuGrupoNutricional> _gruposNutricionales = [];
    private readonly List<Asignacion> _asignaciones = [];
    private readonly List<Validacion> _validaciones = [];

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
    /// Instante de creación del menú, en UTC (consulta 1, §2). Es el ancla temporal de las asignaciones,
    /// los consumos y las valoraciones (consulta 4, §7-D).
    /// </summary>
    public DateTime FechaCreacion { get; }

    /// <summary>Cantidad total de alimentos del menú (&gt; 0). Parámetro de generación.</summary>
    public int CantidadTotalAlimentos { get; }

    /// <summary>Indica si el menú fue generado automáticamente. Parámetro de generación.</summary>
    public bool EsAutomatico { get; }

    /// <summary>Alimentos incluidos directamente en el menú (solo lectura).</summary>
    public IReadOnlyCollection<MenuAlimento> Alimentos => _alimentos.AsReadOnly();

    /// <summary>Platos incluidos en el menú (solo lectura).</summary>
    public IReadOnlyCollection<MenuPlato> Platos => _platos.AsReadOnly();

    /// <summary>Proporción de alimentos por tipo de preparación (solo lectura). Parámetro de generación.</summary>
    public IReadOnlyCollection<MenuDistribucion> Distribuciones => _distribuciones.AsReadOnly();

    /// <summary>Grupos nutricionales que cubre el menú (solo lectura). Parámetro de generación.</summary>
    public IReadOnlyCollection<MenuGrupoNutricional> GruposNutricionales => _gruposNutricionales.AsReadOnly();

    /// <summary>Pacientes a los que se asignó el menú (solo lectura).</summary>
    public IReadOnlyCollection<Asignacion> Asignaciones => _asignaciones.AsReadOnly();

    /// <summary>Validaciones registradas para el menú (solo lectura).</summary>
    public IReadOnlyCollection<Validacion> Validaciones => _validaciones.AsReadOnly();

    /// <summary>Incluye un alimento en el menú (relación <c>Incluye(Menú, Alimento)</c>).</summary>
    /// <param name="alimentoId">Identificador del alimento.</param>
    /// <returns>La inclusión registrada.</returns>
    /// <exception cref="InvalidOperationException">Si el alimento ya está incluido en el menú (R7-07).</exception>
    public MenuAlimento AgregarAlimento(Guid alimentoId)
    {
        if (_alimentos.Any(a => a.AlimentoId == alimentoId))
        {
            throw new InvalidOperationException($"El alimento {alimentoId} ya está incluido en este menú.");
        }

        var alimento = new MenuAlimento(Id, alimentoId);
        _alimentos.Add(alimento);
        return alimento;
    }

    /// <summary>Incluye un plato en el menú (relación <c>Incluye(Menú, Plato)</c>).</summary>
    /// <param name="platoId">Identificador del plato.</param>
    /// <returns>La inclusión registrada.</returns>
    /// <exception cref="InvalidOperationException">Si el plato ya está incluido en el menú (R7-07).</exception>
    public MenuPlato AgregarPlato(Guid platoId)
    {
        if (_platos.Any(p => p.PlatoId == platoId))
        {
            throw new InvalidOperationException($"El plato {platoId} ya está incluido en este menú.");
        }

        var plato = new MenuPlato(Id, platoId);
        _platos.Add(plato);
        return plato;
    }

    /// <summary>Asigna el menú a un paciente (agregación <c>Asignación</c>).</summary>
    /// <param name="pacienteId">Identificador del paciente que recibe el menú.</param>
    /// <returns>La asignación registrada.</returns>
    /// <exception cref="InvalidOperationException">Si el menú ya está asignado a ese paciente (R7-05).</exception>
    public Asignacion AsignarPaciente(Guid pacienteId)
    {
        if (_asignaciones.Any(a => a.PacienteId == pacienteId))
        {
            throw new InvalidOperationException($"El menú ya está asignado al paciente {pacienteId}.");
        }

        var asignacion = new Asignacion(Id, pacienteId);
        _asignaciones.Add(asignacion);
        return asignacion;
    }

    /// <summary>Registra el dictamen de un nutricionista sobre el menú.</summary>
    /// <param name="nutricionistaId">Identificador del nutricionista revisor.</param>
    /// <param name="fechaValidacion">Fecha y hora en que se emite el dictamen.</param>
    /// <param name="aprobado">Indica si el menú fue aprobado.</param>
    /// <param name="observaciones">Señalamientos del revisor, si existen.</param>
    /// <returns>La validación registrada.</returns>
    /// <exception cref="InvalidOperationException">
    /// Si el nutricionista creó el menú o ya registró una validación para él.
    /// </exception>
    /// <exception cref="ArgumentException">Si la fecha de validación no está en UTC.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Si la validación es anterior a la creación del menú.
    /// </exception>
    public Validacion Validar(
        Guid nutricionistaId,
        DateTime fechaValidacion,
        bool aprobado,
        string? observaciones = null)
    {
        if (nutricionistaId == NutricionistaCreadorId)
        {
            throw new InvalidOperationException("El nutricionista que creó el menú no puede validarlo.");
        }

        if (_validaciones.Any(v => v.NutricionistaId == nutricionistaId))
        {
            throw new InvalidOperationException($"El nutricionista {nutricionistaId} ya validó este menú.");
        }

        // Antes de comparar con FechaCreacion (UTC): una fecha local daría una comparación sin sentido.
        if (fechaValidacion.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("El instante de validación debe estar en UTC (DateTimeKind.Utc).", nameof(fechaValidacion));
        }

        if (fechaValidacion < FechaCreacion)
        {
            throw new ArgumentOutOfRangeException(nameof(fechaValidacion), "La fecha de validación no puede ser anterior a la creación del menú.");
        }

        var validacion = new Validacion(nutricionistaId, Id, fechaValidacion, aprobado, observaciones);
        _validaciones.Add(validacion);
        return validacion;
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
