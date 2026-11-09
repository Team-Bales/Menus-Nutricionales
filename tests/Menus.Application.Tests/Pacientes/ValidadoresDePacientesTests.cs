using FluentValidation;
using Menus.Application.Pacientes;
using Menus.Application.Pacientes.ActualizarPaciente;
using Menus.Application.Pacientes.InscribirDieta;
using Menus.Application.Pacientes.ListarPacientes;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Application.Tests.Pacientes;

/// <summary>Pruebas de la forma de la entrada del #73 (ADR-010, §3: 422 con los errores por campo).</summary>
public sealed class ValidadoresDePacientesTests
{
    private static readonly Guid Sala = Guid.NewGuid();

    private static string[] Campos<T>(AbstractValidator<T> validador, T solicitud)
        => [.. validador.Validate(solicitud).Errors.Select(e => e.PropertyName).Distinct()];

    // ---- Actualizar ----

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(95)]
    public void Actualizar_valido_no_tiene_errores(int edad)
        => Assert.Empty(Campos(new ActualizarPacienteValidator(), new ActualizarPacienteCommand(Guid.NewGuid(), "Ana", edad, Sala)));

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Actualizar_con_edad_negativa_falla_en_Edad(int edad)
        => Assert.Equal(
            ["Edad"],
            Campos(new ActualizarPacienteValidator(), new ActualizarPacienteCommand(Guid.NewGuid(), "Ana", edad, Sala)));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Actualizar_sin_nombre_falla_en_Nombre(string? nombre)
        => Assert.Equal(
            ["Nombre"],
            Campos(new ActualizarPacienteValidator(), new ActualizarPacienteCommand(Guid.NewGuid(), nombre!, 30, Sala)));

    [Fact]
    public void Actualizar_con_un_nombre_demasiado_largo_falla_en_Nombre()
    {
        var nombre = new string('a', ReglasDePaciente.LargoMaximoDelNombre + 1);

        Assert.Equal(
            ["Nombre"],
            Campos(new ActualizarPacienteValidator(), new ActualizarPacienteCommand(Guid.NewGuid(), nombre, 30, Sala)));
    }

    [Fact]
    public void Actualizar_sin_sala_falla_en_SalaId()
        => Assert.Equal(
            ["SalaId"],
            Campos(new ActualizarPacienteValidator(), new ActualizarPacienteCommand(Guid.NewGuid(), "Ana", 30, Guid.Empty)));

    // ---- Inscribir ----

    [Fact]
    public void Inscribir_valido_no_tiene_errores()
        => Assert.Empty(Campos(new InscribirDietaValidator(), new InscribirDietaCommand(Guid.NewGuid(), Guid.NewGuid())));

    [Fact]
    public void Inscribir_sin_dieta_falla_en_DietaId()
        => Assert.Equal(
            ["DietaId"],
            Campos(new InscribirDietaValidator(), new InscribirDietaCommand(Guid.NewGuid(), Guid.Empty)));

    // ---- Listar ----

    [Fact]
    public void Listar_por_defecto_no_tiene_errores()
        => Assert.Empty(Campos(new ListarPacientesValidator(), new ListarPacientesQuery()));

    [Fact]
    public void Listar_con_pagina_cero_y_tamano_excesivo_falla_en_los_dos()
        => Assert.Equal(
            ["Page", "Size"],
            Campos(new ListarPacientesValidator(), new ListarPacientesQuery(Page: 0, Size: 101)));

    // ---- Registro ----

    [Fact]
    public void Los_validadores_se_registran_con_la_capa_de_aplicacion()
    {
        using var proveedor = new ServiceCollection().AddApplication().BuildServiceProvider();
        using var alcance = proveedor.CreateScope();

        Assert.IsType<ActualizarPacienteValidator>(
            Assert.Single(alcance.ServiceProvider.GetServices<IValidator<ActualizarPacienteCommand>>()));
        Assert.IsType<InscribirDietaValidator>(
            Assert.Single(alcance.ServiceProvider.GetServices<IValidator<InscribirDietaCommand>>()));
        Assert.IsType<ListarPacientesValidator>(
            Assert.Single(alcance.ServiceProvider.GetServices<IValidator<ListarPacientesQuery>>()));
    }
}
