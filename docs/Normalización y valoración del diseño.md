# Normalización y valoración de la corrección del diseño

**Asignatura:** Bases de Datos II / Ingeniería de Software — Curso 2026-2027
**Issue:** #12
**Dependencias:** submodelos #5 (`Alimento-Plato`), #6 (`Dieta-Nutricionista-Paciente-Sala`) y #7 (`Menú-Valoración-Consumo`), MERX consolidado (#9), especificación de restricciones de integridad (#11) y consultas reformuladas (`docs/consultas/`). El resultado de la validación formal contra las seis consultas (#10) se incorpora cuando esté disponible.
**Alcance de este documento:** verificación de la normalización del esquema relacional consolidado —dependencias funcionales, 1FN, 2FN, 3FN y BCNF—, inventario de redundancias controladas, datos derivados y anomalías de actualización, trayectorias que el esquema admite para las seis consultas analíticas, y texto de valoración para la sección 6 del informe.

---

## 0. Esquema relacional verificado

El esquema que se verifica es el derivado del MERX consolidado, con 29 tablas. Se construyó y se probó en PostgreSQL 17.

**Reglas de derivación aplicadas al MERX consolidado:**

- **Entidad o nomenclador:** una tabla.
- **Relación 1:N:** una clave foránea en el lado N, como `Menú.DietaId` por `Pertenece`.
- **Relación N:M sin atributos:** una tabla cuya clave es el par de claves de los participantes. Cuando el verbo se repite entre relaciones distintas, la tabla se nombra por sus participantes (MERX consolidado, D9-2): `CubreDieta`, `CubreMenú`, `IncluyeAlimento` e `IncluyePlato`.
- **Agregación:** una tabla con la llave heredada y sus atributos propios.
- **`Evalúa`:** una clave foránea compuesta (`MenúId`, `PacienteId`) de `Valoración` hacia `Asignación`.

**Convenciones de la tabla:** el nombre de columna <u>subrayado</u> es clave primaria, en parte de una clave compuesta; la flecha `→` indica la tabla referenciada; las claves foráneas son `ON DELETE RESTRICT`, salvo las marcadas con (CASCADE), que corresponden a la política R11-02 del #11. Los nombres van entre comillas dobles en SQL porque PostgreSQL pasa a minúsculas los identificadores sin comillas.

| Tabla | Origen en el MERX | Columnas (<u>subrayado</u> = clave primaria) |
|---|---|---|
| `GrupoNutricional` | nomenclador | <u>GrupoNutricionalId</u>, Nombre · UNIQUE |
| `TipoPreparación` | nomenclador | <u>TipoPreparaciónId</u>, Nombre · UNIQUE |
| `NivelCalórico` | nomenclador | <u>NivelCalóricoId</u>, Nombre · UNIQUE |
| `ProgramaDeAtención` | nomenclador | <u>ProgramaDeAtenciónId</u>, Nombre · UNIQUE |
| `RestricciónAlimentaria` | nomenclador | <u>RestricciónAlimentariaId</u>, Nombre · UNIQUE |
| `Especialidad` | nomenclador | <u>EspecialidadId</u>, Nombre · UNIQUE |
| `ResultadoNutricional` | nomenclador | <u>ResultadoNutricionalId</u>, Nombre · UNIQUE, Valor · UNIQUE |
| `Sala` | entidad | <u>SalaId</u>, Nombre |
| `Nutricionista` | entidad | <u>NutricionistaId</u>, Nombre, EspecialidadId → Especialidad |
| `Paciente` | entidad | <u>PacienteId</u>, Nombre, Edad, SalaId → Sala |
| `Dieta` | entidad | <u>DietaId</u>, Nombre, ProgramaDeAtenciónId → ProgramaDeAtención |
| `Alimento` | entidad | <u>AlimentoId</u>, Nombre, GrupoNutricionalId → GrupoNutricional, TipoPreparaciónId → TipoPreparación, NivelCalóricoId → NivelCalórico, NutricionistaId → Nutricionista · índice único sobre `lower(btrim(Nombre))` |
| `Plato` | entidad | <u>PlatoId</u>, Nombre, GrupoNutricionalId → GrupoNutricional, TipoPreparaciónId → TipoPreparación, NivelCalóricoId → NivelCalórico, NutricionistaId → Nutricionista · índice único sobre `lower(btrim(Nombre))` |
| `Menú` | entidad | <u>MenúId</u>, FechaCreación, CantidadTotalAlimentos, EsAutomático, NutricionistaId → Nutricionista, DietaId → Dieta |
| `Valoración` | entidad | <u>ValoraciónId</u>, MenúId → Asignación, PacienteId → Asignación, NutricionistaId → Nutricionista, ResultadoNutricionalId → ResultadoNutricional |
| `Revalorización` | entidad | <u>RevalorizaciónId</u>, FechaSolicitud, PacienteId → Paciente, NutricionistaId → Nutricionista, ValoraciónRevisadaId → Valoración, ValoraciónGeneradaId → Valoración · UNIQUE · opcional |
| `Composición` | agregación | <u>PlatoId</u> → Plato (CASCADE), <u>AlimentoId</u> → Alimento, Cantidad |
| `Distribución` | agregación | <u>MenúId</u> → Menú (CASCADE), <u>TipoPreparaciónId</u> → TipoPreparación, Proporción |
| `Validación` | agregación | <u>NutricionistaId</u> → Nutricionista, <u>MenúId</u> → Menú, FechaValidación, Observaciones · opcional, Aprobado |
| `Asignación` | agregación | <u>MenúId</u> → Menú, <u>PacienteId</u> → Paciente |
| `Consumo` | agregación | <u>MenúId</u> → Asignación, <u>PacienteId</u> → Asignación, <u>AlimentoId</u> → Alimento, Aceptación |
| `Autoriza` | relación N:M | <u>NutricionistaId</u> → Nutricionista, <u>DietaId</u> → Dieta |
| `Inscribe` | relación N:M | <u>PacienteId</u> → Paciente, <u>DietaId</u> → Dieta |
| `CubreDieta` | relación N:M `Cubre(Dieta, GrupoNutricional)` | <u>DietaId</u> → Dieta (CASCADE), <u>GrupoNutricionalId</u> → GrupoNutricional |
| `Restringe` | relación N:M | <u>DietaId</u> → Dieta (CASCADE), <u>RestricciónAlimentariaId</u> → RestricciónAlimentaria |
| `Excluye` | relación N:M | <u>RestricciónAlimentariaId</u> → RestricciónAlimentaria, <u>GrupoNutricionalId</u> → GrupoNutricional |
| `IncluyeAlimento` | relación N:M `Incluye(Alimento, Menú)` | <u>MenúId</u> → Menú (CASCADE), <u>AlimentoId</u> → Alimento |
| `IncluyePlato` | relación N:M `Incluye(Menú, Plato)` | <u>MenúId</u> → Menú (CASCADE), <u>PlatoId</u> → Plato |
| `CubreMenú` | relación N:M `Cubre(GrupoNutricional, Menú)` | <u>MenúId</u> → Menú (CASCADE), <u>GrupoNutricionalId</u> → GrupoNutricional |

Dos precisiones sobre este esquema:

- **`Dieta`** queda con `DietaId`, `Nombre` y `ProgramaDeAtenciónId`. No tiene `Descripción` ni `Estado`: las restricciones y los grupos que exige se modelan en `Restringe` y `CubreDieta`.
- **`Paciente.Edad` sí se almacena**, como entero (R6-14 y R6-17). No es un dato derivado; lo abierto es su precisión, que es una decisión del equipo (#6, §6).

---

## 1. Dependencias funcionales

Para cada tabla se lista la dependencia de la clave primaria hacia el resto de sus atributos y, cuando existen, las dependencias inversas que imponen las claves candidatas declaradas con `UNIQUE`: el nombre normalizado de `Alimento` y de `Plato` (R5-16), el `Nombre` de cada nomenclador y el `Valor` de `ResultadoNutricional` (R7-19), y `ValoraciónGeneradaId` en `Revalorización`, que es única pero admite nulos (R7-24).

**Estas claves candidatas no impiden la BCNF:** sus determinantes son claves, que es la condición que la BCNF exige. Lo único que las distingue de la clave primaria es que la instancia se identifica por un dato de negocio además de por el identificador del surrogado.

Ninguna otra dependencia funcional no trivial se cumple dentro de una tabla, salvo la de `Revalorización` que se analiza en §2.3.

| Tabla | Dependencias funcionales no triviales |
|---|---|
| `GrupoNutricional` | GrupoNutricionalId → Nombre; Nombre → GrupoNutricionalId (clave candidata) |
| `TipoPreparación` | TipoPreparaciónId → Nombre; Nombre → TipoPreparaciónId (clave candidata) |
| `NivelCalórico` | NivelCalóricoId → Nombre; Nombre → NivelCalóricoId (clave candidata) |
| `ProgramaDeAtención` | ProgramaDeAtenciónId → Nombre; Nombre → ProgramaDeAtenciónId (clave candidata) |
| `RestricciónAlimentaria` | RestricciónAlimentariaId → Nombre; Nombre → RestricciónAlimentariaId (clave candidata) |
| `Especialidad` | EspecialidadId → Nombre; Nombre → EspecialidadId (clave candidata) |
| `ResultadoNutricional` | ResultadoNutricionalId → Nombre, Valor; Nombre → ResultadoNutricionalId (clave candidata); Valor → ResultadoNutricionalId (clave candidata) |
| `Sala` | SalaId → Nombre |
| `Nutricionista` | NutricionistaId → Nombre, EspecialidadId |
| `Paciente` | PacienteId → Nombre, Edad, SalaId |
| `Dieta` | DietaId → Nombre, ProgramaDeAtenciónId |
| `Alimento` | AlimentoId → Nombre, GrupoNutricionalId, TipoPreparaciónId, NivelCalóricoId, NutricionistaId; lower(btrim(Nombre)) → AlimentoId (clave candidata, índice único R5-16) |
| `Plato` | PlatoId → Nombre, GrupoNutricionalId, TipoPreparaciónId, NivelCalóricoId, NutricionistaId; lower(btrim(Nombre)) → PlatoId (clave candidata, índice único R5-16) |
| `Menú` | MenúId → FechaCreación, CantidadTotalAlimentos, EsAutomático, NutricionistaId, DietaId |
| `Valoración` | ValoraciónId → MenúId, PacienteId, NutricionistaId, ResultadoNutricionalId |
| `Revalorización` | RevalorizaciónId → FechaSolicitud, PacienteId, NutricionistaId, ValoraciónRevisadaId, ValoraciónGeneradaId; ValoraciónRevisadaId → PacienteId (por R7-29; viola la 3FN, §2.3); ValoraciónGeneradaId → RevalorizaciónId (clave candidata; admite nulos) |
| `Composición` | (PlatoId, AlimentoId) → Cantidad |
| `Distribución` | (MenúId, TipoPreparaciónId) → Proporción |
| `Validación` | (NutricionistaId, MenúId) → FechaValidación, Observaciones, Aprobado |
| `Asignación` | (MenúId, PacienteId) → ∅ (solo la clave) |
| `Consumo` | (MenúId, PacienteId, AlimentoId) → Aceptación |
| `Autoriza` | (NutricionistaId, DietaId) → ∅ (solo la clave) |
| `Inscribe` | (PacienteId, DietaId) → ∅ (solo la clave) |
| `CubreDieta` | (DietaId, GrupoNutricionalId) → ∅ (solo la clave) |
| `Restringe` | (DietaId, RestricciónAlimentariaId) → ∅ (solo la clave) |
| `Excluye` | (RestricciónAlimentariaId, GrupoNutricionalId) → ∅ (solo la clave) |
| `IncluyeAlimento` | (MenúId, AlimentoId) → ∅ (solo la clave) |
| `IncluyePlato` | (MenúId, PlatoId) → ∅ (solo la clave) |
| `CubreMenú` | (MenúId, GrupoNutricionalId) → ∅ (solo la clave) |

---

## 2. Verificación de formas normales

### 2.1 Criterio

- **1FN:** se cumple por la regla de atomicidad fijada en las convenciones de modelado (§5): todo atributo es atómico. Ninguna propiedad multivaluada o con subcampos propios se almacena como columna, listas de valores separadas por comas ni documentos anidados. La información de estructura variable —ficha nutricional ampliada, alérgenos, micronutrientes y modo de preparación— reside en MongoDB y se referencia desde el modelo relacional por la clave primaria de `Alimento` o `Plato` (#8).
- **2FN:** se comprueba que todo atributo no clave dependa de la clave completa. En las tablas de clave primaria simple no hay dependencias parciales posibles; la comprobación solo es sustantiva en las tablas de clave compuesta.
- **3FN y BCNF:** se comprueba que no exista ningún determinante que no sea superclave. Se verifican juntas porque en este esquema coinciden: no hay ninguna tabla que cumpla la 3FN y falle la BCNF.

### 2.2 Tabla de verificación

| Tabla | Claves | 1FN | 2FN | 3FN | BCNF | Justificación breve |
|---|---|:---:|:---:|:---:|:---:|---|
| `GrupoNutricional` | `GrupoNutricionalId`; `Nombre` | Sí | Sí | Sí | Sí | Nombre obligatorio y único (R5-10); ambos determinantes son clave. |
| `TipoPreparación` | `TipoPreparaciónId`; `Nombre` | Sí | Sí | Sí | Sí | Catálogo cerrado de cuatro valores (R5-08); nombre único. |
| `NivelCalórico` | `NivelCalóricoId`; `Nombre` | Sí | Sí | Sí | Sí | Catálogo cerrado de tres valores (R5-09); nombre único. |
| `ProgramaDeAtención` | `ProgramaDeAtenciónId`; `Nombre` | Sí | Sí | Sí | Sí | Nombre único (R6-16); desde `Dieta` se referencia por clave foránea, sin copiar el nombre. |
| `RestricciónAlimentaria` | `RestricciónAlimentariaId`; `Nombre` | Sí | Sí | Sí | Sí | Nombre único (R6-16); interviene en `Restringe` y `Excluye` solo por clave foránea. |
| `Especialidad` | `EspecialidadId`; `Nombre` | Sí | Sí | Sí | Sí | Nombre único (R6-16); cada nutricionista referencia una sola especialidad (R6-12, R6-20). |
| `ResultadoNutricional` | `ResultadoNutricionalId`; `Nombre`; `Valor` | Sí | Sí | Sí | Sí | `Nombre` y `Valor` son únicos (R7-19); sus determinantes son clave, y el código numérico permite derivar promedios sin duplicar la categoría. |
| `Sala` | `SalaId` | Sí | Sí | Sí | Sí | Un solo atributo no clave, determinado por la clave. |
| `Nutricionista` | `NutricionistaId` | Sí | Sí | Sí | Sí | La especialidad se referencia por clave foránea; no se duplica su nombre ni sus atributos descriptivos. |
| `Paciente` | `PacienteId` | Sí | Sí | Sí | Sí | `Edad` es un entero atómico (R6-14, R6-17) y la sala se referencia por clave foránea (R6-11, R6-19). |
| `Dieta` | `DietaId` | Sí | Sí | Sí | Sí | Tres atributos, todos determinados por la clave; restricciones y grupos exigidos están en `Restringe` y `CubreDieta`, no como listas. |
| `Alimento` | `AlimentoId`; `lower(btrim(Nombre))` | Sí | Sí | Sí | Sí | Clasificaciones y autor por clave foránea; el nombre normalizado es clave candidata (R5-16) y su determinante es clave. |
| `Plato` | `PlatoId`; `lower(btrim(Nombre))` | Sí | Sí | Sí | Sí | Igual que `Alimento`; los ingredientes están en `Composición`, con su cantidad. |
| `Menú` | `MenúId` | Sí | Sí | Sí | Sí | Creador y dieta por clave foránea (R7-21); los parámetros están en `Distribución` y `CubreMenú` (R7-22). |
| `Valoración` | `ValoraciónId` | Sí | Sí | Sí | Sí | La asignación evaluada se referencia por clave foránea compuesta (R7-12, R7-23) y el resultado por el nomenclador (R7-19). |
| `Revalorización` | `RevalorizaciónId`; `ValoraciónGeneradaId` (admite nulos) | Sí | Sí | **No** | **No** | Cumple 1FN y 2FN; la dependencia transitiva `ValoraciónRevisadaId → PacienteId` rompe la 3FN. Análisis en §2.3. |
| `Composición` | (`PlatoId`, `AlimentoId`) | Sí | Sí | Sí | Sí | `Cantidad` depende del par completo (R5-03, R5-12); no se almacenan nombres de plato ni de alimento. |
| `Distribución` | (`MenúId`, `TipoPreparaciónId`) | Sí | Sí | Sí | Sí | `Proporción` depende del par completo (R7-03, R7-16). |
| `Validación` | (`NutricionistaId`, `MenúId`) | Sí | Sí | Sí | Sí | Un registro por revisor y menú (R7-04); `Observaciones` es un atributo atómico opcional (R7-20), no una lista de comentarios. |
| `Asignación` | (`MenúId`, `PacienteId`) | Sí | Sí | Sí | Sí | Tabla puente sin atributos propios: la única dependencia es la de la clave. |
| `Consumo` | (`MenúId`, `PacienteId`, `AlimentoId`) | Sí | Sí | Sí | Sí | La terna identifica el hecho (R7-06) y `Aceptación` depende de ella completa (R7-18). |
| `Autoriza` | (`NutricionistaId`, `DietaId`) | Sí | Sí | Sí | Sí | Tabla puente N:M sin atributos (R6-03): la única dependencia es la de la clave. |
| `Inscribe` | (`PacienteId`, `DietaId`) | Sí | Sí | Sí | Sí | Tabla puente N:M sin atributos (R6-04): la única dependencia es la de la clave. |
| `CubreDieta` | (`DietaId`, `GrupoNutricionalId`) | Sí | Sí | Sí | Sí | Tabla puente N:M sin atributos (R6-05): la única dependencia es la de la clave. |
| `Restringe` | (`DietaId`, `RestricciónAlimentariaId`) | Sí | Sí | Sí | Sí | Tabla puente N:M sin atributos (R6-05): la única dependencia es la de la clave. |
| `Excluye` | (`RestricciónAlimentariaId`, `GrupoNutricionalId`) | Sí | Sí | Sí | Sí | Tabla puente N:M sin atributos (R9-01): la única dependencia es la de la clave. |
| `IncluyeAlimento` | (`MenúId`, `AlimentoId`) | Sí | Sí | Sí | Sí | Tabla puente N:M sin atributos (R7-07): la única dependencia es la de la clave. |
| `IncluyePlato` | (`MenúId`, `PlatoId`) | Sí | Sí | Sí | Sí | Tabla puente N:M sin atributos (R7-07): la única dependencia es la de la clave. |
| `CubreMenú` | (`MenúId`, `GrupoNutricionalId`) | Sí | Sí | Sí | Sí | Tabla puente N:M sin atributos (R7-07): la única dependencia es la de la clave. |

**Resultado:** 28 de las 29 tablas están en BCNF. La excepción es `Revalorización`, que está en 2FN y no en 3FN.

### 2.3 La excepción: `Revalorización`

- La tabla es `Revalorización(RevalorizaciónId, FechaSolicitud, PacienteId, NutricionistaId, ValoraciónRevisadaId, ValoraciónGeneradaId)`.
- La restricción R7-29 obliga a que el paciente que solicita sea el de la valoración que revisa. Por eso, en toda instancia válida se cumple `ValoraciónRevisadaId → PacienteId`.
- `ValoraciónRevisadaId` **no es clave**: una misma valoración puede revisarse varias veces, porque en `Revalora` hay `(0,*)` del lado de `Revalorización`.
- `PacienteId` no forma parte de ninguna clave. Entonces `RevalorizaciónId → ValoraciónRevisadaId → PacienteId` es una **dependencia transitiva**.
- Conclusión: la tabla está en 2FN, pero **no en 3FN**. Es la única excepción del esquema.

**Decisión adoptada: opción A, mantener la redundancia como controlada.** El solicitante es un hecho explícito del enunciado (línea 36: el paciente solicita la revalorización), de modo que `Solicita` debe poder leerse sin recorrer la valoración revisada, y la consistencia la garantiza el trigger de R7-29, probado en PostgreSQL 17 dentro de la especificación del #11 (§7). El documento la declara como la única excepción a la 3FN, con esta justificación.

La alternativa sería la **opción B**: eliminar `PacienteId` de `Revalorización` y obtener el solicitante por la valoración revisada (`Valoración` → `Asignación` → `PacienteId`), con lo que la tabla quedaría en BCNF. No se adopta porque cambia el modelo: `Solicita` pasaría a ser una relación derivada y habría que actualizar el MERX consolidado (#9) y el submodelo #7. Queda registrada en §6.2 por si el equipo la prefiera.

### 2.4 Reglas que se parecen a una violación y no lo son

Hay otras dos reglas del #11 con la misma forma aparente, pero **no son violaciones de forma normal**, porque relacionan filas de tablas distintas y no son dependencias dentro de una relación:

- **R7-30:** la valoración generada evalúa la misma asignación que la revisada y la emite el nutricionista que ejecutó la revalorización. Atraviesa `Revalorización` y `Valoración`.
- **R6-22 y R6-23:** un nutricionista solo genera o valida menús de las dietas en las que está autorizado, y un paciente solo recibe menús de las dietas en las que está inscrito. Atraviesan `Menú`, `Validación`, `Asignación`, `Autoriza` e `Inscribe`.

Las tres son restricciones entre tablas y las implementan disparadores, no declaraciones del esquema (#11, §7). Lo mismo cabe decir de R9-04, que impide que una dieta exija cubrir un grupo que una de sus restricciones excluye.

---

## 3. Redundancias controladas, datos derivados y anomalías

### 3.1 Redundancias controladas

Son las únicas que el modelo acepta, y cada una tiene una razón y un mecanismo de mantenimiento:

| Redundancia | Por qué se acepta | Cómo se mantiene |
|---|---|---|
| `Revalorización.PacienteId` | `Solicita` es un hecho del enunciado (línea 36); quitarlo convertiría la relación en derivada (§2.3). | Trigger de R7-29 (#11, §7). |
| Índice único sobre `lower(btrim(Nombre))` en `Alimento` y en `Plato` | No es un dato duplicado, sino una expresión indexada: el valor almacenado sigue siendo `Nombre`, y lo indexado es su forma normalizada. | Índice único (R5-16). |
| Copia del nombre de cada ingrediente en la ficha de un plato, en MongoDB | La ficha es un documento de estructura variable; su consulta no puede resolverse con un `JOIN` sobre el modelo relacional. | Conciliación periódica que reconstruye las copias desactualizadas (R8-10, #8). |

### 3.2 Datos que no se almacenan porque se derivan

| Dato | Cómo se obtiene |
|---|---|
| Estado final de un menú | Un menú es final si su **validación más reciente** —la de mayor `FechaValidación`— tiene `Aprobado` verdadero (submodelo #7, §4). El criterio de «más reciente» es decisión del equipo. |
| Tasas de aceptación y de rechazo | Se calculan sobre `Consumo.Aceptación` por alimento, por menú y por paciente, según los denominadores que fija cada consulta. |
| Promedios de resultado nutricional | Se calculan sobre `ResultadoNutricional.Valor`, el código numérico del nomenclador (R7-19). |
| Disponibilidad calórica de una dieta | Se deriva de los alimentos de sus menús y de su nivel calórico; no hay ninguna fila que la almacene. |

A diferencia de estos, la **edad del paciente sí se almacena**, como entero (R6-14 y R6-17). Lo que no está resuelto es su precisión, decisión abierta del equipo (#6, §6).

### 3.3 Anomalías de actualización

- **Inserción.** Ninguna tabla admite una fila «a medio crear»: las claves foráneas `NOT NULL` obligan a clasificar y a fechar desde el primer momento, y las cardinalidades mínimas (un plato con al menos un alimento, una dieta con al menos un grupo, un menú con al menos un parámetro) se comprueban al confirmar la transacción con disparadores diferidos, porque la entidad se registra antes que su detalle (R5-15, R6-21, R7-22).
- **Modificación.** Actualizar el nombre de una sala, de una especialidad o de un programa de atención es una sola operación en la tabla nomencladora, y todas las filas que la referencian reflejan el cambio. Los identificadores son surrogados y los nombres de las entidades no son únicos, de modo que cambiar un nombre no altera ninguna clave foránea.
- **Borrado.** La política es la de R11-02 (#11): `RESTRICT` en todas las claves foráneas, de modo que un hecho del que dependen otros hechos no se pueda borrar —un menú validado o asignado (R7-14), un alimento que forma parte de un plato (R5-07), una dieta con autorizaciones o inscripciones (R6-13)—, y `CASCADE` solo desde un dueño hacia sus propias filas de composición o de parametrización: `Plato` → `Composición`; `Menú` → `Distribución`, `CubreMenú`, `IncluyeAlimento` e `IncluyePlato`; `Dieta` → `CubreDieta` y `Restringe`. Así, un menú nuevo, sin validaciones ni asignaciones, se puede borrar entero, y la inmutabilidad de su parametrización (R7-27) no lo impide.

**La no duplicación de asignaciones no necesita ningún disparador:** la garantiza la clave primaria compuesta (`MenúId`, `PacienteId`) de `Asignación` (R7-05). Lo mismo ocurre con las demás tablas de interconexión, cuya identidad es el par de claves de los participantes.

---

## 4. Trayectorias que el esquema admite

Esta sección enumera las trayectorias de navegación que el esquema permite para cada una de las seis consultas del enunciado (líneas 68–82). Las trayectorias se leen con las flechas de Anexo: `X ← Y` indica que desde `Y` se alcanza `X` por clave foránea.

| Consulta | Qué pide | Trayectoria en el esquema |
|---|---|---|
| 1 | Menús generados automáticamente para una dieta, con el creador, la fecha y los parámetros | `Dieta` ← `Menú` (`DietaId`, filtrado por `EsAutomático`) → `Nutricionista` (`NutricionistaId`, creador) · `FechaCreación` · parámetros: `CantidadTotalAlimentos`, `Distribución` (`TipoPreparación`, `Proporción`) y `CubreMenú` (`GrupoNutricional`) |
| 2 | Alimentos más usados en los menús finales de una dieta, por nivel calórico y grupo | `Dieta` ← `Menú` (final: validación más reciente con `Aprobado`) → `IncluyeAlimento` → `Alimento` ∪ `IncluyePlato` → `Composición` → `Alimento` → `NivelCalórico`, `GrupoNutricional` |
| 3 | Menús validados por un revisor, con la fecha y las observaciones | `Nutricionista` (revisor) ← `Validación` (`FechaValidación`, `Observaciones`, `Aprobado`) → `Menú` → `Dieta`; `Menú` → `Nutricionista` (creador) |
| 4 | Desempeño de los pacientes ante un menú: tasas de aceptación por nivel calórico | `Menú` ← `Asignación` ← `Consumo` (`Aceptación`) → `Alimento` → `NivelCalórico` (y `GrupoNutricional`, `TipoPreparación`) |
| 5 | Comparar menús de distintas dietas: distribución por grupo y nivel calórico y criterios de equilibrio | `Dieta` ← `Menú` → alimentos (como en la 2) → `GrupoNutricional`, `NivelCalórico`; requeridos: `Dieta` → `CubreDieta`; excluidos: `Dieta` → `Restringe` → `Excluye` → `GrupoNutricional`; parámetros del menú como en la 1 |
| 6 | (A) Correlación entre nivel calórico y resultado promedio, por dieta; (B) top-10 de rechazo con su autor y su dieta; (C) revalorizados frente al promedio de su sala en las mismas dietas | (A) `Dieta` ← `Menú` → alimentos → `NivelCalórico`; `Menú` ← `Asignación` ← `Valoración` → `ResultadoNutricional.Valor`. (B) `Consumo` (rechazos) → `Alimento` → `Nutricionista` (`Describe`); `Consumo` → `Asignación` → `Menú` → `Dieta`. (C) `Revalorización` → `ValoraciónRevisadaId` → `Valoración` → `Asignación` → `Paciente` → `Sala`; `Revalorización` → `ValoraciónGeneradaId` (resultado del revalorizado); promedio por (sala, dieta): `Valoración` → `Asignación` → `Paciente` → `Sala` y `Asignación` → `Menú` → `Dieta` |

Observaciones:

- Los cálculos que consumen esas trayectorias —tasas, promedios, correlaciones, el corte del top-10— no están almacenados: se derivan en la consulta (§3.2).
- La trayectoria de la consulta 5 usa `Excluye` para obtener los grupos que una restricción prohíbe, encadenando `Dieta` → `Restringe` → `Excluye` → `GrupoNutricional`, frente a los grupos exigidos, que se obtienen por `CubreDieta`.
- La parte C de la consulta 6 no necesita fechas propias: la dieta del revalorizado se deriva de la valoración revisada (`Valoración` → `Asignación` → `Menú` → `Dieta`), lo que resuelve «para esas mismas dietas» incluso con pacientes inscritos en varias dietas.

> **La validación formal de estas seis consultas contra el modelo es el issue #10.** Las trayectorias anteriores indican qué recorrido ofrece el esquema; no constituyen por sí solas la comprobación de que cada consulta se pueda formular y ejecutar. [pendiente: resultado del #10]

---

## 5. Valoración de la corrección del diseño (sección 6 del informe)

> ### 6. Valoración de la corrección del diseño
>
> **1. Normalización.**
> De las 29 tablas del esquema consolidado, 28 están en la Forma Normal de Boyce-Codd: en cada una, todo determinante de una dependencia funcional no trivial es clave. Las claves candidatas que imponen los `UNIQUE` —el nombre de cada nomenclador, el nombre normalizado de alimentos y platos, el valor numérico de `ResultadoNutricional` y la valoración generada de una revalorización— no alteran ese resultado, porque sus determinantes son claves. La única excepción es `Revalorización`, que está en 2FN pero no en 3FN: la restricción R7-29 obliga a que el paciente que solicita una revalorización sea el de la valoración que revisa, de modo que `ValoraciónRevisadaId → PacienteId` es una dependencia transitiva sobre un determinante que no es clave, pues una misma valoración puede revisarse varias veces. Se mantiene como redundancia controlada: la solicitud es un hecho explícito del enunciado y el trigger de R7-29, probado en PostgreSQL 17, garantiza su consistencia. La alternativa —obtener el solicitante a través de la valoración revisada— dejaría la tabla en BCNF, pero convertiría `Solicita` en una relación derivada y obligaría a modificar el MERX consolidado y el submodelo #7.
>
> **2. Atomicidad y partición.**
> El esquema cumple la Primera Forma Normal por la regla de atomicidad fijada en las convenciones de modelado (§5): ninguna propiedad multivaluada o con subcampos propios se almacena como columna. Las listas que el enunciado sugiere —restricciones y grupos de una dieta, parámetros de generación de un menú, ingredientes de un plato— se modelaron como entidades relacionadas, no como atributos. La ficha nutricional, de estructura variable y distinta para alimentos y platos, se gestiona en MongoDB y se referencia por la clave primaria de `Alimento` o `Plato` (#8); las listas de alérgenos y los bloques de micronutrientes no obligan a columnas opcionales ni a valores separados por comas.
>
> **3. Integridad.**
> La especificación del #11 recoge 86 restricciones: 70 de los submodelos, 4 de la consolidación, 10 de la ficha nutricional y 2 definidas en esa especificación. Por mecanismo —una misma restricción puede necesitar más de uno—: 16 se declaran con clave primaria, 30 con clave foránea, 7 con `UNIQUE`, 10 con `CHECK`, 15 con `NOT NULL` y una con el tipo entero del dominio. Las reglas semánticas, que no admiten una declaración del esquema porque consultan otras tablas, se implementan con 13 disparadores: entre ellas, que quien crea o valida un menú esté autorizado para su dieta (R6-22), que quien valida no haya creado el menú (R7-25), que el paciente que solicita la revalorización sea el de la valoración revisada (R7-29) y que solo se registre consumo de alimentos del menú asignado (R11-01). En la capa de aplicación quedan 10, entre ellas la clasificación de un alimento por quien lo describió (R5-17) y la coherencia entre los ingredientes de la ficha en MongoDB y `Composición` (R8-06, #8). En la ficha nutricional se emplean además validación con `$jsonSchema`, un índice único y una tarea programada de conciliación (#8). La política de borrado es `RESTRICT` en todas las claves foráneas, con `CASCADE` solo desde un dueño hacia sus propias filas de composición o parametrización (R11-02).
>
> **4. Suficiencia.**
> [pendiente: resultado del #10] El esquema ofrece una trayectoria de navegación para cada una de las seis consultas (§4). La comprobación formal de que las seis puedan formularse y ejecutarse sobre este modelo, con el conjunto de datos de prueba, corresponde al issue #10 y se incorpora aquí cuando esté disponible.
>
> **5. Límites y decisiones abiertas.**
> El modelo admite deliberadamente algunas condiciones que por eso no restringe. La principal es que la composición real de un menú no se contrasta con su parametrización: un menú puede incluir un número de alimentos distinto de `CantidadTotalAlimentos`, o proporciones distintas de las de `Distribución`, y puede no cubrir los grupos que su dieta exige. Es una decisión consciente, porque la comparación entre dietas (consulta 5) existe para detectar esas desviaciones; si la base las impidiera, la consulta no tendría nada que detectar. Tampoco se restringen la clasificación de un plato frente a la de sus ingredientes, la composición de un solo nivel, el hecho de que una dieta pueda no declarar restricciones, ni el traslado de un paciente entre salas como historial. Quedan abiertas cuatro decisiones de los submodelos: la unidad de `Cantidad` en `Composición` (#5), la escala de `Proporción` en `Distribución` (#7), la precisión de `Edad` en `Paciente` (#6) y la correspondencia entre las restricciones de una dieta y los alérgenos de los alimentos, que residen en la ficha nutricional (#8).

---

## 6. Decisiones del equipo y puntos abiertos

### 6.1 Decisiones adoptadas en este documento

- **`Revalorización` se mantiene con `PacienteId` como redundancia controlada** (opción A del §2.3). Se declara como la única excepción a la 3FN, con R7-29 y el trigger asociado como garantía de consistencia.
- **El estado final de un menú se deriva de su validación más reciente**, la de mayor `FechaValidación` con `Aprobado` verdadero (submodelo #7, §4). No se almacena ninguna columna de estado.
- **Los identificadores son surrogados y los nombres de las entidades no son únicos**; la unicidad de nombre solo se exige en los nomencladores (R6-16), lo que evita que un cambio de nombre tenga que propagarse.
- **La política de borrado es R11-02**: `RESTRICT` en todas las claves foráneas y `CASCADE` solo desde un dueño hacia sus propias filas.

### 6.2 Puntos abiertos

| Punto | Estado | Referencia |
|---|---|---|
| Validación formal del modelo contra las seis consultas | Pendiente; su resultado se incorporará a §4 y §5 | #10 |
| Confirmación de R11-02 por el equipo | Pendiente de confirmación antes de darla por cerrada | #11, §4 y §8 |
| Numeración de R8-01 a R8-10 y su coherencia con `main` | Pendiente del #8; cuando se mergee hay que verificar su numeración y ajustar las referencias | #8, #11 §8 |
| Unidad de `Cantidad` en `Composición` | Abierta: el enunciado no la fija y R5-12 necesita un sentido inequívoco | #5, §6 |
| Escala de `Proporción` en `Distribución` | Abierta: fracción en (0, 1] o porcentaje; afecta a R7-16 y R7-28 | #7, §6 |
| Precisión de `Edad` en `Paciente` | Abierta: entero en años, meses o fecha de nacimiento | #6, §6 |
| Correspondencia entre restricciones de una dieta y alérgenos | Pendiente: los alérgenos viven en la ficha nutricional, fuera del modelo relacional | #8, #6, §5 |
| Atributos descriptivos propios de `ProgramaDeAtención`, `RestricciónAlimentaria` y `Especialidad` | Abierta: cualquier atributo adicional debe declararse antes de usarse en el diagrama | #6, §6 |
| Si solo pueden asignarse menús finales (aprobados) | Abierta: ni el enunciado ni las consultas lo exigen; sería una restricción semántica más | #7, §6 |
| Opción B para `Revalorización` (eliminar `PacienteId`) | No adoptada; exigiría actualizar el MERX consolidado (#9) y el submodelo #7 | §2.3 |
| Capa de caché (Redis) para la disponibilidad calórica por dieta | Se prevé, pero no hay ningún documento que lo respalde todavía; hasta entonces no forma parte del diseño | — |
| Borrado del borrador anterior (`Verificacion_Normalizacion_Y_Valoracion.md`, en la raíz) | Pendiente de decisión: este documento lo sustituye en contenido | — |