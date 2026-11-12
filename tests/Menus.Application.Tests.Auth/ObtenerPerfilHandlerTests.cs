using System.Threading;
using System.Threading.Tasks;
using Menus.Application.Auth;
using Menus.Application.Auth.Perfil;
using Menus.Application.Comun;
using Menus.Domain;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;
using Menus.Domain.Excepciones;

namespace Menus.Application.Tests.Auth;

public sealed class ObtenerPerfilHandlerTests
{
    private readonly ObtenerPerfilHandler _handler;
    private readonly FakeUsuarioRepository _usuarioRepository = new();
    private readonly FakeIdentidadAutenticada _identidadAutenticada = new();

    public ObtenerPerfilHandlerTests()
    {
        _handler = new ObtenerPerfilHandler(_usuarioRepository, _identidadAutenticada);
    }

    [Fact]
    public async Task HandleAsync_UsuarioExiste_RetornaPerfil()
    {
        var usuarioId = Guid.NewGuid();
        var entidadId = Guid.NewGuid();
        var usuario = Usuario.Crear(usuarioId, "test@test.com", "hash", RolesUsuario.Nutricionista, entidadId);
        _usuarioRepository.Usuarios[usuarioId] = usuario;
        _identidadAutenticada.UsuarioId = usuarioId;

        var resultado = await _handler.HandleAsync(new ObtenerPerfilQuery(), CancellationToken.None);

        Assert.Equal("test@test.com", resultado.Email);
        Assert.Equal(RolesUsuario.Nutricionista, resultado.Rol);
        Assert.Equal(entidadId, resultado.EntidadId);
    }

    [Fact]
    public async Task HandleAsync_UsuarioNoExiste_LanzaNoEncontrado()
    {
        var usuarioId = Guid.NewGuid();
        _identidadAutenticada.UsuarioId = usuarioId;

        await Assert.ThrowsAsync<NoEncontradoException>(
            () => _handler.HandleAsync(new ObtenerPerfilQuery(), CancellationToken.None));
    }

    private sealed class FakeUsuarioRepository : IUsuarioRepository
    {
        public Dictionary<Guid, Usuario> Usuarios { get; } = new();

        public Task<Usuario?> GetByEmailAsync(string emailNormalizado, CancellationToken cancellationToken = default)
            => Task.FromResult(Usuarios.Values.FirstOrDefault(u => u.Email == emailNormalizado));

        public Task<bool> ExisteEmailAsync(string emailNormalizado, CancellationToken cancellationToken = default)
            => Task.FromResult(Usuarios.Values.Any(u => u.Email == emailNormalizado));

        public Task<Usuario?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Usuarios.GetValueOrDefault(id));

        public Task AddAsync(Usuario entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(Usuario entity) { }
        public Task<IReadOnlyList<Usuario>> ListAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Usuario>>(Usuarios.Values.ToList());
    }

    private sealed class FakeIdentidadAutenticada : IIdentidadAutenticada
    {
        public Guid UsuarioId { get; set; }
        public Guid? EntidadId { get; set; }
        public string Rol { get; set; } = RolesUsuario.Admin;
    }
}
