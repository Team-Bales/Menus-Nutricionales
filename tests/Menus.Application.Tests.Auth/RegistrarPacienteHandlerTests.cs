using System.Threading;
using System.Threading.Tasks;
using Menus.Application.Auth;
using Menus.Application.Auth.RegistroPaciente;
using Menus.Application.Comun;
using Menus.Domain;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Abstractions.Security;
using Menus.Domain.Entidades;
using Menus.Domain.Excepciones;

namespace Menus.Application.Tests.Auth;

public sealed class RegistrarPacienteHandlerTests
{
    private readonly RegistrarPacienteHandler _handler;
    private readonly FakeUsuarioRepository _usuarioRepository = new();
    private readonly FakePacienteRepository _pacienteRepository = new();
    private readonly FakeSalaRepository _salaRepository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakePasswordHasher _passwordHasher = new();

    public RegistrarPacienteHandlerTests()
    {
        _handler = new RegistrarPacienteHandler(
            _usuarioRepository,
            _pacienteRepository,
            _salaRepository,
            _unitOfWork,
            _passwordHasher);
    }

    [Fact]
    public async Task HandleAsync_SalaNoExiste_LanzaNoEncontrado()
    {
        var comando = new RegistrarPacienteCommand("Paciente", "paciente@test.com", "password123", 30, Guid.NewGuid());

        await Assert.ThrowsAsync<NoEncontradoException>(
            () => _handler.HandleAsync(comando, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_EmailDuplicado_LanzaReglaDeNegocio()
    {
        var salaId = Guid.NewGuid();
        _salaRepository.Salas[salaId] = new Sala(salaId, "Sala 1");

        var usuarioExistente = Usuario.Crear(Guid.NewGuid(), "paciente@test.com", "hash", RolesUsuario.Admin, null);
        _usuarioRepository.Usuarios[Usuario.NormalizarEmail("paciente@test.com")] = usuarioExistente;

        var comando = new RegistrarPacienteCommand("Paciente", "paciente@test.com", "password123", 30, salaId);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => _handler.HandleAsync(comando, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_RegistroExitoso_CreaPacienteYUsuario_CommitUnaVez()
    {
        var salaId = Guid.NewGuid();
        _salaRepository.Salas[salaId] = new Sala(salaId, "Sala 1");

        var comando = new RegistrarPacienteCommand("Paciente", "paciente@test.com", "password123", 30, salaId);

        var resultado = await _handler.HandleAsync(comando, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, resultado.Id);

        // Verificar que se creó el paciente
        Assert.Contains(_pacienteRepository.Pacientes.Values, p => p.Nombre == "Paciente" && p.Edad == 30 && p.SalaId == salaId);

        // Verificar que se creó el usuario vinculado
        var pacienteCreado = _pacienteRepository.Pacientes.Values.First(p => p.Nombre == "Paciente");
        Assert.Contains(_usuarioRepository.Usuarios.Values, u => u.EntidadId == pacienteCreado.Id && u.Rol == RolesUsuario.Paciente);

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

        public Task AddAsync(Usuario entity, CancellationToken cancellationToken)
        {
            Usuarios[entity.Email] = entity;
            return Task.CompletedTask;
        }
        public void Remove(Usuario entity) { }
        public Task<IReadOnlyList<Usuario>> ListAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Usuario>>(Usuarios.Values.ToList());
    }

    private sealed class FakePacienteRepository : IPacienteRepository
    {
        public Dictionary<Guid, Paciente> Pacientes { get; } = new();

        public Task<Paciente?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(Pacientes.GetValueOrDefault(id));

        public Task AddAsync(Paciente entity, CancellationToken cancellationToken)
        {
            Pacientes[entity.Id] = entity;
            return Task.CompletedTask;
        }
        public void Remove(Paciente entity) { }
        public Task<IReadOnlyList<Paciente>> ListAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Paciente>>(Pacientes.Values.ToList());
        public Task<IReadOnlyList<Paciente>> GetBySalaAsync(Guid salaId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Paciente>>([.. Pacientes.Values.Where(p => p.SalaId == salaId)]);
        public Task<IReadOnlyList<Paciente>> GetByDietaAsync(Guid dietaId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Paciente>>([.. Pacientes.Values.Where(p => p.Dietas.Any(d => d.DietaId == dietaId))]);

        public Task<(IReadOnlyList<Paciente> Pacientes, int Total)> BuscarAsync(
            Guid? salaId,
            Guid? dietaId,
            int pagina,
            int tamano,
            CancellationToken cancellationToken = default)
        {
            var filtrados = Pacientes.Values.AsQueryable();

            if (salaId.HasValue)
            {
                filtrados = filtrados.Where(p => p.SalaId == salaId.Value);
            }

            if (dietaId.HasValue)
            {
                filtrados = filtrados.Where(p => p.Dietas.Any(d => d.DietaId == dietaId.Value));
            }

            var ordenados = filtrados
                .OrderBy(p => p.Nombre, StringComparer.Ordinal)
                .ToList();

            var total = ordenados.Count;
            var enPagina = ordenados
                .Skip((pagina - 1) * tamano)
                .Take(tamano)
                .ToList();

            return Task.FromResult<(IReadOnlyList<Paciente> Pacientes, int Total)>((enPagina, total));
        }
    }

    private sealed class FakeSalaRepository : IRepository<Sala, Guid>
    {
        public Dictionary<Guid, Sala> Salas { get; } = new();

        public Task<Sala?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(Salas.GetValueOrDefault(id));

        public Task AddAsync(Sala entity, CancellationToken cancellationToken) => Task.CompletedTask;
        public void Remove(Sala entity) { }
        public Task<IReadOnlyList<Sala>> ListAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Sala>>(Salas.Values.ToList());
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
