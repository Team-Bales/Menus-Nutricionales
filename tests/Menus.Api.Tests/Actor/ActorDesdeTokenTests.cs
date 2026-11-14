using System.Security.Claims;
using Menus.Api.Actor;
using Menus.Application.Comun;
using Menus.Domain;
using Menus.Infrastructure.Security;
using Microsoft.AspNetCore.Http;

namespace Menus.Api.Tests.Actor;

/// <summary>Pruebas de <see cref="ActorDesdeToken"/>: el nutricionista que actúa sale de los claims del token.</summary>
public sealed class ActorDesdeTokenTests
{
    /// <summary>Un actor sobre una petición con esos claims, armados como los arma el esquema JWT de la API.</summary>
    private static ActorDesdeToken ActorCon(params Claim[] claims)
    {
        var identidad = new ClaimsIdentity(claims, "Bearer", JwtClaimTypes.Subject, JwtClaimTypes.Role);
        var contexto = new DefaultHttpContext { User = new ClaimsPrincipal(identidad) };
        return new ActorDesdeToken(new HttpContextAccessor { HttpContext = contexto });
    }

    [Fact]
    public void Devuelve_el_entidad_id_de_un_nutricionista()
    {
        var nutricionista = Guid.NewGuid();
        var actor = ActorCon(
            new Claim(JwtClaimTypes.Role, RolesUsuario.Nutricionista),
            new Claim(JwtClaimTypes.EntidadId, nutricionista.ToString()));

        Assert.Equal(nutricionista, actor.Id);
    }

    [Theory]
    [InlineData(RolesUsuario.Paciente)]
    [InlineData(RolesUsuario.Admin)]
    public void Otro_rol_no_es_un_actor_aunque_traiga_entidad_id(string rol)
    {
        var actor = ActorCon(
            new Claim(JwtClaimTypes.Role, rol),
            new Claim(JwtClaimTypes.EntidadId, Guid.NewGuid().ToString()));

        var error = Assert.Throws<ActorNoIdentificadoException>(() => actor.Id);
        Assert.Equal("La petición no viene de un nutricionista autenticado.", error.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("no-es-un-guid")]
    public void Un_nutricionista_sin_entidad_id_valido_no_es_un_actor(string? entidadId)
    {
        Claim[] claims = entidadId is null
            ? [new Claim(JwtClaimTypes.Role, RolesUsuario.Nutricionista)]
            : [new Claim(JwtClaimTypes.Role, RolesUsuario.Nutricionista), new Claim(JwtClaimTypes.EntidadId, entidadId)];

        var error = Assert.Throws<ActorNoIdentificadoException>(() => ActorCon(claims).Id);
        Assert.Equal("El token del nutricionista no trae su identificador (entidad_id).", error.Message);
    }

    [Fact]
    public void Fuera_de_una_peticion_no_hay_actor()
    {
        var actor = new ActorDesdeToken(new HttpContextAccessor());

        Assert.Throws<ActorNoIdentificadoException>(() => actor.Id);
    }
}
