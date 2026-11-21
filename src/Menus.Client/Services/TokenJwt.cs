using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Menus.Client.Services;

/// <summary>
/// Lectura del payload de un JWT en el cliente. No valida la firma (eso lo hace la API):
/// solo extrae los claims para la UI y comprueba si el token sigue vigente.
/// </summary>
internal static class TokenJwt
{
    /// <summary>
    /// Devuelve los claims del token si está bien formado y su <c>exp</c> no pasó;
    /// en cualquier otro caso devuelve <see langword="null"/>.
    /// </summary>
    /// <param name="token">JWT tal como lo emite la API.</param>
    public static IReadOnlyList<Claim>? LeerClaimsVigentes(string? token)
    {
        if (string.IsNullOrWhiteSpace(token) || !IntentarLeerClaims(token, out var claims))
        {
            return null;
        }

        var expiracion = claims.FirstOrDefault(c => c.Type == "exp")?.Value;
        if (!long.TryParse(expiracion, out var segundos) || DateTimeOffset.FromUnixTimeSeconds(segundos) <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        return claims;
    }

    private static bool IntentarLeerClaims(string token, out List<Claim> claims)
    {
        claims = [];
        var partes = token.Split('.');
        if (partes.Length != 3)
        {
            return false;
        }

        try
        {
            var payload = partes[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using var documento = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
            if (documento.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            foreach (var propiedad in documento.RootElement.EnumerateObject())
            {
                if (propiedad.Value.ValueKind == JsonValueKind.Array)
                {
                    // Un claim con varios valores (p. ej. "role": ["Admin", "Nutricionista"]) se
                    // convierte en un Claim por valor, que es como lo interpreta IsInRole.
                    foreach (var valor in propiedad.Value.EnumerateArray())
                    {
                        AgregarSiEsEscalar(claims, propiedad.Name, valor);
                    }
                }
                else
                {
                    AgregarSiEsEscalar(claims, propiedad.Name, propiedad.Value);
                }
            }

            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static void AgregarSiEsEscalar(List<Claim> claims, string tipo, JsonElement valor)
    {
        if (valor.ValueKind is JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
        {
            claims.Add(new Claim(tipo, valor.ToString()));
        }
    }
}
