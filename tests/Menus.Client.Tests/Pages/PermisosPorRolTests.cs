using System.Reflection;
using Bunit;
using Bunit.TestDoubles;
using Menus.Client.Pages;
using Menus.Client.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Client.Tests.Pages;

/// <summary>
/// La UI pide los mismos roles que las políticas de la API (#75, ADR-016): las páginas y los
/// botones de escritura no aparecen para quien la API respondería con 403.
/// </summary>
public sealed class PermisosPorRolTests : BunitContext
{
    [Theory]
    [InlineData(typeof(CatalogoAlimentos), RolesDeUsuario.Personal)]
    [InlineData(typeof(CatalogoPlatos), RolesDeUsuario.Personal)]
    [InlineData(typeof(DetallePlato), RolesDeUsuario.Personal)]
    [InlineData(typeof(CatalogoDietas), RolesDeUsuario.Personal)]
    [InlineData(typeof(DetalleDieta), RolesDeUsuario.Personal)]
    [InlineData(typeof(FormularioAlimento), RolesDeUsuario.Nutricionista)]
    [InlineData(typeof(FormularioPlato), RolesDeUsuario.Nutricionista)]
    [InlineData(typeof(Nomencladores), RolesDeUsuario.Admin)]
    [InlineData(typeof(Nutricionistas), RolesDeUsuario.Admin)]
    [InlineData(typeof(DetalleNutricionista), RolesDeUsuario.Admin)]
    [InlineData(typeof(Salas), RolesDeUsuario.Admin)]
    [InlineData(typeof(Pacientes), RolesDeUsuario.Personal)]
    [InlineData(typeof(DetallePaciente), RolesDeUsuario.Personal)]
    [InlineData(typeof(MiPerfilPaciente), RolesDeUsuario.Paciente)]
    public void Cada_pagina_exige_los_roles_de_la_api(Type pagina, string roles)
    {
        var atributo = pagina.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(atributo);
        Assert.Equal(roles, atributo.Roles);
    }

    [Fact]
    public void Un_admin_ve_los_alimentos_pero_no_los_botones_de_escritura()
    {
        Entrar(RolesDeUsuario.Admin);
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());

        var cut = Render<CatalogoAlimentos>();

        cut.WaitForAssertion(() => Assert.Contains("Avena", cut.Markup));
        Assert.DoesNotContain("Nuevo alimento", cut.Markup);
        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent is "Editar" or "Eliminar");
    }

    [Fact]
    public void Un_admin_ve_el_detalle_del_plato_y_solo_puede_volver()
    {
        Entrar(RolesDeUsuario.Admin);
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());

        var cut = Render<DetallePlato>(ps => ps.Add(p => p.Id, CatalogoApiFalsa.PlatoId));

        cut.WaitForAssertion(() => Assert.Contains("Avena con leche", cut.Markup));
        Assert.DoesNotContain("Editar clasificación", cut.Markup);
        Assert.DoesNotContain("Agregar ingrediente", cut.Markup);
        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent == "Quitar");
        Assert.Contains("Volver", cut.Markup);
    }

    [Fact]
    public void Un_admin_ve_los_platos_sin_editar_ni_eliminar_pero_con_el_detalle()
    {
        Entrar(RolesDeUsuario.Admin);
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());

        var cut = Render<CatalogoPlatos>();

        cut.WaitForAssertion(() => Assert.Contains("Avena con leche", cut.Markup));
        Assert.DoesNotContain("Nuevo plato", cut.Markup);
        Assert.Contains(cut.FindAll("button"), b => b.TextContent == "Detalle");
        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent is "Editar" or "Eliminar");
    }

    [Fact]
    public void Un_nutricionista_ve_la_dieta_pero_no_puede_cambiar_grupos_ni_restricciones()
    {
        Entrar(RolesDeUsuario.Nutricionista);
        Services.AddSingleton<ICatalogoApi>(new CatalogoApiFalsa());

        var cut = Render<DetalleDieta>(ps => ps.Add(p => p.Id, CatalogoApiFalsa.DietaId));

        cut.WaitForAssertion(() => Assert.Contains("Sin gluten", cut.Markup));
        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent is "Agregar" or "Quitar");
        Assert.Empty(cut.FindAll("select#grupo"));
    }

    private void Entrar(string rol)
        => AddAuthorization().SetAuthorized($"{rol.ToLowerInvariant()}@centro.test").SetRoles(rol);
}
