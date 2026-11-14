using FluentValidation;
using Menus.Application.GeneracionDeMenus;
using Menus.Application.GeneracionDeMenus.GenerarMenuAutomaticamente;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Application.Tests.GeneracionDeMenus;

/// <summary>Pruebas de la forma de la entrada del #98 (ADR-010, §3: 422 con los errores por campo).</summary>
public sealed class ValidadorDeGenerarMenuAutomaticamenteTests
{
    private static string[] Campos(GenerarMenuAutomaticamenteCommand solicitud)
        => [.. new GenerarMenuAutomaticamenteValidator().Validate(solicitud).Errors.Select(e => e.PropertyName).Distinct()];

    private static GenerarMenuAutomaticamenteCommand Valido() => new(
        Guid.NewGuid(), 10, [(Guid.NewGuid(), 1m)], [Guid.NewGuid()]);

    [Fact]
    public void Valido_no_tiene_errores() => Assert.Empty(Campos(Valido()));

    [Fact]
    public void Sin_dieta_falla_en_DietaId()
        => Assert.Equal(["DietaId"], Campos(Valido() with { DietaId = Guid.Empty }));

    [Fact]
    public void Cantidad_no_positiva_falla_en_CantidadTotalAlimentos()
        => Assert.Equal(["CantidadTotalAlimentos"], Campos(Valido() with { CantidadTotalAlimentos = 0 }));

    [Fact]
    public void Sin_distribucion_falla_en_Distribucion()
        => Assert.Equal(["Distribucion"], Campos(Valido() with { Distribucion = [] }));

    [Fact]
    public void Sin_grupos_falla_en_GruposNutricionalesIds()
        => Assert.Equal(["GruposNutricionalesIds"], Campos(Valido() with { GruposNutricionalesIds = [] }));

    [Fact]
    public void El_validador_se_registra_con_la_capa_de_aplicacion()
    {
        using var proveedor = new ServiceCollection().AddApplication().BuildServiceProvider();
        using var alcance = proveedor.CreateScope();

        Assert.IsType<GenerarMenuAutomaticamenteValidator>(
            Assert.Single(alcance.ServiceProvider.GetServices<IValidator<GenerarMenuAutomaticamenteCommand>>()));
    }

    [Fact]
    public void La_estrategia_se_registra_con_la_capa_de_aplicacion()
    {
        using var proveedor = new ServiceCollection().AddApplication().BuildServiceProvider();
        using var alcance = proveedor.CreateScope();

        Assert.IsType<EstrategiaGeneracionAleatoria>(alcance.ServiceProvider.GetRequiredService<IEstrategiaGeneracionMenu>());
    }
}
