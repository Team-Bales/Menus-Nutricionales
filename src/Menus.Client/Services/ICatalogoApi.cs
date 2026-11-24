using Menus.Client.Catalogo;

namespace Menus.Client.Services;

/// <summary>Contrato de la UI con los endpoints de catálogo.</summary>
/// <remarks>
/// Cada método llama a un endpoint de la API con el token de la sesión. Si la API responde con un error, lanza
/// <see cref="ApiException"/> con el mensaje que se muestra en la página.
/// </remarks>
public interface ICatalogoApi
{
    /// <summary>Grupos nutricionales (<c>GET api/nomencladores/grupos-nutricionales</c>).</summary>
    Task<IReadOnlyList<Nomenclador>> ListarGruposAsync(CancellationToken ct = default);

    /// <summary>Restricciones alimentarias (<c>GET api/nomencladores/restricciones-alimentarias</c>).</summary>
    Task<IReadOnlyList<Nomenclador>> ListarRestriccionesAsync(CancellationToken ct = default);

    /// <summary>Programas de atención (<c>GET api/nomencladores/programas-atencion</c>).</summary>
    Task<IReadOnlyList<Nomenclador>> ListarProgramasAsync(CancellationToken ct = default);

    /// <summary>Especialidades (<c>GET api/nomencladores/especialidades</c>).</summary>
    Task<IReadOnlyList<Nomenclador>> ListarEspecialidadesAsync(CancellationToken ct = default);

    /// <summary>Tipos de preparación, catálogo cerrado (<c>GET api/nomencladores/tipos-preparacion</c>).</summary>
    Task<IReadOnlyList<Nomenclador>> ListarTiposPreparacionAsync(CancellationToken ct = default);

    /// <summary>Niveles calóricos, catálogo cerrado (<c>GET api/nomencladores/niveles-caloricos</c>).</summary>
    Task<IReadOnlyList<Nomenclador>> ListarNivelesCaloricosAsync(CancellationToken ct = default);

    /// <summary>Agrega un elemento a un nomenclador abierto y devuelve su id (<c>POST</c>).</summary>
    Task<Guid> CrearNomencladorAsync(TipoDeNomenclador tipo, NomencladorFormulario formulario, CancellationToken ct = default);

    /// <summary>Elimina un elemento de un nomenclador abierto (<c>DELETE</c>).</summary>
    Task EliminarNomencladorAsync(TipoDeNomenclador tipo, Guid id, CancellationToken ct = default);

    /// <summary>Una página de dietas, opcionalmente de un programa de atención (<c>GET api/dietas</c>).</summary>
    Task<Pagina<Dieta>> ListarDietasAsync(Guid? programa, int page, int size, CancellationToken ct = default);

    /// <summary>Todas las dietas, pidiendo las páginas de a 100 hasta completar el total.</summary>
    Task<IReadOnlyList<Dieta>> ListarTodasLasDietasAsync(CancellationToken ct = default);

    /// <summary>Una dieta con sus grupos cubiertos y sus restricciones (<c>GET api/dietas/{id}</c>).</summary>
    Task<DietaDetalle> ObtenerDietaAsync(Guid id, CancellationToken ct = default);

    /// <summary>Agrega a la dieta un grupo nutricional, requerido o restringido (<c>POST api/dietas/{id}/grupos</c>).</summary>
    Task AgregarGrupoDietaAsync(Guid dietaId, GrupoCubiertoFormulario grupo, CancellationToken ct = default);

    /// <summary>Quita un grupo nutricional de la dieta (<c>DELETE api/dietas/{id}/grupos/{grupoId}</c>).</summary>
    Task QuitarGrupoDietaAsync(Guid dietaId, Guid grupoId, CancellationToken ct = default);

    /// <summary>Agrega una restricción alimentaria a la dieta (<c>POST api/dietas/{id}/restricciones</c>).</summary>
    Task AgregarRestriccionDietaAsync(Guid dietaId, Guid restriccionId, CancellationToken ct = default);

    /// <summary>Quita una restricción alimentaria de la dieta (<c>DELETE api/dietas/{id}/restricciones/{restriccionId}</c>).</summary>
    Task QuitarRestriccionDietaAsync(Guid dietaId, Guid restriccionId, CancellationToken ct = default);

    /// <summary>Una página de alimentos, filtrable por grupo, tipo de preparación y nivel calórico (<c>GET api/alimentos</c>).</summary>
    Task<Pagina<Alimento>> ListarAlimentosAsync(Guid? grupo, Guid? tipo, Guid? nivel, int page, int size, CancellationToken ct = default);

    /// <summary>Todos los alimentos, pidiendo las páginas de a 100 hasta completar el total.</summary>
    Task<IReadOnlyList<Alimento>> ListarTodosLosAlimentosAsync(CancellationToken ct = default);

    /// <summary>Un alimento (<c>GET api/alimentos/{id}</c>).</summary>
    Task<Alimento> ObtenerAlimentoAsync(Guid id, CancellationToken ct = default);

    /// <summary>Registra un alimento y devuelve su id (<c>POST api/alimentos</c>).</summary>
    Task<Guid> CrearAlimentoAsync(ClasificacionFormulario alimento, CancellationToken ct = default);

    /// <summary>Reclasifica un alimento; el nombre no se envía, porque no cambia (<c>PUT api/alimentos/{id}</c>).</summary>
    Task ActualizarAlimentoAsync(Guid id, ClasificacionFormulario alimento, CancellationToken ct = default);

    /// <summary>Elimina un alimento (<c>DELETE api/alimentos/{id}</c>).</summary>
    Task EliminarAlimentoAsync(Guid id, CancellationToken ct = default);

    /// <summary>Una página de platos, filtrable por grupo, tipo de preparación y nivel calórico (<c>GET api/platos</c>).</summary>
    Task<Pagina<Plato>> ListarPlatosAsync(Guid? grupo, Guid? tipo, Guid? nivel, int page, int size, CancellationToken ct = default);

    /// <summary>Un plato con su composición (<c>GET api/platos/{id}</c>).</summary>
    Task<PlatoDetalle> ObtenerPlatoAsync(Guid id, CancellationToken ct = default);

    /// <summary>Registra un plato con sus ingredientes iniciales y devuelve su id (<c>POST api/platos</c>).</summary>
    Task<Guid> CrearPlatoAsync(ClasificacionFormulario plato, IReadOnlyList<IngredienteFormulario> ingredientes, CancellationToken ct = default);

    /// <summary>Reclasifica un plato; el nombre no se envía, porque no cambia (<c>PUT api/platos/{id}</c>).</summary>
    Task ActualizarPlatoAsync(Guid id, ClasificacionFormulario plato, CancellationToken ct = default);

    /// <summary>Elimina un plato (<c>DELETE api/platos/{id}</c>).</summary>
    Task EliminarPlatoAsync(Guid id, CancellationToken ct = default);

    /// <summary>Agrega un alimento a la composición del plato (<c>POST api/platos/{id}/ingredientes</c>).</summary>
    Task AgregarIngredienteAsync(Guid platoId, IngredienteFormulario ingrediente, CancellationToken ct = default);

    /// <summary>Quita un alimento de la composición del plato (<c>DELETE api/platos/{id}/ingredientes/{alimentoId}</c>).</summary>
    Task QuitarIngredienteAsync(Guid platoId, Guid alimentoId, CancellationToken ct = default);
}
