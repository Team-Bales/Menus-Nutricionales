using Bunit;
using Menus.Client.Catalogo;
using Menus.Client.Pages;
using Menus.Client.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Client.Tests.Pages;

/// <summary>Pruebas de la administración de nomencladores del issue #66.</summary>
public sealed class NomencladoresTests : BunitContext
{
    [Fact]
    public void Cambiar_seccion_carga_el_nomenclador_correspondiente()
    {
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());

        var cut = Render<Nomencladores>();
        cut.WaitForAssertion(() => Assert.Contains("Cereales", cut.Markup));

        cut.FindAll("button").Single(button => button.TextContent == "Restricciones alimentarias").Click();

        cut.WaitForAssertion(() => Assert.Contains("Sin gluten", cut.Markup));
    }

    [Fact]
    public void Agregar_envia_el_nombre_y_refresca_la_seccion_actual()
    {
        var api = new CatalogoApiFalsa();
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<Nomencladores>();
        cut.WaitForAssertion(() => Assert.Contains("Cereales", cut.Markup));

        cut.Find("#nombre-nomenclador").Change("Verduras");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(api.UltimoNomencladorCreado);
            Assert.Equal(TipoDeNomenclador.GruposNutricionales, api.UltimoNomencladorCreado!.Value.Tipo);
            Assert.Equal("Verduras", api.UltimoNomencladorCreado.Value.Nombre);
        });
    }

    [Fact]
    public void Error_al_eliminar_se_muestra_de_forma_legible()
    {
        var api = new CatalogoApiFalsa
        {
            ErrorAlEliminar = ErrorDePrueba.Conflicto("El nomenclador está en uso.")
        };
        Services.AddSingleton<ICatalogoApi>(api);

        var cut = Render<Nomencladores>();
        cut.WaitForAssertion(() => Assert.Contains("Cereales", cut.Markup));

        cut.Find("button.btn-outline-danger").Click();

        cut.WaitForAssertion(() => Assert.Contains("El nomenclador está en uso.", cut.Markup));
    }
}
