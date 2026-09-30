# Submodelo Menú-Valoración-Consumo — Restricciones locales

**Asignatura:** Bases de Datos II / Ingeniería de Software — Curso 2026-2027
**Issue:** #7
**Diagrama:** `Submodelo Menu-Valoracion-Consumo.drawio` (misma carpeta): Entidades, Relaciones-1, Relaciones-2, Relaciones-3 y Relaciones-4.
**Alcance de este documento:** restricciones de integridad locales del submodelo — las que involucran únicamente sus entidades, relaciones y agregaciones —, cada una con la evidencia que la sustenta (enunciado o consulta formalizada) y el mecanismo previsto para hacerla cumplir. Es insumo directo de la especificación consolidada de restricciones (issue #11), que las recoge junto con las de los demás submodelos.

---

## 0. Contenido del submodelo

| Elemento | Detalle |
|---|---|
| Entidades | `Menú` (`MenúId`, `FechaCreación`, `CantidadTotalAlimentos`, `EsAutomático`), `Valoración` (`ValoraciónId`), `Revalorización` (`RevalorizaciónId`, `FechaSolicitud`) |
| Nomenclador | `ResultadoNutricional` (`ResultadoNutricionalId`, `Nombre`, `Valor`) |
| Entidades de frontera | `Nutricionista`, `Dieta`, `Paciente` (issue #6); `Alimento`, `Plato`, `GrupoNutricional`, `TipoPreparación` (issue #5) |
| Relaciones | `Crea` (`Nutricionista`, `Menú`); `Pertenece` (`Menú`, `Dieta`); `Incluye` (`Menú` con `Alimento` y con `Plato`); `Cubre` (`GrupoNutricional`, `Menú`); `Emite` (`Nutricionista`, `Valoración`); `Tiene` (`Valoración`, `ResultadoNutricional`); `Solicita` (`Paciente`, `Revalorización`); `Ejecuta` (`Nutricionista`, `Revalorización`); `Revalora` y `Genera` (`Revalorización`, `Valoración`) |
| Agregaciones | `Distribución`: envuelve `Menú`—`Distribuye`—`TipoPreparación`, hereda (`MenúId`, `TipoPreparaciónId`) y lleva `Proporción`. `Validación`: envuelve `Nutricionista`—`Valida`—`Menú`, hereda (`NutricionistaId`, `MenúId`) y lleva `FechaValidación`, `Observaciones` y `Aprobado`. `Asignación`: envuelve `Menú`—`Asigna`—`Paciente` y hereda (`MenúId`, `PacienteId`); participa en `Evalúa` (con `Valoración`) y en `Consume`. `Consumo`: envuelve `Asignación`—`Consume`—`Alimento`, hereda (`MenúId`, `PacienteId`, `AlimentoId`) y lleva `Aceptación` |

Los tres criterios de generación («proporción de alimentos por tipo de preparación, la cobertura de los grupos nutricionales y la cantidad total de alimentos», líneas 38–40) se reparten, por la regla de atomicidad (convenciones de modelado, §5), en el atributo `CantidadTotalAlimentos`, la agregación `Distribución` y la relación `Cubre`. `ResultadoNutricional` se incorpora porque la consulta 6 (§7-G) fija la escala del resultado como nomenclador y asigna la decisión final a este issue.

## 1. Criterio

Se consideran **locales** las restricciones cuyo alcance se agota en las entidades, relaciones y agregaciones de este submodelo, incluidas las llaves que heredan de las entidades de frontera. Las que exigen consultar relaciones propias de otros submodelos se listan aparte (§5) para que la consolidación no las pierda.

Cada restricción cita la línea del enunciado o la sección de la consulta formalizada (`docs/consultas/`) que la sustenta; las que no tienen cita directa se declaran expresamente como **decisión del equipo** (§6). La numeración de líneas es la del texto plano del enunciado, la misma empleada en los issues y en las consultas. Los mecanismos usan el vocabulario del issue #11: PK, FK, UNIQUE, CHECK, NOT NULL, trigger y capa de aplicación.

---

## 2. Restricciones estructurales

### 2.1 Clave

| # | Restricción | Evidencia | Mecanismo |
|---|---|---|---|
| R7-01 | `MenúId` identifica unívocamente a cada menú, `ValoraciónId` a cada valoración y `RevalorizaciónId` a cada revalorización. | líneas 22–23, 35–37 | PK |
| R7-02 | `ResultadoNutricionalId` identifica unívocamente a cada categoría del nomenclador `ResultadoNutricional`. | consulta 6, §7-G | PK |
| R7-03 | Un menú registra a lo sumo una proporción por tipo de preparación: el par (`MenúId`, `TipoPreparaciónId`) identifica cada ocurrencia de `Distribución`. | líneas 38–39 | PK compuesta |
| R7-04 | Un revisor registra a lo sumo una validación por menú: el par (`NutricionistaId`, `MenúId`) identifica cada ocurrencia de `Validación`. Un mismo menú sí admite validaciones de revisores distintos. | consulta 3, §7-A; el tope por revisor, decisión del equipo | PK compuesta |
| R7-05 | Un menú se asigna a lo sumo una vez a un mismo paciente: el par (`MenúId`, `PacienteId`) identifica cada ocurrencia de `Asignación`. | líneas 33–34; consulta 4, §7-D | PK compuesta |
| R7-06 | Cada alimento tiene a lo sumo un registro de consumo por asignación: la terna (`MenúId`, `PacienteId`, `AlimentoId`) identifica cada ocurrencia de `Consumo`. | línea 35; consulta 4, §7-A | PK compuesta |
| R7-07 | Un alimento, un plato o un grupo nutricional aparece a lo sumo una vez en un mismo menú: los pares de `Incluye` (`MenúId`, `AlimentoId`), (`MenúId`, `PlatoId`) y de `Cubre` (`MenúId`, `GrupoNutricionalId`) son únicos. | consulta 2, §8 («cada aparición de un alimento en un menú es identificable») | PK compuesta |

> **Convención adoptada (R7-04):** la llave heredada de `Validación` no incluye la fecha, así que un mismo revisor no puede validar dos veces el mismo menú. Es suficiente para el historial que pide la consulta 3: cuando el jefe de nutrición rechaza un menú, «indica la confección de otro» (línea 41), es decir, la nueva iteración es un menú nuevo con su propia validación. Si el equipo necesitara revisiones repetidas del mismo par, la fecha tendría que pasar a formar parte de la identidad, según la heurística de fecha de las convenciones de modelado (§5).

> **Convención adoptada (R7-06):** como `Consume` solo relaciona la asignación con `Alimento`, el consumo de un plato incluido en el menú se registra por cada alimento que lo compone (vía `Compone`, submodelo #5). Si un mismo alimento llega al menú por más de una vía —directamente y dentro de un plato, o dentro de varios platos—, se registra una sola vez, con una única aceptación: la tasa de aceptación que piden las consultas 4 y 6 es por alimento, no por la vía por la que llegó al menú.

### 2.2 Referenciales

| # | Restricción | Evidencia | Mecanismo |
|---|---|---|---|
| R7-08 | Todo menú referencia la dieta a la que pertenece y el nutricionista que lo creó, ambos existentes. | líneas 38, 68–69 | FK |
| R7-09 | Toda ocurrencia de `Incluye`, `Cubre` y `Distribución` referencia un menú existente y un alimento, plato, grupo nutricional o tipo de preparación existente. | líneas 31, 38–39 | FK |
| R7-10 | Toda ocurrencia de `Validación` referencia un nutricionista y un menú existentes. | líneas 40–41, 72–73 | FK |
| R7-11 | Toda ocurrencia de `Asignación` referencia un menú y un paciente existentes; todo `Consumo` referencia una asignación existente y un alimento existente. Así, solo se registra consumo de menús efectivamente asignados. | líneas 33–35 | FK; FK compuesta (`MenúId`, `PacienteId`) hacia `Asignación` |
| R7-12 | Toda valoración referencia la asignación que evalúa, el nutricionista que la emite y una categoría existente de `ResultadoNutricional`. | líneas 35–36, 50; consulta 4, §7-C; consulta 6, §7-G | FK; FK compuesta hacia `Asignación` |
| R7-13 | Toda revalorización referencia al paciente que la solicita, al nutricionista que la ejecuta y a la valoración que revisa; la valoración generada, cuando existe, también debe existir. | líneas 36–37; consulta 6, §7-D y §7-E | FK |
| R7-14 | No se elimina un menú mientras tenga validaciones o asignaciones, ni una asignación mientras tenga consumos o valoraciones, ni una valoración mientras una revalorización la revise o la haya generado. | decisión del equipo | FK con `ON DELETE RESTRICT` |

### 2.3 Dominio

| # | Restricción | Evidencia | Mecanismo |
|---|---|---|---|
| R7-15 | `CantidadTotalAlimentos` es un entero estrictamente positivo. | líneas 38–40; el mínimo, decisión del equipo | CHECK (`CantidadTotalAlimentos > 0`) |
| R7-16 | `Proporción` de `Distribución` está en el intervalo (0, 1]. | líneas 38–39; el intervalo, decisión del equipo | CHECK (`Proporción > 0 AND Proporción <= 1`) |
| R7-17 | `EsAutomático` (menú) y `Aprobado` (validación) son booleanos obligatorios. | consulta 1, §7-A; consulta 3, §7-C | NOT NULL |
| R7-18 | `Aceptación` de `Consumo` toma exactamente los valores *aceptado* y *rechazado*. | línea 35; consulta 4, §7-A | NOT NULL + CHECK (o booleano) |
| R7-19 | `ResultadoNutricional` tiene exactamente cinco categorías — *Muy mala*, *Mala*, *Normal*, *Buena* y *Excelente* —, con `Valor` 1, 2, 3, 4 y 5 respectivamente; `Nombre` y `Valor` son únicos y obligatorios. | consulta 6, §7-G | catálogo con esos cinco valores: CHECK sobre `Nombre` + CHECK (`Valor BETWEEN 1 AND 5`) + UNIQUE y NOT NULL sobre `Nombre` y sobre `Valor` |
| R7-20 | `FechaCreación`, `FechaValidación` y `FechaSolicitud` son obligatorias. `Observaciones` es opcional. | líneas 36, 69, 72–73; la opcionalidad de `Observaciones`, decisión del equipo | NOT NULL |

> **Convención adoptada (R7-19):** como en los nomencladores del issue #5, el dominio cerrado se garantiza en dos niveles: la FK impide que una valoración use una categoría ajena al catálogo (R7-12), y el CHECK sobre el propio catálogo impide ampliarlo. El `Valor` numérico es el que permite los «promedios» que exige la funcionalidad 6 (líneas 78–79 y 81–82).

### 2.4 Cardinalidad

| # | Restricción | Evidencia | Mecanismo |
|---|---|---|---|
| R7-21 | Cada menú tiene exactamente un creador y una dieta: `(1,1)` del lado de `Nutricionista` en `Crea` y del lado de `Dieta` en `Pertenece`. | líneas 38, 68–69 | FK NOT NULL en `Menú` |
| R7-22 | Todo menú tiene al menos una proporción por tipo de preparación y al menos un grupo nutricional cubierto: `(1,*)` del lado de `TipoPreparación` en `Distribuye` y del lado de `GrupoNutricional` en `Cubre`. | líneas 38–40 | capa de aplicación + trigger diferido |
| R7-23 | Cada valoración evalúa exactamente una asignación, la emite exactamente un nutricionista y tiene exactamente un resultado: `(1,1)` del lado de `Asignación` en `Evalúa`, de `Nutricionista` en `Emite` y de `ResultadoNutricional` en `Tiene`. | líneas 35–36, 50; consulta 4, §7-C; consulta 6, §7-G | FK NOT NULL en `Valoración` |
| R7-24 | Cada revalorización tiene exactamente un solicitante, un ejecutor y una valoración revisada: `(1,1)` en `Solicita`, `Ejecuta` y `Revalora`. Genera a lo sumo una valoración, y cada valoración es generada por a lo sumo una revalorización: `(0,1)` en los dos lados de `Genera`. | líneas 36–37; consulta 6, §7-D | FK NOT NULL; FK anulable + UNIQUE para `Genera` |

**R7-22 — parametrización obligatoria.** El enunciado dice que la parametrización «es almacenada junto al menú generado» (líneas 39–40): un menú sin proporciones ni grupos cubiertos no tendría los «parámetros utilizados» que lista la consulta 1. Igual que la composición mínima de un plato (R5-15), no es expresable con claves foráneas: el menú se registra antes que sus ocurrencias de `Distribución` y `Cubre`. En la capa de aplicación, el alta del menú y la de su parametrización se registran en una misma unidad de trabajo; en PostgreSQL puede reforzarse con un trigger de restricción diferido (`DEFERRABLE INITIALLY DEFERRED`) que verifique, al confirmar la transacción, que el menú tiene al menos una fila en cada una.

**R7-24 — revalorización pendiente.** La solicitud es virtual y el resultado llega después, con la revalorización manual (líneas 36–37). Mientras tanto no existe valoración generada; por eso `Genera` es `(0,1)` y su FK es anulable.

---

## 3. Restricciones semánticas

| # | Restricción | Evidencia | Mecanismo |
|---|---|---|---|
| R7-25 | Un nutricionista no valida un menú que él mismo creó. | decisión del equipo, a partir de la separación de roles de las líneas 38–41 y la consulta 1, §7-C | trigger (`BEFORE INSERT` en `Validación`) |
| R7-26 | La fecha de validación no es anterior a la fecha de creación del menú validado. | decisión del equipo | trigger |
| R7-27 | La parametrización de un menú (`CantidadTotalAlimentos`, `EsAutomático`, `Distribución`, `Cubre`) no cambia después de creado el menú. | líneas 39–40; consulta 1, §7-D y §8 | capa de aplicación + trigger (`BEFORE UPDATE OR DELETE`) |
| R7-28 | La suma de las proporciones de un menú es 1. | decisión del equipo | trigger diferido |
| R7-29 | El paciente que solicita una revalorización es el mismo de la valoración que revisa. | línea 36; consulta 6, §7-E | trigger |
| R7-30 | La valoración generada por una revalorización evalúa la misma asignación que la valoración revisada y la emite el nutricionista que ejecutó la revalorización. | consulta 6, §7-D; la autoría, decisión del equipo | trigger |

**R7-25.** La consulta 1 (§7-C) responde «No» a si el creador de un menú es el mismo que lo revisa: el enunciado separa al «nutricionista específico» que genera el menú (líneas 38–39) de quien «revisa y aprueba» (línea 40). Esa separación es de roles; ni el enunciado ni la consulta prohíben expresamente que una misma persona cree y valide un menú, por lo que la restricción se adopta como decisión del equipo, en favor de que la validación sea independiente. Se verifica comparando el `NutricionistaId` de la validación con el creador registrado en `Crea`.

**R7-27.** La consulta 1 exige que la parametrización sea «inmutable una vez generado» el menú, para que la auditoría no cambie si luego cambian los criterios. Un menú que necesite otros parámetros es un menú nuevo, coherente con R7-04. El trigger rechaza las modificaciones y los borrados sobre esas filas; la eliminación completa del menú queda regida por R7-14.

**R7-29 y R7-30.** Son las que dan sentido a la parte C de la consulta 6: el resultado del revalorizado se compara «para esas mismas dietas» (líneas 81–82), y la dieta se obtiene de la cadena Revalorización → Valoración → Asignación → Menú → Dieta. Si la valoración revisada fuera de otro paciente, o la generada colgara de otra asignación, la comparación mezclaría dietas.

---

## 4. Lo que deliberadamente no se restringe

- **La composición real de un menú no se contrasta con su parametrización.** Un menú puede incluir un número de alimentos distinto de `CantidadTotalAlimentos`, o proporciones distintas de las de `Distribución`, y puede no cubrir los grupos requeridos por su dieta o contener grupos que ella restringe. La consulta 5 existe precisamente para verificar «si los criterios de equilibrio fueron cumplidos» (líneas 76–77), y su conjunto de prueba pide menús con desviaciones intencionadas; si la base las impidiera, la consulta no tendría nada que detectar.
- **El estado final de un menú no se almacena.** Un menú es final si su validación más reciente —la de mayor `FechaValidación`, registrada con fecha y hora (consulta 3, §2)— tiene `Aprobado` verdadero (consulta 2, §7-A). Un menú puede tener validaciones de varios revisores, incluso con dictámenes distintos; prevalece la última. El criterio de la validación más reciente es decisión del equipo.
- **No hay fechas en la asignación, el consumo ni la valoración.** El ancla temporal de estos hechos es `Menú.FechaCreación` (consulta 4, §7-D; consulta 6, §7-F). Dentro de una misma asignación, el orden entre una valoración y la que la revaloriza se deduce de la cadena `Revalora`–`Genera`: la generada es posterior a la revisada. Así se sostiene la serie de «valoraciones más recientes» (línea 58).
- **Una asignación puede no tener consumos ni valoraciones:** `(0,*)` del lado de `Alimento` en `Consume` y de `Valoración` en `Evalúa`. Un paciente asignado sin consumo registrado queda fuera del denominador de la tasa de aceptación (consulta 4, §7-B).
- **El consumo no registra cantidades**, solo la aceptación por alimento (consulta 2, §7-D; consulta 4, §7-A).
- **El jefe de nutrición no es una subclase de `Nutricionista`.** Cualquier nutricionista autorizado para la dieta puede validar (consulta 3, §7-B).

---

## 5. Restricciones que involucran al submodelo pero no son locales

Se registran para la consolidación del issue #11; su definición corresponde a los submodelos indicados.

| Restricción | Submodelos | Estado |
|---|---|---|
| Quien crea un menú (`Crea`) está autorizado para su dieta (`Autoriza`, líneas 43–45). | #6, #7 | Pendiente de consolidar |
| Quien valida un menú (`Valida`) está autorizado para su dieta (`Autoriza`, líneas 43–45; consulta 3, §7-B). | #6, #7 | Pendiente de consolidar |
| Un paciente solo recibe menús de dietas en las que está inscrito (`Asigna` ⇒ `Inscribe`, línea 49). | #6, #7 | Pendiente de consolidar |
| Solo se registra consumo de alimentos que están en el menú asignado, directamente (`Incluye`) o dentro de un plato incluido (`Compone`). | #5, #7 | Pendiente de consolidar. La descomposición de un plato en sus alimentos queda resuelta en este submodelo (convención adoptada de R7-06) |
| Autor del alimento en la parte B de la consulta 6. | #5, #7 | Resuelto en el submodelo #5, que lo modela como `Describe` (la relación `Describe/Ingresa` del glosario) |
| Restricciones de una dieta sobre los grupos nutricionales: el glosario (§6) fija `Restringe(Dieta, RestricciónAlimentaria)` con su nomenclador, mientras que la consulta 5 (§7-C) propone una relación tipificada `Dieta`–`GrupoNutricional` (requerido/restringido). | #5, #6 | Pendiente de alinear |
| Eliminación de un nutricionista, paciente o dieta que tiene menús, validaciones, asignaciones, valoraciones o revalorizaciones. | #6, #7 | Pendiente: política de borrado entre submodelos |

---

## 6. Decisiones del equipo tomadas en este documento

Restricciones sin cita directa en el enunciado ni en las consultas, adoptadas por el equipo: el tope por revisor de R7-04, el registro único por alimento de R7-06, R7-14, los límites de R7-15 y R7-16, la opcionalidad de `Observaciones` en R7-20, R7-25, R7-26, R7-28, la autoría de la valoración generada en R7-30 y el criterio de la validación más reciente para el estado final de un menú (§4).

Quedan abiertas dos decisiones:

- **La escala de `Proporción`** (fracción en (0, 1] o porcentaje). El enunciado no la fija, y conviene resolverla antes de la implementación para que R7-16 y R7-28 tengan un sentido inequívoco.
- **Si solo pueden asignarse menús finales** (aprobados). Ni el enunciado ni las consultas lo exigen; de adoptarse, sería una restricción semántica más entre `Asignación` y `Validación`.
