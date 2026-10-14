# Submodelo Alimento-Plato — Restricciones locales

**Asignatura:** Bases de Datos II / Ingeniería de Software — Curso 2026-2027
**Issue:** #5
**Diagrama:** `Submodelo Alimento-Plato.drawio` (misma carpeta): Página-Entidades, Página-Relaciones-1 y Página-Relaciones-2.
**Alcance de este documento:** restricciones de integridad locales del submodelo — las que involucran únicamente sus entidades y relaciones —, cada una con la evidencia del enunciado que la sustenta y el mecanismo previsto para hacerla cumplir. Es insumo directo de la especificación consolidada de restricciones (issue #11), que las recoge junto con las de los demás submodelos.

---

## 0. Contenido del submodelo

| Elemento | Detalle |
|---|---|
| Entidades | `Alimento`, `Plato` |
| Nomencladores | `GrupoNutricional`, `TipoPreparación`, `NivelCalórico` |
| Entidad de frontera | `Nutricionista` (issue #6), solo como extremo de `Describe` |
| Relaciones | `Pertenece`, `Corresponde` y `Tiene` (de `Alimento` y de `Plato` con cada nomenclador); `Describe` (de `Nutricionista` con `Alimento` y con `Plato`); `Compone` (`Plato`, `Alimento`) |
| Agregación | `Composición`: envuelve `Plato`—`Compone`—`Alimento`, hereda su llave (`PlatoId`, `AlimentoId`) y lleva el atributo `Cantidad` |

`NivelCalórico` se incorpora aunque el título del issue no lo nombre: el glosario lo cataloga como clasificación de `Alimento` (N3) y ningún otro submodelo lo reclama. `Describe` se incorpora porque la consulta 6 asigna a este submodelo la relación entre el alimento y su autor.

**Correspondencia con el glosario del dominio:**

- El glosario (§4.1, E1) enumera el grupo nutricional, el tipo de preparación y el nivel calórico entre los atributos principales de `Alimento`. En el submodelo se modelan como relaciones con sus nomencladores (`Pertenece`, `Corresponde`, `Tiene`), conforme al patrón nomenclador de las convenciones de modelado (§5). La información es la misma y se navega igual.
- El glosario (§4.4) registra `Clasifica` y `Nivela` (`Nutricionista` → `Alimento`) como la acción de clasificar. En el submodelo, el hecho que se almacena es la clasificación misma (`Pertenece`, `Corresponde`, `Tiene`), y quién puede clasificar queda como restricción (R5-17, apoyada en `Describe`). No se modelan como relaciones aparte para no registrar dos veces el mismo hecho.
- `Describe` corresponde a la relación `Describe/Ingresa` del glosario (§4.4). Se conserva el nombre `Describe`, tomado del enunciado: «aquellos alimentos descritos por él» (línea 30).
- El glosario (§4.4) registra `Cantidad` como atributo de `Compone`. En el diagrama, `Cantidad` cuelga de la agregación `Composición`, porque las convenciones de modelado (§3) reservan la agregación para las relaciones que necesitan atributos propios.

---

## 1. Criterio

Se consideran **locales** las restricciones cuyo alcance se agota en las entidades y relaciones de este submodelo. Las que cruzan hacia otros submodelos se listan aparte (§5) para que la consolidación no las pierda.

Se sigue el mismo criterio de rigor del glosario del dominio: cada restricción cita la línea del enunciado que la sustenta; las que no tienen cita directa se declaran expresamente como **decisión del equipo** (§6). La numeración de líneas es la del texto plano del enunciado, la misma empleada en el glosario y en los issues. Los mecanismos usan el vocabulario del issue #11: PK, FK, UNIQUE, CHECK, NOT NULL, trigger y capa de aplicación.

---

## 2. Restricciones estructurales

### 2.1 Clave

| # | Restricción | Evidencia | Mecanismo |
|---|---|---|---|
| R5-01 | `AlimentoId` identifica unívocamente a cada alimento y `PlatoId` a cada plato. | líneas 23–24, 29 | PK |
| R5-02 | `GrupoNutricionalId`, `TipoPreparaciónId` y `NivelCalóricoId` identifican unívocamente a cada valor de su nomenclador. | líneas 24–25, 30–31 | PK |
| R5-03 | Un mismo alimento aparece a lo sumo una vez en la composición de un plato: el par (`PlatoId`, `AlimentoId`) identifica cada ocurrencia de la agregación `Composición`. | decisión del equipo | PK compuesta |

> **Convención adoptada (R5-03):** la cantidad de un ingrediente dentro de un plato se registra en una sola ocurrencia. Dos ocurrencias del mismo par repartirían esa cantidad sin aportar un significado distinto. Es consistente con la notación: la agregación `Composición` hereda como llave la de la relación que envuelve, el par (`PlatoId`, `AlimentoId`).

### 2.2 Referenciales

| # | Restricción | Evidencia | Mecanismo |
|---|---|---|---|
| R5-04 | Toda clasificación (`Pertenece`, `Corresponde`, `Tiene`) de un alimento o de un plato referencia un valor existente del nomenclador correspondiente. | líneas 30–31 | FK |
| R5-05 | Toda ocurrencia de `Compone` referencia un plato y un alimento existentes. | líneas 54–55 | FK |
| R5-06 | Todo alimento y todo plato referencia al nutricionista que lo describió, el cual debe existir. | líneas 29, 78–80 | FK |
| R5-07 | No se elimina un valor de nomenclador mientras lo use algún alimento o plato, ni un alimento mientras forme parte de algún plato. | decisión del equipo | FK con `ON DELETE RESTRICT` |

### 2.3 Dominio

| # | Restricción | Evidencia | Mecanismo |
|---|---|---|---|
| R5-08 | `TipoPreparación` toma exactamente los valores *entrante*, *plato fuerte*, *postre* y *bebida*. | líneas 30–31 | catálogo con esos cuatro valores: CHECK + UNIQUE sobre `Nombre` |
| R5-09 | `NivelCalórico` toma exactamente los valores *bajo*, *medio* y *alto*. | línea 31 | catálogo con esos tres valores: CHECK + UNIQUE sobre `Nombre` |
| R5-10 | `GrupoNutricional` es un catálogo abierto: el enunciado lo define como «nomenclador del sistema» sin enumerar sus valores; se exige únicamente que cada nombre sea único. | líneas 24–25 | UNIQUE + NOT NULL sobre `Nombre` |
| R5-11 | `Nombre` es obligatorio y no vacío en `Alimento`, `Plato` y los tres nomencladores. | decisión del equipo | NOT NULL + CHECK |
| R5-12 | `Cantidad` de `Composición` es estrictamente positiva. | decisión del equipo | CHECK (`Cantidad > 0`) |

> **Convención adoptada (R5-08 y R5-09):** como los dominios cerrados se modelan con el patrón nomenclador (convenciones de modelado, §5), se garantizan en dos niveles: la FK impide que un alimento o un plato use un valor ajeno al catálogo (R5-04), y el CHECK sobre el propio catálogo impide que este se amplíe con valores que el enunciado no contempla.

### 2.4 Cardinalidad

| # | Restricción | Evidencia | Mecanismo |
|---|---|---|---|
| R5-13 | Cada alimento y cada plato tiene exactamente un grupo nutricional, un tipo de preparación y un nivel calórico: `(1,1)` del lado del nomenclador en `Pertenece`, `Corresponde` y `Tiene`. | líneas 30–31 | FK NOT NULL en `Alimento` y en `Plato` |
| R5-14 | Cada alimento y cada plato tiene exactamente un nutricionista que lo describió: `(1,1)` del lado de `Nutricionista` en `Describe`. | líneas 29, 78–80 | FK NOT NULL en `Alimento` y en `Plato` |
| R5-15 | Todo plato se compone de al menos un alimento: `(1,*)` del lado de `Alimento` en `Compone`. | líneas 54–55; el mínimo, decisión del equipo | capa de aplicación + trigger diferido |

**R5-13 — clasificación obligatoria.** Las consultas 2 y 5 (§8 de cada una) dan por hecho que todo alimento expone su nivel calórico, su grupo nutricional y su tipo de preparación; un alimento sin clasificar no podría intervenir en la generación de menús ni en los reportes. En consecuencia, un alimento o un plato se clasifica en la misma operación en que se da de alta.

**R5-15 — composición mínima.** El enunciado describe el plato como «compuesto por múltiples ingredientes» al explicar por qué su ficha nutricional es más extensa que la de un alimento simple. El equipo no toma «múltiples» como un mínimo estricto: un plato puede componerse de un solo alimento y diferenciarse de él por su forma de preparación, que queda registrada en la ficha nutricional ampliada del plato («modo de preparación», líneas 51–52), gestionada fuera del modelo relacional. Sí se exige al menos un alimento, porque un plato sin ingredientes carece de composición.

La restricción no es expresable con claves foráneas: el plato se registra antes que sus ocurrencias de `Compone`, y la comprobación solo tiene sentido al cierre de la operación. En la capa de aplicación, el alta de un plato y la de su composición se registran en una misma unidad de trabajo (patrón Unit of Work del backend). En PostgreSQL puede reforzarse con un trigger de restricción diferido (`DEFERRABLE INITIALLY DEFERRED`) que verifique, al confirmar la transacción, que el plato conserva al menos un alimento. La misma verificación aplica al eliminar ocurrencias de `Compone`.

---

## 3. Restricciones semánticas

| # | Restricción | Evidencia | Mecanismo |
|---|---|---|---|
| R5-16 | No existen alimentos duplicados. | línea 33 | UNIQUE sobre el nombre normalizado |
| R5-17 | Un nutricionista solo clasifica (`Pertenece`, `Corresponde`, `Tiene`) los alimentos que él mismo describió. | línea 30 | capa de aplicación |

**R5-16.** El enunciado encarga esta tarea al administrador del sistema; la base de datos puede impedir el duplicado de forma estructural, en lugar de depender de una depuración manual posterior. Se adopta como criterio de identidad el nombre normalizado, sin distinguir mayúsculas ni espacios sobrantes (índice único sobre `lower(trim(Nombre))`). La regla se extiende a `Plato` como decisión del equipo, ya que el enunciado solo menciona alimentos.

**R5-17.** No restringe los datos almacenados, sino quién puede modificarlos: la base guarda la clasificación, no el autor de cada cambio. Se verifica en la capa de aplicación: antes de modificar la clasificación de un alimento, el nutricionista autenticado debe coincidir con el registrado en `Describe`. Se extiende a `Plato` como decisión del equipo, dado que el plato lleva clasificación propia.

---

## 4. Lo que deliberadamente no se restringe

- **La clasificación de un plato no se deriva de sus ingredientes ni se contrasta con ellos.** Un plato de nivel calórico bajo puede contener alimentos de nivel alto. Es coherente con la decisión del glosario de que `Plato` lleva sus propios atributos base; exigir coherencia sería una regla nueva, no una consecuencia del modelo.
- **Un alimento puede no formar parte de ningún plato**, y un valor de nomenclador puede no estar en uso: `(0,*)` del lado de `Plato` en `Compone` y del lado de `Alimento`/`Plato` en las relaciones de clasificación.
- **La composición tiene un solo nivel:** un plato se compone de alimentos, no de otros platos, conforme al enunciado, que habla de «ingredientes».

---

## 5. Restricciones que involucran al submodelo pero no son locales

Se registran para la consolidación del issue #11; su definición corresponde a los submodelos indicados.

| Restricción | Submodelos | Estado |
|---|---|---|
| Composición de un menú en alimentos y platos (`Incluye`), y su conteo en las consultas 2 y 6: alimentos incluidos directamente más los alimentos de los platos incluidos, vía `Compone`. | #5, #7 | Modelado en el #7 (`Incluye` hacia `Alimento` y hacia `Plato`) |
| Registro del consumo cuando el menú incluye un plato: a qué alimentos se imputa la aceptación o el rechazo. | #5, #7 | Resuelto en el #7: el consumo de un plato se registra por cada alimento que lo compone (convención adoptada de R7-06) |
| Restricciones de una dieta sobre los grupos nutricionales: `Restringe(Dieta, RestricciónAlimentaria)` según el glosario frente a una relación tipificada `Dieta`–`GrupoNutricional` según la consulta 5 (§7-C). | #5, #6 | Resuelto en el #6 con la agregación `Cobertura`, cuyo `Tipo` marca cada grupo como *Requerido* o *Restringido*, y con `Excluye(RestricciónAlimentaria, GrupoNutricional)`; la consolidación fija cómo se combinan (#9, D9-1) |
| Eliminación de un nutricionista que tiene alimentos o platos descritos. | #5, #6 | Especificada en el #11 como R11-02 (política de borrado) |

---

## 6. Decisiones del equipo tomadas en este documento

Restricciones sin cita directa en el enunciado, adoptadas por el equipo: R5-03, R5-07, R5-11 y R5-12; el mínimo de un alimento de R5-15; y la extensión a `Plato` de R5-16 y R5-17.

Queda abierta una decisión: la **unidad de `Cantidad`** en `Composición` (gramos, porciones u otra). El enunciado no la fija, y conviene resolverla antes de la implementación para que R5-12 tenga un sentido inequívoco.
