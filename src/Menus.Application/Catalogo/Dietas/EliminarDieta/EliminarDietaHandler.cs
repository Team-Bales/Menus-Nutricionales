using Menus.Application.Catalogo.Dietas.DisponibilidadCalorica;
using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Excepciones;

namespace Menus.Application.Catalogo.Dietas.EliminarDieta;

/// <summary>Elimina la dieta si no está en uso.</summary>
/// <remarks>
/// El uso se comprueba antes para dar un mensaje claro; las claves foráneas en <c>RESTRICT</c> de <c>Inscribe</c>
/// y de <c>Menú → Dieta</c> siguen siendo la última barrera (ADR-010, §3). También invalida su disponibilidad
/// calórica (informe, «Consultas recurrentes»; #101): sin esto, la clave sobrevive hasta su tiempo de vida y
/// `GET .../disponibilidad-calorica` de una dieta borrada devolvería 200 con el valor cacheado en vez de 404.
/// </remarks>
internal sealed class EliminarDietaHandler(
    IDietaRepository dietas,
    IPacienteRepository pacientes,
    IMenuRepository menus,
    IUnitOfWork unidadDeTrabajo,
    ICache cache) : IHandler<EliminarDietaCommand, Unidad>
{
    /// <exception cref="NoEncontradoException">Si la dieta no existe.</exception>
    /// <exception cref="ReglaDeNegocioException">Si tiene pacientes inscritos (RR-12) o algún menú (RR-13).</exception>
    public async Task<Unidad> HandleAsync(EliminarDietaCommand solicitud, CancellationToken ct)
    {
        var dieta = await dietas.GetByIdAsync(solicitud.Id, ct)
            ?? throw new NoEncontradoException("dieta", solicitud.Id);

        if ((await pacientes.GetByDietaAsync(dieta.Id, ct)).Count > 0)
        {
            throw new ReglaDeNegocioException(
                $"La dieta «{dieta.Nombre}» tiene pacientes inscritos: no se puede eliminar.", "RR-12");
        }

        if ((await menus.GetByDietaAsync(dieta.Id, ct)).Count > 0)
        {
            throw new ReglaDeNegocioException(
                $"La dieta «{dieta.Nombre}» tiene menús: no se puede eliminar.", "RR-13");
        }

        dietas.Remove(dieta);
        await unidadDeTrabajo.CommitAsync(ct);

        await CacheDeDisponibilidadCalorica.InvalidarAsync(cache, dieta.Id, ct);

        return Unidad.Valor;
    }
}
