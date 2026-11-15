using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;

namespace Menus.Application.Catalogo.Dietas.RegistrarDieta;

/// <summary>Registra la dieta con su cobertura y sus restricciones, y devuelve su identificador.</summary>
internal sealed class RegistrarDietaHandler(IDietaRepository dietas, IUnitOfWork unidadDeTrabajo)
    : IHandler<RegistrarDietaCommand, IdCreadoDto>
{
    /// <exception cref="Menus.Domain.Excepciones.ReglaDeNegocioException">
    /// Si un grupo o una restricción se repiten (lo comprueba el dominio, en <see cref="Dieta"/>).
    /// </exception>
    public async Task<IdCreadoDto> HandleAsync(RegistrarDietaCommand solicitud, CancellationToken ct)
    {
        var dieta = new Dieta(Guid.NewGuid(), solicitud.Nombre.Trim(), solicitud.ProgramaAtencionId);

        foreach (var grupo in solicitud.Grupos)
        {
            dieta.AgregarGrupo(grupo.GrupoNutricionalId, grupo.Tipo);
        }

        foreach (var restriccionId in solicitud.Restricciones)
        {
            dieta.AgregarRestriccion(restriccionId);
        }

        await dietas.AddAsync(dieta, ct);
        await unidadDeTrabajo.CommitAsync(ct);

        return new IdCreadoDto(dieta.Id);
    }
}
