# MERX consolidado — Decisiones y convenciones de la integración

**Asignatura:** Bases de Datos II / Ingeniería de Software — Curso 2026-2027
**Issue:** #9
**Diagrama:** `MERX consolidado.drawio` (misma carpeta), en doce páginas A4 verticales con la organización de los submodelos: Página-Entidades-1 a Página-Entidades-3 y Página-Relaciones-1 a Página-Relaciones-9 (§0.4).

**Alcance de este documento:** integra los submodelos de los issues #5 (Alimento-Plato), #6 (Dieta-Nutricionista-Paciente-Sala) y #7 (Menú-Valoración-Consumo) en un MERX único, resuelve las relaciones que cruzan fronteras entre ellos, unifica nombres y notación, y deja constancia de las decisiones propias de la consolidación. Recoge los submodelos tal como están en `main`, incluidos los cambios que la validación de las consultas (issue #10) y la ficha nutricional (issue #8) introdujeron en el #6: la agregación `Cobertura`, con el atributo `Tipo`, y la relación `Excluye`. Es insumo de la especificación consolidada de restricciones (issue #11), de la valoración del diseño (issue #12) y del §4 del informe.

Cuando el glosario del dominio o el documento de un submodelo difiera de lo establecido aquí, prevalece este documento, que es posterior.

---

## 0. Contenido del MERX

### 0.1 Entidades y nomencladores

| Conjunto de entidades | Atributos (llave subrayada en el diagrama) | Tipo | Submodelo |
|---|---|---|---|
| `Alimento` | `AlimentoId`, `Nombre` | Entidad | #5 |
| `Plato` | `PlatoId`, `Nombre` | Entidad | #5 |
| `Dieta` | `DietaId`, `Nombre` | Entidad | #6 |
| `Nutricionista` | `NutricionistaId`, `Nombre` | Entidad | #6 |
| `Paciente` | `PacienteId`, `Nombre`, `Edad` | Entidad | #6 |
| `Sala` | `SalaId`, `Nombre` | Entidad | #6 |
| `Menú` | `MenúId`, `FechaCreación`, `CantidadTotalAlimentos`, `EsAutomático` | Entidad | #7 |
| `Valoración` | `ValoraciónId` | Entidad | #7 |
| `Revalorización` | `RevalorizaciónId`, `FechaSolicitud` | Entidad | #7 |
| `GrupoNutricional` | `GrupoNutricionalId`, `Nombre` | Nomenclador | #5 |
| `TipoPreparación` | `TipoPreparaciónId`, `Nombre` | Nomenclador | #5 |
| `NivelCalórico` | `NivelCalóricoId`, `Nombre` | Nomenclador | #5 |
| `ProgramaDeAtención` | `ProgramaDeAtenciónId`, `Nombre` | Nomenclador | #6 |
| `RestricciónAlimentaria` | `RestricciónAlimentariaId`, `Nombre` | Nomenclador | #6 |
| `Especialidad` | `EspecialidadId`, `Nombre` | Nomenclador | #6 |
| `ResultadoNutricional` | `ResultadoNutricionalId`, `Nombre`, `Valor` | Nomenclador | #7 |

Nueve entidades y siete nomencladores. No hay entidades débiles ni especializaciones: `Consumo`, que el glosario proponía como entidad débil, quedó como agregación (consulta 4, §7-A y §7-D), y el jefe de nutrición es un rol de `Nutricionista` sin entidad propia (glosario, §6).

### 0.2 Agregaciones

| Agregación | Relación que envuelve | Llaves heredadas | Atributos propios | Submodelo |
|---|---|---|---|---|
| `Composición` | `Plato`—`Compone`—`Alimento` | `PlatoId`, `AlimentoId` | `Cantidad` | #5 |
| `Cobertura` | `Dieta`—`Cubre`—`GrupoNutricional` | `DietaId`, `GrupoNutricionalId` | `Tipo` (*Requerido* o *Restringido*) | #6 |
| `Distribución` | `Menú`—`Distribuye`—`TipoPreparación` | `MenúId`, `TipoPreparaciónId` | `Proporción` | #7 |
| `Validación` | `Nutricionista`—`Valida`—`Menú` | `NutricionistaId`, `MenúId` | `FechaValidación`, `Observaciones`, `Aprobado` | #7 |
| `Asignación` | `Menú`—`Asigna`—`Paciente` | `MenúId`, `PacienteId` | — | #7 |
| `Consumo` | `Asignación`—`Consume`—`Alimento` | `MenúId`, `PacienteId`, `AlimentoId` | `Aceptación` | #7 |

`Asignación` participa además en `Evalúa`, y `Consumo` la envuelve: es la única agregación anidada del modelo.

### 0.3 Relaciones

Las cardinalidades siguen la regla de dirección de las convenciones de modelado (§4): el par escrito junto a una entidad indica cuántas instancias de esa entidad se asocian a una sola instancia del otro extremo.

| # | Relación | Participante y cardinalidad | Participante y cardinalidad | Submodelo |
|---|---|---|---|---|
| 1 | `Pertenece` | `Alimento` (0,*) | `GrupoNutricional` (1,1) | #5 |
| 2 | `Pertenece` | `Plato` (0,*) | `GrupoNutricional` (1,1) | #5 |
| 3 | `Corresponde` | `Alimento` (0,*) | `TipoPreparación` (1,1) | #5 |
| 4 | `Corresponde` | `Plato` (0,*) | `TipoPreparación` (1,1) | #5 |
| 5 | `Tiene` | `Alimento` (0,*) | `NivelCalórico` (1,1) | #5 |
| 6 | `Tiene` | `Plato` (0,*) | `NivelCalórico` (1,1) | #5 |
| 7 | `Describe` | `Alimento` (0,*) | `Nutricionista` (1,1) | #5 |
| 8 | `Describe` | `Plato` (0,*) | `Nutricionista` (1,1) | #5 |
| 9 | `Compone` | `Plato` (0,*) | `Alimento` (1,*) | #5 |
| 10 | `Corresponde` | `Dieta` (0,*) | `ProgramaDeAtención` (1,1) | #6 |
| 11 | `Cubre` | `Dieta` (0,*) | `GrupoNutricional` (1,*) | #6 |
| 12 | `Restringe` | `Dieta` (0,*) | `RestricciónAlimentaria` (0,*) | #6 |
| 13 | `Excluye` | `RestricciónAlimentaria` (0,*) | `GrupoNutricional` (0,*) | #6 |
| 14 | `Autoriza` | `Nutricionista` (0,*) | `Dieta` (0,*) | #6 |
| 15 | `Inscribe` | `Paciente` (0,*) | `Dieta` (0,*) | #6 |
| 16 | `Pertenece` | `Paciente` (0,*) | `Sala` (1,1) | #6 |
| 17 | `Ejerce` | `Nutricionista` (0,*) | `Especialidad` (1,1) | #6 |
| 18 | `Crea` | `Nutricionista` (1,1) | `Menú` (0,*) | #7 |
| 19 | `Pertenece` | `Menú` (0,*) | `Dieta` (1,1) | #7 |
| 20 | `Incluye` | `Alimento` (0,*) | `Menú` (0,*) | #7 |
| 21 | `Incluye` | `Menú` (0,*) | `Plato` (0,*) | #7 |
| 22 | `Cubre` | `GrupoNutricional` (1,*) | `Menú` (0,*) | #7 |
| 23 | `Distribuye` | `Menú` (0,*) | `TipoPreparación` (1,*) | #7 |
| 24 | `Valida` | `Nutricionista` (0,*) | `Menú` (0,*) | #7 |
| 25 | `Asigna` | `Menú` (0,*) | `Paciente` (0,*) | #7 |
| 26 | `Consume` | `Asignación` (0,*) | `Alimento` (0,*) | #7 |
| 27 | `Evalúa` | `Asignación` (1,1) | `Valoración` (0,*) | #7 |
| 28 | `Emite` | `Nutricionista` (1,1) | `Valoración` (0,*) | #7 |
| 29 | `Tiene` | `Valoración` (0,*) | `ResultadoNutricional` (1,1) | #7 |
| 30 | `Solicita` | `Paciente` (1,1) | `Revalorización` (0,*) | #7 |
| 31 | `Ejecuta` | `Revalorización` (0,*) | `Nutricionista` (1,1) | #7 |
| 32 | `Revalora` | `Revalorización` (0,*) | `Valoración` (1,1) | #7 |
| 33 | `Genera` | `Revalorización` (0,1) | `Valoración` (0,1) | #7 |

### 0.4 Páginas del diagrama

Como en los submodelos, los atributos de cada entidad se dibujan en las páginas de entidades, y las páginas de relaciones agrupan las relaciones por zona del modelo. En ellas las entidades se repiten como rectángulos sin atributos (D9-4).

| Página | Contenido |
|---|---|
| Página-Entidades-1 | `Alimento`, `Plato`, `GrupoNutricional`, `TipoPreparación`, `NivelCalórico` |
| Página-Entidades-2 | `Dieta`, `Nutricionista`, `Paciente`, `Sala`, `ProgramaDeAtención`, `RestricciónAlimentaria`, `Especialidad` |
| Página-Entidades-3 | `Menú`, `Valoración`, `Revalorización`, `ResultadoNutricional` |
| Página-Relaciones-1 | Clasificación: `Pertenece` y `Corresponde` de `Alimento` y `Plato` |
| Página-Relaciones-2 | Nivel calórico y autoría: `Tiene` y `Describe` de `Alimento` y `Plato` |
| Página-Relaciones-3 | El menú y su contenido: `Cubre`, `Crea`, `Incluye` (dos) y la agregación `Composición` |
| Página-Relaciones-4 | La dieta: `Corresponde`, la agregación `Cobertura`, `Restringe` y `Excluye` |
| Página-Relaciones-5 | Dieta y personas: `Autoriza`, `Inscribe`, `Pertenece(Menú, Dieta)` y `Pertenece(Paciente, Sala)` |
| Página-Relaciones-6 | `Ejerce`, `Emite`, `Ejecuta` y `Solicita` |
| Página-Relaciones-7 | `Revalora`, `Genera` y `Tiene(Valoración, ResultadoNutricional)` |
| Página-Relaciones-8 | Agregaciones `Validación` y `Distribución` |
| Página-Relaciones-9 | Agregaciones `Asignación` y `Consumo`, y `Evalúa` |

---

## 1. Criterio de consolidación

- **Fuentes.** Los diagramas y documentos de los submodelos #5, #6 y #7, las seis consultas formalizadas (`docs/consultas/`), el glosario del dominio y las convenciones de modelado.
- **Conservación.** Cada relación conserva el nombre, los participantes y las cardinalidades de su submodelo, y cada agregación, sus llaves heredadas y sus atributos. Las 33 relaciones y las 6 agregaciones provienen de los submodelos y se cotejaron una por una contra sus diagramas: coinciden el nombre, los dos participantes y el par dibujado junto a cada uno. La consolidación no agrega relaciones.
- **Identidad de las fronteras.** Una entidad que aparece en más de un submodelo —como extremo propio en uno y como entidad de frontera en otro— es la misma en el MERX, con la misma llave en todos: `AlimentoId`, `PlatoId`, `MenúId`, `DietaId`, `NutricionistaId`, `PacienteId`, `GrupoNutricionalId` y `TipoPreparaciónId`. Los atributos de cada entidad se dibujan una sola vez.

---

## 2. Relaciones que cruzan fronteras entre submodelos

| Frontera | Relaciones | Resolución |
|---|---|---|
| Menú ↔ Alimento / Plato | `Incluye` (dos ocurrencias) | Un menú incluye alimentos y platos directamente (glosario, §6). Los alimentos de un plato incluido se obtienen por `Compone`, de modo que las consultas 2, 4 y 6 cuentan los alimentos incluidos directamente más los de los platos incluidos. |
| Menú ↔ Dieta | `Pertenece` | Cada menú pertenece a exactamente una dieta, y una dieta tiene cualquier cantidad de menús (consulta 1, §8). Se adopta el nombre del #7 (D9-3). |
| Valoración ↔ Paciente | `Asigna` (agregación `Asignación`) y `Evalúa` | No hay relación directa: la valoración evalúa una asignación, es decir, un par (menú, paciente). De ahí se obtienen el paciente, el menú y la dieta, sin fechas propias (consulta 4, §7-C y §7-D; consulta 6, §7-D, §7-E y §7-F). |
| Menú ↔ Nutricionista | `Crea`, `Valida` (agregación `Validación`) | El creador y el revisor son roles distintos del mismo conjunto de entidades (consulta 1, §7-C). La validación es histórica y registra su dictamen en `Aprobado` (consulta 3, §7-A y §7-C). |
| Menú ↔ GrupoNutricional / TipoPreparación | `Cubre`, `Distribuye` (agregación `Distribución`) | Junto con `CantidadTotalAlimentos`, son los parámetros de generación del menú (consulta 1, §7-B y §7-D). |
| Alimento / Plato ↔ Nutricionista | `Describe` (dos ocurrencias) | El autor del alimento que pide la parte B de la consulta 6. |
| Dieta ↔ GrupoNutricional | `Cubre` (agregación `Cobertura`, con `Tipo`) y, a través de `RestricciónAlimentaria`, `Excluye` | Grupos requeridos y grupos prohibidos de una dieta, ambos computables (D9-1). |
| Paciente / Nutricionista ↔ Revalorización y Valoración | `Solicita`, `Ejecuta`, `Emite` | Quién solicita, quién ejecuta y quién emite cada valoración. |
| Asignación ↔ Alimento | `Consume` (agregación `Consumo`) | Aceptación por alimento de cada menú asignado. El consumo de un plato se registra por sus alimentos (convención adoptada de R7-06). |

---

## 3. Decisiones de la consolidación

### D9-1 — Grupos requeridos y grupos prohibidos de una dieta

**Problema.** La consulta 5 exige que los grupos nutricionales obligatorios y los prohibidos de una dieta sean computables (§7-A, §7-C y §8). El #6 lo resuelve con dos elementos:

- la agregación `Cobertura`, que envuelve `Dieta`—`Cubre`—`GrupoNutricional` y cuyo atributo `Tipo` marca cada grupo de la dieta como *Requerido* o *Restringido* (issue #10; #6, R6-05b, R6-17b y R6-21);
- la relación `Excluye(RestricciónAlimentaria, GrupoNutricional)`, que indica qué grupos excluye cada restricción alimentaria (issue #8; #6, R6-05c y R6-08b).

Las dos expresan prohibiciones de grupos, así que el MERX tiene que fijar cómo se combinan.

**Decisión.** Se conservan las dos, con este significado:

- los **grupos requeridos** de una dieta son los de `Cobertura` con `Tipo` *Requerido*;
- los **grupos prohibidos** son la unión de dos conjuntos:
  - los de `Cobertura` con `Tipo` *Restringido*, que son prohibiciones propias de esa dieta;
  - los excluidos por sus restricciones alimentarias (`Restringe` seguido de `Excluye`), que se derivan de una restricción clínica y valen en toda dieta que la tenga.

`Excluye` es un catálogo general: se registra una vez y lo hereda cualquier dieta con esa restricción. *Restringido* cubre las prohibiciones que no provienen de una restricción del catálogo. Por ejemplo, en una dieta hipocalórica para un paciente celíaco, con la restricción «sin gluten» que excluye los cereales y con los dulces marcados como *Restringido*, los grupos prohibidos son los dulces y los cereales. Es la misma división que hace la ficha nutricional (#8, documento 2, §5): por grupo, en PostgreSQL; por alérgeno, en la capa de aplicación.

Un grupo prohibido por las dos vías es válido: la prohibición se repite, pero no se contradice. Lo incoherente es que un grupo sea *Requerido* y a la vez esté excluido por una restricción de la misma dieta, y eso lo impide R9-04 (§4).

**Cardinalidad.** La de los submodelos. En `Excluye`, `(0,*)` en ambos extremos: una restricción puede no excluir ningún grupo completo (una dieta hiposódica limita el sodio sin excluir un grupo entero), y un grupo puede no estar excluido por ninguna restricción. En `Cubre`, `(0,*)` del lado de `Dieta` y `(1,*)` del lado de `GrupoNutricional`, y R6-21 exige además que al menos uno de esos grupos sea *Requerido*.

### D9-2 — Nombres de relación repetidos

Se conservan los nombres de los submodelos aunque se repitan entre pares distintos: `Pertenece` (4), `Tiene` (3), `Corresponde` (3), `Cubre` (2), `Incluye` (2) y `Describe` (2). Una relación se identifica por su nombre **y** sus participantes, como el glosario ya hacía con `Cubre` (§4.4). En el diseño lógico, cada una se nombrará por sus participantes cuando haga falta distinguirla. Renombrarlas habría obligado a rehacer los tres submodelos sin cambiar el contenido del modelo.

### D9-3 — `Pertenece(Menú, Dieta)` en lugar de `Generado para`

El glosario (§4.4) registra la relación como `Generado para`. El #7 la dibuja como `Pertenece`, que es un único verbo en presente, como piden las convenciones (§2). Se adopta `Pertenece`.

### D9-4 — Formato y organización del diagrama

- **Hoja:** A4 vertical, como los submodelos. Todo el contenido de cada página queda dentro de la hoja.
- **Organización:** páginas de entidades, con sus atributos, y páginas de relaciones agrupadas por zona del modelo (§0.4). En las páginas de relaciones, las entidades se repiten como rectángulos sin atributos, que representan la misma entidad. Los atributos de cada entidad y de cada agregación se dibujan una sola vez.
- **Tipografía:** Times New Roman 30 en figuras y 25 en cardinalidades. Así se unifica el tamaño de las cardinalidades del #7, que usaba 30.
- **Cardinalidad:** se escribe como texto de la línea, junto a la entidad y fuera de ella. Si la entidad está dentro de una agregación y la relación queda fuera, la cardinalidad va sobre la misma línea, justo fuera del borde de la caja.
- **Nombre de la agregación:** va dentro de la caja, abajo a la derecha. Si líneas o figuras ocupan esa esquina, pasa a otra esquina interior libre.
- **Llaves:** las propias van subrayadas, y las heredadas por una agregación, con óvalo punteado y subrayadas (convenciones, §5).

---

## 4. Restricciones locales de la consolidación

Las restricciones de `Cobertura` y de `Excluye` (clave, referencias, dominio de `Tipo` y cardinalidad mínima) son locales del #6: R6-05b, R6-05c, R6-07, R6-08b, R6-17b y R6-21. La consolidación agrega una sola restricción propia, la que vincula las dos vías de la D9-1. Sigue el criterio y el vocabulario de los submodelos (PK, FK, UNIQUE, CHECK, NOT NULL, trigger, capa de aplicación) y se suma a la especificación consolidada del issue #11.

| # | Restricción | Evidencia | Mecanismo |
|---|---|---|---|
| R9-04 | Un grupo *Requerido* en la `Cobertura` de una dieta no puede estar excluido por una de sus restricciones alimentarias (`Restringe` seguido de `Excluye`). | consulta 5, §7-A y §7-C; la prohibición, decisión del equipo | trigger |

**R9-04.** Una dieta que exige un grupo y a la vez lo excluye por una de sus restricciones es incoherente: ningún menú podría cumplirla. Solo cuenta `Tipo` *Requerido*. Un grupo *Restringido* que además está excluido por una restricción repite la prohibición sin contradecirla (D9-1). La comprobación abarca `Cobertura`, `Restringe` y `Excluye`, así que no es expresable con claves y se implementa con un trigger sobre las tres. El trigger debe filtrar `Tipo = 'Requerido'`.

**Numeración.** R9-01 a R9-03 se retiraron. Describían la clave, las referencias y el borrado de `Excluye`, que ahora están en el #6: R9-01 es R6-05c, y R9-02 y R9-03 son R6-08b. R9-04 conserva su número para que las referencias del #11 sigan siendo válidas.

**Lo que deliberadamente no se restringe:**

- **Un menú puede contener alimentos de grupos prohibidos por su dieta.** La composición real de un menú no se contrasta con su dieta ni con su parametrización (#7, §4). La consulta 5 existe precisamente para detectar esos desvíos, y su conjunto de prueba los incluye a propósito.
- **Un grupo puede quedar prohibido por las dos vías.** No se exige que los grupos excluidos por las restricciones de una dieta figuren también como *Restringido* en su `Cobertura`, ni se impide que figuren: la unión de la D9-1 da el mismo resultado.
- **`Excluye` no sustituye a la ficha nutricional.** Resuelve las restricciones que excluyen grupos completos. La compatibilidad fina con los alérgenos de cada alimento sigue en la ficha nutricional ampliada, gestionada fuera del modelo relacional (issue #8).

---

## 5. Restricciones entre submodelos, para el issue #11

Restricciones que involucran a más de un submodelo y que la consolidación deja identificadas. Su especificación final está en `docs/Restricciones de integridad.md` (issue #11).

| Restricción | Submodelos | Estado tras la consolidación |
|---|---|---|
| Quien crea o valida un menú está autorizado para su dieta (`Crea`, `Valida` ⇒ `Autoriza`). | #6, #7 | Registrada en el #6 como R6-22 |
| Un paciente solo recibe menús de dietas en las que está inscrito (`Asigna` ⇒ `Inscribe`). | #6, #7 | Registrada en el #6 como R6-23 |
| Solo se registra consumo de alimentos que están en el menú asignado, directamente (`Incluye`) o dentro de un plato incluido (`Compone`). | #5, #7 | Especificada en el #11 como R11-01 |
| Un nutricionista solo clasifica los alimentos que él mismo describió. | #5, #6 | Registrada en el #5 como R5-17 |
| Grupos requeridos y prohibidos de una dieta: coherencia (R9-04) y verificación de los menús (consulta 5). | #5, #6, #7 | R9-04 en este documento; la verificación de los menús no se restringe (§4) |
| Eliminación de nutricionistas, pacientes, dietas o menús con hechos asociados en otros submodelos. | #5, #6, #7 | Especificada en el #11 como R11-02 (política de borrado) |

---

## 6. Correspondencia con las consultas

Decisiones de las consultas formalizadas que el MERX recoge. La verificación completa de cada consulta sobre el modelo está en `docs/Validación del modelo contra las consultas.md` (issue #10).

| Consulta | Decisión | Dónde queda en el MERX |
|---|---|---|
| 1 | Discriminador de menú automático (§7-A); parámetros de generación almacenados e inmutables (§7-B y §7-D); creador y revisor distintos (§7-C) | `EsAutomático`; `CantidadTotalAlimentos`, `Distribución` y `Cubre(GrupoNutricional, Menú)`; `Crea` y `Validación` |
| 2 | Menús finales derivables (§7-A); platos descompuestos en alimentos (§7-B); cada aparición de un alimento en un menú es identificable (§8) | La validación más reciente con `Aprobado`; `Compone`; las ocurrencias de `Incluye` |
| 3 | Validación histórica (§7-A) por cualquier nutricionista autorizado (§7-B), con su dictamen (§7-C) | `Validación` con `FechaValidación`, `Observaciones` y `Aprobado`; `Autoriza` |
| 4 | Consumo por (paciente, menú, alimento) con estado de aceptación (§7-A); valoración anclada al menú recibido (§7-C); asignación única por par, sin fechas (§7-D) | `Consumo` con `Aceptación`; `Evalúa(Asignación, Valoración)`; `Asignación` |
| 5 | Criterios de equilibrio en dos niveles (§7-A); grupos obligatorios y prohibidos de la dieta, computables (§7-C) | `Cobertura` con `Tipo`, `Restringe` y `Excluye` (D9-1); parámetros del menú |
| 6 | Dieta del alimento a través del menú (§7-B); el resultado de una revalorización es una nueva valoración (§7-D y §7-E); sin ventanas temporales (§7-F); escala del resultado (§7-G) | `Consume` → `Asignación` → `Menú` → `Dieta`; `Revalora` y `Genera`; ancla en `Menú.FechaCreación`; `ResultadoNutricional` |
