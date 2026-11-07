using Menus.Application.Auth;
using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Abstractions.Security;
using Menus.Domain.Entidades;
using Menus.Domain.Excepciones;
using Menus.Domain.Nomenclators;
using Menus.Domain;

namespace Menus.Application.Auth.RegistroNutricionista;

/// <summary>Manejador para <see cref="RegistrarNutricionistaCommand"/>.</summary>
public sealed class RegistrarNutricionistaHandler : IHandler<RegistrarNutricionistaCommand, IdCreadoDto>
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly INutricionistaRepository _nutricionistaRepository;
    private readonly INomencladorRepository<Especialidad> _especialidadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public RegistrarNutricionistaHandler(
        IUsuarioRepository usuarioRepository,
        INutricionistaRepository nutricionistaRepository,
        INomencladorRepository<Especialidad> especialidadRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _usuarioRepository = usuarioRepository;
        _nutricionistaRepository = nutricionistaRepository;
        _especialidadRepository = especialidadRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<IdCreadoDto> HandleAsync(RegistrarNutricionistaCommand solicitud, CancellationToken ct)
    {
        // Verificar que la especialidad existe
        var especialidad = await _especialidadRepository.GetByIdAsync(solicitud.EspecialidadId, ct);
        if (especialidad is null)
        {
            throw new NoEncontradoException("especialidad", solicitud.EspecialidadId);
        }

        // Verificar unicidad de email (precheck para mensaje temprano)
        var emailNormalizado = Usuario.NormalizarEmail(solicitud.Email);
        if (await _usuarioRepository.ExisteEmailAsync(emailNormalizado, ct))
        {
            throw new ReglaDeNegocioException("Ya existe un usuario con ese email.");
        }

        // Crear el nutricionista
        var nutricionistaId = Guid.NewGuid();
        var nutricionista = new Nutricionista(nutricionistaId, solicitud.Nombre, solicitud.EspecialidadId);
        await _nutricionistaRepository.AddAsync(nutricionista, ct);

        // Crear el usuario vinculado al nutricionista
        var usuarioId = Guid.NewGuid();
        var passwordHash = _passwordHasher.Hash(solicitud.Password);
        var usuario = Usuario.Crear(usuarioId, emailNormalizado, passwordHash, RolesUsuario.Nutricionista, nutricionistaId);
        await _usuarioRepository.AddAsync(usuario, ct);

        // Confirmar la transacción (una sola vez)
        await _unitOfWork.CommitAsync(ct);

        // Devolver el ID del nutricionista creado (EntidadId)
        return new IdCreadoDto(nutricionistaId);
    }
}