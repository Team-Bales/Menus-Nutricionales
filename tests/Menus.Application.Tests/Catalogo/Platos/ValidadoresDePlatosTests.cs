using FluentValidation;
using Menus.Application.Catalogo.Platos.ActualizarPlato;
using Menus.Application.Catalogo.Platos.AgregarIngrediente;
using Menus.Application.Catalogo.Platos.ListarPlatos;
using Menus.Application.Catalogo.Platos.RegistrarPlato;
using Menus.Domain.Nomenclators;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Application.Tests.Catalogo.Platos;

/// <summary>Pruebas de la forma de la entrada del #62 (ADR-010, §3: 422 con los errores por campo).</summary>
public sealed class ValidadoresDePlatosTests
{
    private static readonly Guid Grupo = BancoFalso.GrupoCereales;
    private static readonly Guid Tipo = TipoPreparacion.PlatoFuerte.Id;
    private static readonly Guid Nivel = NivelCalorico.Medio.Id;

    private static string[] Campos<T>(AbstractValidator<T> validador, T solicitud)
        => [.. validador.Validate(solicitud).Errors.Select(e => e.PropertyName).Distinct()];

    private static RegistrarPlatoCommand Registrar(params IngredienteDelPlato[] ingredientes)
        => new("Ensalada", Grupo, Tipo, Nivel, ingredientes);

    [Fact]
    public void Registrar_valido_no_tiene_errores()
        => Assert.Empty(Campos(new RegistrarPlatoValidator(), Registrar(new IngredienteDelPlato(Guid.NewGuid(), 10m))));

    [Fact]
    public void Registrar_sin_ingredientes_falla_por_RK03()
        => Assert.Equal(["Ingredientes"], Campos(new RegistrarPlatoValidator(), Registrar()));

    [Fact]
    public void Registrar_con_los_ingredientes_nulos_falla_en_Ingredientes()
        => Assert.Equal(["Ingredientes"], Campos(new RegistrarPlatoValidator(), new RegistrarPlatoCommand("Ensalada", Grupo, Tipo, Nivel, null!)));

    [Fact]
    public void Registrar_con_un_alimento_repetido_falla_en_Ingredientes()
    {
        var id = Guid.NewGuid();

        Assert.Equal(["Ingredientes"], Campos(new RegistrarPlatoValidator(), Registrar(new IngredienteDelPlato(id, 1m), new(id, 2m))));
    }

    [Fact]
    public void Registrar_con_cantidad_no_positiva_y_sin_alimento_falla_en_cada_ingrediente()
        => Assert.Equal(
            ["Ingredientes[0].Cantidad", "Ingredientes[1].AlimentoId"],
            Campos(new RegistrarPlatoValidator(), Registrar(new IngredienteDelPlato(Guid.NewGuid(), 0m), new(Guid.Empty, 5m))));

    [Fact]
    public void Registrar_sin_nombre_ni_clasificacion_falla_en_cada_campo()
        => Assert.Equal(
            ["Nombre", "GrupoNutricionalId", "TipoPreparacionId", "NivelCaloricoId"],
            Campos(new RegistrarPlatoValidator(), new RegistrarPlatoCommand(" ", Guid.Empty, Guid.Empty, Guid.Empty, [new(Guid.NewGuid(), 1m)])));

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Agregar_con_cantidad_no_positiva_falla_en_Cantidad(int cantidad)
        => Assert.Equal(["Cantidad"], Campos(new AgregarIngredienteValidator(), new AgregarIngredienteCommand(Guid.NewGuid(), Guid.NewGuid(), cantidad)));

    [Fact]
    public void Agregar_sin_alimento_falla_en_AlimentoId()
        => Assert.Equal(["AlimentoId"], Campos(new AgregarIngredienteValidator(), new AgregarIngredienteCommand(Guid.NewGuid(), Guid.Empty, 1m)));

    [Fact]
    public void Actualizar_valida_la_clasificacion()
    {
        Assert.Empty(Campos(new ActualizarPlatoValidator(), new ActualizarPlatoCommand(Guid.NewGuid(), Grupo, Tipo, Nivel)));
        Assert.Equal(
            ["GrupoNutricionalId", "TipoPreparacionId", "NivelCaloricoId"],
            Campos(new ActualizarPlatoValidator(), new ActualizarPlatoCommand(Guid.NewGuid(), Guid.Empty, Guid.Empty, Guid.Empty)));
    }

    [Fact]
    public void Listar_valida_paginacion_y_filtros()
    {
        Assert.Empty(Campos(new ListarPlatosValidator(), new ListarPlatosQuery(Grupo, Tipo, Nivel)));
        Assert.Equal(
            ["Page", "Size", "TipoPreparacionId", "NivelCaloricoId"],
            Campos(new ListarPlatosValidator(), new ListarPlatosQuery(null, Guid.NewGuid(), Guid.NewGuid(), 0, 101)));
    }

    [Fact]
    public void AddApplication_registra_los_validadores_del_62()
    {
        using var proveedor = new ServiceCollection().AddApplication().BuildServiceProvider();
        using var ambito = proveedor.CreateScope();

        Assert.IsType<RegistrarPlatoValidator>(ambito.ServiceProvider.GetRequiredService<IValidator<RegistrarPlatoCommand>>());
        Assert.IsType<AgregarIngredienteValidator>(ambito.ServiceProvider.GetRequiredService<IValidator<AgregarIngredienteCommand>>());
        Assert.IsType<ActualizarPlatoValidator>(ambito.ServiceProvider.GetRequiredService<IValidator<ActualizarPlatoCommand>>());
        Assert.IsType<ListarPlatosValidator>(ambito.ServiceProvider.GetRequiredService<IValidator<ListarPlatosQuery>>());
    }
}
