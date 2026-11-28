using System.Security.Claims;
using Bunit;
using Menus.Client.GestionMenus;
using Menus.Client.Pages;
using Menus.Client.PanelPaciente;
using Menus.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using PanelPacientePage = Menus.Client.Pages.PanelPaciente;

namespace Menus.Client.Tests.Pages;

/// <summary>Pruebas del panel clínico que ve el propio paciente (#88).</summary>
public sealed class PanelPacienteTests : BunitContext
{
    [Fact]
    public void Muestra_menus_consumos_y_valoraciones_del_paciente_del_token()
    {
        var paciente = Guid.NewGuid();
        var api = new PanelPacienteApiFalsa();
        Servicios(paciente, api);

        var cut = Render<PanelPacientePage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Pollo", cut.Markup);
            Assert.Contains("3/4 aceptados", cut.Markup);
            Assert.Contains("Buena", cut.Markup);
            Assert.Equal(paciente, api.PacienteConsultado);
        });
    }

    private void Servicios(Guid paciente, IPanelPacienteApi api)
    {
        var usuario = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, RolesDeUsuario.Paciente), new Claim("entidad_id", paciente.ToString())], "prueba"));
        Services.AddSingleton<AuthenticationStateProvider>(new EstadoFalso(usuario));
        Services.AddSingleton(api);
    }

    private sealed class EstadoFalso(ClaimsPrincipal usuario) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(usuario));
    }

    private sealed class PanelPacienteApiFalsa : IPanelPacienteApi
    {
        public Guid? PacienteConsultado { get; private set; }

        public Task<IReadOnlyList<MenuDetalle>> ListarMenusAsync(Guid pacienteId, CancellationToken ct = default)
        {
            PacienteConsultado = pacienteId;
            return Task.FromResult<IReadOnlyList<MenuDetalle>>([new(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, new(Guid.NewGuid(), "Ana"), true, 4, "Final", null, null, [], [], [new(Guid.NewGuid(), "Pollo")])]);
        }

        public Task<IReadOnlyList<ServicioDePaciente>> ListarConsumosAsync(Guid pacienteId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ServicioDePaciente>>([new(Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), [new(Guid.NewGuid(), "Pollo", true), new(Guid.NewGuid(), "Arroz", true), new(Guid.NewGuid(), "Ensalada", true), new(Guid.NewGuid(), "Jugo", false)])]);

        public Task<IReadOnlyList<ValoracionDePaciente>> ListarValoracionesAsync(Guid pacienteId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ValoracionDePaciente>>([new(Guid.NewGuid(), Guid.NewGuid(), pacienteId, Guid.NewGuid(), new(Guid.NewGuid(), "Buena", 4), DateTime.UtcNow)]);
    }
}
