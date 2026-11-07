using System.Threading;
using System.Threading.Tasks;
using Menus.Application.Auth;
using Menus.Application.Auth.RegistroNutricionista;
using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Abstractions.Security;
using Menus.Domain.Entidades;
using Menus.Domain.Excepciones;
using Menus.Domain.Nomenclators;
using Menus.Domain;

namespace Menus.Application.Tests.Auth;

public sealed class RegistrarNutricionistaHandlerTests
{
    private readonly RegistrarNutricionistaHandler _handler;
    private readonly FakeUsuarioRepository _usuarioRepository = new();
    private readonly FakeNutricionistaRepository _nutricionistaRepository = new();
    private readonly FakeEspecialidadRepository _especialidadRepository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakePasswordHasher _passwordHasher = new();

    public RegistrarNutricionistaHandlerTests()
    {
        _handler = new RegistrarNutricionistaHandler(
            _usuarioRepository,
            _nutricionistaRepository,
            _especialidadRepository,
            _unitOfWork,
            _passwordHasher);
    }

    [Fact]
    public async Task HandleAsync_EspecialidadNoExiste_LanzaNoEncontrado()
    {
        var comando = new RegistrarNutricionistaCommand("Dr. Nutri", "nutri@test.com", "password123", Guid.NewGuid());

        await Assert.ThrowsAsync<NoEncontradoException>(
            () => _handler.HandleAsync(comando, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_EmailDuplicado_LanzaReglaDeNegocio()
    {
        var especialidadId = Guid.NewGuid();
        _especialidadRepository.Especialidades[especialidadId] = new Especialidad(especialidadId, "Nutrición Clínica");

        var usuarioExistente = Usuario.Crear(Guid.NewGuid(), "nutri@test.com", "hash", RolesUsuario.Admin, null);
        _usuarioRepository.Usuarios[Usuario.NormalizarEmail("nutri@test.com")] = usuarioExistente;

        var comando = new RegistrarNutricionistaCommand("Dr. Nutri", "nutri@test.com", "password123", especialidadId);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _handler.HandleAsync(comando, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_RegistroExitoso_CreaNutricionistaYUsuario_CommitUnaVez()
    {
        var especialidadId = Guid.NewGuid();
        _especialidadRepository.Especialidades[especialidadId] = new Especialidad(especialidadId, "Nutrición Clínica");

        var comando = new RegistrarNutricionistaCommand("Dr. Nutri", "nutri@test.com", "password123", especialidadId);

        var resultado = await _handler.HandleAsync(comando, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, resultado.Id);

        // Verificar que se creó el nutricionista
        Assert.Contains(_nutricionistaRepository.Nutricionistas.Values, n => n.Nombre == "Dr. Nutri" && n.EspecialidadId == especialidadId);

        // Verificar que se creó el usuario vinculado
        var nutricionistaCreado = _nutricionistaRepository.Nutricionistas.Values.First(n => n.Nombre == "Dr. Nutri");
        Assert.Contains(_usuarioRepository.Usuarios.Values, u => u.EntidadId == nutricionistaCreado.Id && u.Rol == RolesUsuario.Nutricionista);

        // Verificar que CommitAsync se llamó una vez
        Assert.Equal(1, _unitOfWork.CommitCount);
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

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int CommitCount { get; private set; }

        public Task<int> CommitAsync(CancellationToken cancellationToken)
        {
            CommitCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => "hash";
        public bool Verify(string password, string passwordHash) => true;
        public void VerifyDummy(string attemptedPassword) { }
    }
}