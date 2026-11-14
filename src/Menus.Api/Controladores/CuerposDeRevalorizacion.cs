using System.ComponentModel.DataAnnotations;

namespace Menus.Api.Controladores;

/// <summary>Cuerpo de la solicitud para crear una revalorización.</summary>
/// <param name="ValoracionRevisadaId">Identificador de la valoración que se revisa.</param>
/// <param name="NutricionistaId">Identificador del nutricionista que la ejecutará.</param>
public sealed record CuerpoDeSolicitudDeRevalorizacion(
    [Required] Guid ValoracionRevisadaId,
    [Required] Guid NutricionistaId);

/// <summary>Cuerpo para registrar el resultado de una revalorización.</summary>
/// <param name="ResultadoNutricionalId">Identificador de la categoría de resultado nutricional (RR-17).</param>
public sealed record CuerpoDeResultadoDeRevalorizacion(
    [Required] Guid ResultadoNutricionalId);
