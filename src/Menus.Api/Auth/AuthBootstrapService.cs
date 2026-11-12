using Menus.Domain;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Abstractions.Security;
using Menus.Domain.Entidades;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Menus.Api.Auth;

/// <summary>
/// Servicio de arranque que crea un usuario administrador inicial si no existe ninguno.
/// </summary>
/// <remarks>
/// Se ejecuta al iniciar la aplicación. Usa <see cref="IServiceScopeFactory"/> para resolver
/// servicios con ámbito (repositorios, hasher, UoW).
/// <para>
/// Variables de entorno:
/// <list type="bullet">
/// <item><description><c>AuthBootstrap__Email</c>: Email del administrador inicial.</description></item>
/// <item><description><c>AuthBootstrap__Password</c>: Contraseña del administrador inicial.</description></item>
/// </list>
/// Si falta alguna variable o ya existe un Admin, no hace nada y avisa por log (sin valores sensibles).
/// </para>
/// </remarks>
public sealed class AuthBootstrapService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthBootstrapService> _logger;

    public AuthBootstrapService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<AuthBootstrapService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var email = _configuration["AuthBootstrap:Email"];
        var password = _configuration["AuthBootstrap:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogInformation("Bootstrap de administrador omitido: faltan variables de entorno AuthBootstrap__Email y/o AuthBootstrap__Password.");
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var usuarioRepository = scope.ServiceProvider.GetRequiredService<IUsuarioRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        // Verificar si ya existe algún Admin
        var admins = await usuarioRepository.ListAsync(cancellationToken);
        if (admins.Any(u => u.Rol == RolesUsuario.Admin))
        {
            _logger.LogInformation("Bootstrap de administrador omitido: ya existe al menos un usuario con rol Admin.");
            return;
        }

        // Verificar si el email ya existe con otro rol
        var emailNormalizado = Usuario.NormalizarEmail(email);
        var usuarioExistente = await usuarioRepository.GetByEmailAsync(emailNormalizado, cancellationToken);
        if (usuarioExistente is not null)
        {
            throw new InvalidOperationException(
                $"El email '{email}' ya está registrado con el rol '{usuarioExistente.Rol}'. No se puede cambiar el rol de un usuario existente mediante bootstrap.");
        }

        // Crear el usuario Admin
        var adminId = Guid.NewGuid();
        var passwordHash = passwordHasher.Hash(password);
        var admin = Usuario.Crear(adminId, emailNormalizado, passwordHash, RolesUsuario.Admin, null);
        await usuarioRepository.AddAsync(admin, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation("Usuario administrador inicial creado.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
