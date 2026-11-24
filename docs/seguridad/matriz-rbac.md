# Matriz de Control de Acceso Basado en Roles (RBAC)

Este documento describe la matriz de autorización de la API, indicando qué roles pueden acceder a cada endpoint.

## Roles del Sistema

| Role | Descripción |
|------|-------------|
| `Admin` | Administrador del sistema. Acceso total a gestión de usuarios, dietas, nomencladores y configuración. |
| `Nutricionista` | Profesional de nutrición. Puede crear/validar menús, describir alimentos/platos, registrar valoraciones. |
| `Paciente` | Paciente del sistema. Acceso a sus propios menús, consumos, valoraciones y solicitud de revalorizaciones. |

## Políticas de Autorización

| Política | Roles | Descripción |
|----------|-------|-------------|
| `SoloAdmin` | `Admin` | Solo administradores. |
| `SoloNutricionista` | `Nutricionista` | Solo nutricionistas (no administradores). |
| `SoloPaciente` | `Paciente` | Solo pacientes. |
| `NutricionistaOAdmin` | `Nutricionista`, `Admin` | Nutricionistas y administradores. |
| `PacientePropioOPersonal` | `Paciente` (solo suyo), `Nutricionista`, `Admin` | El paciente ve lo suyo; el personal ve todo. |
| `Autenticado` | Cualquiera autenticado | Política de reserva: cualquier usuario con token válido. |
| `Anónimo` | Sin autenticar | Solo login y registro público. |

## Matriz de Endpoints

### Autenticación y Perfil

| Endpoint | Política |
|----------|----------|
| `POST api/auth/login` | Anónimo |
| `POST api/auth/registro/nutricionista` | SoloAdmin |
| `POST api/auth/registro/paciente` | SoloAdmin |
| `GET api/auth/perfil` | Autenticado |

### Alimentos y Platos

| Endpoint | Política |
|----------|----------|
| `GET api/alimentos` | Autenticado |
| `GET api/alimentos/{id:guid}` | Autenticado |
| `POST api/alimentos` | SoloNutricionista |
| `PUT api/alimentos/{id:guid}` | SoloNutricionista |
| `DELETE api/alimentos/{id:guid}` | SoloNutricionista |
| `GET api/platos` | Autenticado |
| `GET api/platos/{id:guid}` | Autenticado |
| `POST api/platos` | SoloNutricionista |
| `PUT api/platos/{id:guid}` | SoloNutricionista |
| `DELETE api/platos/{id:guid}` | SoloNutricionista |
| `POST api/platos/{id:guid}/ingredientes` | SoloNutricionista |
| `DELETE api/platos/{id:guid}/ingredientes/{alimentoId:guid}` | SoloNutricionista |

### Dietas y Nomencladores

| Endpoint | Política |
|----------|----------|
| `GET api/dietas` | Autenticado |
| `GET api/dietas/{id:guid}` | Autenticado |
| `GET api/dietas/{id:guid}/disponibilidad-calorica` | Autenticado |
| `POST api/dietas` | SoloAdmin |
| `PUT api/dietas/{id:guid}` | SoloAdmin |
| `DELETE api/dietas/{id:guid}` | SoloAdmin |
| `POST api/dietas/{id:guid}/grupos` | SoloAdmin |
| `DELETE api/dietas/{id:guid}/grupos/{grupoId:guid}` | SoloAdmin |
| `POST api/dietas/{id:guid}/restricciones` | SoloAdmin |
| `DELETE api/dietas/{id:guid}/restricciones/{restriccionId:guid}` | SoloAdmin |
| `GET api/nomencladores/*` (lectura) | Autenticado |
| `POST/DELETE api/nomencladores/*` (escritura) | SoloAdmin |

### Pacientes y Salas

| Endpoint | Política |
|----------|----------|
| `GET api/pacientes` | NutricionistaOAdmin |
| `GET api/pacientes/{id:guid}` | NutricionistaOAdmin |
| `PUT api/pacientes/{id:guid}` | NutricionistaOAdmin |
| `POST api/pacientes/{id:guid}/dietas` | NutricionistaOAdmin |
| `DELETE api/pacientes/{id:guid}/dietas/{dietaId:guid}` | NutricionistaOAdmin |
| `GET api/pacientes/{id:guid}/menus` | PacientePropioOPersonal |
| `GET api/pacientes/{id:guid}/consumos` | PacientePropioOPersonal |
| `GET api/salas` | NutricionistaOAdmin |
| `GET api/salas/{id:guid}` | NutricionistaOAdmin |
| `POST/PUT/DELETE api/salas` | SoloAdmin |

### Nutricionistas

| Endpoint | Política |
|----------|----------|
| `GET api/nutricionistas` | SoloAdmin |
| `GET api/nutricionistas/{id:guid}` | Autenticado |
| `PUT api/nutricionistas/{id:guid}` | SoloAdmin |
| `POST/DELETE api/nutricionistas/{id:guid}/dietas` | SoloAdmin |
| `POST api/nutricionistas/{id:guid}/rol-jefe` | SoloAdmin |

### Menús

| Endpoint | Política |
|----------|----------|
| `POST api/dietas/{dietaId:guid}/menus/generacion-automatica` | SoloNutricionista |
| `GET api/menus` | SoloNutricionista |
| `GET api/menus/{id:guid}` | SoloNutricionista |
| `POST api/menus/{id:guid}/validar` | SoloNutricionista |
| `POST api/menus/{id:guid}/reemplazo` | SoloNutricionista |
| `POST api/menus/{id:guid}/asignaciones` | SoloNutricionista |
| `POST api/menus/{id:guid}/asignaciones/{pacienteId:guid}/servicios` | SoloNutricionista |

### Valoraciones y Revalorizaciones

| Endpoint | Política |
|----------|----------|
| `POST api/valoraciones` | SoloNutricionista |
| `GET api/valoraciones/{id:guid}` | Autenticado |
| `GET api/dietas/{dietaId:guid}/valoraciones` | NutricionistaOAdmin |
| `GET api/pacientes/{id:guid}/valoraciones` | PacientePropioOPersonal |
| `POST api/revalorizaciones` | SoloPaciente |
| `GET api/revalorizaciones/{id:guid}` | Autenticado |
| `POST api/revalorizaciones/{id:guid}/resultado` | SoloNutricionista |
| `GET api/nutricionistas/{nutricionistaId:guid}/revalorizaciones/pendientes` | SoloNutricionista |

### Reportes

| Endpoint | Política | Descripción |
|----------|----------|-------------|
| `GET api/reportes/menus-automaticos` | NutricionistaOAdmin | Menús automáticos por dieta (C1) |
| `GET api/reportes/alimentos-mas-usados` | NutricionistaOAdmin | Alimentos más usados en menús finales (C2) |
| `GET api/reportes/dictamenes` | NutricionistaOAdmin | Dictámenes de un revisor (C3) |
| `GET api/reportes/desempeno-menu` | NutricionistaOAdmin | Desempeño ante un menú (C4) |
| `GET api/reportes/comparacion-dietas` | NutricionistaOAdmin | Comparación entre dietas (C5) |
| `GET api/reportes/correlacion-nutricional` | NutricionistaOAdmin | Correlación nutricional (C6) |

## Códigos de Respuesta de Autorización

| Código | Significado | Detalle en ProblemDetails |
|--------|-------------|---------------------------|
| `401 Unauthorized` | Sin token o token inválido/expirado | `NoAutenticado` |
| `403 Forbidden` | Token válido pero rol insuficiente | `SinPermisos` |

## Notas

- La política de reserva (`Autenticado`) se aplica a cualquier endpoint que no declare una política explícita.
- Las reglas de negocio adicionales (ej. nutricionista autorizado para la dieta, paciente inscrito) se validan en la capa de aplicación, no en la política de autorización.
- Los reportes son de solo lectura y no modifican estado; su acceso se restringe a roles con visión analítica (Nutricionista/Admin).