using Menus.Application.IA;
using Microsoft.EntityFrameworkCore;

namespace Menus.Infrastructure.Persistence.Consultas;

/// <summary>
/// Implementación de <see cref="IConteoDePacientesPorDieta"/> sobre PostgreSQL (#325): cuenta los pacientes inscritos
/// en una dieta cuya edad está en un rango, para la regla de combinaciones raras de la IA (contrato de IA, §5.2).
/// </summary>
/// <remarks>
/// La dieta llega por nombre, porque es lo que maneja el transformador. Si hubiera dos dietas con el mismo nombre, se
/// cuentan los pacientes de las dos: para la privacidad importa cuántos comparten lo que se ve en el prompt, y lo
/// que se ve es el nombre. Solo lectura y sin rastreo.
/// </remarks>
/// <param name="contexto">Contexto de EF Core.</param>
internal sealed class ConteoDePacientesPorDieta(MenusDbContext contexto) : IConteoDePacientesPorDieta
{
    /// <inheritdoc />
    public Task<int> ContarAsync(string nombreDieta, int edadMinima, int? edadMaxima, CancellationToken ct)
    {
        var dietas = contexto.Dietas.Where(d => d.Nombre == nombreDieta).Select(d => d.Id);

        return contexto.Pacientes
            .AsNoTracking()
            .Where(p => p.Edad >= edadMinima && (edadMaxima == null || p.Edad <= edadMaxima))
            .Where(p => p.Dietas.Any(pd => dietas.Contains(pd.DietaId)))
            .CountAsync(ct);
    }
}
