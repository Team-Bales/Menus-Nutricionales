using Bunit;
using Bunit.TestDoubles;
using Menus.Client.Pages;
using Menus.Client.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Client.Tests.Pages;

/// <summary>Pruebas del detalle y autorizaciones de un nutricionista del issue #76.</summary>
public sealed class DetalleNutricionistaTests : BunitContext
{
    public DetalleNutricionistaTests() => AddAuthorization().SetAuthorized("admin@centro.test").SetRoles(RolesDeUsuario.Admin);

    [Fact]
    public void Muestra_las_dietas_autorizadas_y_permite_revocarlas()
    {
        var api = new AdministracionApiFalsa();
        Services.AddSingleton<IAdministracionApi>(api);
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());
        var cut = Render<DetalleNutricionista>(ps => ps.Add(p => p.Id, AdministracionApiFalsa.NutricionistaId));
        cut.WaitForAssertion(() => Assert.Contains("Dieta basal", cut.Markup));

        cut.FindAll("button").Single(b => b.TextContent == "Revocar").Click();

        cut.WaitForAssertion(() => Assert.Equal((AdministracionApiFalsa.NutricionistaId, CatalogoApiFalsa.DietaId), api.UltimaRevocacion));
    }

    [Fact]
    public void Autorizar_envia_la_dieta_seleccionada()
    {
        var api = new AdministracionApiFalsa { Detalle = new(AdministracionApiFalsa.NutricionistaId, "Ana Pérez", CatalogoApiFalsa.Especialidad, false, []) };
        Services.AddSingleton<IAdministracionApi>(api);
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());
        var cut = Render<DetalleNutricionista>(ps => ps.Add(p => p.Id, AdministracionApiFalsa.NutricionistaId));
        cut.WaitForAssertion(() => Assert.Contains("Autorizar dieta", cut.Markup));

        cut.Find("#dieta").Change(CatalogoApiFalsa.DietaId.ToString());
        cut.FindAll("button").Single(b => b.TextContent == "Autorizar dieta").Click();

        cut.WaitForAssertion(() => Assert.Equal((AdministracionApiFalsa.NutricionistaId, CatalogoApiFalsa.DietaId), api.UltimaAutorizacion));
    }

    [Fact]
    public void Autorizar_sin_dieta_muestra_la_validacion()
    {
        Services.AddSingleton<IAdministracionApi>(new AdministracionApiFalsa());
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());
        var cut = Render<DetalleNutricionista>(ps => ps.Add(p => p.Id, AdministracionApiFalsa.NutricionistaId));
        cut.WaitForAssertion(() => Assert.Contains("Dieta basal", cut.Markup));

        cut.FindAll("button").Single(b => b.TextContent == "Autorizar dieta").Click();

        Assert.Contains("Selecciona una dieta.", cut.Markup);
    }

    [Fact]
    public void Designar_jefe_envia_la_operacion_y_refresca_el_detalle()
    {
        var api = new AdministracionApiFalsa();
        Services.AddSingleton<IAdministracionApi>(api);
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());
        var cut = Render<DetalleNutricionista>(ps => ps.Add(p => p.Id, AdministracionApiFalsa.NutricionistaId));
        cut.WaitForAssertion(() => Assert.Contains("Designar jefe", cut.Markup));

        cut.FindAll("button").Single(b => b.TextContent == "Designar jefe").Click();

        cut.WaitForAssertion(() => Assert.Equal(AdministracionApiFalsa.NutricionistaId, api.UltimoJefe));
    }

    [Fact]
    public void Volver_navega_al_listado()
    {
        Services.AddSingleton<IAdministracionApi>(new AdministracionApiFalsa());
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        var cut = Render<DetalleNutricionista>(ps => ps.Add(p => p.Id, AdministracionApiFalsa.NutricionistaId));
        cut.WaitForAssertion(() => Assert.Contains("Volver a nutricionistas", cut.Markup));

        cut.FindAll("button").Single(b => b.TextContent == "Volver a nutricionistas").Click();

        Assert.EndsWith("/admin/nutricionistas", nav.Uri);
    }

    [Fact]
    public void Jefe_sin_dietas_muestra_la_insignia_y_el_estado_vacio()
    {
        var api = new AdministracionApiFalsa
        {
            Detalle = new(AdministracionApiFalsa.NutricionistaId, "Ana Pérez", CatalogoApiFalsa.Especialidad, true, []),
        };
        Services.AddSingleton<IAdministracionApi>(api);
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());

        var cut = Render<DetalleNutricionista>(ps => ps.Add(p => p.Id, AdministracionApiFalsa.NutricionistaId));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Jefe de nutrición", cut.Markup);
            Assert.Contains("No tiene dietas autorizadas.", cut.Markup);
        });
    }

    [Fact]
    public void Error_al_cargar_se_muestra_de_forma_legible()
    {
        Services.AddSingleton<IAdministracionApi>(new AdministracionApiFalsa { Error = ErrorDePrueba.Conflicto("No se pudo cargar el nutricionista.") });
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());

        var cut = Render<DetalleNutricionista>(ps => ps.Add(p => p.Id, AdministracionApiFalsa.NutricionistaId));

        cut.WaitForAssertion(() => Assert.Contains("No se pudo cargar el nutricionista.", cut.Markup));
    }

    [Fact]
    public void Dieta_y_especialidad_desconocidas_se_muestran_con_guion()
    {
        var api = new AdministracionApiFalsa
        {
            Detalle = new(AdministracionApiFalsa.NutricionistaId, "Ana Pérez", Guid.NewGuid(), false, [new(Guid.NewGuid())]),
        };
        Services.AddSingleton<IAdministracionApi>(api);
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());

        var cut = Render<DetalleNutricionista>(ps => ps.Add(p => p.Id, AdministracionApiFalsa.NutricionistaId));

        cut.WaitForAssertion(() => Assert.Equal(2, cut.Markup.Split('—').Length - 1));
    }
}
