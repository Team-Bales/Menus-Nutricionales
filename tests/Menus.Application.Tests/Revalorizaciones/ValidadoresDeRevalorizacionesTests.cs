using FluentValidation.TestHelper;
using Menus.Application.Revalorizaciones.ListarPendientes;
using Menus.Application.Revalorizaciones.RegistrarResultadoRevalorizacion;
using Menus.Application.Revalorizaciones.SolicitarRevalorizacion;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Tests.Revalorizaciones;

/// <summary>Pruebas de los validadores del flujo de revalorización (#87).</summary>
public sealed class ValidadoresDeRevalorizacionesTests
{
    private readonly SolicitarRevalorizacionValidator _solicitarValidator = new();
    private readonly RegistrarResultadoRevalorizacionValidator _registrarValidator = new();

    // ---- SolicitarRevalorizacionValidator ----

    [Fact]
    public void Solicitar_Valido_NoTieneErrores()
    {
        var comando = new SolicitarRevalorizacionCommand(Guid.NewGuid(), Guid.NewGuid());
        var resultado = _solicitarValidator.TestValidate(comando);
        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Solicitar_ValoracionRevisadaId_Vacio_TieneError()
    {
        var comando = new SolicitarRevalorizacionCommand(Guid.Empty, Guid.NewGuid());
        var resultado = _solicitarValidator.TestValidate(comando);
        resultado.ShouldHaveValidationErrorFor(c => c.ValoracionRevisadaId);
    }

    [Fact]
    public void Solicitar_NutricionistaId_Vacio_TieneError()
    {
        var comando = new SolicitarRevalorizacionCommand(Guid.NewGuid(), Guid.Empty);
        var resultado = _solicitarValidator.TestValidate(comando);
        resultado.ShouldHaveValidationErrorFor(c => c.NutricionistaId);
    }

    // ---- RegistrarResultadoRevalorizacionValidator ----

    [Fact]
    public void RegistrarResultado_Valido_NoTieneErrores()
    {
        var comando = new RegistrarResultadoRevalorizacionCommand(Guid.NewGuid(), ResultadoNutricional.Normal.Id);
        var resultado = _registrarValidator.TestValidate(comando);
        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void RegistrarResultado_RevalorizacionId_Vacio_TieneError()
    {
        var comando = new RegistrarResultadoRevalorizacionCommand(Guid.Empty, ResultadoNutricional.Normal.Id);
        var resultado = _registrarValidator.TestValidate(comando);
        resultado.ShouldHaveValidationErrorFor(c => c.RevalorizacionId);
    }

    [Fact]
    public void RegistrarResultado_ResultadoNutricionalId_Invalido_TieneError()
    {
        var comando = new RegistrarResultadoRevalorizacionCommand(Guid.NewGuid(), Guid.NewGuid());
        var resultado = _registrarValidator.TestValidate(comando);
        resultado.ShouldHaveValidationErrorFor(c => c.ResultadoNutricionalId);
    }

    [Fact]
    public void RegistrarResultado_ResultadoNutricionalId_Vacio_TieneError()
    {
        var comando = new RegistrarResultadoRevalorizacionCommand(Guid.NewGuid(), Guid.Empty);
        var resultado = _registrarValidator.TestValidate(comando);
        resultado.ShouldHaveValidationErrorFor(c => c.ResultadoNutricionalId);
    }
}