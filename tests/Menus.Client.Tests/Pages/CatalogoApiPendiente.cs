using Menus.Client.Catalogo;
using Menus.Client.Services;

namespace Menus.Client.Tests.Pages;

/// <summary>
/// Falso de <see cref="ICatalogoApi"/> que deja pendientes las lecturas que necesitan las páginas
/// para comprobar su estado de carga. El resto de métodos delegan en <see cref="CatalogoApiFalsa"/>.
/// </summary>
internal sealed class CatalogoApiPendiente : ICatalogoApi
{
    private readonly CatalogoApiFalsa _real = new();
    private readonly TaskCompletionSource<IReadOnlyList<Nomenclador>> _grupos = new();
    private readonly TaskCompletionSource<IReadOnlyList<Nomenclador>> _tipos = new();
    private readonly TaskCompletionSource<IReadOnlyList<Nomenclador>> _niveles = new();
    private readonly TaskCompletionSource<IReadOnlyList<Nomenclador>> _restricciones = new();
    private readonly TaskCompletionSource<IReadOnlyList<Nomenclador>> _programas = new();
    private readonly TaskCompletionSource<IReadOnlyList<Nomenclador>> _especialidades = new();
    private readonly TaskCompletionSource<IReadOnlyList<Alimento>> _alimentos = new();
    private readonly TaskCompletionSource<PlatoDetalle> _plato = new();

    /// <summary>Completa todas las lecturas pendientes con los datos del falso normal.</summary>
    public void Completar()
    {
        _grupos.TrySetResult(_real.Grupos);
        _tipos.TrySetResult(_real.Tipos);
        _niveles.TrySetResult(_real.Niveles);
        _restricciones.TrySetResult(_real.Restricciones);
        _programas.TrySetResult(_real.Programas);
        _especialidades.TrySetResult(_real.Especialidades);
        _alimentos.TrySetResult(_real.Alimentos);
        _plato.TrySetResult(_real.PlatoDetalle);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Nomenclador>> ListarGruposAsync(CancellationToken ct = default) => _grupos.Task;

    /// <inheritdoc/>
    public Task<IReadOnlyList<Nomenclador>> ListarRestriccionesAsync(CancellationToken ct = default) => _restricciones.Task;

    /// <inheritdoc/>
    public Task<IReadOnlyList<Nomenclador>> ListarProgramasAsync(CancellationToken ct = default) => _programas.Task;

    /// <inheritdoc/>
    public Task<IReadOnlyList<Nomenclador>> ListarEspecialidadesAsync(CancellationToken ct = default) => _especialidades.Task;

    /// <inheritdoc/>
    public Task<IReadOnlyList<Nomenclador>> ListarTiposPreparacionAsync(CancellationToken ct = default) => _tipos.Task;

    /// <inheritdoc/>
    public Task<IReadOnlyList<Nomenclador>> ListarNivelesCaloricosAsync(CancellationToken ct = default) => _niveles.Task;

    /// <inheritdoc/>
    public Task<Guid> CrearNomencladorAsync(TipoDeNomenclador tipo, NomencladorFormulario formulario, CancellationToken ct = default) => _real.CrearNomencladorAsync(tipo, formulario, ct);

    /// <inheritdoc/>
    public Task EliminarNomencladorAsync(TipoDeNomenclador tipo, Guid id, CancellationToken ct = default) => _real.EliminarNomencladorAsync(tipo, id, ct);

    /// <inheritdoc/>
    public Task<Pagina<Dieta>> ListarDietasAsync(Guid? programa, int page, int size, CancellationToken ct = default) => _real.ListarDietasAsync(programa, page, size, ct);

    /// <inheritdoc/>
    public Task<IReadOnlyList<Dieta>> ListarTodasLasDietasAsync(CancellationToken ct = default) => _real.ListarTodasLasDietasAsync(ct);

    /// <inheritdoc/>
    public Task<DietaDetalle> ObtenerDietaAsync(Guid id, CancellationToken ct = default) => _real.ObtenerDietaAsync(id, ct);

    /// <inheritdoc/>
    public Task AgregarGrupoDietaAsync(Guid dietaId, GrupoCubiertoFormulario grupo, CancellationToken ct = default) => _real.AgregarGrupoDietaAsync(dietaId, grupo, ct);

    /// <inheritdoc/>
    public Task QuitarGrupoDietaAsync(Guid dietaId, Guid grupoId, CancellationToken ct = default) => _real.QuitarGrupoDietaAsync(dietaId, grupoId, ct);

    /// <inheritdoc/>
    public Task AgregarRestriccionDietaAsync(Guid dietaId, Guid restriccionId, CancellationToken ct = default) => _real.AgregarRestriccionDietaAsync(dietaId, restriccionId, ct);

    /// <inheritdoc/>
    public Task QuitarRestriccionDietaAsync(Guid dietaId, Guid restriccionId, CancellationToken ct = default) => _real.QuitarRestriccionDietaAsync(dietaId, restriccionId, ct);

    /// <inheritdoc/>
    public Task<Pagina<Alimento>> ListarAlimentosAsync(Guid? grupo, Guid? tipo, Guid? nivel, int page, int size, CancellationToken ct = default) => _real.ListarAlimentosAsync(grupo, tipo, nivel, page, size, ct);

    /// <inheritdoc/>
    public Task<IReadOnlyList<Alimento>> ListarTodosLosAlimentosAsync(CancellationToken ct = default) => _alimentos.Task;

    /// <inheritdoc/>
    public Task<Alimento> ObtenerAlimentoAsync(Guid id, CancellationToken ct = default) => _real.ObtenerAlimentoAsync(id, ct);

    /// <inheritdoc/>
    public Task<Guid> CrearAlimentoAsync(ClasificacionFormulario alimento, CancellationToken ct = default) => _real.CrearAlimentoAsync(alimento, ct);

    /// <inheritdoc/>
    public Task ActualizarAlimentoAsync(Guid id, ClasificacionFormulario alimento, CancellationToken ct = default) => _real.ActualizarAlimentoAsync(id, alimento, ct);

    /// <inheritdoc/>
    public Task EliminarAlimentoAsync(Guid id, CancellationToken ct = default) => _real.EliminarAlimentoAsync(id, ct);

    /// <inheritdoc/>
    public Task<Pagina<Plato>> ListarPlatosAsync(Guid? grupo, Guid? tipo, Guid? nivel, int page, int size, CancellationToken ct = default) => _real.ListarPlatosAsync(grupo, tipo, nivel, page, size, ct);

    /// <inheritdoc/>
    public Task<PlatoDetalle> ObtenerPlatoAsync(Guid id, CancellationToken ct = default) => _plato.Task;

    /// <inheritdoc/>
    public Task<Guid> CrearPlatoAsync(ClasificacionFormulario plato, IReadOnlyList<IngredienteFormulario> ingredientes, CancellationToken ct = default) => _real.CrearPlatoAsync(plato, ingredientes, ct);

    /// <inheritdoc/>
    public Task ActualizarPlatoAsync(Guid id, ClasificacionFormulario plato, CancellationToken ct = default) => _real.ActualizarPlatoAsync(id, plato, ct);

    /// <inheritdoc/>
    public Task EliminarPlatoAsync(Guid id, CancellationToken ct = default) => _real.EliminarPlatoAsync(id, ct);

    /// <inheritdoc/>
    public Task AgregarIngredienteAsync(Guid platoId, IngredienteFormulario ingrediente, CancellationToken ct = default) => _real.AgregarIngredienteAsync(platoId, ingrediente, ct);

    /// <inheritdoc/>
    public Task QuitarIngredienteAsync(Guid platoId, Guid alimentoId, CancellationToken ct = default) => _real.QuitarIngredienteAsync(platoId, alimentoId, ct);
}
