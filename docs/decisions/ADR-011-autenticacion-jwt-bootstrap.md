# ADR-011: Autenticación — JWT, login, registro y bootstrap de Admin

**Estado:** Aceptado  
**Fecha:** 2026-11-07

## Contexto

El issue #70 requiere implementar autenticación completa: login, registro de nutricionistas y pacientes (solo Admin), endpoint de perfil, hash BCrypt, tokens JWT con claims específicos, y bootstrap seguro del primer administrador. El issue #69 ya dejó la infraestructura JWT (JwtTokenService, JwtOptions, JwtBearerConfiguration) en main.

Decisiones de diseño previas (informe, ADR-010) que condicionan esta implementación:
- **Entidad `Usuario`** en Domain con `Id`, `Email` normalizado, `PasswordHash`, `Rol`, `EntidadId?`.
- **Reglas de `EntidadId`**: Admin → `null`; Nutricionista/Paciente → non-null.
- **Persistencia PostgreSQL**: tabla `usuarios`, `CHECK` de rol/EntidadId, índice único simple en `email` (ya normalizado), índice único filtrado en `entidad_id` (non-null). **Sin FK polimórfica** desde `EntidadId` a `nutricionistas`/`pacientes`.
- **Registro**: un solo `IUnitOfWork.CommitAsync` crea la entidad de dominio (Nutricionista/Paciente) + Usuario. Devuelve `IdCreadoDto` con el `EntidadId`.
- **BCrypt.Net-Next** solo en `Menus.Infrastructure`. `IPasswordHasher` en `Domain.Abstractions.Security`. `VerifyDummy` con hash dummy generado **una vez por proceso** (static `Lazy`), no por request, no inválido.
- **JWT**: `sub=Usuario.Id`, `role`, `entidad_id` solo si non-null. Para Nutricionista, login carga entidad y emite `especialidad` y `jefe_nutricion` con datos reales.
- **Perfil**: `IIdentidadAutenticada` en `Application.Comun` (UsuarioId, EntidadId?, Rol), implementación en API leyendo **solo** `sub`/`entidad_id`/`role` del JWT. No enriquece con campos role-specific.
- **API**: `[AllowAnonymous]` login; `[Authorize(Roles=Admin)]` registros; `[Authorize]` perfil.
- **Errores**: login inválido → `CredencialesInvalidasException` → 401 uniforme (mismo mensaje para email inexistente/password incorrecto). Usuario inexistente llama `VerifyDummy`.
- **Bootstrap Admin**: `AuthBootstrapService` (`IHostedService`) en API, usa `IServiceScopeFactory` para servicios scoped. Variables `AuthBootstrap__Email`/`AuthBootstrap__Password`. Idempotente, falla claro si email colisiona con otro rol. Sin secretos en logs.
- **Migración EF**: solo esquema, nunca bootstrap/credenciales.
- **Fuera de alcance**: refresh/revocación/cambio password/gestión roles/#74 (migración `IActorActual`).

## Decisión

Se implementa lo descrito arriba siguiendo al pie el diseño aprobado en el issue #70. Puntos clave:

1. **Domain** (`Menus.Domain`):
   - `RolesUsuario` (constantes `Admin`, `Nutricionista`, `Paciente` + `EsValido`) — única fuente de verdad de roles.
   - `Usuario : Entity<Guid>` con factory `Crear` que normaliza email, valida reglas de `EntidadId` por rol.
   - `IPasswordHasher`, `ITokenEmitter` + `TokenEmitido` en `Abstractions.Security`.
   - `CredencialesInvalidasException` en `Excepciones`.
   - `IUsuarioRepository` en `Abstractions.Repositories`.

2. **Infrastructure** (`Menus.Infrastructure`):
   - `BCryptPasswordHasher` implementa `IPasswordHasher` (BCrypt.Net-Next, workFactor 12, `VerifyDummy` con `Lazy<string>`).
   - `JwtTokenService` implementa `ITokenEmitter` (método `Emitir` que devuelve `TokenEmitido`). Mantiene `GenerateToken` legacy `[Obsolete]`.
   - `JwtClaimTypes` añade `EntidadId = "entidad_id"`. `JwtClaimTypes.IsValid` reenvía a `RolesUsuario.EsValido`.
   - `UsuarioRepository`, `UsuarioConfiguration` (tabla, CHECKs, índices).
   - Migración `AddUsuariosTable`.
   - DI registra `IUsuarioRepository`, `IPasswordHasher`, `ITokenEmitter`.

3. **Application** (`Menus.Application`):
   - `IIdentidadAutenticada` (interfaz) en `Comun`.
   - Comandos/Queries: `LoginCommand`/`LoginResult`, `RegistrarNutricionistaCommand`, `RegistrarPacienteCommand`, `ObtenerPerfilQuery`/`PerfilDto`.
   - Validadores FluentValidation para cada comando.
   - Handlers: `LoginHandler` (usa `VerifyDummy` si usuario inexistente), `RegistrarNutricionistaHandler`/`RegistrarPacienteHandler` (precheck email, verifica FK especialidad/sala, un solo `CommitAsync`), `ObtenerPerfilHandler` (lee de `IIdentidadAutenticada`).

4. **API** (`Menus.Api`):
   - `AuthController` con endpoints `POST /api/auth/login`, `POST /api/auth/registro/nutricionista`, `POST /api/auth/registro/paciente`, `GET /api/auth/perfil`.
   - `IdentidadAutenticada` implementa `IIdentidadAutenticada` leyendo claims JWT.
   - `ManejadorDeExcepciones` mapea `CredencialesInvalidasException` → 401.
   - `AuthBootstrapService` (`IHostedService`) para Admin inicial.
   - DI registra `IIdentidadAutenticada`, `AuthBootstrapService`.

5. **Tests**:
   - Domain: `UsuarioTests`, `RolesUsuarioTests`.
   - Infrastructure: `BCryptPasswordHasherTests`, `JwtTokenServiceTests` (nuevos tests para `Emitir`/`ITokenEmitter`).
   - Application: `LoginHandlerTests`, `RegistrarNutricionistaHandlerTests`, `RegistrarPacienteHandlerTests`, `ObtenerPerfilHandlerTests` (fakes en memoria).
   - API: `ManejadorDeExcepcionesTests` añade test para `CredencialesInvalidasException`.
   - Integración (Testcontainers): `AuthEndpointsTests`, `UsuarioMigrationTests` (escritos, no ejecutables local sin Docker).

## Alternativas consideradas

| Alternativa | Razón de descarte |
|---|---|
| `citext` para email case-insensitive | El diseño exige email normalizado (lowercase+trim) en Domain y índice único simple; `citext` añade dependencia y no aporta valor si la normalización es determinista. |
| Doble índice (lower + simple) | Innecesario: la normalización garantiza unicidad case-insensitive con un solo índice. |
| FK polimórfica `EntidadId → nutricionistas/pacientes` | El informe y el diseño prohíben FK polimórfica; la integridad la garantiza el índice único filtrado + lógica de aplicación. |
| Hash dummy inválido / generado por request para `VerifyDummy` | Vulnerable a timing attacks o ineficiente. `Lazy<string>` con hash BCrypt válido generado una vez por proceso es la solución estándar. |
| `GenerateToken` legacy como API principal | Se reemplaza por `Emitir`/`ITokenEmitter` que devuelve `TokenEmitido` (token + expiración) para evitar recálculo de expiración en el llamador. |
| Enriquecer `IIdentidadAutenticada` con especialidad/jefe | Fuera de alcance (#74); el token ya lleva esos claims si el rol lo requiere. |
| Bootstrap en migración / endpoint / seed data | Inseguro: credenciales en código/migraciones. Variables de entorno + `IHostedService` scoped es el patrón seguro. |
| `IActorActual` migrado ya a JWT | Fuera de alcance (#74); `IIdentidadAutenticada` coexiste y no rompe `IActorActual`. |

## Consecuencias

- ✅ Autenticación completa end-to-end con JWT, BCrypt, roles, claims.
- ✅ 401 uniforme en login (timing-safe vía `VerifyDummy`).
- ✅ Bootstrap Admin seguro, idempotente, sin secretos en código/logs.
- ✅ Esquema BD con constraints declarativas (CHECK, índices únicos).
- ✅ Separación de capas: Domain define reglas, Infrastructure implementa crypto/JWT, Application orquesta casos de uso, API expone endpoints.
- ✅ Tests unitarios cubren reglas de dominio, handlers, crypto, JWT, mapeo de errores.
- ⚠️ Integración requiere Docker/Testcontainers (no corren local).
- ⚠️ `JwtTokenService.GenerateToken` marcado `[Obsolete]`; consumidores internos (tests legacy) siguen funcionando.
- ⚠️ `IIdentidadAutenticada` vs `IActorActual`: coexisten hasta #74.

## Referencias

- Issue #70: feat(app+api): autenticación — login y registro
- Issue #69: JWT base (ya en main)
- ADR-010: Capa de aplicación
- `docs/informe.md` §2.1 (actores, roles), §4 (restricciones)
- `docs/decisions/README.md` (índice ADRs)