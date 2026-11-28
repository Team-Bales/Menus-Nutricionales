using System.Security.Claims;
using Bunit;
using Menus.Client.GestionMenus;
using Menus.Client.Pages;
using Menus.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Client.Tests.Pages;

/// <summary>Pruebas de la revisión de menús por un jefe de nutrición (#82).</summary>
public sealed class RevisionMenuTests : BunitContext
{
    [Fact]
    public void El_jefe_ve_el_menu_y_puede_aprobarlo()
    {
        var api = new MenusApiFalsa();
        Servicios(EsJefe: true, api);

        var cut = Render<RevisionMenu>(ps => ps.Add(p => p.Id, MenusApiFalsa.MenuId));
        cut.WaitForAssertion(() => Assert.Contains("Ana Pérez", cut.Markup));

        cut.Find("#observaciones").Change("Conforme a los criterios.");
        cut.FindAll("button").Single(b => b.TextContent == "Aprobar menú").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal((MenusApiFalsa.MenuId, true, "Conforme a los criterios."), api.UltimoDictamen);
            Assert.Contains("fue aprobado", cut.Markup);
        });
    }

    [Fact]
    public void Un_nutricionista_sin_jefatura_no_ve_el_formulario_de_dictamen()
    {
        Servicios(EsJefe: false, new MenusApiFalsa());

        var cut = Render<RevisionMenu>(ps => ps.Add(p => p.Id, MenusApiFalsa.MenuId));

        Assert.DoesNotContain("Registrar dictamen", cut.Markup);
        Assert.DoesNotContain("Aprobar menú", cut.Markup);
    }

    private void Servicios(bool EsJefe, IMenusApi api)
    {
        var claims = new List<Claim> { new(ClaimTypes.Role, RolesDeUsuario.Nutricionista) };
        if (EsJefe) { claims.Add(new Claim(RolesDeUsuario.JefeNutricion, bool.TrueString)); }
        var usuario = new ClaimsPrincipal(new ClaimsIdentity(claims, "prueba"));
        Services.AddSingleton<AuthenticationStateProvider>(new EstadoDeAutenticacionFalso(usuario));
        Services.AddSingleton(api);
    }

    private sealed class EstadoDeAutenticacionFalso(ClaimsPrincipal usuario) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(usuario));
    }

    private sealed class MenusApiFalsa : IMenusApi
    {
        public static readonly Guid MenuId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        public (Guid MenuId, bool Aprobado, string? Observaciones)? UltimoDictamen { get; private set; }

        public Task<MenuDetalle> ObtenerAsync(Guid menuId, CancellationToken ct = default)
            => Task.FromResult(new MenuDetalle(
                menuId, Guid.NewGuid(), DateTime.UtcNow, new NutricionistaDelMenu(Guid.NewGuid(), "Ana Pérez"), true, 6,
                "Pendiente", null, null, [new(Guid.NewGuid(), "Plato fuerte", .5m)],
                [new(Guid.NewGuid(), "Proteínas")], [new(Guid.NewGuid(), "Pollo")])) ;

        public Task ValidarAsync(Guid menuId, bool aprobado, DictamenFormulario formulario, CancellationToken ct = default)
        {
            UltimoDictamen = (menuId, aprobado, formulario.Observaciones);
            return Task.CompletedTask;
        }
    }
}
