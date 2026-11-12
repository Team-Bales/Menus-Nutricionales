using Menus.Application.Comun;

namespace Menus.Application.Pacientes.ListarPacientes;

/// <summary>Lista los pacientes por páginas, ordenados por nombre, con filtros opcionales por sala y por dieta.</summary>
/// <param name="SalaId">Solo los de esta sala (relación <c>Pertenece</c>).</param>
/// <param name="DietaId">Solo los inscritos en esta dieta (relación <c>Inscribe</c>).</param>
/// <param name="Page">Número de página, desde 1.</param>
/// <param name="Size">Pacientes por página, entre 1 y 100.</param>
public sealed record ListarPacientesQuery(
    Guid? SalaId = null,
    Guid? DietaId = null,
    int Page = 1,
    int Size = 20) : IQuery<ResultadoPaginado<PacienteDto>>;
