using Microsoft.Extensions.Configuration;

namespace Menus.Client.Services;

/// <summary>Agrega la identidad provisional del nutricionista mientras el cliente no usa autenticación.</summary>
public sealed class ActorHeaderHandler(IConfiguration configuration) : DelegatingHandler
{
    private const string HeaderName = "X-Actor-Id";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var actorId = configuration["Catalogo:ActorId"];
        if (Guid.TryParse(actorId, out _))
        {
            request.Headers.TryAddWithoutValidation(HeaderName, actorId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
