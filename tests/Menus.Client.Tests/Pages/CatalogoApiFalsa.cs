using System.Net;
using Menus.Client.Catalogo;
using Menus.Client.Services;

namespace Menus.Client.Tests.Pages;

/// <summary>
/// Falso de <see cref="ICatalogoApi"/> para las pruebas bUnit de las páginas del catálogo (issue #65): datos fijos
/// configurables y errores inyectables, sin levantar servidor.
/// </summary>
internal sealed class CatalogoApiFalsa : ICatalogoApi
{
    public static readonly Guid Grupo = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid Tipo = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid Nivel = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid AlimentoId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly Guid PlatoId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    public List<Nomenclador> Grupos { get; set; } = [new(Grupo, "Cereales")];
    public List<Nomenclador> Tipos { get; set; } = [new(Tipo, "Plato fuerte")];
    public List<Nomenclador> Niveles { get; set; } = [new(Nivel, "Medio")];
    public List<Alimento> Alimentos { get; set; } = [new(AlimentoId, "Avena", Grupo, Tipo, Nivel, Guid.NewGuid())];
    public List<Plato> Platos { get; set; } = [new(PlatoId, "Avena con leche", Grupo, Tipo, Nivel, Guid.NewGuid(), 2)];
    public PlatoDetalle PlatoDetalle { get; set; } = new(PlatoId, "Avena con leche", Grupo, Tipo, Nivel, Guid.NewGuid(), [new(AlimentoId, "Avena", 100m)]);
    public Alimento Alimento { get; set; } = new(AlimentoId, "Avena", Grupo, Tipo, Nivel, Guid.NewGuid());
    public Guid IdCreado { get; set; } = Guid.NewGuid();

    public ApiException? ErrorAlCargarNomencladores { get; set; }
    public ApiException? ErrorAlCargarAlimento { get; set; }
    public ApiException? ErrorAlCargarPlato { get; set; }
    public ApiException? ErrorAlGuardar { get; set; }
    public ApiException? ErrorAlEliminar { get; set; }
    public ApiException? ErrorAlAgregarIngrediente { get; set; }
    public ApiException? ErrorAlQuitarIngrediente { get; set; }

    public ClasificacionFormulario? UltimaClasificacionCreada { get; private set; }
    public (Guid Id, ClasificacionFormulario Formulario)? UltimaActualizacion { get; private set; }
    public Guid? UltimoEliminado { get; private set; }
    public IReadOnlyList<IngredienteFormulario>? UltimosIngredientesCreados { get; private set; }

    // Guarda (AlimentoId, Cantidad) copiados, no la instancia de IngredienteFormulario: la página reutiliza y
    // reinicia ese mismo objeto después de llamar aquí, así que guardar la referencia vería el valor ya reseteado.
    public (Guid PlatoId, Guid AlimentoId, decimal Cantidad)? UltimoIngredienteAgregado { get; private set; }
    public (Guid PlatoId, Guid AlimentoId)? UltimoIngredienteQuitado { get; private set; }
    public (Guid? Grupo, Guid? Tipo, Guid? Nivel, int Page, int Size)? UltimaConsultaAlimentos { get; private set; }
    public (Guid? Grupo, Guid? Tipo, Guid? Nivel, int Page, int Size)? UltimaConsultaPlatos { get; private set; }

    public Task<IReadOnlyList<Nomenclador>> ListarGruposAsync(CancellationToken ct = default)
        => Resultado<IReadOnlyList<Nomenclador>>(ErrorAlCargarNomencladores, [.. Grupos]);

    public Task<IReadOnlyList<Nomenclador>> ListarTiposPreparacionAsync(CancellationToken ct = default)
        => Resultado<IReadOnlyList<Nomenclador>>(ErrorAlCargarNomencladores, [.. Tipos]);

    public Task<IReadOnlyList<Nomenclador>> ListarNivelesCaloricosAsync(CancellationToken ct = default)
        => Resultado<IReadOnlyList<Nomenclador>>(ErrorAlCargarNomencladores, [.. Niveles]);

    public Task<Pagina<Alimento>> ListarAlimentosAsync(Guid? grupo, Guid? tipo, Guid? nivel, int page, int size, CancellationToken ct = default)
    {
        UltimaConsultaAlimentos = (grupo, tipo, nivel, page, size);
        return Resultado(ErrorAlCargarAlimento, new Pagina<Alimento>([.. Alimentos], Alimentos.Count, page, size));
    }

    public Task<IReadOnlyList<Alimento>> ListarTodosLosAlimentosAsync(CancellationToken ct = default)
        => Resultado<IReadOnlyList<Alimento>>(ErrorAlCargarAlimento, [.. Alimentos]);

    public Task<Alimento> ObtenerAlimentoAsync(Guid id, CancellationToken ct = default)
        => Resultado(ErrorAlCargarAlimento, Alimento);

    public Task<Guid> CrearAlimentoAsync(ClasificacionFormulario alimento, CancellationToken ct = default)
    {
        UltimaClasificacionCreada = alimento;
        return Resultado(ErrorAlGuardar, IdCreado);
    }

    public Task ActualizarAlimentoAsync(Guid id, ClasificacionFormulario alimento, CancellationToken ct = default)
    {
        UltimaActualizacion = (id, alimento);
        return Resultado(ErrorAlGuardar);
    }

    public Task EliminarAlimentoAsync(Guid id, CancellationToken ct = default)
    {
        UltimoEliminado = id;
        return Resultado(ErrorAlEliminar);
    }

    public Task<Pagina<Plato>> ListarPlatosAsync(Guid? grupo, Guid? tipo, Guid? nivel, int page, int size, CancellationToken ct = default)
    {
        UltimaConsultaPlatos = (grupo, tipo, nivel, page, size);
        return Resultado(ErrorAlCargarPlato, new Pagina<Plato>([.. Platos], Platos.Count, page, size));
    }

    public Task<PlatoDetalle> ObtenerPlatoAsync(Guid id, CancellationToken ct = default)
        => Resultado(ErrorAlCargarPlato, PlatoDetalle);

    public Task<Guid> CrearPlatoAsync(ClasificacionFormulario plato, IReadOnlyList<IngredienteFormulario> ingredientes, CancellationToken ct = default)
    {
        UltimaClasificacionCreada = plato;
        UltimosIngredientesCreados = ingredientes;
        return Resultado(ErrorAlGuardar, IdCreado);
    }

    public Task ActualizarPlatoAsync(Guid id, ClasificacionFormulario plato, CancellationToken ct = default)
    {
        UltimaActualizacion = (id, plato);
        return Resultado(ErrorAlGuardar);
    }

    public Task EliminarPlatoAsync(Guid id, CancellationToken ct = default)
    {
        UltimoEliminado = id;
        return Resultado(ErrorAlEliminar);
    }

    public Task AgregarIngredienteAsync(Guid platoId, IngredienteFormulario ingrediente, CancellationToken ct = default)
    {
        UltimoIngredienteAgregado = (platoId, ingrediente.AlimentoId, ingrediente.Cantidad);
        return Resultado(ErrorAlAgregarIngrediente);
    }

    public Task QuitarIngredienteAsync(Guid platoId, Guid alimentoId, CancellationToken ct = default)
    {
        UltimoIngredienteQuitado = (platoId, alimentoId);
        return Resultado(ErrorAlQuitarIngrediente);
    }

    // Cede el hilo de verdad (en vez de Task.FromResult) para que las páginas pasen por su estado "cargando":
    // con una tarea ya completada, el await no suspende y ese renglón intermedio nunca se alcanza a renderizar.
    private static async Task<T> Resultado<T>(ApiException? error, T valor)
    {
        await Task.Yield();
        if (error is not null)
        {
            throw error;
        }

        return valor;
    }

    private static async Task Resultado(ApiException? error)
    {
        await Task.Yield();
        if (error is not null)
        {
            throw error;
        }
    }
}

/// <summary>Un <see cref="ApiException"/> cualquiera, para las pruebas que solo necesitan que algo falle.</summary>
internal static class ErrorDePrueba
{
    public static ApiException Conflicto(string mensaje) => new(HttpStatusCode.Conflict, mensaje);
}
