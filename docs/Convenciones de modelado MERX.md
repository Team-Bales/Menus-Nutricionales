# Convenciones de modelado MERX

**Asignatura:** Bases de Datos II / Ingeniería de Software — Curso 2026-2027
**Fuente:** contenidos de Modelo Entidad-Relación Extendido, atributos y sus restricciones, estudiados previamente en Bases de Datos I — asignatura precedente cuyos contenidos esta materia retoma y profundiza, según sus propias orientaciones metodológicas.
**Alcance de este documento:** notación gráfica extendida (generalización/especialización, entidades débiles, restricciones sobre atributos), convención de nombres, y herramienta de diagramado para el MERX del proyecto, fijadas antes de iniciar el modelado. Aplica a partir de los submodelos (issues #5, #6, #7) y de su consolidación (issue #9).

---

## 1. Herramienta de diagramado

**draw.io**, tal como adelantaba el propio issue.

El MERX del proyecto probablemente va a necesitar generalización/especialización (ver §6 y la decisión abierta D1 del glosario) y, en menor medida, agregación (§3). Ninguna de las dos tiene una primitiva nativa en Mermaid con la semántica exacta que exige este modelo (discriminador propio para partición disjunta, atributo heredado marcado con borde punteado, rectángulo de agregación envolvente), por lo que draw.io evita tener que forzar esas construcciones.

**Versionado:** el archivo fuente editable `.drawio` se guarda en `docs/diagramas/`, junto con su exportación a `.svg` para poder consultar el diagrama sin necesidad de abrir la herramienta.

---

## 2. Convención de nombres

| Elemento | Convención | Ejemplo |
|---|---|---|
| Entidad / relación | PascalCase, singular, español | `Alimento`, `GrupoNutricional`, `Compone` |
| Relación | Verbo en infinitivo o presente | `Crea`, `Valida`, `Incluye`, `Compone` |
| Atributo | PascalCase | `FechaCreación`, `NivelCalórico` |
| Atributo llave/identificador | Sufijo `Id` | `AlimentoId`, `DietaId` |
| Multi-palabra | Sin guion bajo ni espacio (PascalCase ya separa) | `TipoPreparación` |

> **Convención adoptada:** se usa PascalCase en vez de las mayúsculas con que se dibujan las entidades en el material de referencia, para mantener consistencia con los nombres ya fijados en el glosario del dominio (issue #1). Es una diferencia puramente tipográfica, no de fondo, y no afecta la lectura ni la evaluación del diagrama.
>
> Para la llave se usa el sufijo `Id` (`AlimentoId`) en vez de la abreviatura de una sola letra (`#A`, `#P`...): esa abreviatura resulta ambigua entre entidades del dominio que comparten inicial (por ejemplo, Dieta y Desempeño, o Consumo y Cobertura), así que se descarta en favor del nombre real del atributo.

---

## 3. Notación de entidades

| Tipo | Notación | Ejemplo en el dominio |
|---|---|---|
| Entidad fuerte | Rectángulo de borde simple | `Alimento`, `Dieta`, `Paciente` |
| Entidad débil | Rectángulo de **borde doble** — depende de otra entidad para identificarse | `Consumo` (identificado por Paciente + Menú + Fecha, ya señalado así en el glosario) |
| Agregación | Rectángulo de borde simple que **envuelve** una relación completa (dos entidades + su rombo) | Se emplea cuando esa relación necesita atributos propios o participar como extremo de otra relación. No hay un caso confirmado todavía en el dominio; queda disponible para cuando algún submodelo lo requiera. |

---

## 4. Notación de relaciones

- **Forma:** rombo de borde simple; **borde doble** cuando la relación es identificadora de una entidad débil (en ese caso, la cardinalidad del lado de la entidad fuerte/dueña es siempre `(1,1)`).
- **Nombre:** un verbo, escrito dentro del rombo.
- **Cardinalidad:** par **`(mínimo, máximo)`** junto a la línea, en el extremo de cada entidad participante — no se usa notación de pata de gallo. Se emplea `*` para "muchos" cuando no hay un tope conocido.
  - Ejemplo: `Alimento (0,*) — Incluye — (0,*) Menú`.
  - Cuando la cardinalidad exacta no está especificada, se adopta la menos restrictiva, `(0,*)`, salvo que el enunciado del proyecto indique explícitamente lo contrario para ese caso.
- **Grado:** binaria (el caso general), unaria/recursiva (dos líneas del mismo rombo hacia el mismo rectángulo, cada una con su propia cardinalidad y rol), o n-aria (el rombo se conecta directamente a las n entidades participantes, sin intermediarios).

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
- `Restricción alimentaria` y `Grupo nutricional a cubrir` no son atributos-lista de `Dieta`, sino entidades conectadas por relaciones N:M — la relación `Cubre` con `GrupoNutricional` ya está planteada así en el glosario; falta la relación equivalente para `Restricción alimentaria`.
- `Parámetros de generación` de `Menú` tampoco es un atributo único: se descompone en un atributo simple (`CantidadTotalAlimentos`) más relaciones con atributo propio hacia los nomencladores que ya existen en el modelo (`TipoPreparación`, `GrupoNutricional`). Este ajuste ya fue comunicado al equipo responsable del issue #7.

**Patrón "nomenclador":** cuando un atributo debería restringirse a un catálogo cerrado de valores válidos, se modela como entidad independiente referenciada, no como texto libre — patrón ya aplicado en el dominio a `GrupoNutricional`, `TipoPreparación` y `NivelCalórico`.

**Heurística fecha — ¿atributo o entidad?** Si el hecho puede repetirse en el tiempo entre el mismo par de participantes, la fecha pasa a formar parte de la identidad (entidad débil, o llave de la relación); si el hecho ocurre una sola vez, la fecha queda como atributo simple. Es el mismo razonamiento ya aplicado a `Consumo` (Paciente + Menú + Fecha) en el glosario.

---

## 6. Notación de generalización/especialización (ISA)

- **Símbolo:** un arco pequeño sobre la línea que conecta la subclase con la superclase, pegado al lado de la subclase.
- **Especialización simple** (una sola subclase, sin discriminador): línea directa entre superclase y subclase con el símbolo ISA. Solo se dibuja si la subclase aporta atributos o relaciones propias; si no aporta nada, no hace falta modelarla aparte.
- **Especialización por partición** (dos o más subclases disjuntas): se agrega un discriminador en forma de **hexágono alargado** — distinto de un rombo de relación — etiquetado con el atributo que distingue las subclases. Cada rama lleva también el símbolo ISA antes de entrar a su rectángulo.

**Relevancia inmediata para el proyecto:** la decisión abierta D1 del glosario del dominio (issue #1) — si `Plato` termina siendo una especialización de `Alimento` en vez de una entidad separada con relación `Compone` — es la que determina si algún submodelo va a necesitar esta notación. Si D1 se resuelve hacia especialización, se aplica tal como queda descrito arriba.

---

## 7. Decisiones propias del equipo

Casos donde el contenido de Bases de Datos I no define una convención y el equipo debe fijar la propia, dejando constancia de que no proviene de la asignatura.

### E1 — Especialización total y especialización solapada

**Situación:** el contenido revisado cubre especialización simple y partición disjunta (con discriminador), pero no define notación para especialización total/obligatoria (que todo elemento de la superclase deba pertenecer a alguna subclase) ni para subclases solapadas (no disjuntas).

**Decisión:** queda pendiente. Si algún submodelo llega a necesitar alguno de estos dos casos, se documenta aquí la extensión adoptada antes de usarla en el diagrama, y se valida con el profesor en la próxima consulta.

---

## 8. Resumen ejecutivo — leyenda de notación

| Concepto | Símbolo |
|---|---|
| Entidad fuerte | Rectángulo, borde simple |
| Entidad débil | Rectángulo, borde doble |
| Agregación | Rectángulo envolvente, borde simple |
| Relación | Rombo, borde simple |
| Relación identificadora | Rombo, borde doble |
| Cardinalidad | `(mínimo, máximo)` en el extremo de cada entidad |
| Atributo simple | Óvalo, borde simple |
| Atributo llave | Óvalo, nombre subrayado |
| Atributo heredado | Óvalo, borde punteado |
| Especialización (ISA) | Arco junto a la línea, pegado a la subclase |
| Partición disjunta | Hexágono alargado como discriminador |

---

## 9. Conclusiones y siguientes pasos

La notación queda fijada con base directa en lo estudiado en Bases de Datos I, sin recurrir a convenciones genéricas ajenas al curso salvo en el único punto donde el contenido no alcanza (E1, §7), dejado explícitamente abierto en vez de resuelto por conveniencia. Los submodelos (issues #5, #6, #7) y su consolidación (issue #9) deben ajustarse a este documento; en particular, el submodelo que resuelva D1 del glosario es el que decide si la sección 6 (ISA) termina teniendo aplicación en el proyecto.
