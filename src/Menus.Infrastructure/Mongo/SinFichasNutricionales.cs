using Menus.Application.Catalogo;
using Menus.Domain.Entidades;

namespace Menus.Infrastructure.Mongo;

/// <summary>
/// Implementación de <see cref="IFichasNutricionales"/> cuando no hay MongoDB configurado: sin almacén, no hay fichas
/// que mantener, y la API funciona igual que antes (ver <see cref="DependencyInjection.AddInfrastructure"/>).
/// </summary>
public sealed class SinFichasNutricionales : IFichasNutricionales
{
    /// <inheritdoc/>
    public Task EliminarAsync(ItemAlimentario item) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task AgregarIngredienteAsync(Guid platoId, Alimento alimento) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task QuitarIngredienteAsync(Guid platoId, Guid alimentoId) => Task.CompletedTask;
}
