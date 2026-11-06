namespace Menus.Api.Controladores;

/// <summary>Cuerpo de <c>PUT /api/alimentos/{id}</c>: la clasificación nueva. El id va en la ruta.</summary>
/// <param name="GrupoNutricionalId">Nuevo grupo nutricional.</param>
/// <param name="TipoPreparacionId">Nuevo tipo de preparación (RD-01).</param>
/// <param name="NivelCaloricoId">Nuevo nivel calórico (RD-02).</param>
public sealed record ClasificacionDeAlimento(Guid GrupoNutricionalId, Guid TipoPreparacionId, Guid NivelCaloricoId);
