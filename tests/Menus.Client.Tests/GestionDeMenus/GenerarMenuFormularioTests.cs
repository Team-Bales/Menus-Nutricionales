using System.ComponentModel.DataAnnotations;
using Menus.Client.GestionDeMenus;

namespace Menus.Client.Tests.GestionDeMenus;

/// <summary>Reglas del formulario de generación (#81): las mismas que exige la API de menús (#80).</summary>
public sealed class GenerarMenuFormularioTests
{
    private static readonly Guid Dieta = Guid.NewGuid();
    private static readonly Guid TipoUno = Guid.NewGuid();
    private static readonly Guid TipoDos = Guid.NewGuid();
    private static readonly Guid TipoTres = Guid.NewGuid();
    private static readonly Guid TipoCuatro = Guid.NewGuid();
    private static readonly Guid Grupo = Guid.NewGuid();

    [Fact]
    public void Un_formulario_valido_no_tiene_errores()
    {
        Assert.Empty(Validar(Valido()));
    }

    [Fact]
    public void Exige_una_dieta()
    {
        var formulario = Valido();
        formulario.DietaId = Guid.Empty;

        Assert.Contains("Selecciona una dieta.", Validar(formulario));
    }

    [Fact]
    public void Exige_una_cantidad_positiva()
    {
        var formulario = Valido();
        formulario.CantidadTotalAlimentos = 0;

        Assert.Contains("La cantidad total debe ser mayor que cero.", Validar(formulario));
    }

    [Fact]
    public void Exige_al_menos_una_proporcion()
    {
        var formulario = Valido();
        formulario.Distribucion = [];

        Assert.Contains("Indica la proporción de al menos un tipo de preparación.", Validar(formulario));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Exige_porcentajes_entre_cero_y_cien(int porcentaje)
    {
        var formulario = Valido();
        formulario.Distribucion = [new() { TipoPreparacionId = TipoUno, Porcentaje = porcentaje }];

        Assert.Contains("Cada porcentaje debe estar entre 0 y 100.", Validar(formulario));
    }

    [Fact]
    public void No_permite_repetir_tipos_de_preparacion()
    {
        var formulario = Valido();
        formulario.Distribucion =
        [
            new() { TipoPreparacionId = TipoUno, Porcentaje = 50 },
            new() { TipoPreparacionId = TipoUno, Porcentaje = 50 }
        ];

        Assert.Contains("No se puede repetir un tipo de preparación.", Validar(formulario));
    }

    [Fact]
    public void Exige_que_los_porcentajes_sumen_cien()
    {
        var formulario = Valido();
        formulario.Distribucion =
        [
            new() { TipoPreparacionId = TipoUno, Porcentaje = 30 },
            new() { TipoPreparacionId = TipoDos, Porcentaje = 60 }
        ];

        Assert.Contains("Los porcentajes deben sumar 100.", Validar(formulario));
    }

    [Fact]
    public void Acepta_un_subconjunto_de_tipos_con_alguno_en_cero()
    {
        var formulario = Valido();
        formulario.Distribucion =
        [
            new() { TipoPreparacionId = TipoUno, Porcentaje = 25 },
            new() { TipoPreparacionId = TipoDos, Porcentaje = 50 },
            new() { TipoPreparacionId = TipoTres, Porcentaje = 25 },
            new() { TipoPreparacionId = TipoCuatro, Porcentaje = 0 }
        ];

        Assert.Empty(Validar(formulario));
    }

    [Fact]
    public void Rechaza_que_todos_los_porcentajes_sean_cero()
    {
        var formulario = Valido();
        formulario.Distribucion = [new() { TipoPreparacionId = TipoUno, Porcentaje = 0 }];

        Assert.Contains("Indica el porcentaje de al menos un tipo de preparación.", Validar(formulario));
    }

    [Fact]
    public void Exige_al_menos_un_grupo_nutricional()
    {
        var formulario = Valido();
        formulario.GruposNutricionalesIds = [];

        Assert.Contains("Selecciona al menos un grupo nutricional.", Validar(formulario));
    }

    private static GenerarMenuFormulario Valido() => new()
    {
        DietaId = Dieta,
        CantidadTotalAlimentos = 12,
        Distribucion = [new() { TipoPreparacionId = TipoUno, Porcentaje = 100 }],
        GruposNutricionalesIds = [Grupo]
    };

    private static IReadOnlyList<string> Validar(GenerarMenuFormulario formulario)
    {
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(formulario, new ValidationContext(formulario), resultados, validateAllProperties: true);
        return [.. resultados.Select(r => r.ErrorMessage ?? string.Empty)];
    }
}
