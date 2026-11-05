# ADR-010: Capa de aplicación: casos de uso, validación, errores y transacción

**Estado:** Aceptado  
**Fecha:** 2026-10-31 (propuesto, PR #216); aceptado el 2026-11-04

## Contexto

`Menus.Application` está vacía, y los issues de v0.2 en adelante (#61 a #107) escriben en ella sus casos de uso. Si cada uno resuelve a su manera cómo se organiza un caso de uso, dónde se valida, cómo se traduce un error a HTTP o quién es el usuario que actúa, la capa queda inconsistente y cada endpoint se comporta distinto ante el mismo error.

El dominio y la base ya están completos y fijan parte de la respuesta:
- **El dominio** protege sus invariantes lanzando `ArgumentException` (dato inválido) e `InvalidOperationException` (regla violada): 57 en total.
- **La base** protege lo que cruza agregados con FK, `CHECK`, índices únicos y triggers. Los triggers lanzan su error con el código de la regla al principio del mensaje («RS-03: …»), y los diferidos saltan al confirmar la transacción.
- **El ADR-004** ya decidió repositorios por agregado y una `IUnitOfWork` que el caso de uso confirma una sola vez.

Los issues piden además respuestas concretas: 201 al crear, 409 ante un nombre duplicado o un borrado en uso, 422 ante un dato inválido, y el sobre `{ data, total, page, size }` en los listados (#61 a #64).

Este ADR fija esas convenciones para toda la capa. Recoge las decisiones D3, D6 y D7 de la revisión del backlog.

## Decisión

### 1. Un caso de uso es un comando o una consulta con su manejador

- **Cada caso de uso es un `record` inmutable,** comando o consulta, con su manejador. Los nombres siguen los de los issues: `RegistrarAlimentoCommand` y `RegistrarAlimentoHandler`, `ListarAlimentosQuery` y `ListarAlimentosHandler`.
  - Los comandos cambian estado; las consultas solo leen. Es separación de comandos y consultas (CQS), sin dos modelos de datos.
- **Abstracciones en `Menus.Application/Comun`:**
  - `ICommand<TResultado>` y `IQuery<TResultado>`;
  - `IHandler<TSolicitud, TResultado>`, con `Task<TResultado> HandleAsync(TSolicitud solicitud, CancellationToken ct)`;
  - `IDespachador`, con `Task<TResultado> EnviarAsync<TResultado>(…)`.
- **El despachador** busca el manejador en la inyección de dependencias, ejecuta antes los validadores del comando (§3) y después el manejador. Los controladores solo hablan con el despachador. Es el patrón *Mediator*, con una implementación propia de unas pocas decenas de líneas.
- **Organización por módulo y caso de uso:** `Catalogo/Alimentos/RegistrarAlimento/` contiene el comando, el manejador y su validador. Lo común va en `Comun/`.
- **Registro:** `AddApplication()` registra por reflexión los manejadores y los validadores del ensamblado. Así un caso de uso nuevo no exige tocar la configuración.

### 2. Los manejadores devuelven DTO, nunca entidades

- La API no expone las entidades del dominio. Cada caso de uso devuelve un `record` propio (`AlimentoDto`, `IdCreadoDto`…), y el mapeo se escribe a mano en el manejador.
- **Los listados** devuelven `ResultadoPaginado<T>` con `Data`, `Total`, `Page` y `Size`, que es el sobre que piden los issues.

### 3. La validación, en tres niveles

| Nivel | Qué valida | Dónde | Respuesta |
|---|---|---|---|
| **Forma de la entrada** | Vacíos, rangos, formatos, identificadores de catálogo (`TipoPreparacion.Existe`) | Un validador de FluentValidation por comando, que ejecuta el despachador antes del manejador | 422, con los errores por campo |
| **Reglas del agregado** | Lo que la entidad garantiza siempre (RK-03, RK-07, RS-02, RS-05…) | El dominio, que lanza su excepción | 409 o 422 (§4) |
| **Integridad entre agregados** | FK, unicidad, triggers | La base | 409 o 422 (§4) |

- **El manejador comprueba antes lo que depende de otros agregados o del usuario,** para dar un mensaje claro en lugar de esperar al error de la base:
  - el nombre duplicado entre alimentos y platos (`ExisteNombreAsync`, RC-01);
  - que quien dictamina sea jefe y esté autorizado (RS-03);
  - que el nutricionista esté autorizado para la dieta del menú.

  La base sigue siendo la última barrera.

### 4. Los errores se traducen en un solo lugar, a `ProblemDetails`

- **Un `IExceptionHandler` global en `Menus.Api`** convierte las excepciones en respuestas `ProblemDetails` (RFC 9457).
- **El cuerpo lleva dos campos propios,** además de los estándar:
  - `regla`: el código del informe, cuando se conoce (por ejemplo «RS-03»);
  - `errores`: los errores de validación por campo.
- **Ningún error 500 expone detalles internos.**

| Origen | Señal | HTTP |
|---|---|---|
| Validación de la entrada | `ValidationException` de FluentValidation | 422 |
| Recurso inexistente | `NoEncontradoException` (de `Menus.Application`) | 404 |
| Regla del dominio | `ReglaDeNegocioException` (de `Menus.Domain`, ver abajo) | 409 |
| Dato inválido detectado por el dominio | `DatoInvalidoException` (de `Menus.Domain`) | 422 |
| PostgreSQL: unicidad | `23505` | 409 |
| PostgreSQL: FK (en uso, o no existe) | `23503` | 409 |
| PostgreSQL: `CHECK` | `23514` | 422 |
| PostgreSQL: trigger | `P0001`; el código de la regla es lo que el mensaje tiene antes de «:» | 409 |
| Cualquier otra | — | 500 |

**Las dos excepciones del dominio son la parte de esta decisión que cambia código existente.** Hoy el dominio lanza `InvalidOperationException` y `ArgumentException`, que también lanza el framework cuando hay un error de programación. Traducirlas sin más a 409 y 422 convertiría un error del programa en una respuesta «de negocio». Por ejemplo, una consulta que EF no puede traducir lanza `InvalidOperationException`, y el cliente vería un 409 en lugar de un 500.

El dominio lanza excepciones propias:
- una base, `ExcepcionDeDominio`;
- `ReglaDeNegocioException`, para una regla violada;
- `DatoInvalidoException`, para un dato inválido.

Las dos llevan opcionalmente el código de la regla. Es un cambio mecánico, en un PR aparte: los 57 `throw` del dominio y los `Assert.Throws` de sus tests.

### 5. Un manejador, una transacción

- **Siguiendo el ADR-004,** el manejador prepara los cambios con los repositorios y llama a `IUnitOfWork.CommitAsync()` una sola vez, al final.
- **Los triggers diferidos** (RK-03, RK-07, RK-09, RS-07, RS-14 y RS-16) saltan en ese `CommitAsync`. Su error llega por el mismo camino de §4.
- **Lo que no está en PostgreSQL va después de confirmar:** la ficha en MongoDB (ADR-005, ADR-009) y la invalidación de la caché en Redis. Así no se invalida ni se escribe nada por un cambio que no llegó a confirmarse.

### 6. Quién actúa: `IActorActual`

- **Varios casos de uso necesitan saber quién los ejecuta:** el descriptor de un ítem (RF-01, RS-02), el creador de un menú y el revisor de un dictamen. Los manejadores lo obtienen de `IActorActual` (definida en `Menus.Application`, implementada en `Menus.Api`), nunca de un campo del comando que el cliente pueda inventar.
- **Mientras no haya autenticación (v0.2),** la implementación lee el identificador de una cabecera (`X-Actor-Id`). Desde el #74, lo lee del token. Los manejadores no cambian.

### 7. Duplicados: idempotencia en los vínculos, 409 en las entidades

- **Los vínculos son idempotentes:** autorizar, inscribir, agregar un grupo o una restricción a una dieta, o excluir un grupo. Si ya existe, el manejador responde éxito sin cambios. El dominio lanza una excepción ante el duplicado, así que el manejador comprueba antes.
- **Una entidad con nombre duplicado** (ítem, nomenclador) **devuelve 409.**

### 8. La API usa controladores

- Los issues ya los nombran (`NomencladorController`, `[Authorize]` en el #74), y dejan cada recurso en una clase con sus rutas.
- Un controlador recibe la solicitud, arma el comando o la consulta, lo envía al despachador y devuelve el código que corresponde (201 con `Location` al crear, 200 o 204 en lo demás).

### 9. Pruebas

- **Los manejadores se prueban en `Menus.Application.Tests`** con repositorios falsos escritos a mano (en memoria), sin EF Core: el manejador se prueba solo, y el dominio ya tiene sus propias pruebas.
- **Lo que depende de la base** (SQL, triggers, el mapeo de errores de PostgreSQL) **se prueba con Testcontainers** (#210).

### Paquetes

- **Se agregan** `FluentValidation` y `FluentValidation.DependencyInjectionExtensions` (licencia Apache 2.0) en `Menus.Application`.
- **No se usan MediatR ni AutoMapper.** Las versiones nuevas de los dos pasaron a un modelo de licencia comercial (2025). Para lo que se necesita alcanza con el despachador propio y el mapeo a mano, que además son más fáciles de explicar.

## Alternativas consideradas

| Alternativa | Razón de descarte |
|---|---|
| MediatR (pipeline de validación incluido) | Depende de una biblioteca que cambió de licencia, y su comportamiento es menos visible que unas pocas clases propias |
| Inyectar los manejadores directamente en los controladores | Sin un punto común, la validación y cualquier otro paso transversal se repiten en cada controlador |
| Validar solo con DataAnnotations en los DTO de la API | No llega a los comandos que no vienen de HTTP, y las reglas que dependen de otro dato quedan incómodas |
| Traducir `InvalidOperationException` y `ArgumentException` directamente a 409 y 422 | Confunde errores de programación con reglas de negocio (ver §4) |
| Clasificar las excepciones por el ensamblado que las lanzó | Evita tocar el dominio, pero depende de inspeccionar la pila de llamadas: frágil y difícil de explicar |
| Devolver un `Result<T>` en lugar de lanzar excepciones | El dominio ya usa excepciones; mezclar los dos estilos complica más de lo que aporta |
| Minimal APIs en lugar de controladores | Son igual de válidas, pero los issues ya están escritos en términos de controladores |
| Pasar el identificador del actor en el cuerpo del comando | El cliente podría decir que es otro nutricionista, y con la autenticación habría que cambiar todos los comandos |

## Consecuencias

- ✅ Todos los casos de uso tienen la misma forma, y cada error llega al cliente con el mismo formato y el mismo código, venga del dominio, de la base o de la validación.
- ✅ Los manejadores se prueban sin base de datos, y el paso a autenticación (#74) no los toca.
- ✅ Suma dos patrones explícitos para el checklist de IS (#96): *Mediator* (el despachador) y *Command*.
- ⚠️ Hay que hacer, antes del primer caso de uso:
  - el cambio de las excepciones del dominio (§4), en su propio PR;
  - las abstracciones de `Comun/`;
  - el `IExceptionHandler`;
  - una implementación de `IUnitOfWork`, que hoy no existe (estaba prevista en el #60).
- ⚠️ El mapeo a DTO es código manual, uno por caso de uso.
