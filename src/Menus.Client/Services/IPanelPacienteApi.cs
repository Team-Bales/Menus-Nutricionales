using Menus.Client.GestionMenus;
using Menus.Client.PanelPaciente;

namespace Menus.Client.Services;

/// <summary>Contrato HTTP del panel clínico que consulta el propio paciente (#88).</summary>
public interface IPanelPacienteApi
{
    /// <summary>Menús asignados al paciente, con su detalle.</summary>
    Task<IReadOnlyList<MenuDetalle>> ListarMenusAsync(Guid pacienteId, CancellationToken ct = default);

    /// <summary>Servicios y aceptación de ítems del paciente.</summary>
    Task<IReadOnlyList<ServicioDePaciente>> ListarConsumosAsync(Guid pacienteId, CancellationToken ct = default);

    /// <summary>Historial de valoraciones nutricionales del paciente.</summary>
    Task<IReadOnlyList<ValoracionDePaciente>> ListarValoracionesAsync(Guid pacienteId, CancellationToken ct = default);
}
