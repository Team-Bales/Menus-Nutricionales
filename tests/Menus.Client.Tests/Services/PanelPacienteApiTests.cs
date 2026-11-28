using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Menus.Client.Services;

namespace Menus.Client.Tests.Services;

/// <summary>Pruebas del contrato HTTP del panel de paciente (#88).</summary>
public sealed class PanelPacienteApiTests
{
    [Fact]
    public async Task Las_tres_consultas_usan_las_rutas_del_paciente_autenticado()
    {
        var paciente = Guid.NewGuid();
        var rutas = new List<string>();
        var handler = new HandlerFalso(req =>
        {
            rutas.Add(req.RequestUri!.AbsolutePath);
            return Json(req, "[]");
        });
        var api = new PanelPacienteApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        await api.ListarMenusAsync(paciente);
        await api.ListarConsumosAsync(paciente);
        await api.ListarValoracionesAsync(paciente);

        Assert.Equal(
            [$"/api/pacientes/{paciente}/menus", $"/api/pacientes/{paciente}/consumos", $"/api/pacientes/{paciente}/valoraciones"],
            rutas);
    }

    private static HttpResponseMessage Json(HttpRequestMessage _, string json)
        => new(HttpStatusCode.OK) { Content = JsonContent.Create(JsonDocument.Parse(json).RootElement) };

    private sealed class HandlerFalso(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }
}
