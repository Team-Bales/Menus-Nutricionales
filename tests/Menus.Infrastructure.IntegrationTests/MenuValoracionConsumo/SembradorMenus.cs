using System;
using System.Threading.Tasks;
using Npgsql;

namespace Menus.Infrastructure.IntegrationTests.Datos;

/// <summary>
/// Extensión del sembrador para las pruebas del submodelo Menú-Valoración-Consumo (issue #202).
/// </summary>
/// <remarks>
/// Añade solo lo que faltaba: los dos agregados «menú con dictamen». El resto de los helpers de este
/// submodelo ya lo dan <c>Sembrador.cs</c> y <c>Sembrador.Consultas.cs</c>: <c>ValidarAsync</c>,
/// <c>MenuDeReemplazoAsync</c>, <c>AsignarAsync</c>, <c>ServirAsync</c>, <c>ValorarAsync</c>,
/// <c>RevalorizarAsync</c>, <c>HacerJefeAsync</c> y <c>MenuConParametrosAsync</c>.
/// </remarks>
public sealed partial class Sembrador
{
    /// <summary>
    /// Crea un menú válido y le registra una aprobación: queda final y, asignado, apto para servicios
    /// (RS-16) y valoraciones. El revisor debe ser distinto del creador (RS-05); el helper lo hace jefe
    /// y lo autoriza para la dieta, en el orden que exige el disparo de los triggers
    /// (<c>trg_rs_03</c> exige <c>es_jefe</c> y <c>fn_validacion</c> exige autorización).
    /// </summary>
    /// <param name="dietaId">Dieta del menú; el creador debe estar autorizado en ella.</param>
    /// <param name="creadorId">Nutricionista creador, autorizado en la dieta.</param>
    /// <param name="revisorId">Nutricionista revisor, distinto del creador; se hace jefe y se le autoriza.</param>
    /// <param name="fechaValidacion">Fecha del dictamen; por defecto, ahora.</param>
    /// <returns>ID del menú aprobado.</returns>
    public async Task<Guid> MenuAprobadoAsync(Guid dietaId, Guid creadorId, Guid revisorId, DateTime? fechaValidacion = null)
    {
        if (revisorId == creadorId)
        {
            throw new ArgumentException("El revisor no puede ser el creador del menú (RS-05).", nameof(revisorId));
        }

        await HacerJefeAsync(revisorId);
        await AutorizarAsync(revisorId, dietaId);
        var menuId = await MenuAsync(dietaId, creadorId);
        await ValidarAsync(menuId, revisorId, aprobado: true, fechaValidacion);
        return menuId;
    }

    /// <summary>
    /// Igual que <see cref="MenuAprobadoAsync"/>, pero registra un rechazo (<c>aprobado = false</c>):
    /// el menú queda rechazado y puede originar un menú de reemplazo (RS-17).
    /// </summary>
    /// <returns>ID del menú rechazado.</returns>
    public async Task<Guid> MenuRechazadoAsync(Guid dietaId, Guid creadorId, Guid revisorId, DateTime? fechaValidacion = null)
    {
        if (revisorId == creadorId)
        {
            throw new ArgumentException("El revisor no puede ser el creador del menú (RS-05).", nameof(revisorId));
        }

        await HacerJefeAsync(revisorId);
        await AutorizarAsync(revisorId, dietaId);
        var menuId = await MenuAsync(dietaId, creadorId);
        await ValidarAsync(menuId, revisorId, aprobado: false, fechaValidacion);
        return menuId;
    }
}
