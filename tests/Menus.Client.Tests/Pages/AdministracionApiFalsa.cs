using System.Net;
using Menus.Client.Administracion;
using Menus.Client.Catalogo;
using Menus.Client.Services;

namespace Menus.Client.Tests.Pages;

/// <summary>Falso del adaptador administrativo para las páginas del issue #76.</summary>
internal sealed class AdministracionApiFalsa : IAdministracionApi
{
    public static readonly Guid NutricionistaId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid SalaId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public List<Nutricionista> Nutricionistas { get; set; } = [new(NutricionistaId, "Ana Pérez", CatalogoApiFalsa.Especialidad, false)];
    public NutricionistaDetalle Detalle { get; set; } = new(NutricionistaId, "Ana Pérez", CatalogoApiFalsa.Especialidad, false, [new(CatalogoApiFalsa.DietaId)]);
    public List<Sala> Salas { get; set; } = [new(SalaId, "Sala 101")];
    public Dictionary<Guid, int> PacientesPorSala { get; } = new() { [SalaId] = 2 };
    public ApiException? Error { get; set; }
    public ApiException? ErrorAlGuardarSala { get; set; }
    public ApiException? ErrorAlEliminarSala { get; set; }
    public (Guid? Especialidad, int Page, int Size)? UltimaConsultaNutricionistas { get; private set; }
    public Guid? NutricionistaConsultado { get; private set; }
    public (Guid NutricionistaId, Guid DietaId)? UltimaAutorizacion { get; private set; }
    public (Guid NutricionistaId, Guid DietaId)? UltimaRevocacion { get; private set; }
    public Guid? UltimoJefe { get; private set; }
    public string? SalaCreada { get; private set; }
    public (Guid Id, string Nombre)? SalaActualizada { get; private set; }
    public Guid? SalaEliminada { get; private set; }

    public Task<Pagina<Nutricionista>> ListarNutricionistasAsync(Guid? especialidadId, int page, int size, CancellationToken ct = default)
    {
        UltimaConsultaNutricionistas = (especialidadId, page, size);
        return Resultado(new Pagina<Nutricionista>([.. Nutricionistas], Nutricionistas.Count, page, size));
    }

    public Task<NutricionistaDetalle> ObtenerNutricionistaAsync(Guid id, CancellationToken ct = default)
    {
        NutricionistaConsultado = id;
        return Resultado(Detalle);
    }

    public Task AutorizarDietaAsync(Guid nutricionistaId, Guid dietaId, CancellationToken ct = default) { UltimaAutorizacion = (nutricionistaId, dietaId); return Resultado(); }
    public Task RevocarDietaAsync(Guid nutricionistaId, Guid dietaId, CancellationToken ct = default) { UltimaRevocacion = (nutricionistaId, dietaId); return Resultado(); }
    public Task AsignarRolJefeAsync(Guid nutricionistaId, CancellationToken ct = default) { UltimoJefe = nutricionistaId; return Resultado(); }
    public Task<IReadOnlyList<Sala>> ListarSalasAsync(CancellationToken ct = default) => Resultado<IReadOnlyList<Sala>>([.. Salas]);
    public Task<int> ContarPacientesEnSalaAsync(Guid salaId, CancellationToken ct = default) => Resultado(PacientesPorSala.GetValueOrDefault(salaId));
    public Task<Guid> CrearSalaAsync(SalaFormulario formulario, CancellationToken ct = default) { SalaCreada = formulario.Nombre; return Resultado(ErrorAlGuardarSala, Guid.NewGuid()); }
    public Task ActualizarSalaAsync(Guid salaId, SalaFormulario formulario, CancellationToken ct = default) { SalaActualizada = (salaId, formulario.Nombre); return Resultado(ErrorAlGuardarSala); }
    public Task EliminarSalaAsync(Guid salaId, CancellationToken ct = default) { SalaEliminada = salaId; return Resultado(ErrorAlEliminarSala); }

    private async Task<T> Resultado<T>(T valor) { await Task.Yield(); if (Error is not null) { throw Error; } return valor; }
    private async Task Resultado() { await Task.Yield(); if (Error is not null) { throw Error; } }
    private static async Task<T> Resultado<T>(ApiException? error, T valor) { await Task.Yield(); if (error is not null) { throw error; } return valor; }
    private static async Task Resultado(ApiException? error) { await Task.Yield(); if (error is not null) { throw error; } }
}
