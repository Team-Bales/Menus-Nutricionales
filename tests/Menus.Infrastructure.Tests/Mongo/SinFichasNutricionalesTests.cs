using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Menus.Infrastructure.Mongo;

namespace Menus.Infrastructure.Tests.Mongo;

/// <summary>
/// Pruebas de <see cref="SinFichasNutricionales"/>: sin MongoDB, los casos de uso igual llaman al puerto, y cada
/// operación tiene que terminar sin hacer nada ni fallar.
/// </summary>
public sealed class SinFichasNutricionalesTests
{
    private static readonly Alimento Arroz = new(
        Guid.NewGuid(),
        "Arroz",
        Guid.NewGuid(),
        TipoPreparacion.PlatoFuerte.Id,
        NivelCalorico.Medio.Id,
        Guid.NewGuid());

    private readonly SinFichasNutricionales _fichas = new();

    [Fact]
    public void EliminarAsync_termina_sin_hacer_nada()
        => Assert.True(_fichas.EliminarAsync(Arroz).IsCompletedSuccessfully);

    [Fact]
    public void AgregarIngredienteAsync_termina_sin_hacer_nada()
        => Assert.True(_fichas.AgregarIngredienteAsync(Guid.NewGuid(), Arroz).IsCompletedSuccessfully);

    [Fact]
    public void QuitarIngredienteAsync_termina_sin_hacer_nada()
        => Assert.True(_fichas.QuitarIngredienteAsync(Guid.NewGuid(), Arroz.Id).IsCompletedSuccessfully);
}
