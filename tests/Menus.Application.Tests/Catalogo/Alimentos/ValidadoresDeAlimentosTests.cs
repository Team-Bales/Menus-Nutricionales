using FluentValidation;
using Menus.Application.Catalogo.Alimentos.ActualizarAlimento;
using Menus.Application.Catalogo.Alimentos.ListarAlimentos;
using Menus.Application.Catalogo.Alimentos.RegistrarAlimento;
using Menus.Domain.Nomenclators;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Application.Tests.Catalogo.Alimentos;

/// <summary>Pruebas de la forma de la entrada del #61 (ADR-010, §3: 422 con los errores por campo).</summary>
public sealed class ValidadoresDeAlimentosTests
{
    private static readonly Guid Grupo = BancoFalso.GrupoCereales;
    private static readonly Guid Tipo = TipoPreparacion.Entrante.Id;
    private static readonly Guid Nivel = NivelCalorico.Bajo.Id;

    private static string[] Campos<T>(AbstractValidator<T> validador, T solicitud)
        => [.. validador.Validate(solicitud).Errors.Select(e => e.PropertyName).Distinct()];

    [Fact]
    public void Registrar_valido_no_tiene_errores()
        => Assert.Empty(Campos(new RegistrarAlimentoValidator(), new RegistrarAlimentoCommand("Avena", Grupo, Tipo, Nivel)));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Registrar_sin_nombre_falla_en_Nombre(string? nombre)
        => Assert.Equal(["Nombre"], Campos(new RegistrarAlimentoValidator(), new RegistrarAlimentoCommand(nombre!, Grupo, Tipo, Nivel)));

    [Fact]
    public void Registrar_con_un_nombre_demasiado_largo_falla_en_Nombre()
    {
        var nombre = new string('a', RegistrarAlimentoValidator.LargoMaximoDelNombre + 1);

        Assert.Equal(["Nombre"], Campos(new RegistrarAlimentoValidator(), new RegistrarAlimentoCommand(nombre, Grupo, Tipo, Nivel)));
    }

    [Fact]
    public void Registrar_sin_grupo_y_con_tipo_y_nivel_fuera_del_catalogo_falla_en_los_tres()
        => Assert.Equal(
            ["GrupoNutricionalId", "TipoPreparacionId", "NivelCaloricoId"],
            Campos(new RegistrarAlimentoValidator(), new RegistrarAlimentoCommand("Avena", Guid.Empty, Guid.NewGuid(), Guid.NewGuid())));

    [Fact]
    public void Actualizar_valida_la_clasificacion()
    {
        Assert.Empty(Campos(new ActualizarAlimentoValidator(), new ActualizarAlimentoCommand(Guid.NewGuid(), Grupo, Tipo, Nivel)));
        Assert.Equal(
            ["GrupoNutricionalId", "TipoPreparacionId", "NivelCaloricoId"],
            Campos(new ActualizarAlimentoValidator(), new ActualizarAlimentoCommand(Guid.NewGuid(), Guid.Empty, Guid.Empty, Guid.Empty)));
    }

    [Fact]
    public void Listar_sin_filtros_es_valido()
        => Assert.Empty(Campos(new ListarAlimentosValidator(), new ListarAlimentosQuery()));

    [Fact]
    public void Listar_con_filtros_del_catalogo_es_valido()
        => Assert.Empty(Campos(new ListarAlimentosValidator(), new ListarAlimentosQuery(Grupo, Tipo, Nivel, 3, 100)));

    [Fact]
    public void Listar_con_paginacion_o_filtros_invalidos_falla_en_cada_campo()
        => Assert.Equal(
            ["Page", "Size", "TipoPreparacionId", "NivelCaloricoId"],
            Campos(new ListarAlimentosValidator(), new ListarAlimentosQuery(null, Guid.NewGuid(), Guid.NewGuid(), 0, 101)));

    [Fact]
    public void AddApplication_registra_los_validadores_del_61()
    {
        using var proveedor = new ServiceCollection().AddApplication().BuildServiceProvider();
        using var ambito = proveedor.CreateScope();

        Assert.IsType<RegistrarAlimentoValidator>(ambito.ServiceProvider.GetRequiredService<IValidator<RegistrarAlimentoCommand>>());
        Assert.IsType<ActualizarAlimentoValidator>(ambito.ServiceProvider.GetRequiredService<IValidator<ActualizarAlimentoCommand>>());
        Assert.IsType<ListarAlimentosValidator>(ambito.ServiceProvider.GetRequiredService<IValidator<ListarAlimentosQuery>>());
    }
}
