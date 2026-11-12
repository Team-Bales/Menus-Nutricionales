using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Menus.Application.Auth;
using Menus.Application.Auth.Login;
using Menus.Application.Comun;
using Menus.Domain;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Abstractions.Security;
using Menus.Domain.Entidades;
using Menus.Domain.Excepciones;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Tests.Auth;

public sealed class LoginHandlerTests
{
    private readonly LoginHandler _handler;
    private readonly FakeUsuarioRepository _usuarioRepository = new();
    private readonly FakeNutricionistaRepository _nutricionistaRepository = new();
    private readonly FakeEspecialidadRepository _especialidadRepository = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakeTokenEmitter _tokenEmitter = new();

    public LoginHandlerTests()
    {
        _handler = new LoginHandler(
            _usuarioRepository,
            _nutricionistaRepository,
            _especialidadRepository,
            _passwordHasher,
            _tokenEmitter);
    }

    [Fact]
    public async Task HandleAsync_UsuarioInexistente_LanzaCredencialesInvalidasYLlamaVerifyDummy()
    {
        var comando = new LoginCommand("noexiste@test.com", "password123");

        await Assert.ThrowsAsync<CredencialesInvalidasException>(
            () => _handler.HandleAsync(comando, CancellationToken.None));

        Assert.True(_passwordHasher.VerifyDummyCalled);
    }

    [Fact]
    public async Task HandleAsync_PasswordIncorrecto_LanzaCredencialesInvalidas()
    {
        var usuario = Usuario.Crear(Guid.NewGuid(), "test@test.com", "hashCorrecto", RolesUsuario.Admin, null);
        _usuarioRepository.Usuarios[Usuario.NormalizarEmail("test@test.com")] = usuario;

        var comando = new LoginCommand("test@test.com", "passwordIncorrecto");

        await Assert.ThrowsAsync<CredencialesInvalidasException>(
            () => _handler.HandleAsync(comando, CancellationToken.None));

        Assert.False(_passwordHasher.VerifyDummyCalled);
    }

    [Fact]
    public async Task HandleAsync_LoginExitosoAdmin_RetornaToken()
    {
        var usuarioId = Guid.NewGuid();
        var usuario = Usuario.Crear(usuarioId, "admin@test.com", "hashCorrecto", RolesUsuario.Admin, null);
        _usuarioRepository.Usuarios[Usuario.NormalizarEmail("admin@test.com")] = usuario;
        _passwordHasher.Verificaciones["password123"] = true;
        _tokenEmitter.Tokens[usuarioId] = new TokenEmitido("token-admin", DateTime.UtcNow.AddHours(1));

        var comando = new LoginCommand("admin@test.com", "password123");
        var resultado = await _handler.HandleAsync(comando, CancellationToken.None);

        Assert.Equal("token-admin", resultado.Token);
        Assert.Equal(RolesUsuario.Admin, resultado.Rol);
    }

    [Fact]
    public async Task HandleAsync_LoginExitosoNutricionista_IncluyeEntidadIdYEspecialidad()
    {
        var usuarioId = Guid.NewGuid();
        var nutricionistaId = Guid.NewGuid();
        var especialidadId = Guid.NewGuid();

        var usuario = Usuario.Crear(usuarioId, "nutri@test.com", "hashCorrecto", RolesUsuario.Nutricionista, nutricionistaId);
        _usuarioRepository.Usuarios[Usuario.NormalizarEmail("nutri@test.com")] = usuario;

        var nutricionista = new Nutricionista(nutricionistaId, "Dr. Nutri", especialidadId);
        _nutricionistaRepository.Nutricionistas[nutricionistaId] = nutricionista;

        var especialidad = new Especialidad(especialidadId, "Nutrición Clínica");
        _especialidadRepository.Especialidades[especialidadId] = especialidad;

        _passwordHasher.Verificaciones["password123"] = true;
        _tokenEmitter.Tokens[usuarioId] = new TokenEmitido("token-nutri", DateTime.UtcNow.AddHours(1));

        var comando = new LoginCommand("nutri@test.com", "password123");
        var resultado = await _handler.HandleAsync(comando, CancellationToken.None);

        Assert.Equal("token-nutri", resultado.Token);
        Assert.Equal(RolesUsuario.Nutricionista, resultado.Rol);

        // Verificar que se pasó el entidadId y especialidad al token emitter
        Assert.True(_tokenEmitter.UltimaLlamada.EntidadId.HasValue);
        Assert.Equal(nutricionistaId, _tokenEmitter.UltimaLlamada.EntidadId.Value);
        Assert.Equal("Nutrición Clínica", _tokenEmitter.UltimaLlamada.Especialidad);
    }

    private sealed class FakeUsuarioRepository : IUsuarioRepository
    {
        public Dictionary<string, Usuario> Usuarios { get; } = new();

        public Task<Usuario?> GetByEmailAsync(string emailNormalizado, CancellationToken cancellationToken)
            => Task.FromResult(Usuarios.GetValueOrDefault(emailNormalizado));

        public Task<bool> ExisteEmailAsync(string emailNormalizado, CancellationToken cancellationToken)
            => Task.FromResult(Usuarios.ContainsKey(emailNormalizado));

        public Task<Usuario?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(Usuarios.Values.FirstOrDefault(u => u.Id == id));

        public Task AddAsync(Usuario entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public void Remove(Usuario entity) { }
        public Task<IReadOnlyList<Usuario>> ListAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Usuario>>(Usuarios.Values.ToList());
    }

    private sealed class FakeNutricionistaRepository : INutricionistaRepository
    {
        public Dictionary<Guid, Nutricionista> Nutricionistas { get; } = new();

        public Task<Nutricionista?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(Nutricionistas.GetValueOrDefault(id));

        public Task AddAsync(Nutricionista entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public void Remove(Nutricionista entity) { }
        public Task<IReadOnlyList<Nutricionista>> ListAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Nutricionista>>(Nutricionistas.Values.ToList());
        public Task<IReadOnlyList<Nutricionista>> GetByDietaAsync(Guid dietaId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Nutricionista>>([]);
    }

    private sealed class FakeEspecialidadRepository : INomencladorRepository<Especialidad>
    {
        public Dictionary<Guid, Especialidad> Especialidades { get; } = new();

        public Task<Especialidad?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(Especialidades.GetValueOrDefault(id));

        public Task AddAsync(Especialidad entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public void Remove(Especialidad entity) { }
        public Task<IReadOnlyList<Especialidad>> ListAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Especialidad>>(Especialidades.Values.ToList());
        public Task<bool> ExisteNombreAsync(string nombre, CancellationToken cancellationToken)
            => Task.FromResult(false);
        public Task<bool> EstaEnUsoAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(false);
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public Dictionary<string, bool> Verificaciones { get; } = new();
        public bool VerifyDummyCalled { get; private set; }

        public string Hash(string password) => "hash";

        public bool Verify(string password, string passwordHash)
            => Verificaciones.GetValueOrDefault(password);

        public void VerifyDummy(string attemptedPassword)
            => VerifyDummyCalled = true;
    }

    private sealed class FakeTokenEmitter : ITokenEmitter
    {
        public Dictionary<Guid, TokenEmitido> Tokens { get; } = new();
        public (Guid UsuarioId, string Rol, Guid? EntidadId, string? Especialidad, bool EsJefeNutricion) UltimaLlamada { get; private set; }

        public TokenEmitido Emitir(Guid usuarioId, string rol, Guid? entidadId = null, string? especialidad = null, bool esJefeNutricion = false)
        {
            UltimaLlamada = (usuarioId, rol, entidadId, especialidad, esJefeNutricion);
            return Tokens.GetValueOrDefault(usuarioId, new TokenEmitido("default", DateTime.UtcNow.AddHours(1)));
        }
    }
}
