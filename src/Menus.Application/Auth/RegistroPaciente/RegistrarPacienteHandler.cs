using Menus.Application.Auth;
using Menus.Application.Comun;
using Menus.Domain;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Abstractions.Security;
using Menus.Domain.Entidades;
using Menus.Domain.Excepciones;

namespace Menus.Application.Auth.RegistroPaciente;

/// <summary>Manejador para <see cref="RegistrarPacienteCommand"/>.</summary>
public sealed class RegistrarPacienteHandler : IHandler<RegistrarPacienteCommand, IdCreadoDto>
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPacienteRepository _pacienteRepository;
    private readonly IRepository<Sala, Guid> _salaRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    /// <summary>Crea el manejador con sus dependencias.</summary>
    /// <param name="usuarioRepository">Usuarios, para comprobar el email y crear la cuenta.</param>
    /// <param name="pacienteRepository">Pacientes, para crear la entidad de la cuenta.</param>
    /// <param name="salaRepository">Salas, para comprobar que exista.</param>
    /// <param name="unitOfWork">Confirma la cuenta y el paciente en una transacción.</param>
    /// <param name="passwordHasher">Calcula el hash de la contraseña.</param>
    public RegistrarPacienteHandler(
        IUsuarioRepository usuarioRepository,
        IPacienteRepository pacienteRepository,
        IRepository<Sala, Guid> salaRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _usuarioRepository = usuarioRepository;
        _pacienteRepository = pacienteRepository;
        _salaRepository = salaRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    /// <exception cref="NoEncontradoException">Si la sala no existe.</exception>
    /// <exception cref="ReglaDeNegocioException">Si ya hay una cuenta con ese email.</exception>
    public async Task<IdCreadoDto> HandleAsync(RegistrarPacienteCommand solicitud, CancellationToken ct)
    {
        // Verificar que la sala existe
        var sala = await _salaRepository.GetByIdAsync(solicitud.SalaId, ct);
        if (sala is null)
        {
            throw new NoEncontradoException("sala", solicitud.SalaId);
        }

        // Verificar unicidad de email (precheck para mensaje temprano)
        var emailNormalizado = Usuario.NormalizarEmail(solicitud.Email);
        if (await _usuarioRepository.ExisteEmailAsync(emailNormalizado, ct))
        {
            throw new ReglaDeNegocioException("Ya existe un usuario con ese email.");
        }

        // Crear el paciente
        var pacienteId = Guid.NewGuid();
        var paciente = new Paciente(pacienteId, solicitud.Nombre, solicitud.Edad, solicitud.SalaId);
        await _pacienteRepository.AddAsync(paciente, ct);

        // Crear el usuario vinculado al paciente
        var usuarioId = Guid.NewGuid();
        var passwordHash = _passwordHasher.Hash(solicitud.Password);
        var usuario = Usuario.Crear(usuarioId, emailNormalizado, passwordHash, RolesUsuario.Paciente, pacienteId);
        await _usuarioRepository.AddAsync(usuario, ct);

        // Confirmar la transacción (una sola vez)
        await _unitOfWork.CommitAsync(ct);

        // Devolver el ID del paciente creado (EntidadId)
        return new IdCreadoDto(pacienteId);
    }
}
