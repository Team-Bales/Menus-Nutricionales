using System.Security.Claims;
using Menus.Api.Auth;
using Menus.Application.Comun;
using Microsoft.AspNetCore.Http;

namespace Menus.Api.Tests.Auth;

public sealed class IdentidadAutenticadaTests
{
    private static IdentidadAutenticada Crear(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        var contexto = new DefaultHttpContext { User = principal };
        return new IdentidadAutenticada(new HttpContextAccessor { HttpContext = contexto });
    }

    private static IdentidadAutenticada CrearSinContexto()
        => new(new HttpContextAccessor());

    [Fact]
    public void Claims_aspnet_mapeados_devuelven_usuarioid_rol_y_entidadid()
    {
        var usuarioId = Guid.NewGuid();
        var entidadId = Guid.NewGuid();
        var rol = "Nutricionista";

        var identidad = Crear(
            new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()),
            new Claim(ClaimTypes.Role, rol),
            new Claim("entidad_id", entidadId.ToString())
        );

        Assert.Equal(usuarioId, identidad.UsuarioId);
        Assert.Equal(rol, identidad.Rol);
        Assert.Equal(entidadId, identidad.EntidadId);
    }

    [Fact]
    public void Claims_jwt_crudos_sub_y_role_sin_entidad_id_fallback_correcto()
    {
        var usuarioId = Guid.NewGuid();
        var rol = "Paciente";

        var identidad = Crear(
            new Claim("sub", usuarioId.ToString()),
            new Claim("role", rol)
        );

        Assert.Equal(usuarioId, identidad.UsuarioId);
        Assert.Equal(rol, identidad.Rol);
        Assert.Null(identidad.EntidadId);
    }

    [Fact]
    public void Precedencia_nameidentifier_sobre_sub_para_usuarioid()
    {
        var nameIdentifier = Guid.NewGuid();
        var sub = Guid.NewGuid();

        var identidad = Crear(
            new Claim(ClaimTypes.NameIdentifier, nameIdentifier.ToString()),
            new Claim("sub", sub.ToString()),
            new Claim("role", "Rol")
        );

        Assert.Equal(nameIdentifier, identidad.UsuarioId);
    }

    [Fact]
    public void Precedencia_claimtypesrole_sobre_role_para_rol()
    {
        var roleAspNet = "Nutricionista";
        var roleJwt = "Paciente";

        var identidad = Crear(
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, roleAspNet),
            new Claim("role", roleJwt)
        );

        Assert.Equal(roleAspNet, identidad.Rol);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-es-guid")]
    public void Usuarioid_sin_sub_valido_lanza_invalidoperation(string? subClaim)
    {
        var claims = new List<Claim>
        {
            new Claim("role", "Rol")
        };
        if (subClaim is not null)
        {
            claims.Add(new Claim("sub", subClaim));
        }

        var identidad = Crear(claims.ToArray());

        var ex = Assert.Throws<InvalidOperationException>(() => identidad.UsuarioId);
        Assert.Equal("El token no contiene un claim 'sub' válido.", ex.Message);
    }

    [Fact]
    public void Usuarioid_con_httpcontext_null_lanza_invalidoperation()
    {
        var identidad = CrearSinContexto();

        var ex = Assert.Throws<InvalidOperationException>(() => identidad.UsuarioId);
        Assert.Equal("El token no contiene un claim 'sub' válido.", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rol_sin_role_valido_lanza_invalidoperation(string? roleClaim)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        };
        if (roleClaim is not null)
        {
            claims.Add(new Claim("role", roleClaim));
        }

        var identidad = Crear(claims.ToArray());

        var ex = Assert.Throws<InvalidOperationException>(() => identidad.Rol);
        Assert.Equal("El token no contiene un claim 'role' válido.", ex.Message);
    }

    [Fact]
    public void Rol_con_httpcontext_null_lanza_invalidoperation()
    {
        var identidad = CrearSinContexto();

        var ex = Assert.Throws<InvalidOperationException>(() => identidad.Rol);
        Assert.Equal("El token no contiene un claim 'role' válido.", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-es-guid")]
    public void Entidadid_con_entidad_id_invalido_devuelve_null_sin_lanzar(string entidadIdClaim)
    {
        var identidad = Crear(
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim("role", "Rol"),
            new Claim("entidad_id", entidadIdClaim)
        );

        Assert.Null(identidad.EntidadId);
    }

    [Fact]
    public void Entidadid_sin_claim_entidad_id_devuelve_null()
    {
        var identidad = Crear(
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim("role", "Rol")
        );

        Assert.Null(identidad.EntidadId);
    }

    [Fact]
    public void Interfaz_no_expone_especialidad_ni_esjefe()
    {
        var tipo = typeof(IIdentidadAutenticada);
        var miembros = tipo.GetProperties().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("Especialidad", miembros);
        Assert.DoesNotContain("EsJefe", miembros);
        Assert.Equal(new[] { "EntidadId", "Rol", "UsuarioId" }.OrderBy(x => x), miembros.OrderBy(x => x));
    }
}
