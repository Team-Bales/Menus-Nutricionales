using Menus.Api.Actor;
using Menus.Application.Comun;
using Microsoft.AspNetCore.Http;

namespace Menus.Api.Tests.Actor;

public sealed class ActorDesdeCabeceraTests
{
    private static ActorDesdeCabecera Crear(string? cabecera)
    {
        var contexto = new DefaultHttpContext();
        if (cabecera is not null)
        {
            contexto.Request.Headers[ActorDesdeCabecera.Cabecera] = cabecera;
        }

        return new ActorDesdeCabecera(new HttpContextAccessor { HttpContext = contexto });
    }

    [Fact]
    public void Id_lee_el_identificador_de_la_cabecera()
    {
        var id = Guid.NewGuid();

        Assert.Equal(id, Crear(id.ToString()).Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no-es-un-guid")]
    public void Id_sin_una_cabecera_valida_lanza_ActorNoIdentificadoException(string? cabecera)
    {
        var actor = Crear(cabecera);

        Assert.Throws<ActorNoIdentificadoException>(() => actor.Id);
    }

    [Fact]
    public void Id_fuera_de_una_peticion_lanza_ActorNoIdentificadoException()
    {
        var actor = new ActorDesdeCabecera(new HttpContextAccessor());

        Assert.Throws<ActorNoIdentificadoException>(() => actor.Id);
    }
}
