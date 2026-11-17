namespace Menus.Api.Controladores;

/// <summary>Cuerpo de <c>PUT /api/nutricionistas/{id}</c>: nombre y especialidad. El id va en la ruta.</summary>
/// <param name="Nombre">Nuevo nombre.</param>
/// <param name="EspecialidadId">Nueva especialidad.</param>
public sealed record CuerpoDeActualizacionDeNutricionista(string Nombre, Guid EspecialidadId);

/// <summary>
/// Cuerpo de <c>POST /api/nutricionistas/{id}/dietas</c>: la dieta a autorizar. El id del nutricionista va en la
/// ruta.
/// </summary>
/// <param name="DietaId">Identificador de la dieta a autorizar.</param>
public sealed record CuerpoDeAutorizacionDeDieta(Guid DietaId);
