using Menus.Client.Catalogo;

namespace Menus.Client.Services;

/// <summary>Contrato de la UI con los endpoints de catálogo.</summary>
public interface ICatalogoApi
{
    Task<IReadOnlyList<Nomenclador>> ListarGruposAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Nomenclador>> ListarRestriccionesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Nomenclador>> ListarProgramasAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Nomenclador>> ListarEspecialidadesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Nomenclador>> ListarTiposPreparacionAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Nomenclador>> ListarNivelesCaloricosAsync(CancellationToken ct = default);
    Task<Guid> CrearNomencladorAsync(TipoDeNomenclador tipo, NomencladorFormulario formulario, CancellationToken ct = default);
    Task EliminarNomencladorAsync(TipoDeNomenclador tipo, Guid id, CancellationToken ct = default);
    Task<Pagina<Dieta>> ListarDietasAsync(Guid? programa, int page, int size, CancellationToken ct = default);
    Task<DietaDetalle> ObtenerDietaAsync(Guid id, CancellationToken ct = default);
    Task AgregarGrupoDietaAsync(Guid dietaId, GrupoCubiertoFormulario grupo, CancellationToken ct = default);
    Task QuitarGrupoDietaAsync(Guid dietaId, Guid grupoId, CancellationToken ct = default);
    Task AgregarRestriccionDietaAsync(Guid dietaId, Guid restriccionId, CancellationToken ct = default);
    Task QuitarRestriccionDietaAsync(Guid dietaId, Guid restriccionId, CancellationToken ct = default);
    Task<Pagina<Alimento>> ListarAlimentosAsync(Guid? grupo, Guid? tipo, Guid? nivel, int page, int size, CancellationToken ct = default);
    Task<IReadOnlyList<Alimento>> ListarTodosLosAlimentosAsync(CancellationToken ct = default);
    Task<Alimento> ObtenerAlimentoAsync(Guid id, CancellationToken ct = default);
    Task<Guid> CrearAlimentoAsync(ClasificacionFormulario alimento, CancellationToken ct = default);
    Task ActualizarAlimentoAsync(Guid id, ClasificacionFormulario alimento, CancellationToken ct = default);
    Task EliminarAlimentoAsync(Guid id, CancellationToken ct = default);
    Task<Pagina<Plato>> ListarPlatosAsync(Guid? grupo, Guid? tipo, Guid? nivel, int page, int size, CancellationToken ct = default);
    Task<PlatoDetalle> ObtenerPlatoAsync(Guid id, CancellationToken ct = default);
    Task<Guid> CrearPlatoAsync(ClasificacionFormulario plato, IReadOnlyList<IngredienteFormulario> ingredientes, CancellationToken ct = default);
    Task ActualizarPlatoAsync(Guid id, ClasificacionFormulario plato, CancellationToken ct = default);
    Task EliminarPlatoAsync(Guid id, CancellationToken ct = default);
    Task AgregarIngredienteAsync(Guid platoId, IngredienteFormulario ingrediente, CancellationToken ct = default);
    Task QuitarIngredienteAsync(Guid platoId, Guid alimentoId, CancellationToken ct = default);
}
