using Menus.Application.Comun;

namespace Menus.Application.Catalogo.Nomencladores.RestriccionesAlimentarias;

/// <summary>Registra una restricción alimentaria nueva en el catálogo.</summary>
/// <param name="Nombre">Nombre de la restricción: no vacío y única entre las restricciones (RD-08).</param>
public sealed record RegistrarRestriccionAlimentariaCommand(string Nombre) : ICommand<IdCreadoDto>;
