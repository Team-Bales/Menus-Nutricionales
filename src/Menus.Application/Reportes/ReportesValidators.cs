using FluentValidation;
using Menus.Domain.Abstractions.Repositories;

namespace Menus.Application.Reportes;

/// <summary>
/// Conjuntos de columnas permitidas para el ordenamiento de cada reporte (C1 a C6).
/// La validación de columnas y direcciones de ordenamiento se hace como texto (no enum)
/// comparando contra estas listas blancas.
/// </summary>
internal static class ColumnasPermitidas
{
    // C1
    public static readonly HashSet<string> C1 = new(StringComparer.OrdinalIgnoreCase)
    {
        "menuId", "creador", "fechaCreacion", "cantidadTotalAlimentos", "distribucion", "gruposCubiertos"
    };

    // C2
    public static readonly HashSet<string> C2 = new(StringComparer.OrdinalIgnoreCase)
    {
        "alimentoId", "alimento", "nivelCalorico", "grupoNutricional", "menusFinales"
    };

    // C3
    public static readonly HashSet<string> C3 = new(StringComparer.OrdinalIgnoreCase)
    {
        "menuId", "dieta", "creador", "fechaCreacion", "fechaValidacion", "aprobado", "observaciones", "menuReemplazoId"
    };

    // C4
    public static readonly HashSet<string> C4 = new(StringComparer.OrdinalIgnoreCase)
    {
        "itemId", "item", "clase", "nivelCalorico", "serviciosConConsumo", "aceptaciones", "rechazos", "tasaAceptacion"
    };

    // C5-Grupos
    public static readonly HashSet<string> C5Grupos = new(StringComparer.OrdinalIgnoreCase)
    {
        "dietaId", "dieta", "grupoNutricional", "items", "porcentaje"
    };

    // C5-Niveles
    public static readonly HashSet<string> C5Niveles = new(StringComparer.OrdinalIgnoreCase)
    {
        "dietaId", "dieta", "nivelCalorico", "items", "porcentaje"
    };

    // C5-Equilibrio
    public static readonly HashSet<string> C5Equilibrio = new(StringComparer.OrdinalIgnoreCase)
    {
        "dietaId", "dieta", "menuId", "estado", "items", "cantidadDeclarada", "cantidadCumple",
        "proporcionesCumplen", "tiposFueraDeProporcion", "requeridosCubiertos", "requeridosFaltantes",
        "prohibidosAusentes", "prohibidosPresentes"
    };

    // C6-Correlación
    public static readonly HashSet<string> C6Correlacion = new(StringComparer.OrdinalIgnoreCase)
    {
        "dietaId", "dieta", "menusValorados", "coeficienteSpearman"
    };

    // C6-Rechazos
    public static readonly HashSet<string> C6Rechazos = new(StringComparer.OrdinalIgnoreCase)
    {
        "itemId", "item", "clase", "descriptor", "consumos", "rechazos", "tasaRechazo", "dietas"
    };

    // C6-Revalorizaciones
    public static readonly HashSet<string> C6Revalorizaciones = new(StringComparer.OrdinalIgnoreCase)
    {
        "revalorizacionId", "pacienteId", "paciente", "sala", "dieta", "valorRevalorizacion", "promedioSala", "diferencia"
    };
}

/// <summary>
/// Validador común para <see cref="OrdenDeReporte"/> contra una lista blanca de columnas.
/// </summary>
internal static class OrdenValidator
{
    /// <summary>
    /// Añade reglas de validación para un <see cref="OrdenDeReporte"/> opcional.
    /// </summary>
    /// <typeparam name="T">Tipo de la consulta que contiene el orden.</typeparam>
    /// <param name="ruleBuilder">Builder de la regla para la propiedad Orden.</param>
    /// <param name="columnasPermitidas">HashSet con los nombres de columnas permitidos (camelCase JSON).</param>
    /// <param name="prefijoCampo">Prefijo para el nombre del campo en los mensajes de error.</param>
    public static void ValidarOrden<T>(
        this IRuleBuilder<T, OrdenDeReporte?> ruleBuilder,
        HashSet<string> columnasPermitidas,
        string prefijoCampo = "Orden")
    {
        ruleBuilder.Custom((orden, context) =>
        {
            if (orden is null)
            {
                return;
            }

            // Validar dirección
            if (!orden.DireccionValida)
            {
                context.AddFailure($"{prefijoCampo}.Direccion", $"La dirección debe ser 'asc' o 'desc' (recibido: '{orden.Direccion}').");
                return;
            }

            // Validar columna
            if (!columnasPermitidas.Contains(orden.Columna!))
            {
                context.AddFailure($"{prefijoCampo}.Columna", $"La columna '{orden.Columna}' no es válida. Columnas permitidas: {string.Join(", ", columnasPermitidas.OrderBy(c => c))}.");
            }
        });
    }
}

#region C1: ListarMenusAutomaticosValidator

/// <summary>Validador para <see cref="ListarMenusAutomaticosQuery"/>.</summary>
internal sealed class ListarMenusAutomaticosValidator : AbstractValidator<ListarMenusAutomaticosQuery>
{
    public ListarMenusAutomaticosValidator()
    {
        RuleFor(q => q.DietaId).NotEmpty().WithMessage("El identificador de la dieta es obligatorio.");
        RuleFor(q => q.Orden).ValidarOrden<ListarMenusAutomaticosQuery>(ColumnasPermitidas.C1, "Orden");
    }
}

#endregion

#region C2: ListarAlimentosMasUsadosValidator

/// <summary>Validador para <see cref="ListarAlimentosMasUsadosQuery"/>.</summary>
internal sealed class ListarAlimentosMasUsadosValidator : AbstractValidator<ListarAlimentosMasUsadosQuery>
{
    public ListarAlimentosMasUsadosValidator()
    {
        RuleFor(q => q.DietaId).NotEmpty().WithMessage("El identificador de la dieta es obligatorio.");
        RuleFor(q => q.Top).InclusiveBetween(1, 20).WithMessage("El parámetro 'top' debe estar entre 1 y 20.");
        RuleFor(q => q.Orden).ValidarOrden<ListarAlimentosMasUsadosQuery>(ColumnasPermitidas.C2, "Orden");
    }
}

#endregion

#region C3: ListarDictamenesValidator

/// <summary>Validador para <see cref="ListarDictamenesQuery"/>.</summary>
internal sealed class ListarDictamenesValidator : AbstractValidator<ListarDictamenesQuery>
{
    public ListarDictamenesValidator()
    {
        RuleFor(q => q.RevisorId).NotEmpty().WithMessage("El identificador del revisor es obligatorio.");
        RuleFor(q => q.Orden).ValidarOrden<ListarDictamenesQuery>(ColumnasPermitidas.C3, "Orden");
    }
}

#endregion

#region C4: ListarDesempenoMenuValidator

/// <summary>Validador para <see cref="ListarDesempenoMenuQuery"/>.</summary>
internal sealed class ListarDesempenoMenuValidator : AbstractValidator<ListarDesempenoMenuQuery>
{
    public ListarDesempenoMenuValidator()
    {
        RuleFor(q => q.MenuId).NotEmpty().WithMessage("El identificador del menú es obligatorio.");
        RuleFor(q => q.Orden).ValidarOrden<ListarDesempenoMenuQuery>(ColumnasPermitidas.C4, "Orden");
    }
}

#endregion

#region C5: CompararDietasValidator

/// <summary>Validador para <see cref="CompararDietasQuery"/>.</summary>
internal sealed class CompararDietasValidator : AbstractValidator<CompararDietasQuery>
{
    public CompararDietasValidator()
    {
        RuleFor(q => q.Dietas)
            .NotNull().WithMessage("La lista de dietas es obligatoria.")
            .Must(d => d.Count >= 2).WithMessage("Se requieren al menos dos dietas distintas para comparar.")
            .Must(d => d.Distinct().Count() == d.Count).WithMessage("Las dietas no pueden repetirse.");

        RuleFor(q => q.OrdenGrupos).ValidarOrden<CompararDietasQuery>(ColumnasPermitidas.C5Grupos, "OrdenGrupos");
        RuleFor(q => q.OrdenNiveles).ValidarOrden<CompararDietasQuery>(ColumnasPermitidas.C5Niveles, "OrdenNiveles");
        RuleFor(q => q.OrdenEquilibrio).ValidarOrden<CompararDietasQuery>(ColumnasPermitidas.C5Equilibrio, "OrdenEquilibrio");
    }
}

#endregion

#region C6: ObtenerCorrelacionNutricionalValidator

/// <summary>Validador para <see cref="ObtenerCorrelacionNutricionalQuery"/>.</summary>
internal sealed class ObtenerCorrelacionNutricionalValidator : AbstractValidator<ObtenerCorrelacionNutricionalQuery>
{
    public ObtenerCorrelacionNutricionalValidator()
    {
        RuleFor(q => q)
            .Must(q => q.Desde is null || q.Hasta is null || q.Desde < q.Hasta)
            .WithMessage("'desde' debe ser anterior a 'hasta'.")
            .OverridePropertyName("Desde");

        RuleFor(q => q.MinimoConsumos).GreaterThanOrEqualTo(1).WithMessage("'minimoConsumos' debe ser mayor o igual a 1.");

        RuleFor(q => q.OrdenCorrelacion).ValidarOrden<ObtenerCorrelacionNutricionalQuery>(ColumnasPermitidas.C6Correlacion, "OrdenCorrelacion");
        RuleFor(q => q.OrdenRechazos).ValidarOrden<ObtenerCorrelacionNutricionalQuery>(ColumnasPermitidas.C6Rechazos, "OrdenRechazos");
        RuleFor(q => q.OrdenRevalorizaciones).ValidarOrden<ObtenerCorrelacionNutricionalQuery>(ColumnasPermitidas.C6Revalorizaciones, "OrdenRevalorizaciones");
    }
}

#endregion
