using Menus.Application.Comun;
using Menus.Application.Revalorizaciones.SolicitarRevalorizacion;
using Menus.Domain;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;

namespace Menus.Application.Revalorizaciones.SolicitarRevalorizacion;

/// <summary>Manejador de <see cref="SolicitarRevalorizacionCommand"/>.</summary>
/// <remarks>
/// La solicita el paciente de la valoración revisada, y nadie más (RS-09, #267). La base lo cumple por estructura,
/// porque el solicitante no se guarda: se deriva de la valoración. Lo que la base no puede saber es quién hace la
/// petición, y eso se comprueba aquí con la identidad del token. La API ya exige el rol de paciente
/// (<c>SoloPaciente</c>); aquí se comprueba que la valoración sea suya.
/// </remarks>
internal sealed class SolicitarRevalorizacionHandler(
    IRevalorizacionRepository revalorizaciones,
    IValoracionRepository valoraciones,
    INutricionistaRepository nutricionistas,
    IUnitOfWork unidadDeTrabajo,
    IIdentidadAutenticada identidad) : IHandler<SolicitarRevalorizacionCommand, IdCreadoDto>
{
    /// <exception cref="NoEncontradoException">Si la valoración revisada o el nutricionista no existen.</exception>
    /// <exception cref="AccesoDenegadoException">Si quien la pide no es el paciente de la valoración revisada (RS-09).</exception>
    public async Task<IdCreadoDto> HandleAsync(SolicitarRevalorizacionCommand comando, CancellationToken ct)
    {
        // Verificar que la valoración revisada existe
        var valoracionRevisada = await valoraciones.GetByIdAsync(comando.ValoracionRevisadaId, ct)
            ?? throw new NoEncontradoException("valoración", comando.ValoracionRevisadaId);

        // RS-09: solo su paciente pide revalorizarla. Va después del 404, para no confundir una valoración que no
        // existe con una ajena.
        if (identidad.Rol != RolesUsuario.Paciente || identidad.EntidadId != valoracionRevisada.PacienteId)
        {
            throw new AccesoDenegadoException("Solo el paciente de la valoración puede solicitar su revalorización (RS-09).");
        }

        // Verificar que el nutricionista existe
        var nutricionista = await nutricionistas.GetByIdAsync(comando.NutricionistaId, ct)
            ?? throw new NoEncontradoException("nutricionista", comando.NutricionistaId);

        // Crear la revalorización con ValoracionGeneradaId en null (resultado nulo)
        var revalorizacion = new Revalorizacion(
            Guid.NewGuid(),
            DateTime.UtcNow,
            comando.NutricionistaId,
            comando.ValoracionRevisadaId);

        await revalorizaciones.AddAsync(revalorizacion, ct);
        await unidadDeTrabajo.CommitAsync(ct);

        return new IdCreadoDto(revalorizacion.Id);
    }
}
