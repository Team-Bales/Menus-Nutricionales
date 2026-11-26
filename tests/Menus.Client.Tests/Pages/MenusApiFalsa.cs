using Menus.Client.Catalogo;
using Menus.Client.GestionDeMenus;
using Menus.Client.Services;

namespace Menus.Client.Tests.Pages;

/// <summary>Falso de <see cref="IMenusApi"/> para las pruebas bUnit de la generación de menús (#81).</summary>
internal sealed class MenusApiFalsa : IMenusApi
{
    public static readonly Guid MenuId = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111");
    public static readonly Guid OtroMenuId = Guid.Parse("aaaaaaaa-2222-2222-2222-222222222222");

    public List<MenuResumen> Menus { get; set; } =
    [
        new(MenuId, CatalogoApiFalsa.DietaId, new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            new(Guid.NewGuid(), "Ana Pérez"), true, 12, "Pendiente", null, null),
        new(OtroMenuId, CatalogoApiFalsa.DietaId, new DateTime(2026, 1, 10, 9, 0, 0, DateTimeKind.Utc),
            new(Guid.NewGuid(), "Luis García"), false, 8, "Final", null, null),
    ];

    public MenuDetalle Detalle { get; set; } = new(
        MenuId, CatalogoApiFalsa.DietaId, new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc),
        new(Guid.NewGuid(), "Ana Pérez"), true, 12, "Pendiente", null, null,
        [new(CatalogoApiFalsa.Tipo, "Plato fuerte", 1m)],
        [new(CatalogoApiFalsa.Grupo, "Cereales")],
        [new(CatalogoApiFalsa.AlimentoId, "Avena")]);

    /// <summary>Total que se declara en la página; por defecto coincide con los menús devueltos.</summary>
    public int? Total { get; set; }

    public ApiException? ErrorAlGenerar { get; set; }
    public ApiException? ErrorAlListar { get; set; }
    public ApiException? ErrorAlObtener { get; set; }

    public SolicitudDeGeneracion? UltimaSolicitud { get; private set; }
    public (Guid DietaId, int Page, int Size)? UltimaConsulta { get; private set; }
    public List<Guid> DetallesPedidos { get; } = [];

    public Task<Guid> GenerarAutomaticamenteAsync(SolicitudDeGeneracion solicitud, CancellationToken ct = default)
        => Resultado(ErrorAlGenerar, solicitud);

    public Task<Pagina<MenuResumen>> ListarPorDietaAsync(Guid dietaId, int page, int size, CancellationToken ct = default)
    {
        UltimaConsulta = (dietaId, page, size);
        var total = Total ?? Menus.Count;
        return Resultado(ErrorAlListar, new Pagina<MenuResumen>([.. Menus], total, page, size));
    }

    public Task<MenuDetalle> ObtenerAsync(Guid id, CancellationToken ct = default)
    {
        DetallesPedidos.Add(id);
        return Resultado(ErrorAlObtener, Detalle);
    }

    private async Task<Guid> Resultado(ApiException? error, SolicitudDeGeneracion solicitud)
    {
        await Task.Yield();
        if (error is not null)
        {
            throw error;
        }

        UltimaSolicitud = solicitud;
        return MenuId;
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
