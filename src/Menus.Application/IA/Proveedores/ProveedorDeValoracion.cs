using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Nomenclators;
using Microsoft.Extensions.Options;

namespace Menus.Application.IA.Proveedores;

/// <summary>
/// Fuente <c>valoracion</c> (#325, contrato de IA §4): el resultado de la valoración, con su nombre y su valor de 1 a
/// 5, y la dieta del menú valorado.
/// </summary>
/// <remarks>
/// <para>
/// La dieta es un dato clínico del paciente, así que pasa por el <see cref="TransformadorDePaciente"/>. Si el paciente
/// no dio su consentimiento y menos de <see cref="IAOptions.KMinimo"/> pacientes de su rango de edad están en esa
/// dieta, se reemplaza por el marcador de privacidad y se avisa en <see cref="ResultadoDeContexto.Advertencias"/>
/// (§5.2).
/// </para>
/// <para>
/// El conteo se pide en dos pasos, porque el transformador es una función pura que recibe una función de conteo
/// sincrónica. Una primera llamada averigua el rango de edad que va a usar, se cuenta en la base con
/// <see cref="IConteoDePacientesPorDieta"/>, y una segunda llamada transforma con el conteo real.
/// </para>
/// </remarks>
/// <param name="valoraciones">Repositorio de valoraciones.</param>
/// <param name="menus">Repositorio de menús, para la dieta del menú valorado.</param>
/// <param name="dietas">Repositorio de dietas, para su nombre.</param>
/// <param name="conteo">Conteo de pacientes por rango de edad y dieta.</param>
/// <param name="opciones">Opciones de IA, de donde sale <see cref="IAOptions.KMinimo"/>.</param>
internal sealed class ProveedorDeValoracion(
    IValoracionRepository valoraciones,
    IMenuRepository menus,
    IDietaRepository dietas,
    IConteoDePacientesPorDieta conteo,
    IOptions<IAOptions> opciones) : IProveedorDeContexto
{
    /// <summary>Nombre de la fuente.</summary>
    public const string NombreFuente = "valoracion";

    /// <summary>Versión del esquema del bloque.</summary>
    public const int VersionDelBloque = 1;

    /// <inheritdoc />
    public string Fuente => NombreFuente;

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// Si el paciente de la valoración no está entre los pacientes del caso: sin su ficha no se puede aplicar la
    /// regla de privacidad, y es un error de quien arma el caso.
    /// </exception>
    public async Task<ResultadoDeContexto?> ConstruirAsync(ContextoDeCaso caso, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(caso);
        if (caso.ValoracionId is not { } valoracionId)
        {
            return null;
        }

        var valoracion = await valoraciones.GetByIdAsync(valoracionId, ct);
        var resultado = valoracion is null
            ? null
            : ResultadoNutricional.Todos.FirstOrDefault(r => r.Id == valoracion.ResultadoNutricionalId);
        if (valoracion is null || resultado is null)
        {
            return null;
        }

        var paciente = caso.Pacientes.FirstOrDefault(p => p.Id == valoracion.PacienteId)
            ?? throw new InvalidOperationException("El paciente de la valoración no está entre los pacientes del caso.");

        var menu = await menus.GetByIdAsync(valoracion.MenuId, ct);
        var dieta = menu is null ? null : await dietas.GetByIdAsync(menu.DietaId, ct);

        var advertencias = new List<string>();
        var texto = $"Valoración nutricional {TextoDeBloque.DeLaReferencia(Referencia(caso, paciente.Id))} sobre el menú valorado: "
            + $"{resultado.Nombre} ({resultado.Valor} de {ResultadoNutricional.Todos.Count}).";

        if (dieta is not null)
        {
            var (dietaTexto, advertenciasDeDieta) = await DietaTransformadaAsync(paciente, caso, dieta.Nombre, ct);
            texto += $" Dieta del menú: {dietaTexto}.";
            advertencias.AddRange(advertenciasDeDieta);
        }

        var cifras = new Dictionary<string, double>
        {
            ["valoracion.valor"] = resultado.Valor,
            ["valoracion.maximo"] = ResultadoNutricional.Todos.Count,
        };

        return new ResultadoDeContexto(new BloqueDeContexto(NombreFuente, VersionDelBloque, texto, cifras), advertencias);
    }

    /// <summary>
    /// La dieta del menú tal como puede entrar en el prompt: su nombre, o el marcador de privacidad si es una
    /// combinación rara sin consentimiento.
    /// </summary>
    private async Task<(string Texto, IReadOnlyList<string> Advertencias)> DietaTransformadaAsync(
        PacienteSnapshot paciente,
        ContextoDeCaso caso,
        string nombreDieta,
        CancellationToken ct)
    {
        // Solo la dieta del menú valorado; su tipo clínico, si quien arma el caso lo informó.
        var tipo = paciente.Dietas.TryGetValue(nombreDieta, out var tipoInformado) ? tipoInformado : "dieta";
        var soloLaDieta = paciente with { Dietas = new Dictionary<string, string> { [nombreDieta] = tipo } };
        var kMinimo = opciones.Value.KMinimo;

        // Primer paso: averiguar el rango de edad que usa el transformador, sin contar todavía.
        string? rango = null;
        TransformadorDePaciente.Transformar(soloLaDieta, caso, kMinimo, (r, _, _) =>
        {
            rango = r;
            return kMinimo;
        });

        var (minima, maxima) = RangoDeEdad.Limites(rango!);
        var pacientes = await conteo.ContarAsync(nombreDieta, minima, maxima, ct);

        // Segundo paso: transformar con el conteo real.
        var transformado = TransformadorDePaciente.Transformar(soloLaDieta, caso, kMinimo, (_, _, _) => pacientes);
        var dato = transformado.Dietas.Single();
        var generalizada = transformado.Advertencias.Count > 0;

        return (generalizada ? dato.ValorFinal : nombreDieta, transformado.Advertencias);
    }

    private static string Referencia(ContextoDeCaso caso, Guid pacienteId)
        => caso.ObtenerCodigoPaciente(pacienteId) ?? "el paciente";
}
