# Convenciones de modelado MERX

**Asignatura:** Bases de Datos II / Ingeniería de Software — Curso 2026-2027
**Fuente:** contenidos de Modelo Entidad-Relación Extendido, atributos y sus restricciones, estudiados previamente en Bases de Datos I — asignatura precedente cuyos contenidos esta materia retoma y profundiza, según sus propias orientaciones metodológicas.
**Alcance de este documento:** notación gráfica extendida (generalización/especialización, entidades débiles, restricciones sobre atributos), convención de nombres, y herramienta de diagramado para el MERX del proyecto, fijadas antes de iniciar el modelado. Aplica a partir de los submodelos (issues #5, #6, #7) y de su consolidación (issue #9).

> **Actualización del 28/10.** Esta notación es también la del MERX del informe (`docs/informe.md`, §3; lámina en `docs/MERX consolidado/MERXInforme.drawio`), que reemplazó al MERX consolidado del #9 como modelo vigente. Del 19/10 al 27/10, el rediseño posterior a la revisión del profesor fijó tres convenciones nuevas:
> - **nombres únicos de relación** (§2, #168);
> - **agregación solo cuando la relación participa en otra** (§3, #166);
> - **especialización total** marcada con línea doble (E1, §7).
>
> También pasó a tener aplicación lo que antes no la tenía: una entidad débil (`Servicio`) y una especialización (`ItemAlimentario`). Los ejemplos de las secciones siguientes citan el modelo vigente; los del MERX consolidado se conservan donde explican cómo se llegó a él.

---

## 1. Herramienta de diagramado

**draw.io**, tal como adelantaba el propio issue.

La notación del curso incluye generalización/especialización (§6) y agregación (§3), construcciones que el MERX puede requerir. Ninguna de las dos tiene una primitiva nativa en Mermaid con la semántica exacta que exige este modelo (discriminador propio para partición disjunta, atributo heredado marcado con borde punteado, rectángulo de agregación envolvente), por lo que draw.io evita tener que forzar esas construcciones.

**Versionado:** el archivo fuente editable `.drawio` de cada submodelo se guarda en `docs/Submodelos Relacionales del MERX/`; el MERX consolidado (issue #9), con su documento de decisiones, en `docs/MERX consolidado/`.

**Formato:** Times New Roman 30 en figuras y 25 en cardinalidades. Los submodelos y el MERX consolidado usan páginas A4 verticales, separadas en páginas de entidades —donde se dibujan los atributos— y páginas de relaciones.

---

## 2. Convención de nombres

| Elemento | Convención | Ejemplo |
|---|---|---|
| Entidad / relación | PascalCase, singular, español | `Alimento`, `GrupoNutricional`, `Compone` |
| Relación | Verbo en infinitivo o presente | `Crea`, `Valida`, `Incluye`, `Compone` |
| Atributo | PascalCase | `FechaCreación`, `CantidadTotalAlimentos` |
| Atributo llave/identificador | Sufijo `Id` | `AlimentoId`, `DietaId` |
| Multi-palabra | Sin guion bajo ni espacio (PascalCase ya separa) | `TipoPreparación` |

> **Convención adoptada:** se usa PascalCase en vez de las mayúsculas con que se dibujan las entidades en el material de referencia, para mantener consistencia con los nombres ya fijados en el glosario del dominio (issue #1). Es una diferencia puramente tipográfica, no de fondo, y no afecta la lectura ni la evaluación del diagrama.
>
> Para la llave se usa el sufijo `Id` (`AlimentoId`) en vez de la abreviatura de una sola letra (`#A`, `#P`...): esa abreviatura resulta ambigua entre entidades del dominio que comparten inicial (por ejemplo, Dieta y Desempeño, o Consumo y Cobertura), así que se descarta en favor del nombre real del atributo.
>
> **Cada relación tiene un nombre único** (#168). Cuando dos relaciones comparten verbo, el nombre se completa con el participante que las distingue: `PerteneceGrupo`, `PerteneceDieta`, `PerteneceSala`, `CubreDieta`, `CubreMenú`. Hasta el MERX consolidado, una relación se identificaba por su nombre **y** sus participantes, y el mismo verbo se repetía entre pares distintos (`Pertenece`, `Tiene`, `Corresponde`, `Cubre`). Se cambió porque un mismo nombre para dos relaciones es ambiguo cuando el texto las nombra sin sus participantes. Es una ambigüedad del mismo tipo que la que el profesor señaló en P2-6, sobre qué relaciona `Corresponde`, y el informe ya había tenido que distinguir `CubreDieta` de `CubreMenú`.

---

## 3. Notación de entidades

| Tipo | Notación | Ejemplo en el dominio |
|---|---|---|
| Entidad fuerte | Rectángulo de borde simple | `ItemAlimentario`, `Dieta`, `Paciente` |
| Entidad débil | Rectángulo de **borde doble** — depende de otra entidad para identificarse | `Servicio`, que depende de la agregación `Asignación` por la relación identificadora `Sirve`: su llave es la de la asignación más `FechaServicio`, porque un menú asignado se sirve en varios días (heurística de la fecha, §5). En el MERX consolidado no había ninguna: `Consumo`, que el glosario señalaba como débil, había quedado como agregación sin fecha. |
| Agregación | Rectángulo de borde simple que **envuelve** una relación completa (dos entidades + su rombo), con su nombre escrito dentro, abajo a la derecha, o en otra esquina interior si líneas o figuras ocupan esa. Hereda como llave la de la relación que envuelve (atributos heredados, §5). | Se emplea **solo cuando la relación participa a su vez en otra relación**. Si la relación solo tiene atributos propios, estos cuelgan del rombo, sin agregación. Esta versión de la regla es la del profesor (#166). La anterior admitía también la agregación «cuando esa relación necesita atributos propios», y esa mitad era incorrecta. En el modelo vigente hay dos: `Validación`, que participa en `Origina`, y `Asignación`, que participa en `Evalúa` y en `Sirve`. En el MERX consolidado también eran agregaciones `Composición`, `Distribución`, `Cobertura` y `Consumo`, solo para colgar sus atributos. |

> **Convención adoptada — entidades repetidas:** una entidad puede dibujarse más de una vez, como rectángulo sin atributos que representa la misma entidad y puede llevar relaciones. Se repite entre páginas, y también dentro de cada agregación en la que participa, porque una misma figura no puede quedar dentro de varias cajas sin que sus bordes se crucen. Los atributos se dibujan en una sola aparición, en la página de entidades. Es el caso de `Menú`, que en el MERX consolidado aparece dentro de `Validación` y de `Distribución` en la misma página (issue #9, decisión D9-4). La cardinalidad de una entidad que está dentro de una agregación, hacia una relación que queda fuera, se escribe sobre su línea, justo fuera del borde de la caja.

---

## 4. Notación de relaciones

- **Forma:** rombo de borde simple; **borde doble** cuando la relación es identificadora de una entidad débil (en ese caso, la cardinalidad del lado de la entidad fuerte/dueña es siempre `(1,1)`).
- **Nombre:** un verbo, escrito dentro del rombo.
- **Cardinalidad:** par **`(mínimo, máximo)`** junto a la línea, en el extremo de cada entidad participante — no se usa notación de pata de gallo. Se emplea `*` para "muchos" cuando no hay un tope conocido.
  - Ejemplo: `Alimento (0,*) — Incluye — (0,*) Menú`.
  - Cuando la cardinalidad exacta no está especificada, se adopta la menos restrictiva, `(0,*)`, salvo que el enunciado del proyecto indique explícitamente lo contrario para ese caso.
- **Grado:** binaria (el caso general), unaria/recursiva (dos líneas del mismo rombo hacia el mismo rectángulo, cada una con su propia cardinalidad y rol), o n-aria (el rombo se conecta directamente a las n entidades participantes, sin intermediarios).

> **Regla de dirección:** el par dibujado junto a una entidad A **no** describe la participación propia de A. Describe cuántas instancias de A se asocian a **una sola instancia fija de la entidad opuesta**. Para hallarlo: se fija una instancia de la entidad del otro extremo y se cuenta cuántas instancias de A pueden asociarse a ella.
>
> Ejemplo sin ambigüedad posible: `Paciente — Pertenece — Sala`. La etiqueta junto a `Paciente` responde "¿cuántos pacientes hay por cada sala?" → muchos, `(0,*)`. La etiqueta junto a `Sala` responde "¿cuántas salas hay por cada paciente?" → una, `(1,1)`. Correcto: `Paciente (0,*) — Pertenece — (1,1) Sala`.
>
> Antes de dibujar cualquier cardinalidad, verificar con esta regla.

> **Convención adoptada:** en el contenido revisado convive, para la cardinalidad, una notación con coma (`0,*`) junto a una variante con dos puntos (`0:M`), incluso combinadas dentro de un mismo material. Se fija la notación con coma, `(mínimo, máximo)`, como la única válida para este proyecto, por ser la de mayor rigor teórico; la variante con dos puntos no debe usarse.

---

## 5. Notación de atributos

| Tipo | Notación |
|---|---|
| Simple | Óvalo de borde simple sólido, conectado directo a la entidad |
| Llave / identificador | Nombre subrayado dentro del óvalo (puede ser compuesta: varios óvalos subrayados) |
| Heredado (de entidad débil, de agregación, o de superclase en una especialización) | Óvalo de **borde punteado**, generalmente también subrayado |

**Restricción de atomicidad — no existen los atributos multivaluados ni compuestos.** El MERX de este curso exige que todo atributo sea atómico. Toda propiedad que pueda tomar más de un valor a la vez, o que tenga subcampos propios, se modela como **entidad más relación**, nunca como un atributo-lista o un atributo con estructura interna.

Esta restricción ya tiene aplicación directa sobre el dominio del proyecto:
- `Restricción alimentaria` y `Grupo nutricional a cubrir` no son atributos-lista de `Dieta`, sino entidades conectadas por relaciones N:M: `CubreDieta` con `GrupoNutricional`, con el atributo `Tipo` en el rombo, y `Restringe` con `RestricciónAlimentaria` (issue #6; en el MERX consolidado, `Cubre` dentro de la agregación `Cobertura`).
- `Parámetros de generación` de `Menú` tampoco es un atributo único: se descompone en un atributo simple (`CantidadTotalAlimentos`), la relación `Distribuye` hacia `TipoPreparación`, con `Proporción` en el rombo, y la relación `CubreMenú` hacia `GrupoNutricional` (issue #7; en el MERX consolidado, la agregación `Distribución` y la relación `Cubre`).

**Patrón "nomenclador":** cuando un atributo debería restringirse a un catálogo cerrado de valores válidos, se modela como entidad independiente referenciada, no como texto libre. En el modelo hay siete: `GrupoNutricional`, `TipoPreparación`, `NivelCalórico`, `ProgramaDeAtención`, `RestricciónAlimentaria`, `Especialidad` y `ResultadoNutricional`.

**Heurística fecha — ¿atributo o entidad?** Si el hecho puede repetirse en el tiempo entre el mismo par de participantes, la fecha pasa a formar parte de la identidad (entidad débil, o llave de la relación); si el hecho ocurre una sola vez, la fecha queda como atributo simple. En el modelo vigente se aplica en los dos sentidos: un menú se asigna una sola vez a cada paciente, así que la asignación no lleva fecha; pero un menú asignado se sirve en varios días, así que el servicio es una entidad débil con `FechaServicio` en su llave, y el consumo se registra por servicio. El MERX consolidado (consulta 4, §7-D) no distinguía el servicio, y el ancla temporal era `Menú.FechaCreación`.

---

## 6. Notación de generalización/especialización (ISA)

- **Símbolo:** un arco pequeño sobre la línea que conecta la subclase con la superclase, pegado al lado de la subclase.
- **Especialización simple** (una sola subclase, sin discriminador): línea directa entre superclase y subclase con el símbolo ISA. Solo se dibuja si la subclase aporta atributos o relaciones propias; si no aporta nada, no hace falta modelarla aparte.
- **Especialización por partición** (dos o más subclases disjuntas): se agrega un discriminador en forma de **hexágono alargado** — distinto de un rombo de relación — etiquetado con el atributo que distingue las subclases. Cada rama lleva también el símbolo ISA antes de entrar a su rectángulo.

**Aplicación en el proyecto:** `ItemAlimentario` es la superclase de `Alimento` y `Plato`, con una especialización **total y disjunta**:
- **es disjunta** porque la marca el discriminador `Clase`;
- **es total** porque todo ítem es un alimento o un plato, y se dibuja con la línea doble de la E1 (§7).

Las dos subclases comparten las clasificaciones y la inclusión en el menú, y `Compone` relaciona un plato con sus alimentos (informe, §2.1). Hasta el MERX consolidado, la decisión D1 del glosario (issue #1) modelaba `Plato` como entidad separada de `Alimento`, sin especialización, y esta notación no tenía aplicación. El jefe de nutrición sigue sin entidad propia: es el atributo `EsJefe` de `Nutricionista` (#189).

---

## 7. Decisiones propias del equipo

Casos donde el contenido de Bases de Datos I no define una convención y el equipo debe fijar la propia, dejando constancia de que no proviene de la asignatura.

### E1 — Especialización total y especialización solapada

**Situación:** el contenido revisado cubre especialización simple y partición disjunta (con discriminador), pero no define notación para especialización total/obligatoria (que todo elemento de la superclase deba pertenecer a alguna subclase) ni para subclases solapadas (no disjuntas).

**Decisión:**
- **Especialización total:** se marca con una **línea doble entre la superclase y el discriminador**, y la lámina lleva una leyenda que lo explica («Línea doble: especialización total»), porque la notación no proviene del curso. La necesitó `ItemAlimentario` (§6) y se usó en el MERX de la reentrega del 27/10. Esta decisión se documenta después de usarla, no antes como pedía la E1. Queda validarla con el profesor.
- **Especialización solapada:** sigue pendiente. Ningún caso del modelo la necesita.

---

## 8. Resumen ejecutivo — leyenda de notación

| Concepto | Símbolo |
|---|---|
| Entidad fuerte | Rectángulo, borde simple |
| Entidad débil | Rectángulo, borde doble |
| Agregación | Rectángulo envolvente, borde simple |
| Entidad repetida en otra agregación | Mismo rectángulo, sin atributos |
| Relación | Rombo, borde simple |
| Relación identificadora | Rombo, borde doble |
| Cardinalidad | `(mínimo, máximo)` en el extremo de cada entidad |
| Atributo simple | Óvalo, borde simple |
| Atributo llave | Óvalo, nombre subrayado |
| Atributo heredado | Óvalo, borde punteado |
| Especialización (ISA) | Arco junto a la línea, pegado a la subclase |
| Partición disjunta | Hexágono alargado como discriminador |
| Especialización total | Línea doble entre la superclase y el discriminador, con leyenda en la lámina (E1) |

---

## 9. Conclusiones y siguientes pasos

La notación queda fijada con base directa en lo estudiado en Bases de Datos I. Solo se aparta del curso donde el contenido no alcanza (E1, §7): la especialización total se resolvió cuando el modelo la necesitó, y la solapada sigue abierta.

Los submodelos (issues #5, #6, #7) y su consolidación (issue #9) se ajustan a este documento; las decisiones propias de la consolidación están en `docs/MERX consolidado/MERX consolidado.md`. El MERX del informe, que es el modelo vigente, también se ajusta a él, con las tres convenciones que se agregaron después (nombres únicos, criterio de agregación y especialización total).
