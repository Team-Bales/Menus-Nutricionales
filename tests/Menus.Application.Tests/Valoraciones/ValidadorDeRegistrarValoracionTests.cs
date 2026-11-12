using FluentValidation;
using Menus.Application.Valoraciones.RegistrarValoracion;
using Menus.Domain.Nomenclators;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Application.Tests.Valoraciones;

/// <summary>Pruebas de la forma de la entrada del #86 (ADR-010, §3: 422 con los errores por campo).</summary>
public sealed class ValidadorDeRegistrarValoracionTests
{
    private static string[] Campos(RegistrarValoracionCommand solicitud)
        => [.. new RegistrarValoracionValidator().Validate(solicitud).Errors.Select(e => e.PropertyName).Distinct()];

    [Fact]
    public void Valido_no_tiene_errores()
        => Assert.Empty(Campos(new RegistrarValoracionCommand(Guid.NewGuid(), Guid.NewGuid(), ResultadoNutricional.Buena.Id)));

    [Fact]
    public void Sin_menu_falla_en_MenuId()
        => Assert.Equal(
            ["MenuId"],
            Campos(new RegistrarValoracionCommand(Guid.Empty, Guid.NewGuid(), ResultadoNutricional.Buena.Id)));

    [Fact]
    public void Sin_paciente_falla_en_PacienteId()
        => Assert.Equal(
            ["PacienteId"],
            Campos(new RegistrarValoracionCommand(Guid.NewGuid(), Guid.Empty, ResultadoNutricional.Buena.Id)));

    [Fact]
    public void Con_un_resultado_que_no_es_del_catalogo_falla_en_ResultadoNutricionalId()
        => Assert.Equal(
            ["ResultadoNutricionalId"],
            Campos(new RegistrarValoracionCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid())));

    [Fact]
    public void El_validador_se_registra_con_la_capa_de_aplicacion()
    {
        using var proveedor = new ServiceCollection().AddApplication().BuildServiceProvider();
        using var alcance = proveedor.CreateScope();

        Assert.IsType<RegistrarValoracionValidator>(
            Assert.Single(alcance.ServiceProvider.GetServices<IValidator<RegistrarValoracionCommand>>()));
    }
}
