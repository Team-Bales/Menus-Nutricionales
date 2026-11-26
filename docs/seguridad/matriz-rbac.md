# Matriz de autorización por roles (RBAC)

> Documento de trabajo para el §13.1 del informe. Se incorporará al
> informe cuando esa sección exista; hasta entonces vive aquí. La tabla se deriva del diccionario `Esperado` de
> `AutorizacionEnLaApiTests`, la fuente única de verdad de qué política tiene cada endpoint: ese test comprueba
> que el diccionario refleja los endpoints reales, y `MatrizDeAutorizacionTests` prueba los accesos negativos y
> que cada política admite a los roles que declara.

## Roles

| Rol | Descripción |
|---|---|
| Anónimo | Sin token. Solo `POST /api/auth/login` (y el documento OpenAPI en Development). |
| Paciente | Consulta lo suyo, sus valoraciones y solicita revalorizaciones. |
| Nutricionista | Gestiona el banco, los menús, los pacientes y las valoraciones; valida menús si es jefe. |
| Admin | Administra dietas, nomencladores, salas y usuarios. No actúa como nutricionista. |

## Políticas

| Política | Roles que la cumplen |
|---|---|
| `SoloAdmin` | Admin |
| `SoloNutricionista` | Nutricionista |
| `SoloPaciente` | Paciente |
| `NutricionistaOAdmin` | Nutricionista o Admin |
| `PacientePropioOPersonal` | Paciente dueño, Nutricionista o Admin |
| (reserva) | Cualquier rol autenticado |
| `[AllowAnonymous]` | Sin token |

## Resumen recurso–acción

Vista de alto nivel para el §13.1 del informe. Agrupa las acciones del detalle por endpoint; donde una acción
admite variantes, «·» indica que depende del recurso concreto (por ejemplo, el paciente solo ve lo suyo).

Sin token, todo responde 401 salvo el login (y el OpenAPI en Development). Leyenda: ✓ permitido; ✗ 403;
· depende del recurso.

| Recurso | Acción | Paciente | Nutricionista | Admin |
|---|---|---|---|---|
| Autenticación | Iniciar sesión | ✓ | ✓ | ✓ |
| Autenticación | Consultar el perfil | ✓ | ✓ | ✓ |
| Usuario | Registrar nutricionista o paciente | ✗ | ✗ | ✓ |
| Alimento | Leer | ✓ | ✓ | ✓ |
| Alimento | Crear, editar o borrar | ✗ | ✓ | ✗ |
| Plato | Leer | ✓ | ✓ | ✓ |
| Plato | Crear, editar o borrar | ✗ | ✓ | ✗ |
| Dieta | Leer | ✓ | ✓ | ✓ |
| Dieta | Crear, editar o borrar | ✗ | ✗ | ✓ |
| Menú | Generar, listar, validar, reemplazar o asignar | ✗ | ✓ | ✗ |
| Paciente | Gestionar | ✗ | ✓ | ✓ |
| Paciente | Ver sus menús, consumos o valoraciones | · | ✓ | ✓ |
| Sala | Leer | ✗ | ✓ | ✓ |
| Sala | Crear, editar o borrar | ✗ | ✗ | ✓ |
| Nomenclador | Leer | ✓ | ✓ | ✓ |
| Nomenclador | Gestionar | ✗ | ✗ | ✓ |
| Nutricionista | Leer una ficha | ✗ | · | ✓ |
| Nutricionista | Gestionar | ✗ | ✗ | ✓ |
| Valoración | Registrar | ✗ | ✓ | ✗ |
| Valoración | Leer | · | ✓ | ✓ |
| Revalorización | Solicitar | ✓ | ✗ | ✗ |
| Revalorización | Leer | · | ✓ | ✓ |
| Revalorización | Resolver | ✗ | ✓ | ✗ |
| Reporte | Consultar (C1–C6) | ✗ | ✓ | ✓ |

## Detalle por endpoint

Cada fila es un endpoint y su política exacta.

Leyenda: ✓ permitido; ✗ 403; — 401 (sin token); · depende de los datos.

### Autenticación

| Endpoint | Anónimo | Paciente | Nutricionista | Admin | Política |
|---|---|---|---|---|---|
| `POST /api/auth/login` | ✓ | ✓ | ✓ | ✓ | Anónimo |
| `POST /api/auth/registro/nutricionista` | — | ✗ | ✗ | ✓ | SoloAdmin |
| `POST /api/auth/registro/paciente` | — | ✗ | ✗ | ✓ | SoloAdmin |
| `GET /api/auth/perfil` | — | ✓ | ✓ | ✓ | Autenticado |

### Alimentos

| Endpoint | Anónimo | Paciente | Nutricionista | Admin | Política |
|---|---|---|---|---|---|
| `GET /api/alimentos` | — | ✓ | ✓ | ✓ | Autenticado |
| `GET /api/alimentos/{id}` | — | ✓ | ✓ | ✓ | Autenticado |
| `POST /api/alimentos` | — | ✗ | ✓ | ✗ | SoloNutricionista |
| `PUT /api/alimentos/{id}` | — | ✗ | ✓ | ✗ | SoloNutricionista |
| `DELETE /api/alimentos/{id}` | — | ✗ | ✓ | ✗ | SoloNutricionista |
| `GET /api/alimentos/{id}/ficha` | — | ✓ | ✓ | ✓ | Autenticado |
| `PUT /api/alimentos/{id}/ficha` | — | ✗ | ✓ | ✗ | SoloNutricionista |

> El Admin no crea ni modifica alimentos: no tiene un nutricionista asociado en el token. `POST`/`PUT` además
> exige que el actor sea quien describió el ítem (RS-02, en el caso de uso). También para escribir la ficha
> nutricional (`PUT .../ficha`, #283, ADR-012).

### Platos

| Endpoint | Anónimo | Paciente | Nutricionista | Admin | Política |
|---|---|---|---|---|---|
| `GET /api/platos` | — | ✓ | ✓ | ✓ | Autenticado |
| `GET /api/platos/{id}` | — | ✓ | ✓ | ✓ | Autenticado |
| `POST /api/platos` | — | ✗ | ✓ | ✗ | SoloNutricionista |
| `PUT /api/platos/{id}` | — | ✗ | ✓ | ✗ | SoloNutricionista |
| `DELETE /api/platos/{id}` | — | ✗ | ✓ | ✗ | SoloNutricionista |
| `POST /api/platos/{id}/ingredientes` | — | ✗ | ✓ | ✗ | SoloNutricionista |
| `DELETE /api/platos/{id}/ingredientes/{alimentoId}` | — | ✗ | ✓ | ✗ | SoloNutricionista |
| `GET /api/platos/{id}/ficha` | — | ✓ | ✓ | ✓ | Autenticado |
| `PUT /api/platos/{id}/ficha` | — | ✗ | ✓ | ✗ | SoloNutricionista |

### Dietas

| Endpoint | Anónimo | Paciente | Nutricionista | Admin | Política |
|---|---|---|---|---|---|
| `GET /api/dietas` | — | ✓ | ✓ | ✓ | Autenticado |
| `GET /api/dietas/{id}` | — | ✓ | ✓ | ✓ | Autenticado |
| `GET /api/dietas/{id}/disponibilidad-calorica` | — | ✓ | ✓ | ✓ | Autenticado |
| `POST /api/dietas` | — | ✗ | ✗ | ✓ | SoloAdmin |
| `PUT /api/dietas/{id}` | — | ✗ | ✗ | ✓ | SoloAdmin |
| `DELETE /api/dietas/{id}` | — | ✗ | ✗ | ✓ | SoloAdmin |
| `POST /api/dietas/{id}/grupos` | — | ✗ | ✗ | ✓ | SoloAdmin |
| `DELETE /api/dietas/{id}/grupos/{grupoId}` | — | ✗ | ✗ | ✓ | SoloAdmin |
| `POST /api/dietas/{id}/restricciones` | — | ✗ | ✗ | ✓ | SoloAdmin |
| `DELETE /api/dietas/{id}/restricciones/{restriccionId}` | — | ✗ | ✗ | ✓ | SoloAdmin |

### Menús

| Endpoint | Anónimo | Paciente | Nutricionista | Admin | Política |
|---|---|---|---|---|---|
| `POST /api/dietas/{dietaId}/menus/generacion-automatica` | — | ✗ | ✓ | ✗ | SoloNutricionista |
| `GET /api/menus` | — | ✗ | ✓ | ✗ | SoloNutricionista |
| `GET /api/menus/{id}` | — | ✗ | ✓ | ✗ | SoloNutricionista |
| `POST /api/menus/{id}/validar` | — | ✗ | ✓ | ✗ | SoloNutricionista |
| `POST /api/menus/{id}/reemplazo` | — | ✗ | ✓ | ✗ | SoloNutricionista |
| `POST /api/menus/{id}/asignaciones` | — | ✗ | ✓ | ✗ | SoloNutricionista |
| `POST /api/menus/{id}/asignaciones/{pacienteId}/servicios` | — | ✗ | ✓ | ✗ | SoloNutricionista |

> Generar exige estar autorizado para la dieta (RF-06); validar exige además ser jefe (RS-03) y no ser el
> creador (RS-05). Lo comprueban los casos de uso.

### Pacientes

| Endpoint | Anónimo | Paciente | Nutricionista | Admin | Política |
|---|---|---|---|---|---|
| `GET /api/pacientes` | — | ✗ | ✓ | ✓ | NutricionistaOAdmin |
| `GET /api/pacientes/{id}` | — | ✗ | ✓ | ✓ | NutricionistaOAdmin |
| `PUT /api/pacientes/{id}` | — | ✗ | ✓ | ✓ | NutricionistaOAdmin |
| `POST /api/pacientes/{id}/dietas` | — | ✗ | ✓ | ✓ | NutricionistaOAdmin |
| `DELETE /api/pacientes/{id}/dietas/{dietaId}` | — | ✗ | ✓ | ✓ | NutricionistaOAdmin |
| `GET /api/pacientes/{id}/menus` | — | · | ✓ | ✓ | PacientePropioOPersonal |
| `GET /api/pacientes/{id}/consumos` | — | · | ✓ | ✓ | PacientePropioOPersonal |

> En `menus` y `consumos`, el Paciente solo pasa si el `entidad_id` del token coincide con el `id` de la
> ruta; si no, 403 (#285).

### Salas

| Endpoint | Anónimo | Paciente | Nutricionista | Admin | Política |
|---|---|---|---|---|---|
| `GET /api/salas` | — | ✗ | ✓ | ✓ | NutricionistaOAdmin |
| `GET /api/salas/{id}` | — | ✗ | ✓ | ✓ | NutricionistaOAdmin |
| `POST /api/salas` | — | ✗ | ✗ | ✓ | SoloAdmin |
| `PUT /api/salas/{id}` | — | ✗ | ✗ | ✓ | SoloAdmin |
| `DELETE /api/salas/{id}` | — | ✗ | ✗ | ✓ | SoloAdmin |

### Nomencladores

| Endpoint | Anónimo | Paciente | Nutricionista | Admin | Política |
|---|---|---|---|---|---|
| `GET /api/nomencladores/tipos-preparacion` | — | ✓ | ✓ | ✓ | Autenticado |
| `GET /api/nomencladores/niveles-caloricos` | — | ✓ | ✓ | ✓ | Autenticado |
| `GET /api/nomencladores/grupos-nutricionales` | — | ✓ | ✓ | ✓ | Autenticado |
| `POST /api/nomencladores/grupos-nutricionales` | — | ✗ | ✗ | ✓ | SoloAdmin |
| `DELETE /api/nomencladores/grupos-nutricionales/{id}` | — | ✗ | ✗ | ✓ | SoloAdmin |
| `GET /api/nomencladores/restricciones-alimentarias` | — | ✓ | ✓ | ✓ | Autenticado |
| `POST /api/nomencladores/restricciones-alimentarias` | — | ✗ | ✗ | ✓ | SoloAdmin |
| `DELETE /api/nomencladores/restricciones-alimentarias/{id}` | — | ✗ | ✗ | ✓ | SoloAdmin |
| `GET /api/nomencladores/programas-atencion` | — | ✓ | ✓ | ✓ | Autenticado |
| `POST /api/nomencladores/programas-atencion` | — | ✗ | ✗ | ✓ | SoloAdmin |
| `DELETE /api/nomencladores/programas-atencion/{id}` | — | ✗ | ✗ | ✓ | SoloAdmin |
| `GET /api/nomencladores/especialidades` | — | ✓ | ✓ | ✓ | Autenticado |
| `POST /api/nomencladores/especialidades` | — | ✗ | ✗ | ✓ | SoloAdmin |
| `DELETE /api/nomencladores/especialidades/{id}` | — | ✗ | ✗ | ✓ | SoloAdmin |

### Nutricionistas

| Endpoint | Anónimo | Paciente | Nutricionista | Admin | Política |
|---|---|---|---|---|---|
| `GET /api/nutricionistas` | — | ✗ | ✗ | ✓ | SoloAdmin |
| `GET /api/nutricionistas/{id}` | — | ✗ | · | ✓ | Autenticado |
| `PUT /api/nutricionistas/{id}` | — | ✗ | ✗ | ✓ | SoloAdmin |
| `POST /api/nutricionistas/{id}/dietas` | — | ✗ | ✗ | ✓ | SoloAdmin |
| `DELETE /api/nutricionistas/{id}/dietas/{dietaId}` | — | ✗ | ✗ | ✓ | SoloAdmin |
| `POST /api/nutricionistas/{id}/rol-jefe` | — | ✗ | ✗ | ✓ | SoloAdmin |

> `GET /api/nutricionistas/{id}`: cualquier autenticado llega, pero el caso de uso devuelve 403 si no es
> Admin ni el propio nutricionista (`ObtenerNutricionistaHandler`).

### Valoraciones

| Endpoint | Anónimo | Paciente | Nutricionista | Admin | Política |
|---|---|---|---|---|---|
| `POST /api/valoraciones` | — | ✗ | ✓ | ✗ | SoloNutricionista |
| `GET /api/valoraciones/{id}` | — | · | ✓ | ✓ | Autenticado |
| `GET /api/dietas/{dietaId}/valoraciones` | — | ✗ | ✓ | ✓ | NutricionistaOAdmin |
| `GET /api/pacientes/{id}/valoraciones` | — | · | ✓ | ✓ | PacientePropioOPersonal |

> El detalle (`GET /api/valoraciones/{id}`) es para el paciente valorado y el personal; la ruta lleva el id de
> la valoración y no el del paciente, así que lo comprueba `ObtenerValoracionHandler`. En la lista por paciente,
> el Paciente solo pasa si es el suyo (`PacientePropioOPersonal`, #285).

### Revalorizaciones

| Endpoint | Anónimo | Paciente | Nutricionista | Admin | Política |
|---|---|---|---|---|---|
| `POST /api/revalorizaciones` | — | ✓ | ✗ | ✗ | SoloPaciente |
| `GET /api/revalorizaciones/{id}` | — | · | ✓ | ✓ | Autenticado |
| `POST /api/revalorizaciones/{id}/resultado` | — | ✗ | ✓ | ✗ | SoloNutricionista |
| `GET /api/nutricionistas/{nutricionistaId}/revalorizaciones/pendientes` | — | ✗ | ✓ | ✗ | SoloNutricionista |

> `GET /api/revalorizaciones/{id}`: cualquier autenticado llega, pero el caso de uso devuelve 403 si es un
> paciente ajeno al de la valoración revisada (`ObtenerRevalorizacionHandler`, #267).

### Reportes

| Endpoint | Anónimo | Paciente | Nutricionista | Admin | Política |
|---|---|---|---|---|---|
| `GET /api/reportes/menus-automaticos` | — | ✗ | ✓ | ✓ | NutricionistaOAdmin |
| `GET /api/reportes/alimentos-mas-usados` | — | ✗ | ✓ | ✓ | NutricionistaOAdmin |
| `GET /api/reportes/dictamenes` | — | ✗ | ✓ | ✓ | NutricionistaOAdmin |
| `GET /api/reportes/desempeno-menu` | — | ✗ | ✓ | ✓ | NutricionistaOAdmin |
| `GET /api/reportes/comparacion-dietas` | — | ✗ | ✓ | ✓ | NutricionistaOAdmin |
| `GET /api/reportes/correlacion-nutricional` | — | ✗ | ✓ | ✓ | NutricionistaOAdmin |

> Los reportes son de solo lectura y no modifican estado; su acceso se restringe a roles con visión analítica (Nutricionista/Admin).

## Reglas dependientes de datos

Algunas reglas de autorización dependen de los datos y viven en los casos de uso, que devuelven 403 con
`AccesoDenegadoException`:

| Regla | Caso de uso | Descripción |
|---|---|---|
| RS-02 | `ActualizarAlimentoHandler`, `ActualizarPlatoHandler`, `AgregarIngredienteHandler`, `QuitarIngredienteHandler` | Un nutricionista solo clasifica los ítems que él mismo describió. |
| RS-03 | `GenerarMenuAutomaticamenteHandler`, `ValidarMenuHandler` | El nutricionista debe estar autorizado para la dieta; para validar, además, debe ser jefe. |
| RS-05 | `ValidarMenuHandler` | Un nutricionista no valida un menú que él mismo creó. |
| RS-09 | `SolicitarRevalorizacionHandler` | Un paciente solo solicita la revalorización de su propia valoración. |
| — | `ObtenerNutricionistaHandler` | Solo el Admin o el propio nutricionista. |
| — | `ObtenerRevalorizacionHandler` | Solo el personal o el paciente de la valoración revisada. |
| — | `ObtenerValoracionHandler` | Solo el paciente valorado o el personal. |

Todas se traducen a 403 en `ManejadorDeExcepciones`.

## Cobertura

`tests/Menus.Api.Tests/Seguridad/MatrizDeAutorizacionTests.cs` verifica:

- **Acceso negativo por rol:** cada endpoint con política de rol de `Esperado` recibe 403 con los roles que
  no la cumplen (96 casos con los tres roles actuales). Los casos se derivan de `Esperado` — la fuente única
  de verdad —, así que agregar un endpoint en `Esperado` con una política de rol produce sus casos
  automáticamente. Los roles se leen de `RolesUsuario`, así que un rol nuevo también se prueba sin tocar la
  matriz.
- **Sincronía:** un test verifica que toda entrada de `Esperado` con política de rol tiene casos negativos, o
  está entre las tres políticas ya cubiertas por `AutorizacionEnLaApiTests` (Anónimo, Autenticado,
  PacientePropioOPersonal). Si alguien agrega una política nueva sin mapearla, falla. Otro test comprueba que
  los roles alcanzados existen en el dominio y que cada política mapeada excluye al menos un rol.
- **Acceso positivo por rol:** un test resuelve cada política registrada y comprueba que sus requisitos de rol
  admiten exactamente los roles de `Alcanzan`. Así, si una política deja fuera a un rol permitido, falla sin
  necesidad de base de datos ni de peticiones HTTP.

> **Mantenimiento:** la tabla se deriva de `Esperado`; al cambiar un endpoint o una política, actualizar este
> documento y ejecutar `MatrizDeAutorizacionTests`. La cobertura negativa se genera sola, pero los cambios de
> política y de ruta deben reflejarse aquí.

Fuera de este archivo, ya cubierto por otros tests:

- **Sin token (401):** `AutorizacionEnLaApiTests.Sin_token_responde_401_con_ProblemDetails_y_WWW_Authenticate`.
- **Paciente ajeno (403):** `AutorizacionEnLaApiTests.Un_paciente_que_pide_los_menus_de_otro_recibe_403`,
  `..._el_historico_de_consumo_de_otro_recibe_403` y `..._las_valoraciones_de_otro_recibe_403`.
- **403 que dependen de los datos (RS-02, RS-03, RS-05, RS-09):** necesitan base de datos y viven en
  `Menus.Application.Tests` — por ejemplo `ManejadoresDeRevalorizacionesTests`,
  `ManejadoresDeNutricionistasTests` y `ManejadoresDeValoracionesTests`.