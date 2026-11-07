using FluentValidation;
using Menus.Application.Catalogo.Nomencladores.Especialidades;
using Menus.Application.Catalogo.Nomencladores.GruposNutricionales;
using Menus.Application.Catalogo.Nomencladores.ProgramasAtencion;
using Menus.Application.Catalogo.Nomencladores.RestriccionesAlimentarias;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Application.Tests.Catalogo.Nomencladores;

/// <summary>Pruebas de la forma de la entrada del #64 (ADR-010, §3: 422 con los errores por campo).</summary>
public sealed class ValidadoresDeNomencladoresTests
{
    private static string[] Campos<T>(AbstractValidator<T> validador, T solicitud)
        => [.. validador.Validate(solicitud).Errors.Select(e => e.PropertyName).Distinct()];

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void RegistrarGrupo_sin_nombre_falla_en_Nombre(string? nombre)
        => Assert.Equal(["Nombre"], Campos(new RegistrarGrupoNutricionalValidator(), new RegistrarGrupoNutricionalCommand(nombre!)));

    [Fact]
    public void RegistrarGrupo_con_un_nombre_demasiado_largo_falla_en_Nombre()
    {
        var nombre = new string('a', RegistrarGrupoNutricionalValidator.LargoMaximoDelNombre + 1);
        Assert.Equal(["Nombre"], Campos(new RegistrarGrupoNutricionalValidator(), new RegistrarGrupoNutricionalCommand(nombre)));
    }

    [Fact]
    public void RegistrarGrupo_valido_no_tiene_errores()
        => Assert.Empty(Campos(new RegistrarGrupoNutricionalValidator(), new RegistrarGrupoNutricionalCommand("Cereales")));

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void RegistrarRestriccion_sin_nombre_falla_en_Nombre(string? nombre)
        => Assert.Equal(["Nombre"], Campos(new RegistrarRestriccionAlimentariaValidator(), new RegistrarRestriccionAlimentariaCommand(nombre!)));

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void RegistrarPrograma_sin_nombre_falla_en_Nombre(string? nombre)
        => Assert.Equal(["Nombre"], Campos(new RegistrarProgramaAtencionValidator(), new RegistrarProgramaAtencionCommand(nombre!)));

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void RegistrarEspecialidad_sin_nombre_falla_en_Nombre(string? nombre)
        => Assert.Equal(["Nombre"], Campos(new RegistrarEspecialidadValidator(), new RegistrarEspecialidadCommand(nombre!)));

    [Fact]
    public void AddApplication_registra_los_validadores_del_64()
    {
        using var proveedor = new ServiceCollection().AddApplication().BuildServiceProvider();
        using var ambito = proveedor.CreateScope();

        Assert.IsType<RegistrarGrupoNutricionalValidator>(
            ambito.ServiceProvider.GetRequiredService<IValidator<RegistrarGrupoNutricionalCommand>>());
        Assert.IsType<RegistrarRestriccionAlimentariaValidator>(
            ambito.ServiceProvider.GetRequiredService<IValidator<RegistrarRestriccionAlimentariaCommand>>());
        Assert.IsType<RegistrarProgramaAtencionValidator>(
            ambito.ServiceProvider.GetRequiredService<IValidator<RegistrarProgramaAtencionCommand>>());
        Assert.IsType<RegistrarEspecialidadValidator>(
            ambito.ServiceProvider.GetRequiredService<IValidator<RegistrarEspecialidadCommand>>());
    }
}
