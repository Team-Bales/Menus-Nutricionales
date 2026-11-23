using Bunit;
using Bunit.TestDoubles;
using Menus.Client.Pages;
using Menus.Client.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Client.Tests.Pages;

/// <summary>Pruebas de la gestión de salas del issue #76.</summary>
public sealed class SalasTests : BunitContext
{
    public SalasTests() => AddAuthorization().SetAuthorized("admin@centro.test").SetRoles(RolesDeUsuario.Admin);

    [Fact]
    public void Muestra_la_sala_y_sus_pacientes()
    {
        Services.AddSingleton<IAdministracionApi>(new AdministracionApiFalsa());
        var cut = Render<Salas>();

        cut.WaitForAssertion(() => { Assert.Contains("Sala 101", cut.Markup); Assert.Contains(">2<", cut.Markup); });
    }

    [Fact]
    public void Crear_envia_el_nombre_de_la_sala()
    {
        var api = new AdministracionApiFalsa();
        Services.AddSingleton<IAdministracionApi>(api);
        var cut = Render<Salas>();
        cut.WaitForAssertion(() => Assert.Contains("Sala 101", cut.Markup));

        cut.Find("#nombre-sala").Change("Sala 102");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Equal("Sala 102", api.SalaCreada));
    }

    [Fact]
    public void Editar_y_guardar_envia_el_id_y_el_nombre()
    {
        var api = new AdministracionApiFalsa();
        Services.AddSingleton<IAdministracionApi>(api);
        var cut = Render<Salas>();
        cut.WaitForAssertion(() => Assert.Contains("Sala 101", cut.Markup));

        cut.FindAll("button").Single(b => b.TextContent == "Editar").Click();
        cut.Find("#nombre-sala").Change("Sala A");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Equal((AdministracionApiFalsa.SalaId, "Sala A"), api.SalaActualizada));
    }

    [Fact]
    public void Eliminar_envia_el_id_de_la_sala()
    {
        var api = new AdministracionApiFalsa();
        Services.AddSingleton<IAdministracionApi>(api);
        var cut = Render<Salas>();
        cut.WaitForAssertion(() => Assert.Contains("Sala 101", cut.Markup));

        cut.FindAll("button").Single(b => b.TextContent == "Eliminar").Click();

        cut.WaitForAssertion(() => Assert.Equal(AdministracionApiFalsa.SalaId, api.SalaEliminada));
    }

    [Fact]
    public void Error_de_la_api_se_muestra_de_forma_legible()
    {
        Services.AddSingleton<IAdministracionApi>(new AdministracionApiFalsa { Error = ErrorDePrueba.Conflicto("La sala tiene pacientes asignados.") });
        var cut = Render<Salas>();

        cut.WaitForAssertion(() => Assert.Contains("La sala tiene pacientes asignados.", cut.Markup));
    }

    [Fact]
    public void Cancelar_edicion_restaura_el_formulario_de_creacion()
    {
        Services.AddSingleton<IAdministracionApi>(new AdministracionApiFalsa());
        var cut = Render<Salas>();
        cut.WaitForAssertion(() => Assert.Contains("Sala 101", cut.Markup));

        cut.FindAll("button").Single(b => b.TextContent == "Editar").Click();
        cut.FindAll("button").Single(b => b.TextContent == "Cancelar").Click();

        Assert.Contains("Nueva sala", cut.Markup);
        Assert.Equal("Crear", cut.Find("button.btn-primary").TextContent);
    }

    [Fact]
    public void Error_al_guardar_se_muestra_de_forma_legible()
    {
        var api = new AdministracionApiFalsa { ErrorAlGuardarSala = ErrorDePrueba.Conflicto("Ya existe una sala con ese nombre.") };
        Services.AddSingleton<IAdministracionApi>(api);
        var cut = Render<Salas>();
        cut.WaitForAssertion(() => Assert.Contains("Sala 101", cut.Markup));

        cut.Find("#nombre-sala").Change("Sala 101");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Contains("Ya existe una sala con ese nombre.", cut.Markup));
    }
}
