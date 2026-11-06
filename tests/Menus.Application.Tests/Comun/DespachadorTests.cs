using FluentValidation;
using Menus.Application.Comun;
using Microsoft.Extensions.DependencyInjection;

namespace Menus.Application.Tests.Comun;

/// <summary>
/// Pruebas del despachador y del registro por reflexión (ADR-010, §1 y §3), con solicitudes, manejadores y
/// validadores propios de este ensamblado.
/// </summary>
public sealed class DespachadorTests
{
    private static ServiceProvider CrearProveedor()
    {
        var services = new ServiceCollection();
        services.AddSingleton<Registro>();
        services.AddApplication(typeof(DespachadorTests).Assembly);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task EnviarAsync_con_un_comando_valido_ejecuta_su_manejador_y_devuelve_el_resultado()
    {
        using var proveedor = CrearProveedor();
        using var ambito = proveedor.CreateScope();

        var resultado = await ambito.ServiceProvider.GetRequiredService<IDespachador>().EnviarAsync(new Sumar(2, 3));

        Assert.Equal(5, resultado);
        Assert.Equal(1, proveedor.GetRequiredService<Registro>().Llamadas);
    }

    [Fact]
    public async Task EnviarAsync_con_una_consulta_ejecuta_su_manejador()
    {
        using var proveedor = CrearProveedor();
        using var ambito = proveedor.CreateScope();

        var resultado = await ambito.ServiceProvider.GetRequiredService<IDespachador>().EnviarAsync(new Leer("menú"));

        Assert.Equal("MENÚ", resultado);
    }

    [Fact]
    public async Task EnviarAsync_con_errores_lanza_ValidationException_con_los_de_todos_los_validadores_y_no_ejecuta_el_manejador()
    {
        using var proveedor = CrearProveedor();
        using var ambito = proveedor.CreateScope();
        var despachador = ambito.ServiceProvider.GetRequiredService<IDespachador>();

        var ex = await Assert.ThrowsAsync<ValidationException>(() => despachador.EnviarAsync(new Sumar(-1, -1)));

        Assert.Equal(["A", "B"], ex.Errors.Select(e => e.PropertyName).Order());
        Assert.Equal(0, proveedor.GetRequiredService<Registro>().Llamadas);
    }

    [Fact]
    public async Task EnviarAsync_sin_manejador_registrado_lanza_InvalidOperationException()
    {
        using var proveedor = CrearProveedor();
        using var ambito = proveedor.CreateScope();
        var despachador = ambito.ServiceProvider.GetRequiredService<IDespachador>();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => despachador.EnviarAsync(new SinManejador()));

        Assert.Contains(nameof(SinManejador), ex.Message);
    }

    [Fact]
    public async Task EnviarAsync_con_una_solicitud_nula_lanza_ArgumentNullException()
    {
        using var proveedor = CrearProveedor();
        using var ambito = proveedor.CreateScope();
        var despachador = ambito.ServiceProvider.GetRequiredService<IDespachador>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => despachador.EnviarAsync<int>(null!));
    }

    [Fact]
    public void AddApplication_registra_el_despachador_con_ambito_de_peticion()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        var registro = Assert.Single(services, d => d.ServiceType == typeof(IDespachador));
        Assert.Equal(ServiceLifetime.Scoped, registro.Lifetime);
    }

    [Fact]
    public void AddApplication_registra_los_manejadores_y_los_validadores_del_ensamblado()
    {
        var services = new ServiceCollection();

        services.AddApplication(typeof(DespachadorTests).Assembly);

        Assert.Contains(services, d => d.ServiceType == typeof(IHandler<Sumar, int>) && d.ImplementationType == typeof(SumarHandler));
        Assert.Contains(services, d => d.ServiceType == typeof(IHandler<Leer, string>) && d.ImplementationType == typeof(LeerHandler));
        Assert.Equal(2, services.Count(d => d.ServiceType == typeof(IValidator<Sumar>)));
    }
}

public sealed class Registro
{
    public int Llamadas { get; set; }
}

public sealed record Sumar(int A, int B) : ICommand<int>;

public sealed class SumarHandler(Registro registro) : IHandler<Sumar, int>
{
    public Task<int> HandleAsync(Sumar solicitud, CancellationToken ct)
    {
        registro.Llamadas++;
        return Task.FromResult(solicitud.A + solicitud.B);
    }
}

public sealed class SumarAValidator : AbstractValidator<Sumar>
{
    public SumarAValidator() => RuleFor(s => s.A).GreaterThanOrEqualTo(0);
}

public sealed class SumarBValidator : AbstractValidator<Sumar>
{
    public SumarBValidator() => RuleFor(s => s.B).GreaterThanOrEqualTo(0);
}

public sealed record Leer(string Texto) : IQuery<string>;

public sealed class LeerHandler : IHandler<Leer, string>
{
    public Task<string> HandleAsync(Leer solicitud, CancellationToken ct) => Task.FromResult(solicitud.Texto.ToUpperInvariant());
}

public sealed record SinManejador : ICommand<int>;
