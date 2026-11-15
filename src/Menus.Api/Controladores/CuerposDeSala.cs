namespace Menus.Api.Controladores;

/// <summary>Cuerpo de <c>POST /api/salas</c> y <c>PUT /api/salas/{id}</c>: el nombre de la sala.</summary>
/// <param name="Nombre">Nombre descriptivo de la sala.</param>
public sealed record CuerpoDeSala(string Nombre);
