using Menus.Client.Catalogo;
using Menus.Client.MenusGenerados;
using Menus.Client.Services;

namespace Menus.Client.Tests.Pages;

/// <summary>Falso de <see cref="IMenusApi"/> para las pruebas bUnit de la página de menús (issue #81).</summary>
internal sealed class MenusApiFalsa : IMenusApi
{
    public static readonly Guid MenuId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    public static readonly DateTime Fecha = new(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    public List<Menu> Menus { get; set; } =
    [
        new(MenuId, CatalogoApiFalsa.DietaId, Fecha, new(AdministracionApiFalsa.NutricionistaId, "Ana Pérez"), true, 10, "Pendiente", null, null),
    ];

    public MenuDetalle Detalle { get; set; } = new(
        MenuId,
        CatalogoApiFalsa.DietaId,
        Fecha,
        new(AdministracionApiFalsa.NutricionistaId, "Ana Pérez"),
        true,
        10,
        "Pendiente",
        null,
        null,
        [new(CatalogoApiFalsa.Tipo, "Plato fuerte", 1m)],
        [new(CatalogoApiFalsa.Grupo, "Cereales")],
        []);

    public Dictionary<Guid, MenuDetalle> Detalles { get; } = new();
    public HashSet<Guid> SinDetalle { get; } = [];
    public ApiException? Error { get; set; }
    public ApiException? ErrorAlListar { get; set; }
    public ApiException? ErrorAlObtener { get; set; }
    public ApiException? ErrorAlGenerar { get; set; }
    public (Guid DietaId, string? Estado, int Page, int Size)? UltimaConsulta { get; private set; }
    public GeneracionDeMenu? UltimaGeneracion { get; private set; }

    public Task<Pagina<Menu>> ListarMenusAsync(Guid dietaId, string? estado, int page, int size, CancellationToken ct = default)
    {
        UltimaConsulta = (dietaId, estado, page, size);
        var filtrados = Menus
            .Where(m => string.IsNullOrEmpty(estado) || m.Estado == estado)
            .ToList();
        return Resultado(ErrorAlListar ?? Error, new Pagina<Menu>([.. filtrados], filtrados.Count, page, size));
    }

    public Task<MenuDetalle> ObtenerMenuAsync(Guid id, CancellationToken ct = default)
    {
        var detalle = Detalles.TryGetValue(id, out var propio)
            ? propio
            : Detalle with { Id = SinDetalle.Contains(id) ? Guid.NewGuid() : id };
        return Resultado(ErrorAlObtener ?? Error, detalle);
    }

    public Task<Guid> GenerarMenuAsync(GeneracionDeMenu generacion, CancellationToken ct = default)
    {
        UltimaGeneracion = generacion;
        return Resultado(ErrorAlGenerar ?? Error, Guid.NewGuid());
    }

    private static async Task<T> Resultado<T>(ApiException? error, T valor)
    {
        await Task.Yield();
        if (error is not null)
        {
            throw error;
        }

        return valor;
    }
}
