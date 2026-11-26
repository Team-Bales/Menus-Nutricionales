using Menus.Application.Comun;
using Menus.Application.Tests.Catalogo;
using Menus.Domain;

namespace Menus.Application.Tests.Comun;

/// <summary>Pruebas de <see cref="ReglasDeIdentidad"/>: quién ve un dato de un paciente (#316).</summary>
public sealed class ReglasDeIdentidadTests
{
    private static readonly Guid Paciente = Guid.NewGuid();

    [Theory]
    [InlineData(RolesUsuario.Admin, true)]
    [InlineData(RolesUsuario.Nutricionista, true)]
    [InlineData(RolesUsuario.Paciente, false)]
    [InlineData("", false)]
    [InlineData("Invalido", false)]
    public void EsPersonal_segun_el_rol(string rol, bool esperado)
    {
        Assert.Equal(esperado, new IdentidadFalsa(rol, null).EsPersonal());
    }

    [Fact]
    public void El_paciente_dueno_es_el_paciente()
    {
        var identidad = new IdentidadFalsa(RolesUsuario.Paciente, Paciente);

        Assert.True(identidad.EsElPaciente(Paciente));
        Assert.True(identidad.EsPersonalOElPaciente(Paciente));
    }

    [Fact]
    public void Otro_paciente_no_ve_el_dato()
    {
        var identidad = new IdentidadFalsa(RolesUsuario.Paciente, Guid.NewGuid());

        Assert.False(identidad.EsElPaciente(Paciente));
        Assert.False(identidad.EsPersonalOElPaciente(Paciente));
    }

    [Fact]
    public void Un_paciente_sin_entidad_id_no_es_dueno_de_nada()
    {
        var identidad = new IdentidadFalsa(RolesUsuario.Paciente, null);

        Assert.False(identidad.EsElPaciente(Paciente));
        Assert.False(identidad.EsPersonalOElPaciente(Paciente));
    }

    [Theory]
    [InlineData(RolesUsuario.Admin)]
    [InlineData(RolesUsuario.Nutricionista)]
    public void El_personal_ve_el_dato_de_cualquier_paciente(string rol)
    {
        Assert.True(new IdentidadFalsa(rol, Guid.NewGuid()).EsPersonalOElPaciente(Paciente));
    }

    [Fact]
    public void Un_entidad_id_igual_no_alcanza_sin_el_rol_de_paciente()
    {
        // El entidad_id de un nutricionista es el de su ficha. Aunque coincidiera, no lo hace dueño del dato; lo ve por
        // ser personal, no por ser el paciente.
        var identidad = new IdentidadFalsa(RolesUsuario.Nutricionista, Paciente);

        Assert.False(identidad.EsElPaciente(Paciente));
        Assert.False(new IdentidadFalsa("Invalido", Paciente).EsPersonalOElPaciente(Paciente));
    }
}
