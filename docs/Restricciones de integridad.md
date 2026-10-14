# Especificación de restricciones de integridad

**Asignatura:** Bases de Datos II / Ingeniería de Software — Curso 2026-2027
**Issue:** #11
**Fuentes:** submodelos #5, #6 y #7 (`docs/Submodelos Relacionales del MERX/`), MERX consolidado (`docs/MERX consolidado/`, issue #9) y ficha nutricional (`docs/Ficha nutricional MongoDB/`, issue #8).
**Alcance de este documento:** especificación consolidada de las restricciones de integridad del modelo —estructurales (clave, referenciales, dominio y cardinalidad) y semánticas—, cada una con su evidencia y el mecanismo con que se hace cumplir. Es la sección 5 del informe «Diseño de la base de datos».

---

## 0. Criterio

Se consideran **restricciones de integridad** las condiciones que todo estado válido de la base de datos debe cumplir. Se agrupan por el elemento del modelo que las origina: **clave**, **referenciales**, **dominio**, **cardinalidad** y **semánticas**. Las estructurales se expresan con el modelo; las semánticas acotan quién puede operar sobre los datos y con qué condiciones.

**Procedencia y numeración.** Se conservan los identificadores originales de cada submodelo —`R5-xx` del #5, `R6-xx` del #6, `R7-xx` del #7—, de modo que cada restricción se pueda rastrear hasta el submodelo que la justifica. Las de la consolidación son `R9-xx` (MERX consolidado, issue #9), las de la ficha nutricional son `R8-xx` (issue #8) y las que define este documento son `R11-01` y `R11-02`. No se renumera ninguna.

**Evidencia.** Cada restricción cita la línea del enunciado o la sección de la consulta formalizada (`docs/consultas/`) que la sustenta; la numeración de líneas es la del texto plano del enunciado, la misma empleada en el glosario del dominio, en los submodelos y en las consultas. Las que no tienen cita directa se declaran expresamente como **decisión del equipo**, y se reunen en §8.

**Vocabulario de mecanismos.** Los mecanismos usan el vocabulario del issue #11: PK, FK, UNIQUE, CHECK, NOT NULL, trigger y capa de aplicación. Una misma restricción puede necesitar más de uno, y en ese caso la fila los declara todos.

**Alcance material.** Este documento especifica las restricciones y sus mecanismos; no incluye el esquema. El esquema relacional completo está en la guía del #12, con los nombres de tablas y columnas que usan los fragmentos de §7, y la implementación corresponde al backend.

**Qué se deja abierto.** Las ausencias deliberadas —condiciones que el modelo admite y que por tanto no se restringen— se reúnen en §5, con su origen.

---

## 1. Restricciones estructurales

### 1.1 Clave

| # | Restricción | Evidencia | Mecanismo | Origen |
|---|---|---|---|---|
| R5-01 | `AlimentoId` identifica unívocamente a cada alimento y `PlatoId` a cada plato. | líneas 23–24, 29 | PK | #5 |
| R5-02 | `GrupoNutricionalId`, `TipoPreparaciónId` y `NivelCalóricoId` identifican unívocamente a cada valor de su nomenclador. | líneas 24–25, 30–31 | PK | #5 |
| R5-03 | Un mismo alimento aparece a lo sumo una vez en la composición de un plato: el par (`PlatoId`, `AlimentoId`) identifica cada ocurrencia de la agregación `Composición`. | decisión del equipo | PK compuesta | #5 |
| R6-01 | `DietaId` identifica unívocamente a cada dieta, `NutricionistaId` a cada nutricionista, `PacienteId` a cada paciente y `SalaId` a cada sala. | líneas 42, 43-45, 49 | PK | #6 |
| R6-02 | `ProgramaDeAtenciónId`, `RestricciónAlimentariaId` y `EspecialidadId` identifican unívocamente a cada valor de su nomenclador. | líneas 27, 42, 44 | PK | #6 |
| R6-03 | El par (`NutricionistaId`, `DietaId`) identifica cada autorización: un nutricionista no aparece dos veces autorizado para la misma dieta. | decisión del equipo | PK compuesta | #6 |
| R6-04 | El par (`PacienteId`, `DietaId`) identifica cada inscripción: un paciente no aparece dos veces inscrito en la misma dieta. | decisión del equipo | PK compuesta | #6 |
| R6-05 | El par (`DietaId`, `GrupoNutricionalId`) y el par (`DietaId`, `RestricciónAlimentariaId`) identifican cada ocurrencia de `Cubre` y de `Restringe`, respectivamente. | decisión del equipo | PK compuesta | #6 |
| R7-01 | `MenúId` identifica unívocamente a cada menú, `ValoraciónId` a cada valoración y `RevalorizaciónId` a cada revalorización. | líneas 22–23, 35–37 | PK | #7 |
| R7-02 | `ResultadoNutricionalId` identifica unívocamente a cada categoría del nomenclador `ResultadoNutricional`. | consulta 6, §7-G | PK | #7 |
| R7-03 | Un menú registra a lo sumo una proporción por tipo de preparación: el par (`MenúId`, `TipoPreparaciónId`) identifica cada ocurrencia de `Distribución`. | líneas 38–39 | PK compuesta | #7 |
| R7-04 | Un revisor registra a lo sumo una validación por menú: el par (`NutricionistaId`, `MenúId`) identifica cada ocurrencia de `Validación`. Un mismo menú sí admite validaciones de revisores distintos. | consulta 3, §7-A; el tope por revisor, decisión del equipo | PK compuesta | #7 |
| R7-05 | Un menú se asigna a lo sumo una vez a un mismo paciente: el par (`MenúId`, `PacienteId`) identifica cada ocurrencia de `Asignación`. | líneas 33–34; consulta 4, §7-D | PK compuesta | #7 |
| R7-06 | Cada alimento tiene a lo sumo un registro de consumo por asignación: la terna (`MenúId`, `PacienteId`, `AlimentoId`) identifica cada ocurrencia de `Consumo`. | línea 35; consulta 4, §7-A | PK compuesta | #7 |
| R7-07 | Un alimento, un plato o un grupo nutricional aparece a lo sumo una vez en un mismo menú: los pares de `Incluye` (`MenúId`, `AlimentoId`), (`MenúId`, `PlatoId`) y de `Cubre` (`MenúId`, `GrupoNutricionalId`) son únicos. | consulta 2, §8 («cada aparición de un alimento en un menú es identificable») | PK compuesta | #7 |
| R9-01 | El par (`RestricciónAlimentariaId`, `GrupoNutricionalId`) identifica cada ocurrencia de `Excluye`: una restricción no excluye dos veces el mismo grupo. | decisión del equipo | PK compuesta | #9 |

> **Nota sobre R6-03 a R6-05.** `Autoriza`, `Inscribe`, `Cubre` y `Restringe` son tablas de interconexión sin atributos propios: su identidad es el par de claves primarias de los participantes (ver submodelo #6, R6-03).

### 1.2 Referenciales

| # | Restricción | Evidencia | Mecanismo | Origen |
|---|---|---|---|---|
| R5-04 | Toda clasificación (`Pertenece`, `Corresponde`, `Tiene`) de un alimento o de un plato referencia un valor existente del nomenclador correspondiente. | líneas 30–31 | FK | #5 |
| R5-05 | Toda ocurrencia de `Compone` referencia un plato y un alimento existentes. | líneas 54–55 | FK | #5 |
| R5-06 | Todo alimento y todo plato referencia al nutricionista que lo describió, el cual debe existir. | líneas 29, 78–80 | FK | #5 |
| R5-07 | No se elimina un valor de nomenclador mientras lo use algún alimento o plato, ni un alimento mientras forme parte de algún plato. | decisión del equipo | FK con `ON DELETE RESTRICT` | #5 |
| R6-06 | Toda dieta corresponde a un programa de atención existente. | línea 42 | FK | #6 |
| R6-07 | Toda ocurrencia de `Cubre` referencia una dieta y un grupo nutricional existentes. | líneas 24-25, 42-43 | FK | #6 |
| R6-08 | Toda ocurrencia de `Restringe` referencia una dieta y una restricción alimentaria existentes. | líneas 27, 42-43 | FK | #6 |
| R6-09 | Toda autorización referencia un nutricionista y una dieta existentes. | líneas 32, 44-45 | FK | #6 |
| R6-10 | Toda inscripción referencia un paciente y una dieta existentes. | líneas 49-50 | FK | #6 |
| R6-11 | Todo paciente referencia la sala a la que pertenece, la cual debe existir. | línea 49 | FK | #6 |
| R6-12 | Todo nutricionista referencia la especialidad que ejerce, la cual debe existir. | línea 44 | FK | #6 |
| R6-13 | No se elimina un valor de nomenclador mientras lo use alguna dieta o algún nutricionista, ni una dieta mientras figure en alguna autorización o alguna inscripción. | decisión del equipo | FK con `ON DELETE RESTRICT` | #6 |
| R7-08 | Todo menú referencia la dieta a la que pertenece y el nutricionista que lo creó, ambos existentes. | líneas 38, 68–69 | FK | #7 |
| R7-09 | Toda ocurrencia de `Incluye`, `Cubre` y `Distribución` referencia un menú existente y un alimento, plato, grupo nutricional o tipo de preparación existente. | líneas 31, 38–39 | FK | #7 |
| R7-10 | Toda ocurrencia de `Validación` referencia un nutricionista y un menú existentes. | líneas 40–41, 72–73 | FK | #7 |
| R7-11 | Toda ocurrencia de `Asignación` referencia un menú y un paciente existentes; todo `Consumo` referencia una asignación existente y un alimento existente. Así, solo se registra consumo de menús efectivamente asignados. | líneas 33–35 | FK; FK compuesta (`MenúId`, `PacienteId`) hacia `Asignación` | #7 |
| R7-12 | Toda valoración referencia la asignación que evalúa, el nutricionista que la emite y una categoría existente de `ResultadoNutricional`. | líneas 35–36, 50; consulta 4, §7-C; consulta 6, §7-G | FK; FK compuesta hacia `Asignación` | #7 |
| R7-13 | Toda revalorización referencia al paciente que la solicita, al nutricionista que la ejecuta y a la valoración que revisa; la valoración generada, cuando existe, también debe existir. | líneas 36–37; consulta 6, §7-D y §7-E | FK | #7 |
| R7-14 | No se elimina un menú mientras tenga validaciones o asignaciones, ni una asignación mientras tenga consumos o valoraciones, ni una valoración mientras una revalorización la revise o la haya generado. | decisión del equipo | FK con `ON DELETE RESTRICT` | #7 |
| R9-02 | Toda ocurrencia de `Excluye` referencia una restricción alimentaria y un grupo nutricional existentes. | consulta 5, §7-C | FK | #9 |
| R9-03 | No se elimina una restricción alimentaria ni un grupo nutricional mientras figuren en `Excluye`. | decisión del equipo | FK con `ON DELETE RESTRICT` | #9 |

> **Nota sobre las políticas de borrado.** R5-07, R6-13, R7-14 y R9-03 son las cuatro restricciones que, dentro de cada submodelo, impedirán borrar un hecho del que dependen otros. La política que las coordina entre submodelos es R11-02 (§4).

### 1.3 Dominio

| # | Restricción | Evidencia | Mecanismo | Origen |
|---|---|---|---|---|
| R5-08 | `TipoPreparación` toma exactamente los valores *entrante*, *plato fuerte*, *postre* y *bebida*. | líneas 30–31 | catálogo con esos cuatro valores: CHECK + UNIQUE sobre `Nombre` | #5 |
| R5-09 | `NivelCalórico` toma exactamente los valores *bajo*, *medio* y *alto*. | línea 31 | catálogo con esos tres valores: CHECK + UNIQUE sobre `Nombre` | #5 |
| R5-10 | `GrupoNutricional` es un catálogo abierto: el enunciado lo define como «nomenclador del sistema» sin enumerar sus valores; se exige únicamente que cada nombre sea único. | líneas 24–25 | UNIQUE + NOT NULL sobre `Nombre` | #5 |
| R5-11 | `Nombre` es obligatorio y no vacío en `Alimento`, `Plato` y los tres nomencladores. | decisión del equipo | NOT NULL + CHECK | #5 |
| R5-12 | `Cantidad` de `Composición` es estrictamente positiva. | decisión del equipo | CHECK (`Cantidad > 0`) | #5 |
| R6-14 | `Edad` es mayor o igual que cero. | línea 49 | CHECK (`Edad >= 0`) | #6 |
| R6-15 | `Nombre` es obligatorio y no vacío en `Dieta`, `Nutricionista`, `Paciente`, `Sala` y en los tres nomencladores. | decisión del equipo | NOT NULL + CHECK | #6 |
| R6-16 | `Nombre` es único dentro de cada nomenclador: no hay dos programas de atención, dos restricciones alimentarias ni dos especialidades con el mismo nombre. | decisión del equipo | UNIQUE | #6 |
| R6-17 | `Edad` se registra como un número entero de años. | decisión del equipo | tipo entero | #6 |
| R7-15 | `CantidadTotalAlimentos` es un entero estrictamente positivo. | líneas 38–40; el mínimo, decisión del equipo | CHECK (`CantidadTotalAlimentos > 0`) | #7 |
| R7-16 | `Proporción` de `Distribución` está en el intervalo (0, 1]. | líneas 38–39; el intervalo, decisión del equipo | CHECK (`Proporción > 0 AND Proporción <= 1`) | #7 |
| R7-17 | `EsAutomático` (menú) y `Aprobado` (validación) son booleanos obligatorios. | consulta 1, §7-A; consulta 3, §7-C | NOT NULL | #7 |
| R7-18 | `Aceptación` de `Consumo` toma exactamente los valores *aceptado* y *rechazado*. | línea 35; consulta 4, §7-A | NOT NULL + CHECK (o booleano) | #7 |
| R7-19 | `ResultadoNutricional` tiene exactamente cinco categorías — *Muy mala*, *Mala*, *Normal*, *Buena* y *Excelente* —, con `Valor` 1, 2, 3, 4 y 5 respectivamente; `Nombre` y `Valor` son únicos y obligatorios. | consulta 6, §7-G | catálogo con esos cinco valores: CHECK sobre `Nombre` + CHECK (`Valor BETWEEN 1 AND 5`) + UNIQUE y NOT NULL sobre `Nombre` y sobre `Valor` | #7 |
| R7-20 | `FechaCreación`, `FechaValidación` y `FechaSolicitud` son obligatorias. `Observaciones` es opcional. | líneas 36, 69, 72–73; la opcionalidad de `Observaciones`, decisión del equipo | NOT NULL | #7 |

> **Nota sobre los dominios cerrados.** R5-08, R5-09 y R7-19 se garantizan en dos niveles: la clave foránea impide que una entidad use un valor ajeno al catálogo, y el CHECK sobre el propio catálogo impide ampliarlo (ver submodelo #5, R5-08, y submodelo #7, R7-19).

### 1.4 Cardinalidad

| # | Restricción | Evidencia | Mecanismo | Origen |
|---|---|---|---|---|
| R5-13 | Cada alimento y cada plato tiene exactamente un grupo nutricional, un tipo de preparación y un nivel calórico: `(1,1)` del lado del nomenclador en `Pertenece`, `Corresponde` y `Tiene`. | líneas 30–31 | FK NOT NULL en `Alimento` y en `Plato` | #5 |
| R5-14 | Cada alimento y cada plato tiene exactamente un nutricionista que lo describió: `(1,1)` del lado de `Nutricionista` en `Describe`. | líneas 29, 78–80 | FK NOT NULL en `Alimento` y en `Plato` | #5 |
| R5-15 | Todo plato se compone de al menos un alimento: `(1,*)` del lado de `Alimento` en `Compone`. | líneas 54–55; el mínimo, decisión del equipo | capa de aplicación + trigger diferido | #5 |
| R6-18 | Cada dieta corresponde a exactamente un programa de atención: `(1,1)` del lado de `ProgramaDeAtención` en `Corresponde`. | línea 42 | FK NOT NULL en `Dieta` | #6 |
| R6-19 | Cada paciente pertenece a exactamente una sala: `(1,1)` del lado de `Sala` en `Pertenece`. | línea 49 | FK NOT NULL en `Paciente` | #6 |
| R6-20 | Cada nutricionista ejerce exactamente una especialidad: `(1,1)` del lado de `Especialidad` en `Ejerce`. | línea 44 | FK NOT NULL en `Nutricionista` | #6 |
| R6-21 | Toda dieta cubre al menos un grupo nutricional: `(1,*)` del lado de `GrupoNutricional` en `Cubre`. | líneas 42-43; el mínimo, decisión del equipo | capa de aplicación + trigger diferido | #6 |
| R7-21 | Cada menú tiene exactamente un creador y una dieta: `(1,1)` del lado de `Nutricionista` en `Crea` y del lado de `Dieta` en `Pertenece`. | líneas 38, 68–69 | FK NOT NULL en `Menú` | #7 |
| R7-22 | Todo menú tiene al menos una proporción por tipo de preparación y al menos un grupo nutricional cubierto: `(1,*)` del lado de `TipoPreparación` en `Distribuye` y del lado de `GrupoNutricional` en `Cubre`. | líneas 38–40 | capa de aplicación + trigger diferido | #7 |
| R7-23 | Cada valoración evalúa exactamente una asignación, la emite exactamente un nutricionista y tiene exactamente un resultado: `(1,1)` del lado de `Asignación` en `Evalúa`, de `Nutricionista` en `Emite` y de `ResultadoNutricional` en `Tiene`. | líneas 35–36, 50; consulta 4, §7-C; consulta 6, §7-G | FK NOT NULL en `Valoración` | #7 |
| R7-24 | Cada revalorización tiene exactamente un solicitante, un ejecutor y una valoración revisada: `(1,1)` en `Solicita`, `Ejecuta` y `Revalora`. Genera a lo sumo una valoración, y cada valoración es generada por a lo sumo una revalorización: `(0,1)` en los dos lados de `Genera`. | líneas 36–37; consulta 6, §7-D | FK NOT NULL; FK anulable + UNIQUE para `Genera` | #7 |

> **Nota sobre las cardinalidades mínimas.** R5-15, R6-21 y R7-22 no son expresables con claves foráneas: la entidad se registra antes que sus ocurrencias y la comprobación solo tiene sentido al cierre de la operación. En la capa de aplicación, el alta y su detalle se registran en una misma unidad de trabajo; en PostgreSQL se refuerzan con un trigger de restricción diferido (`DEFERRABLE INITIALLY DEFERRED`), cuyo fragmento está en §7.

---

## 2. Restricciones semánticas

Las restricciones semánticas no acotan los valores que se pueden almacenar, sino **quién puede operar sobre ellos** y bajo qué condiciones. El issue señala cinco de ellas de forma expresa: no duplicar alimentos (R5-16, línea 33); no asignar dos veces un menú al mismo paciente (R7-05, línea 34); que solo el nutricionista autorizado genere o valide menús, y solo de las dietas en las que lo está (R6-22, líneas 44–45); que la clasificación de un alimento sea la de quien lo describió (R5-17, línea 30); y que los dominios de `TipoPreparación` y `NivelCalórico` sean cerrados (R5-08 y R5-09, línea 31). Las cinco están en este documento, cada una en la sección que le corresponde por tipo: tres en la tabla siguiente y las otras dos en §1 (R7-05 en §1.1; R5-08 y R5-09 en §1.3).

| # | Restricción | Evidencia | Mecanismo | Origen |
|---|---|---|---|---|
| R5-16 | No existen alimentos duplicados. | línea 33 | UNIQUE sobre el nombre normalizado | #5 |
| R5-17 | Un nutricionista solo clasifica (`Pertenece`, `Corresponde`, `Tiene`) los alimentos que él mismo describió. | línea 30 | capa de aplicación | #5 |
| R6-22 | Un nutricionista solo genera o valida menús de las dietas en las que está autorizado. | líneas 43-45 | capa de aplicación + trigger (§7) | #6 |
| R6-23 | Un paciente solo recibe menús de las dietas en las que está inscrito. | líneas 49-50 | capa de aplicación + trigger (§7) | #6 |
| R7-25 | Un nutricionista no valida un menú que él mismo creó. | decisión del equipo, a partir de la separación de roles de las líneas 38–41 y la consulta 1, §7-C | trigger (`BEFORE INSERT` en `Validación`) | #7 |
| R7-26 | La fecha de validación no es anterior a la fecha de creación del menú validado. | decisión del equipo | trigger | #7 |
| R7-27 | La parametrización de un menú (`CantidadTotalAlimentos`, `EsAutomático`, `Distribución`, `Cubre`) no cambia después de creado el menú. | líneas 39–40; consulta 1, §7-D y §8 | capa de aplicación + trigger (`BEFORE UPDATE OR DELETE`) | #7 |
| R7-28 | La suma de las proporciones de un menú es 1. | decisión del equipo | trigger diferido | #7 |
| R7-29 | El paciente que solicita una revalorización es el mismo de la valoración que revisa. | línea 36; consulta 6, §7-E | trigger | #7 |
| R7-30 | La valoración generada por una revalorización evalúa la misma asignación que la valoración revisada y la emite el nutricionista que ejecutó la revalorización. | consulta 6, §7-D; la autoría, decisión del equipo | trigger | #7 |
| R9-04 | Un grupo que una dieta exige cubrir (`Cubre`) no puede estar excluido por una de sus restricciones (`Restringe` seguido de `Excluye`). | consulta 5, §7-A y §7-C; la prohibición, decisión del equipo | trigger | #9 |

Las explicaciones largas de estas restricciones quedan en sus submodelos de origen: R5-16 y R5-17 en el submodelo #5; R6-22 y R6-23 en el #6; R7-25, R7-27, R7-29 y R7-30 en el #7. Aquí solo se recoge su enunciado, su evidencia y su mecanismo.

---

## 3. Restricciones de la ficha nutricional (MongoDB)

Las restricciones siguientes pertenecen a la ficha nutricional, que se almacena en MongoDB y, en parte, se relaciona con PostgreSQL. Se listan **por referencia**, sin duplicar la especificación del issue #8 (`docs/Ficha nutricional MongoDB/`): se conserva su identificador, se resume en una línea y se indica su mecanismo. Se separan de las anteriores porque no las aplica el motor relacional.

| # | Restricción | Evidencia | Mecanismo | Origen |
|---|---|---|---|---|
| R8-01 | Toda ficha identifica la fila de PostgreSQL a la que pertenece (`origen.tabla`, `origen.id`). | líneas 51, 56–57 | `$jsonSchema` (`required`) | ficha nutricional #8, documento 1 |
| R8-02 | `origen.tabla` solo puede ser `Alimento` o `Plato`. | líneas 51, 54 | `$jsonSchema` (`enum`) | ficha nutricional #8, documento 1 |
| R8-03 | `tipo` es `simple` o `compuesto`, y es coherente con la tabla: `Alimento` → `simple`, `Plato` → `compuesto`; solo las fichas de plato llevan `ingredientes`. | líneas 53–55 | `$jsonSchema` (`enum` y `anyOf`) | ficha nutricional #8, documento 1 |
| R8-04 | Los valores numéricos de la porción son positivos y los alérgenos no se repiten. | decisión del equipo | `$jsonSchema` | ficha nutricional #8, documento 1 |
| R8-05 | Los bloques `macronutrientes`, `micronutrientes` y `modoPreparacion` no tienen campos obligatorios. | líneas 52–53, 56–57 | ausencia de `required` en esos bloques | ficha nutricional #8, documento 1 |
| R8-06 | En la ficha de un plato, el conjunto de `ingredientes.alimentoId` coincide con los alimentos del plato en `Compone`. | líneas 54–55; composición del submodelo #5 | capa de aplicación (al guardar la ficha y al modificar `Compone`) | ficha nutricional #8, documento 2 |
| R8-07 | Solo existe ficha para filas existentes de `Alimento` o `Plato`: `origen.id` corresponde a una PK vigente de la tabla indicada en `origen.tabla`. | líneas 51, 56–57 | capa de aplicación (comprobación previa) + conciliación (R8-10) | ficha nutricional #8, documento 2 |
| R8-08 | Cada fila tiene a lo sumo una ficha. | línea 51 | índice único (`origen.tabla`, `origen.id`) | ficha nutricional #8, documento 2 |
| R8-09 | Al eliminar un alimento o un plato se elimina su ficha. | decisión del equipo | capa de aplicación (paso 2 de la baja) | ficha nutricional #8, documento 2 |
| R8-10 | Periódicamente se eliminan las fichas huérfanas (su `origen` ya no existe en PostgreSQL) y se reconstruyen las copias de `nombre` desactualizadas. | decisión del equipo | tarea programada del backend | ficha nutricional #8, documento 2 |

> **Estado.** Estas diez restricciones son las que propone la corrección del PR de la ficha nutricional (#8). Se citan por referencia y quedan **pendientes del #8**: una vez mergeado, su numeración debe verificarse contra `main` y ajustarse aquí si difiere (§8).

---

## 4. Restricciones que define este documento

Los tres submodelos y el MERX consolidado dejaron dos restricciones sin especificar y las remitieron a este issue. Aquí se enuncian, con su evidencia y su mecanismo.

| # | Restricción | Evidencia | Mecanismo | Origen |
|---|---|---|---|---|
| R11-01 | Solo se registra consumo de alimentos que están en el menú asignado, directamente (`Incluye`) o dentro de un plato incluido (`Compone`). | línea 35; consulta 4, §7-A; submodelo #7, §5 | trigger (`BEFORE INSERT OR UPDATE` en `Consumo`) | #11 |
| R11-02 | Política de borrado entre submodelos: `RESTRICT` en todas las claves foráneas, salvo las que van del dueño a sus propias filas de composición o parametrización, que son `CASCADE`: `Plato` → `Composición`; `Menú` → `Distribución`, `CubreMenú`, `IncluyeAlimento` e `IncluyePlato`; `Dieta` → `CubreDieta` y `Restringe`. | decisión del equipo; coherente con R5-07, R6-13, R7-14 y R9-03 | FK con `ON DELETE RESTRICT` / `ON DELETE CASCADE` | #11 |

**R11-01.** Es la fila «pendiente de consolidar» del submodelo #7 (§5) y del MERX consolidado (§5). El fragmento de trigger está en §7.

**R11-02, por qué así:**

- **Qué protege.** Un hecho que otros hechos usan no se puede borrar. Por ejemplo, un menú validado o asignado (R7-14), un alimento que forma parte de un plato (R5-07) o una dieta con autorizaciones o inscripciones (R6-13).
- **Qué sí permite.** Las filas que solo describen a su dueño, como la parametrización de un menú o la composición de un plato, desaparecen con él. Así un menú nuevo, sin validaciones ni asignaciones, se puede borrar entero.
- **Compatibilidad con R7-27.** Con esta política, la inmutabilidad de la parametrización (R7-27) bloquea los cambios sueltos y deja pasar solo el borrado en cascada. Así está escrito el trigger de §7.

> **Estado.** R11-02 es una decisión del equipo y queda **pendiente de confirmación** antes de darla por cerrada (§8).

---

## 5. Lo que deliberadamente no se restringe

Las condiciones siguientes se admiten en el modelo y por eso no se restringen. Provienen de los §4 de los submodelos y del MERX consolidado.

| Condición que se admite | Origen |
|---|---|
| La clasificación de un plato no se deriva de sus ingredientes ni se contrasta con ellos: un plato de nivel calórico bajo puede contener alimentos de nivel alto. | submodelo #5, §4 |
| Un alimento puede no formar parte de ningún plato, y un valor de nomenclador puede no estar en uso: `(0,*)` del lado de `Plato` en `Compone`. | submodelo #5, §4 |
| La composición tiene un solo nivel: un plato se compone de alimentos, no de otros platos. | submodelo #5, §4 |
| Una dieta puede no declarar ninguna restricción alimentaria: el mínimo del lado de `RestricciónAlimentaria` en `Restringe` es 0. | submodelo #6, §4 |
| Un paciente puede no estar inscrito en ninguna dieta y un nutricionista puede no estar autorizado en ninguna: ambas relaciones son N:M puras, `(0,*)` en ambos extremos. | submodelo #6, §4 |
| Los nombres de las entidades no son únicos: el identificador es lo que las distingue; la unicidad de nombre solo se exige en los nomencladores (R6-16). | submodelo #6, §4 |
| `Especialidad` no se deriva de `Autoriza` ni al revés: un nutricionista puede estar autorizado en una dieta ajena a su especialidad. | submodelo #6, §4 |
| `Sala` no depende de `Dieta`: el paciente pertenece a una sala y está inscrito en varias dietas, sin que su sala restrinja las dietas. | submodelo #6, §4 |
| El traslado de un paciente entre salas no se modela como historial: `Pertenece` es N:1, con FK NOT NULL en `Paciente` (R6-19), y un traslado actualiza la fila del paciente. | submodelo #6, §4 |
| La composición real de un menú no se contrasta con su parametrización: un menú puede incluir un número de alimentos distinto de `CantidadTotalAlimentos`, o proporciones distintas de las de `Distribución`. | submodelo #7, §4 |
| El estado final de un menú no se almacena: se deduce de su validación más reciente. | submodelo #7, §4 |
| No hay fechas en la asignación, el consumo ni la valoración: el ancla temporal de esos hechos es `Menú.FechaCreación`. | submodelo #7, §4 |
| Una asignación puede no tener consumos ni valoraciones: `(0,*)` del lado de `Alimento` en `Consume` y de `Valoración` en `Evalúa`. | submodelo #7, §4 |
| El consumo no registra cantidades: solo la aceptación por alimento. | submodelo #7, §4 |
| El jefe de nutrición no es una subclase de `Nutricionista`: cualquier nutricionista autorizado para la dieta puede validar. | submodelo #7, §4 |
| Un menú puede contener alimentos de grupos excluidos por su dieta. | MERX consolidado #9, §4 |
| `Excluye` no sustituye a la ficha nutricional. | MERX consolidado #9, §4 |

---

## 6. Resumen por mecanismo

Una restricción puede necesitar más de un mecanismo; en ese caso cuenta en cada uno. Los mecanismos de las filas de PostgreSQL usan el vocabulario del issue (§0): PK, FK, UNIQUE, CHECK, NOT NULL, trigger y capa de aplicación. Los mecanismos propios de MongoDB se cuentan aparte, al final.

| Mecanismo | Restricciones | Cantidad |
|---|---|---|
| PK | 16 de §1.1 (R5-01 a R7-07 y R9-01) | 16 |
| FK | 21 de §1.2; 8 de §1.4 con FK NOT NULL (R5-13, R5-14, R6-18, R6-19, R6-20, R7-21, R7-23, R7-24); R11-02 | 30 |
| UNIQUE | R5-08, R5-09, R5-10, R6-16, R7-19 (§1.3); R7-24 (§1.4); R5-16 (§2) | 7 |
| CHECK | R5-08, R5-09, R5-11, R5-12, R6-14, R6-15, R7-15, R7-16, R7-18, R7-19 (§1.3) | 10 |
| NOT NULL | R5-10, R5-11, R6-15, R7-17, R7-18, R7-19, R7-20 (§1.3); R5-13, R5-14, R6-18, R6-19, R6-20, R7-21, R7-23, R7-24 (§1.4) | 15 |
| trigger | R5-15, R6-21, R7-22 (§1.4); R6-22, R6-23, R7-25, R7-26, R7-27, R7-28, R7-29, R7-30, R9-04 (§2); R11-01 (§4) | 13 |
| capa de aplicación | R5-15, R6-21, R7-22 (§1.4); R5-17, R6-22, R6-23, R7-27 (§2); R8-06, R8-07, R8-09 (§3) | 10 |
| tipo de dominio (entero) | R6-17 (§1.3) | 1 |

**Por acción sobre la clave foránea:** `ON DELETE RESTRICT` en R5-07, R6-13, R7-14, R9-03 y R11-02; `ON DELETE CASCADE` solo en R11-02, y solo desde un dueño hacia sus propias filas de composición o parametrización.

**Mecanismos propios de la ficha nutricional (§3):** `$jsonSchema` en R8-01 a R8-05 (5); índice único en R8-08 (1); tarea programada del backend en R8-10 (1). Las tres filas cuya verificación ocurre en la capa de aplicación —R8-06, R8-07 y R8-09— ya están contadas arriba.

**Totales.** 86 restricciones: 70 de los submodelos, 4 de la consolidación (#9), 10 de la ficha nutricional (#8) y 2 de este documento (#11).

> **Nota de conteo.** R6-17 se realiza con el tipo entero del dominio, no con una declaración del vocabulario del issue; se cuenta aparte. Las filas de §1.4 que declaran «FK NOT NULL» se han contado en FK y en NOT NULL, y R7-24, que declara además UNIQUE para `Genera`, se cuenta en los tres.

---

## 7. Implementación de las restricciones no declarativas

Las restricciones declarativas —clave, referenciales, dominio y parte de la cardinalidad— las implementa el propio motor con las declaraciones del esquema relacional del #12. Las que no admiten una declaración, o requieren consultar tablas de otro submodelo, se implementan con triggers o en la capa de aplicación.

Los fragmentos siguientes usan los nombres de tablas y columnas del esquema relacional del #12, entre comillas dobles porque PostgreSQL pasa a minúsculas los identificadores sin comillas. Se probaron en PostgreSQL 17 sobre el esquema completo (29 tablas) con la política R11-02: las 25 comprobaciones se comportaron como se esperaba, cada violación falló con el identificador de su restricción y los casos válidos pasaron. Implementan R6-22 (creador y revisor), R7-25, R7-26, R6-23, R11-01, R7-29, R7-30, R9-04, R5-15 —el mismo patrón sirve para R6-21 y R7-22— y R7-27.

```sql
-- R6-22: quien crea un menú está autorizado para su dieta.
CREATE FUNCTION "fn_R6_22_creador"() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM "Autoriza" a
                   WHERE a."NutricionistaId" = NEW."NutricionistaId" AND a."DietaId" = NEW."DietaId") THEN
        RAISE EXCEPTION 'R6-22: el nutricionista % no está autorizado para la dieta %', NEW."NutricionistaId", NEW."DietaId";
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER "trg_R6_22_creador" BEFORE INSERT OR UPDATE OF "NutricionistaId", "DietaId" ON "Menú"
    FOR EACH ROW EXECUTE FUNCTION "fn_R6_22_creador"();

-- R6-22 (revisor autorizado), R7-25 (no valida lo que creó) y R7-26 (no valida antes de la creación).
CREATE FUNCTION "fn_validación"() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE m "Menú"%ROWTYPE;
BEGIN
    SELECT * INTO m FROM "Menú" WHERE "MenúId" = NEW."MenúId";
    IF m."NutricionistaId" = NEW."NutricionistaId" THEN
        RAISE EXCEPTION 'R7-25: el nutricionista % creó el menú % y no puede validarlo', NEW."NutricionistaId", NEW."MenúId";
    END IF;
    IF NOT EXISTS (SELECT 1 FROM "Autoriza" a
                   WHERE a."NutricionistaId" = NEW."NutricionistaId" AND a."DietaId" = m."DietaId") THEN
        RAISE EXCEPTION 'R6-22: el revisor % no está autorizado para la dieta %', NEW."NutricionistaId", m."DietaId";
    END IF;
    IF NEW."FechaValidación" < m."FechaCreación" THEN
        RAISE EXCEPTION 'R7-26: la validación es anterior a la creación del menú %', NEW."MenúId";
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER "trg_validación" BEFORE INSERT OR UPDATE ON "Validación"
    FOR EACH ROW EXECUTE FUNCTION "fn_validación"();

-- R6-23: un paciente solo recibe menús de dietas en las que está inscrito.
CREATE FUNCTION "fn_R6_23"() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM "Menú" m JOIN "Inscribe" i ON i."DietaId" = m."DietaId"
                   WHERE m."MenúId" = NEW."MenúId" AND i."PacienteId" = NEW."PacienteId") THEN
        RAISE EXCEPTION 'R6-23: el paciente % no está inscrito en la dieta del menú %', NEW."PacienteId", NEW."MenúId";
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER "trg_R6_23" BEFORE INSERT OR UPDATE ON "Asignación"
    FOR EACH ROW EXECUTE FUNCTION "fn_R6_23"();

-- R11-01: solo se registra consumo de alimentos del menú asignado, incluidos directamente o dentro de un plato.
CREATE FUNCTION "fn_R11_01"() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM "IncluyeAlimento" ia
                   WHERE ia."MenúId" = NEW."MenúId" AND ia."AlimentoId" = NEW."AlimentoId")
       AND NOT EXISTS (SELECT 1 FROM "IncluyePlato" ip JOIN "Composición" c ON c."PlatoId" = ip."PlatoId"
                       WHERE ip."MenúId" = NEW."MenúId" AND c."AlimentoId" = NEW."AlimentoId") THEN
        RAISE EXCEPTION 'R11-01: el alimento % no forma parte del menú %', NEW."AlimentoId", NEW."MenúId";
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER "trg_R11_01" BEFORE INSERT OR UPDATE ON "Consumo"
    FOR EACH ROW EXECUTE FUNCTION "fn_R11_01"();

-- R7-29 y R7-30: la revalorización revisa una valoración de su solicitante, y la valoración que genera
-- evalúa la misma asignación y la emite el nutricionista que la ejecutó.
CREATE FUNCTION "fn_revalorización"() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE r "Valoración"%ROWTYPE; g "Valoración"%ROWTYPE;
BEGIN
    SELECT * INTO r FROM "Valoración" WHERE "ValoraciónId" = NEW."ValoraciónRevisadaId";
    IF r."PacienteId" <> NEW."PacienteId" THEN
        RAISE EXCEPTION 'R7-29: la valoración revisada % no es del paciente %', NEW."ValoraciónRevisadaId", NEW."PacienteId";
    END IF;
    IF NEW."ValoraciónGeneradaId" IS NOT NULL THEN
        SELECT * INTO g FROM "Valoración" WHERE "ValoraciónId" = NEW."ValoraciónGeneradaId";
        IF (g."MenúId", g."PacienteId") <> (r."MenúId", r."PacienteId") OR g."NutricionistaId" <> NEW."NutricionistaId" THEN
            RAISE EXCEPTION 'R7-30: la valoración generada % no evalúa la misma asignación o no la emitió el ejecutor', NEW."ValoraciónGeneradaId";
        END IF;
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER "trg_revalorización" BEFORE INSERT OR UPDATE ON "Revalorización"
    FOR EACH ROW EXECUTE FUNCTION "fn_revalorización"();

-- R9-04: un grupo que la dieta exige cubrir no puede estar excluido por una de sus restricciones.
CREATE FUNCTION "fn_R9_04"() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF EXISTS (SELECT 1 FROM "CubreDieta" cd
               JOIN "Restringe" r ON r."DietaId" = cd."DietaId"
               JOIN "Excluye" e ON e."RestricciónAlimentariaId" = r."RestricciónAlimentariaId"
                               AND e."GrupoNutricionalId" = cd."GrupoNutricionalId") THEN
        RAISE EXCEPTION 'R9-04: una dieta exige cubrir un grupo que una de sus restricciones excluye';
    END IF;
    RETURN NULL;
END $$;
CREATE TRIGGER "trg_R9_04_cubre"     AFTER INSERT OR UPDATE ON "CubreDieta" FOR EACH STATEMENT EXECUTE FUNCTION "fn_R9_04"();
CREATE TRIGGER "trg_R9_04_restringe" AFTER INSERT OR UPDATE ON "Restringe"  FOR EACH STATEMENT EXECUTE FUNCTION "fn_R9_04"();
CREATE TRIGGER "trg_R9_04_excluye"   AFTER INSERT OR UPDATE ON "Excluye"    FOR EACH STATEMENT EXECUTE FUNCTION "fn_R9_04"();

-- R5-15 (y el mismo patrón para R6-21 y R7-22): cardinalidad mínima, comprobada al confirmar la transacción.
CREATE FUNCTION "fn_R5_15"() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE p int := CASE WHEN TG_TABLE_NAME = 'Plato' THEN NEW."PlatoId" ELSE OLD."PlatoId" END;
BEGIN
    IF EXISTS (SELECT 1 FROM "Plato" WHERE "PlatoId" = p)
       AND NOT EXISTS (SELECT 1 FROM "Composición" WHERE "PlatoId" = p) THEN
        RAISE EXCEPTION 'R5-15: el plato % no tiene ningún alimento', p;
    END IF;
    RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER "trg_R5_15_plato" AFTER INSERT ON "Plato"
    DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION "fn_R5_15"();
CREATE CONSTRAINT TRIGGER "trg_R5_15_composición" AFTER DELETE OR UPDATE OF "PlatoId" ON "Composición"
    DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION "fn_R5_15"();

-- R7-27: la parametrización no cambia después de creado el menú (solo desaparece con el menú, por cascada).
CREATE FUNCTION "fn_R7_27_menú"() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF (NEW."CantidadTotalAlimentos", NEW."EsAutomático") IS DISTINCT FROM (OLD."CantidadTotalAlimentos", OLD."EsAutomático") THEN
        RAISE EXCEPTION 'R7-27: la parametrización del menú % no se modifica', OLD."MenúId";
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER "trg_R7_27_menú" BEFORE UPDATE ON "Menú" FOR EACH ROW EXECUTE FUNCTION "fn_R7_27_menú"();
CREATE FUNCTION "fn_R7_27_parámetros"() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF TG_OP = 'DELETE' AND pg_trigger_depth() > 1 THEN   -- borrado en cascada desde Menú
        RETURN OLD;
    END IF;
    RAISE EXCEPTION 'R7-27: la parametrización del menú % no se modifica', OLD."MenúId";
END $$;
CREATE TRIGGER "trg_R7_27_distribución" BEFORE UPDATE OR DELETE ON "Distribución" FOR EACH ROW EXECUTE FUNCTION "fn_R7_27_parámetros"();
CREATE TRIGGER "trg_R7_27_cubremenú"    BEFORE UPDATE OR DELETE ON "CubreMenú"    FOR EACH ROW EXECUTE FUNCTION "fn_R7_27_parámetros"();
```

**Lo que queda para la implementación.** R7-28, la suma de las proporciones igual a 1, es también un trigger diferido y no se incluye entre los fragmentos probados; su implementación corresponde al backend. R5-17, la clasificación propia, se resuelve en la capa de aplicación, igual que las restricciones de la ficha que contrastan PostgreSQL con MongoDB (R8-06 a R8-09); R8-10 es una tarea programada.

---

## 8. Decisiones del equipo tomadas en este documento

- **R6-22 y R6-23 pasan a «capa de aplicación + trigger».** El submodelo #6 (§3) las declaraba como comprobación de la capa de aplicación, y el submodelo #7 (§5) las dejó «pendientes de consolidar». Como decisión de esta consolidación, se refuerza su cumplimiento con los triggers de §7: la autoridad se verifica por dieta, tanto al crear el menú como al validarlo, y la inscripción, al asignarlo. La comprobación en la capa de aplicación se conserva.
- **Se define R11-01**, la fila que el submodelo #7 (§5) y el MERX consolidado (§5) remitieron a este issue: el consumo solo puede referirse a alimentos del menú asignado.
- **Se propone R11-02** como política de borrado entre submodelos: `RESTRICT` en todas las claves foráneas y `CASCADE` solo desde un dueño hacia sus propias filas de composición o parametrización. Es coherente con R5-07, R6-13, R7-14 y R9-03, y es compatible con R7-27. **Queda pendiente de confirmación del equipo** antes de darla por cerrada.
- **Se incorporan R9-01 a R9-04** del MERX consolidado (#9) y **R8-01 a R8-10** de la ficha nutricional (#8), estos últimos por referencia. Las R9 dependen del merge del MERX consolidado; las R8 son **pendientes del #8**: cuando se mergee su corrección, hay que verificar su numeración contra `main` y ajustar las referencias de §3 y de §6 si difieren.
- **Queda por hacer en el MERX consolidado:** actualizar (§5) la tabla de restricciones entre submodelos, que da por pendientes las que aquí quedan especificadas. Ese cambio corresponde al issue del MERX consolidado, no a este documento.
- **No se incluye el esquema.** Este documento especifica restricciones y mecanismos; el script DDL pertenece a la implementación, con el esquema relacional de la guía del #12.
- **R6-17** se realiza con el tipo entero del dominio, que no es una declaración del vocabulario de mecanismos del issue; se cuenta aparte en §6 y conviene decidir si se reformula como NOT NULL + CHECK.