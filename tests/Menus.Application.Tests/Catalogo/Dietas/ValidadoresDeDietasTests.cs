using FluentValidation;
using Menus.Application.Catalogo.Dietas.ActualizarDieta;
using Menus.Application.Catalogo.Dietas.AgregarGrupoDieta;
using Menus.Application.Catalogo.Dietas.AgregarRestriccionDieta;
using Menus.Application.Catalogo.Dietas.ListarDietas;
using Menus.Application.Catalogo.Dietas.RegistrarDieta;
using Menus.Domain.Nomenclators;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Application.Tests.Catalogo.Dietas;

/// <summary>Pruebas de la forma de la entrada del #63 (ADR-010, §3: 422 con los errores por campo).</summary>
public sealed class ValidadoresDeDietasTests
{
    private static readonly Guid Programa = BancoDeDietasFalso.ProgramaPediatria;
    private static readonly Guid Grupo = BancoDeDietasFalso.GrupoCereales;
    private static readonly Guid Restriccion = BancoDeDietasFalso.RestriccionSinGluten;

    private static string[] Campos<T>(AbstractValidator<T> validador, T solicitud)
        => [.. validador.Validate(solicitud).Errors.Select(e => e.PropertyName).Distinct()];

    [Fact]
    public void Registrar_valido_no_tiene_errores()
        => Assert.Empty(Campos(
            new RegistrarDietaValidator(),
            new RegistrarDietaCommand("Dieta renal", Programa, [new GrupoACubrir(Grupo, TipoCobertura.Requerido)], [Restriccion])));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Registrar_sin_nombre_falla_en_Nombre(string? nombre)
        => Assert.Equal(
            ["Nombre"],
            Campos(new RegistrarDietaValidator(), new RegistrarDietaCommand(nombre!, Programa, [new GrupoACubrir(Grupo, TipoCobertura.Requerido)], [])));

    [Fact]
    public void Registrar_con_un_nombre_demasiado_largo_falla_en_Nombre()
    {
        var nombre = new string('a', RegistrarDietaValidator.LargoMaximoDelNombre + 1);
        var comando = new RegistrarDietaCommand(nombre, Programa, [new GrupoACubrir(Grupo, TipoCobertura.Requerido)], []);

        Assert.Equal(["Nombre"], Campos(new RegistrarDietaValidator(), comando));
    }

    [Fact]
    public void Registrar_sin_programa_falla_en_ProgramaAtencionId()
        => Assert.Equal(
            ["ProgramaAtencionId"],
            Campos(new RegistrarDietaValidator(), new RegistrarDietaCommand("Dieta", Guid.Empty, [new GrupoACubrir(Grupo, TipoCobertura.Requerido)], [])));

    [Fact]
    public void Registrar_sin_grupos_falla_en_Grupos()
        => Assert.Equal(
            ["Grupos"],
            Campos(new RegistrarDietaValidator(), new RegistrarDietaCommand("Dieta", Programa, [], [])));

    [Fact]
    public void Registrar_sin_ningun_grupo_requerido_falla_en_Grupos()
        => Assert.Equal(
            ["Grupos"],
            Campos(new RegistrarDietaValidator(), new RegistrarDietaCommand("Dieta", Programa, [new GrupoACubrir(Grupo, TipoCobertura.Restringido)], [])));

    [Fact]
    public void Actualizar_valido_no_tiene_errores()
        => Assert.Empty(Campos(new ActualizarDietaValidator(), new ActualizarDietaCommand(Guid.NewGuid(), "Dieta", Programa)));

    [Fact]
    public void Actualizar_sin_nombre_ni_programa_falla_en_los_dos()
        => Assert.Equal(
            ["Nombre", "ProgramaAtencionId"],
            Campos(new ActualizarDietaValidator(), new ActualizarDietaCommand(Guid.NewGuid(), "", Guid.Empty)));

    [Fact]
    public void AgregarGrupo_valido_no_tiene_errores()
        => Assert.Empty(Campos(
            new AgregarGrupoDietaValidator(), new AgregarGrupoDietaCommand(Guid.NewGuid(), Grupo, TipoCobertura.Requerido)));

    [Fact]
    public void AgregarGrupo_sin_grupo_falla_en_GrupoNutricionalId()
        => Assert.Equal(
            ["GrupoNutricionalId"],
            Campos(new AgregarGrupoDietaValidator(), new AgregarGrupoDietaCommand(Guid.NewGuid(), Guid.Empty, TipoCobertura.Requerido)));

    [Fact]
    public void AgregarRestriccion_sin_restriccion_falla_en_RestriccionAlimentariaId()
        => Assert.Equal(
            ["RestriccionAlimentariaId"],
            Campos(new AgregarRestriccionDietaValidator(), new AgregarRestriccionDietaCommand(Guid.NewGuid(), Guid.Empty)));

    [Fact]
    public void Listar_sin_filtros_es_valido()
        => Assert.Empty(Campos(new ListarDietasValidator(), new ListarDietasQuery()));

    [Fact]
    public void Listar_con_paginacion_invalida_falla_en_Page_y_Size()
        => Assert.Equal(
            ["Page", "Size"],
            Campos(new ListarDietasValidator(), new ListarDietasQuery(null, 0, 101)));

    [Fact]
    public void AddApplication_registra_los_validadores_del_63()
    {
        using var proveedor = new ServiceCollection().AddApplication().BuildServiceProvider();
        using var ambito = proveedor.CreateScope();

        Assert.IsType<RegistrarDietaValidator>(ambito.ServiceProvider.GetRequiredService<IValidator<RegistrarDietaCommand>>());
        Assert.IsType<ActualizarDietaValidator>(ambito.ServiceProvider.GetRequiredService<IValidator<ActualizarDietaCommand>>());
        Assert.IsType<AgregarGrupoDietaValidator>(ambito.ServiceProvider.GetRequiredService<IValidator<AgregarGrupoDietaCommand>>());
        Assert.IsType<AgregarRestriccionDietaValidator>(ambito.ServiceProvider.GetRequiredService<IValidator<AgregarRestriccionDietaCommand>>());
        Assert.IsType<ListarDietasValidator>(ambito.ServiceProvider.GetRequiredService<IValidator<ListarDietasQuery>>());
    }
}
