namespace Menus.Application.IA;

using System.Collections.Frozen;
using System.Text.Json.Serialization;

/// <summary>
/// Transformador puro de pacientes para contexto de IA. Sin DI, reloj, repositorios ni estado mutable.
/// </summary>
/// <remarks>
/// Recibe un <see cref="PacienteSnapshot"/> y devuelve un <see cref="PacienteTransformado"/> con:
/// - Referencia opaca (código del <see cref="ContextoDeCaso"/>)
/// - Rango de edad (0-12, 13-17, 18-39, 40-64, 65+)
/// - Datos clínicos transformados (dietas y alérgenos con generalización por privacidad)
/// - Fechas relativas a <see cref="ContextoDeCaso.FechaReferencia"/>
/// - Advertencias generadas
/// Nunca devuelve nombre, Guid, sala, edad exacta ni texto libre.
/// </remarks>
public static class TransformadorDePaciente
{
    private const string MarcadorPrivacidad = "dato clínico no detallado por privacidad";
    private const string AdvertenciaPrivacidad = "Se generalizó información clínica poco frecuente por privacidad.";

    /// <summary>
    /// Transforma un paciente para el contexto de IA.
    /// </summary>
    /// <param name="paciente">Snapshot del paciente.</param>
    /// <param name="contexto">Contexto del caso para obtener el código opaco y la fecha de referencia.</param>
    /// <param name="kMinimo">Valor mínimo K para generalizar combinaciones raras sin consentimiento. Debe ser >= 2.</param>
    /// <param name="frecuenciaCombinaciones">Función que devuelve el conteo de pacientes distintos con la misma combinación (rango de edad + tipo + valor clínico). Conteo ausente = 0. Conteo negativo o K < 2 lanza error.</param>
    /// <returns>Paciente transformado con referencia opaca, rango de edad, datos clínicos, fechas relativas y advertencias.</returns>
    /// <exception cref="ArgumentException"><paramref name="kMinimo"/> < 2; <paramref name="frecuenciaCombinaciones"/> devuelve conteo negativo.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="paciente"/>, <paramref name="contexto"/> o <paramref name="frecuenciaCombinaciones"/> es nulo.</exception>
    public static PacienteTransformado Transformar(
        PacienteSnapshot paciente,
        ContextoDeCaso contexto,
        int kMinimo,
        Func<string, string, string, int> frecuenciaCombinaciones)
    {
        ArgumentNullException.ThrowIfNull(paciente);
        ArgumentNullException.ThrowIfNull(contexto);
        ArgumentNullException.ThrowIfNull(frecuenciaCombinaciones);

        if (kMinimo < 2)
        {
            throw new ArgumentException("KMinimo debe ser >= 2.", nameof(kMinimo));
        }

        if (paciente.Edad < 0)
        {
            throw new ArgumentException("La edad no puede ser negativa.", nameof(paciente));
        }

        var codigo = contexto.ObtenerCodigoPaciente(paciente.Id) ?? "desconocido";
        var rangoEdad = CalcularRangoEdad(paciente.Edad);
        var advertencias = new List<string>();

        // Transformar dietas
        var dietasTransformadas = new List<DatoClinicoTransformado>();
        foreach (var (nombre, tipo) in paciente.Dietas)
        {
            var valor = $"{tipo}:{nombre}";
            var count = frecuenciaCombinaciones(rangoEdad, "Dieta", valor);
            if (count < 0)
            {
                throw new ArgumentException($"El conteo para la combinación (rango={rangoEdad}, tipo=Dieta, valor={valor}) es negativo: {count}.", nameof(frecuenciaCombinaciones));
            }

            var (valorFinal, generaAdvertencia) = AplicarReglaPrivacidad(paciente.ConsentimientoIA, count, kMinimo, valor);
            dietasTransformadas.Add(new DatoClinicoTransformado("Dieta", nombre, tipo, valorFinal, count));

            if (generaAdvertencia)
            {
                advertencias.Add(AdvertenciaPrivacidad);
            }
        }

        // Transformar alérgenos
        var alergenosTransformados = new List<DatoClinicoTransformado>();
        foreach (var (nombre, tipo) in paciente.Alergenos)
        {
            var valor = $"{tipo}:{nombre}";
            var count = frecuenciaCombinaciones(rangoEdad, "Alergeno", valor);
            if (count < 0)
            {
                throw new ArgumentException($"El conteo para la combinación (rango={rangoEdad}, tipo=Alergeno, valor={valor}) es negativo: {count}.", nameof(frecuenciaCombinaciones));
            }

            var (valorFinal, generaAdvertencia) = AplicarReglaPrivacidad(paciente.ConsentimientoIA, count, kMinimo, valor);
            alergenosTransformados.Add(new DatoClinicoTransformado("Alergeno", nombre, tipo, valorFinal, count));

            if (generaAdvertencia)
            {
                advertencias.Add(AdvertenciaPrivacidad);
            }
        }

        // Fechas relativas
        var fechasRelativas = new List<FechaRelativa>();
        if (paciente.FechaIngreso.HasValue)
        {
            fechasRelativas.Add(new FechaRelativa("ingreso", CalcularFechaRelativa(paciente.FechaIngreso.Value, contexto.FechaReferencia)));
        }
        if (paciente.FechaAltaEstimada.HasValue)
        {
            fechasRelativas.Add(new FechaRelativa("altaEstimada", CalcularFechaRelativa(paciente.FechaAltaEstimada.Value, contexto.FechaReferencia)));
        }

        return new PacienteTransformado(
            codigo,
            rangoEdad,
            dietasTransformadas.ToArray(),
            alergenosTransformados.ToArray(),
            fechasRelativas.ToArray(),
            advertencias.Distinct().ToArray());
    }

    private static string CalcularRangoEdad(int edad)
    {
        return edad switch
        {
            >= 0 and <= 12 => "0-12",
            >= 13 and <= 17 => "13-17",
            >= 18 and <= 39 => "18-39",
            >= 40 and <= 64 => "40-64",
            _ => "65+" // la edad negativa ya se rechazó en Transformar
        };
    }

    private static (string valorFinal, bool generaAdvertencia) AplicarReglaPrivacidad(
        EstadoConsentimientoIA consentimiento,
        int count,
        int kMinimo,
        string valorOriginal)
    {
        // Consentimiento Si: el valor pasa tal cual
        if (consentimiento == EstadoConsentimientoIA.Si)
        {
            return (valorOriginal, false);
        }

        // Sin consentimiento (NoRegistrado o No): generalizar si count < K
        if (count < kMinimo)
        {
            return (MarcadorPrivacidad, true);
        }

        // Count >= K: el valor pasa
        return (valorOriginal, false);
    }

    private static string CalcularFechaRelativa(DateOnly fecha, DateOnly referencia)
    {
        var diff = fecha.DayNumber - referencia.DayNumber;
        return diff switch
        {
            0 => "hoy",
            < 0 => $"hace {-diff} días",
            > 0 => $"dentro de {diff} días"
        };
    }
}

/// <summary>
/// Dato clínico transformado (dieta o alérgeno) con su valor final y conteo de frecuencia.
/// </summary>
/// <remarks>
/// <see cref="ValorFinal"/> es el valor que entra al prompt (puede ser el original o el marcador de privacidad).
/// <see cref="ConteoPacientesMismaCombinacion"/> es el número de pacientes distintos con la misma combinación
/// (rango de edad + tipo + valor clínico) en todo el sistema. Ausente = 0.
/// </remarks>
public sealed record DatoClinicoTransformado
{
    /// <summary>
    /// Tipo del dato: "Dieta" o "Alergeno".
    /// </summary>
    public string Tipo { get; }

    /// <summary>
    /// Nombre del dato (nombre de la dieta o del alérgeno).
    /// </summary>
    public string Nombre { get; }

    /// <summary>
    /// Tipo clínico del dato (p. ej., "terapeutica", "preventiva", "alimentario", "farmacologico").
    /// </summary>
    public string TipoClinico { get; }

    /// <summary>
    /// Valor final que entra al prompt. Puede ser el original o el marcador "dato clínico no detallado por privacidad".
    /// </summary>
    public string ValorFinal { get; }

    /// <summary>
    /// Número de pacientes distintos con la misma combinación (rango de edad + tipo + valor clínico) en todo el sistema.
    /// Ausente = 0.
    /// </summary>
    public int ConteoPacientesMismaCombinacion { get; }

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="DatoClinicoTransformado"/>.
    /// </summary>
    /// <param name="tipo">Tipo del dato: "Dieta" o "Alergeno".</param>
    /// <param name="nombre">Nombre del dato.</param>
    /// <param name="tipoClinico">Tipo clínico.</param>
    /// <param name="valorFinal">Valor final para el prompt.</param>
    /// <param name="conteoPacientesMismaCombinacion">Conteo de pacientes con la misma combinación.</param>
    public DatoClinicoTransformado(
        string tipo,
        string nombre,
        string tipoClinico,
        string valorFinal,
        int conteoPacientesMismaCombinacion)
    {
        ArgumentNullException.ThrowIfNull(tipo);
        ArgumentNullException.ThrowIfNull(nombre);
        ArgumentNullException.ThrowIfNull(tipoClinico);
        ArgumentNullException.ThrowIfNull(valorFinal);

        Tipo = tipo;
        Nombre = nombre;
        TipoClinico = tipoClinico;
        ValorFinal = valorFinal;
        ConteoPacientesMismaCombinacion = conteoPacientesMismaCombinacion;
    }
}

/// <summary>
/// Fecha expresada relativamente a la fecha de referencia.
/// </summary>
public sealed record FechaRelativa
{
    /// <summary>
    /// Tipo de fecha: "ingreso" o "altaEstimada".
    /// </summary>
    public string Tipo { get; }

    /// <summary>
    /// Expresión relativa: "hoy", "hace N días" o "dentro de N días".
    /// </summary>
    public string Expresion { get; }

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="FechaRelativa"/>.
    /// </summary>
    /// <param name="tipo">Tipo de fecha.</param>
    /// <param name="expresion">Expresión relativa.</param>
    public FechaRelativa(string tipo, string expresion)
    {
        ArgumentNullException.ThrowIfNull(tipo);
        ArgumentNullException.ThrowIfNull(expresion);
        Tipo = tipo;
        Expresion = expresion;
    }
}

/// <summary>
/// Paciente transformado para el contexto de IA.
/// </summary>
/// <remarks>
/// Solo contiene referencia opaca, rango de edad, datos clínicos transformados, fechas relativas y advertencias.
/// Nunca nombre, Guid, sala, edad exacta ni texto libre.
/// </remarks>
public sealed record PacienteTransformado
{
    /// <summary>
    /// Referencia opaca del paciente ("el paciente", "P1", "P2", ...).
    /// </summary>
    public string Referencia { get; }

    /// <summary>
    /// Rango de edad: "0-12", "13-17", "18-39", "40-64", "65+".
    /// </summary>
    public string RangoEdad { get; }

    /// <summary>
    /// Dietas transformadas.
    /// </summary>
    public IReadOnlyList<DatoClinicoTransformado> Dietas { get; }

    /// <summary>
    /// Alérgenos transformados.
    /// </summary>
    public IReadOnlyList<DatoClinicoTransformado> Alergenos { get; }

    /// <summary>
    /// Fechas relativas a la fecha de referencia.
    /// </summary>
    public IReadOnlyList<FechaRelativa> FechasRelativas { get; }

    /// <summary>
    /// Advertencias generadas durante la transformación (p. ej., generalización por privacidad).
    /// </summary>
    public IReadOnlyList<string> Advertencias { get; }

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="PacienteTransformado"/>.
    /// </summary>
    /// <param name="referencia">Referencia opaca.</param>
    /// <param name="rangoEdad">Rango de edad.</param>
    /// <param name="dietas">Dietas transformadas.</param>
    /// <param name="alergenos">Alérgenos transformados.</param>
    /// <param name="fechasRelativas">Fechas relativas.</param>
    /// <param name="advertencias">Advertencias.</param>
    public PacienteTransformado(
        string referencia,
        string rangoEdad,
        IReadOnlyList<DatoClinicoTransformado> dietas,
        IReadOnlyList<DatoClinicoTransformado> alergenos,
        IReadOnlyList<FechaRelativa> fechasRelativas,
        IReadOnlyList<string> advertencias)
    {
        ArgumentNullException.ThrowIfNull(referencia);
        ArgumentNullException.ThrowIfNull(rangoEdad);
        ArgumentNullException.ThrowIfNull(dietas);
        ArgumentNullException.ThrowIfNull(alergenos);
        ArgumentNullException.ThrowIfNull(fechasRelativas);
        ArgumentNullException.ThrowIfNull(advertencias);

        Referencia = referencia;
        RangoEdad = rangoEdad;
        Dietas = dietas;
        Alergenos = alergenos;
        FechasRelativas = fechasRelativas;
        Advertencias = advertencias;
    }
}
