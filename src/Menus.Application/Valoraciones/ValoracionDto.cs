using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Valoraciones;

/// <summary>Categoría de <see cref="ResultadoNutricional"/>, tal como la ve el cliente.</summary>
/// <param name="Id">Identificador de la categoría.</param>
/// <param name="Nombre">Nombre de la categoría (p. ej. «Buena»).</param>
/// <param name="Valor">Valor numérico, de 1 a 5.</param>
public sealed record ResultadoNutricionalDto(Guid Id, string Nombre, int Valor);

/// <summary>Una valoración nutricional, con su resultado expandido.</summary>
/// <param name="Id">Identificador de la valoración.</param>
/// <param name="MenuId">Menú de la asignación evaluada.</param>
/// <param name="PacienteId">Paciente de la asignación evaluada.</param>
/// <param name="NutricionistaId">Nutricionista que emitió la valoración.</param>
/// <param name="Resultado">Categoría de resultado nutricional.</param>
/// <param name="FechaValoracion">Fecha y hora en que se emitió, en UTC.</param>
public sealed record ValoracionDto(
    Guid Id,
    Guid MenuId,
    Guid PacienteId,
    Guid NutricionistaId,
    ResultadoNutricionalDto Resultado,
    DateTime FechaValoracion)
{
    /// <summary>Copia los datos de la entidad; el resultado lo busca en el catálogo (en memoria, sin consultar la base).</summary>
    /// <param name="valoracion">Valoración del dominio.</param>
    internal static ValoracionDto Desde(ValoracionNutricional valoracion)
    {
        var resultado = ResultadoNutricional.Todos.Single(r => r.Id == valoracion.ResultadoNutricionalId);

        return new ValoracionDto(
            valoracion.Id,
            valoracion.MenuId,
            valoracion.PacienteId,
            valoracion.NutricionistaId,
            new ResultadoNutricionalDto(resultado.Id, resultado.Nombre, resultado.Valor),
            valoracion.FechaValoracion);
    }
}
