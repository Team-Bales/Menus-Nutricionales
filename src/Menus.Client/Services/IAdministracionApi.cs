using Menus.Client.Administracion;
using Menus.Client.Catalogo;

namespace Menus.Client.Services;

/// <summary>Contrato de la UI con los endpoints administrativos de nutricionistas y salas.</summary>
public interface IAdministracionApi
{
    Task<Pagina<Nutricionista>> ListarNutricionistasAsync(Guid? especialidadId, int page, int size, CancellationToken ct = default);
    Task<NutricionistaDetalle> ObtenerNutricionistaAsync(Guid id, CancellationToken ct = default);
    Task AutorizarDietaAsync(Guid nutricionistaId, Guid dietaId, CancellationToken ct = default);
    Task RevocarDietaAsync(Guid nutricionistaId, Guid dietaId, CancellationToken ct = default);
    Task AsignarRolJefeAsync(Guid nutricionistaId, CancellationToken ct = default);
    Task<IReadOnlyList<Sala>> ListarSalasAsync(CancellationToken ct = default);
    Task<int> ContarPacientesEnSalaAsync(Guid salaId, CancellationToken ct = default);
    Task<Guid> CrearSalaAsync(SalaFormulario formulario, CancellationToken ct = default);
    Task ActualizarSalaAsync(Guid salaId, SalaFormulario formulario, CancellationToken ct = default);
    Task EliminarSalaAsync(Guid salaId, CancellationToken ct = default);
}
