using Menus.Application.Comun;

namespace Menus.Application.Catalogo.Nomencladores.RestriccionesAlimentarias;

/// <summary>Elimina una restricción alimentaria que no está en uso.</summary>
/// <param name="Id">Identificador de la restricción alimentaria.</param>
public sealed record EliminarRestriccionAlimentariaCommand(Guid Id) : ICommand<Unidad>;
