using System.Security.Claims;
using Menus.Api.Auth;
using Menus.Domain;
using Menus.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Menus.Api.Tests.Auth;

/// <summary>
/// Pruebas de <see cref="Politicas.EsPersonalOElPropioPaciente"/>, la regla de <see cref="Politicas.PacientePropioOPersonal"/>
/// (#84): el personal pasa siempre, y un paciente solo si la ruta es la suya.
/// </summary>
public sealed class PoliticasTests
{
    private static readonly Guid DeLaRuta = Guid.NewGuid();

    private static bool Evaluar(object? recurso, params Claim[] claims)
    {
        var usuario = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer", JwtClaimTypes.Subject, JwtClaimTypes.Role));
        return Politicas.EsPersonalOElPropioPaciente(new AuthorizationHandlerContext([], usuario, recurso));
    }

    private static DefaultHttpContext PeticionDe(Guid paciente)
    {
        var peticion = new DefaultHttpContext();
        peticion.Request.RouteValues[Politicas.ParametroDelPaciente] = paciente.ToString();
        return peticion;
    }

    private static Claim Rol(string rol) => new(JwtClaimTypes.Role, rol);

    private static Claim Entidad(Guid id) => new(JwtClaimTypes.EntidadId, id.ToString());

    [Theory]
    [InlineData(RolesUsuario.Nutricionista)]
    [InlineData(RolesUsuario.Admin)]
    public void El_personal_pasa_con_cualquier_paciente_en_la_ruta(string rol)
        => Assert.True(Evaluar(PeticionDe(DeLaRuta), Rol(rol)));

    [Fact]
    public void El_paciente_de_la_ruta_pasa()
        => Assert.True(Evaluar(PeticionDe(DeLaRuta), Rol(RolesUsuario.Paciente), Entidad(DeLaRuta)));

    [Fact]
    public void Otro_paciente_no_pasa()
        => Assert.False(Evaluar(PeticionDe(DeLaRuta), Rol(RolesUsuario.Paciente), Entidad(Guid.NewGuid())));

    [Fact]
    public void Un_paciente_sin_entidad_id_no_pasa()
        => Assert.False(Evaluar(PeticionDe(DeLaRuta), Rol(RolesUsuario.Paciente)));

    [Fact]
    public void Sin_la_peticion_como_recurso_un_paciente_no_pasa()
        => Assert.False(Evaluar(null, Rol(RolesUsuario.Paciente), Entidad(DeLaRuta)));

    [Fact]
    public void Una_ruta_sin_el_parametro_del_paciente_no_deja_pasar_a_un_paciente()
        => Assert.False(Evaluar(new DefaultHttpContext(), Rol(RolesUsuario.Paciente), Entidad(DeLaRuta)));

    [Fact]
    public void Sin_rol_no_pasa_nadie()
        => Assert.False(Evaluar(PeticionDe(DeLaRuta), Entidad(DeLaRuta)));
}
