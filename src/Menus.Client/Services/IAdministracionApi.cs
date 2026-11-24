using Menus.Client.Administracion;
using Menus.Client.Catalogo;

namespace Menus.Client.Services;

/// <summary>Contrato de la UI con los endpoints administrativos de nutricionistas y salas.</summary>
/// <remarks>
/// Cada método llama a un endpoint de la API con el token de la sesión. Si la API responde con un error, lanza
/// <see cref="ApiException"/> con el mensaje que se muestra en la página.
/// </remarks>
public interface IAdministracionApi
{
    /// <summary>Una página de nutricionistas, opcionalmente de una especialidad (<c>GET api/nutricionistas</c>).</summary>
    Task<Pagina<Nutricionista>> ListarNutricionistasAsync(Guid? especialidadId, int page, int size, CancellationToken ct = default);

    /// <summary>Un nutricionista con sus dietas autorizadas (<c>GET api/nutricionistas/{id}</c>).</summary>
    Task<NutricionistaDetalle> ObtenerNutricionistaAsync(Guid id, CancellationToken ct = default);

    /// <summary>Autoriza al nutricionista en una dieta (<c>POST api/nutricionistas/{id}/dietas</c>).</summary>
    Task AutorizarDietaAsync(Guid nutricionistaId, Guid dietaId, CancellationToken ct = default);

    /// <summary>Revoca la autorización del nutricionista en una dieta (<c>DELETE api/nutricionistas/{id}/dietas/{dietaId}</c>).</summary>
    Task RevocarDietaAsync(Guid nutricionistaId, Guid dietaId, CancellationToken ct = default);

    /// <summary>Designa al nutricionista jefe de nutrición (<c>POST api/nutricionistas/{id}/rol-jefe</c>).</summary>
    Task AsignarRolJefeAsync(Guid nutricionistaId, CancellationToken ct = default);

    /// <summary>Todas las salas (<c>GET api/salas</c>).</summary>
    Task<IReadOnlyList<Sala>> ListarSalasAsync(CancellationToken ct = default);

    /// <summary>Cantidad de pacientes de una sala: el total de <c>GET api/pacientes?sala=…</c>, pidiendo una página de un solo elemento.</summary>
    Task<int> ContarPacientesEnSalaAsync(Guid salaId, CancellationToken ct = default);

    /// <summary>Crea una sala y devuelve su id (<c>POST api/salas</c>).</summary>
    Task<Guid> CrearSalaAsync(SalaFormulario formulario, CancellationToken ct = default);

    /// <summary>Renombra una sala (<c>PUT api/salas/{id}</c>).</summary>
    Task ActualizarSalaAsync(Guid salaId, SalaFormulario formulario, CancellationToken ct = default);

    /// <summary>Elimina una sala (<c>DELETE api/salas/{id}</c>).</summary>
    Task EliminarSalaAsync(Guid salaId, CancellationToken ct = default);
}
