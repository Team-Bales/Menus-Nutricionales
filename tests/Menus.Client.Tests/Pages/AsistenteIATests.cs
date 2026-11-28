using System.Net;
using Bunit;
using Menus.Client.AsistenteIA;
using Menus.Client.Pages;
using Menus.Client.Services;
using Microsoft.Extensions.DependencyInjection;
using AsistenteIAPage = Menus.Client.Pages.AsistenteIA;

namespace Menus.Client.Tests.Pages;

/// <summary>Pruebas de la pestaña de consultas de IA para nutricionistas (#332).</summary>
public sealed class AsistenteIATests : BunitContext
{
    [Fact]
    public void Muestra_la_configuracion_y_los_bloques_por_defecto_de_la_explicacion()
    {
        Servicios(new AsistenteIAApiFalsa());
        AddAuthorization().SetAuthorized("nutri@centro.test").SetRoles(RolesDeUsuario.Nutricionista);

        var cut = Render<AsistenteIAPage>();

        Assert.Contains("Asistente de IA", cut.Markup);
        Assert.True(cut.Find("input[value='valoracion']").HasAttribute("checked"));
        Assert.True(cut.Find("input[value='aceptacion']").HasAttribute("checked"));
        Assert.False(cut.Find("input[value='ficha']").HasAttribute("checked"));
        Assert.Contains("Aún no hay una consulta", cut.Markup);
    }

    [Fact]
    public void Consulta_y_muestra_resultado_fuentes_y_aviso_fijo()
    {
        var api = new AsistenteIAApiFalsa();
        Servicios(api);
        AddAuthorization().SetAuthorized("nutri@centro.test").SetRoles(RolesDeUsuario.Nutricionista);
        var cut = Render<AsistenteIAPage>();
        var valoracionId = Guid.NewGuid();

        cut.Find("#valoracion-ia").Change(valoracionId.ToString());
        cut.Find("#pregunta-ia").Change("¿Qué debo revisar?");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Contains("Resumen de prueba", cut.Markup));
        Assert.Equal(valoracionId, api.SolicitudRecibida!.ValoracionId);
        Assert.Equal(TareaDeIA.ExplicarAnalisis, api.SolicitudRecibida.Tarea);
        Assert.Contains("generado con asistencia de IA, revisado por el nutricionista", cut.Markup);
        Assert.Contains("valoracion", cut.Markup);
        Assert.Contains("Advertencia de prueba", cut.Markup);
    }

    [Fact]
    public void Guion_para_paciente_limpia_y_deshabilita_la_pregunta()
    {
        Servicios(new AsistenteIAApiFalsa());
        AddAuthorization().SetAuthorized("nutri@centro.test").SetRoles(RolesDeUsuario.Nutricionista);
        var cut = Render<AsistenteIAPage>();

        cut.Find("#pregunta-ia").Change("texto previo");
        cut.Find("#tarea-ia").Change(TareaDeIA.GuionParaPaciente.ToString());

        Assert.True(cut.Find("#pregunta-ia").HasAttribute("disabled"));
        Assert.DoesNotContain("texto previo", cut.Find("#pregunta-ia").GetAttribute("value") ?? string.Empty);
        Assert.True(cut.Find("input[value='valoracion']").HasAttribute("checked"));
        Assert.True(cut.Find("input[value='aceptacion']").HasAttribute("checked"));
    }

    [Fact]
    public void Muestra_el_error_del_contrato_si_la_ia_no_esta_disponible()
    {
        Servicios(new AsistenteIAApiFalsa { Error = new ApiException(HttpStatusCode.ServiceUnavailable, "La IA no está disponible ahora.") });
        AddAuthorization().SetAuthorized("nutri@centro.test").SetRoles(RolesDeUsuario.Nutricionista);
        var cut = Render<AsistenteIAPage>();

        cut.Find("#valoracion-ia").Change(Guid.NewGuid().ToString());
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Contains("La IA no está disponible ahora.", cut.Markup));
    }

    private void Servicios(IAsistenteIAApi api) => Services.AddSingleton(api);

    private sealed class AsistenteIAApiFalsa : IAsistenteIAApi
    {
        public SolicitudDeIA? SolicitudRecibida { get; private set; }

        public ApiException? Error { get; init; }

        public Task<RespuestaDeIA> ConsultarAsync(SolicitudDeIA solicitud, CancellationToken ct = default)
        {
            SolicitudRecibida = solicitud;
            if (Error is not null)
            {
                return Task.FromException<RespuestaDeIA>(Error);
            }

            return Task.FromResult(new RespuestaDeIA(
                "Resumen de prueba",
                [new SeccionDeIA("Hallazgo", "Texto de prueba")],
                ["Recomendación de prueba"],
                ["Advertencia de prueba"],
                ["valoracion", "aceptacion"],
                true));
        }
    }
}
