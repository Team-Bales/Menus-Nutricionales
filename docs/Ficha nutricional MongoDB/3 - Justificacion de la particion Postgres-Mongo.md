# Ficha nutricional — Justificación de la partición PostgreSQL / MongoDB

**Asignatura:** Bases de Datos II / Ingeniería de Software — Curso 2026-2027
**Issue:** #8 — criterio de aceptación 3: *«Justificación formal de la partición»*
**Documentos relacionados:** [1 - Esquema del documento](./1%20-%20Esquema%20del%20documento%20de%20ficha%20nutricional.md) · [2 - Mecanismo de referencia](./2%20-%20Mecanismo%20de%20referencia%20Postgres-Mongo.md) · consultas formalizadas (`docs/consultas/`)
**Alcance de este documento:** qué datos van a cada gestor, con qué criterio se decide, por qué las alternativas puramente relacionales no sirven para la ficha, qué costes tiene la partición y cómo se mitigan. Cierra con un texto listo para el informe «Diseño de la base de datos».

---

## 1. La partición

| Gestor | Datos | Papel |
|---|---|---|
| **PostgreSQL** | Todo el MERX: alimentos, platos y su clasificación y composición (submodelo #5); dietas, nutricionistas, pacientes y salas (submodelo #6); menús, validaciones, asignaciones, consumos, valoraciones y revalorizaciones (submodelo #7). | Fuente de verdad de los datos estructurados, sus restricciones y las seis consultas informacionales. |
| **MongoDB** | La ficha nutricional ampliada de alimentos y platos (colección `fichas_nutricionales`). | Almacén de documentos de estructura variable, referenciados desde PostgreSQL (documento 2). |

Redis, también presente en `docker-compose.yml`, no forma parte de esta partición: es una caché de las consultas recurrentes (líneas 58–60) y no guarda datos propios. Se trata en otro issue.

---

## 2. Criterio de partición

Un conjunto de datos se ubica en el almacén de documentos si cumple **todas** estas condiciones; si falla alguna, se queda en PostgreSQL.

| # | Condición | Por qué importa |
|---|---|---|
| C1 | **Estructura heterogénea:** los atributos presentes cambian de una instancia a otra. | Un esquema relacional fija las columnas para todas las filas. |
| C2 | **Estructura anidada:** contiene subestructuras y listas de profundidad variable. | En relacional, cada nivel de anidamiento es una tabla más y un join más. |
| C3 | **Acceso unitario por clave:** se lee y se escribe completo, a partir de su dueño. | Es el patrón para el que está optimizado un almacén de documentos. |
| C4 | **Fuera de las consultas analíticas:** no participa en joins, agrupaciones ni filtros de las consultas informacionales. | Si participara, cada consulta cruzaría dos gestores sin un plan de ejecución común. |
| C5 | **Sin restricciones con otras entidades,** salvo la pertenencia a su dueño. | Las restricciones entre tablas solo se declaran dentro de un mismo gestor. |

### 2.1 Evaluación

| Condición | Ficha nutricional | Datos del MERX (p. ej., `Menú`, `Consumo`) |
|---|---|---|
| C1 | **Sí.** «cantidad y tipo de atributos varía de un alimento a otro y no se ajusta a una estructura fija ni común» (líneas 52–53). | No. Atributos fijos y atómicos (regla de atomicidad del MERX, `Atributos.pdf`). |
| C2 | **Sí.** El plato requiere una ficha «considerablemente más extensa y anidada» (líneas 54–55): micronutrientes por grupos, pasos de preparación, desglose por ingrediente (documento 1, §4). | No. |
| C3 | **Sí.** Se consulta la ficha de un alimento o plato concreto, completa (documento 2, §2). | No. Se filtran, agrupan y combinan entre sí. |
| C4 | **Sí.** Ninguna de las consultas 1 a 6 usa macronutrientes, micronutrientes, alérgenos ni modo de preparación: la clasificación que usan (nivel calórico, grupo nutricional, tipo de preparación) está en PostgreSQL. | No. Son el objeto de las seis consultas. |
| C5 | **Sí.** Solo pertenece a su alimento o plato; las reglas F-06 a F-10 cubren esa pertenencia (documento 2, §4). | No. Participan en claves foráneas y restricciones cruzadas (issue #11). |

La ficha cumple las cinco condiciones y ningún otro dato del dominio cumple ninguna. La frontera de la partición, por tanto, **no corta ninguna consulta**: las seis se resuelven enteras en PostgreSQL.

A la evaluación se suma el mandato explícito del enunciado: la ficha «no debe modelarse junto al resto de las tablas de la solución, sino gestionarse de forma independiente, permitiendo que cada alimento almacene únicamente los atributos que le correspondan» (líneas 55–57).

---

## 3. Alternativas relacionales descartadas

| Alternativa | Cómo sería | Por qué se descarta |
|---|---|---|
| **Tabla ancha** | Una columna por cada nutriente, alérgeno o dato de preparación posible, en `Alimento` o en una tabla 1:1. | Casi todas las celdas serían nulas (un alimento simple usa «pocos campos», línea 53), y cada dato nuevo exige alterar la tabla. Contradice «almacene únicamente los atributos que le correspondan» (líneas 56–57). |
| **Tablas normalizadas por bloque** | Tablas `Macronutriente`, `Micronutriente`, `Alérgeno`, `PasoPreparación`, `IngredienteFicha`… relacionadas con `Alimento`. | Es la forma que exige el MERX para un dato compuesto y multivaluado, pero fija de antemano qué subcampos existen, y la heterogeneidad (C1) obliga a genéricos del tipo «nombre–valor». Leer una sola ficha requeriría varios joins, sin que ninguna consulta se beneficie de tener esos datos en tablas (C4). |
| **Entidad–atributo–valor (EAV)** | Una tabla `(AlimentoId, Atributo, Valor)`. | Pierde los tipos (todo valor es texto), no representa el anidamiento (C2) sin claves artificiales de jerarquía, y no admite restricciones de dominio por atributo. |
| **Columna `JSONB` en PostgreSQL** | La ficha como documento dentro de una columna de `Alimento`. | Técnicamente resolvería C1 a C3, pero guarda la ficha junto a las tablas de la solución, que es exactamente lo que el enunciado descarta (líneas 55–57). |

---

## 4. Costes de la partición y cómo se mitigan

| Coste | Mitigación |
|---|---|
| **Sin transacción común:** una operación que escribe en los dos gestores no es atómica. | Orden de escritura fijo (PostgreSQL primero, en altas y bajas), de modo que el único estado intermedio es «fila sin ficha» (válido) o «ficha huérfana» (invisible y recuperable). Documento 2, §3. |
| **Sin claves foráneas entre gestores.** | Comprobación previa en la capa de aplicación (F-07), índice único para la cardinalidad (F-08) y conciliación periódica de huérfanas (F-10). Documento 2, §4. |
| **Copias desactualizadas** (el `nombre` de cada ingrediente en la ficha de un plato). | Se actualizan al renombrar el alimento y se reconstruyen en la conciliación. La fuente de verdad sigue siendo PostgreSQL. |
| **Validación más débil** que en un esquema relacional. | Validador `$jsonSchema` sobre lo común a toda ficha, sin restringir los bloques variables. Documento 1, §5. |
| **Un gestor más que operar.** | MongoDB ya forma parte del entorno (`docker-compose.yml`, `.env.example`) y el backend incluye el controlador oficial (`MongoDB.Driver`). |

---

## 5. Texto propuesto para el informe

Para el informe «Diseño de la base de datos», en la sección 6 (*Valoración de la corrección del diseño de la base de datos*) o como nota de la sección 4 (*MERX*):

> **Partición de los datos entre PostgreSQL y MongoDB.** La ficha nutricional ampliada de alimentos y platos se almacena en MongoDB; el resto del modelo, en PostgreSQL. El criterio adoptado ubica un conjunto de datos en el almacén de documentos solo si su estructura es heterogénea y anidada, se accede a él de forma unitaria a partir de su dueño, no participa en las consultas informacionales y no tiene restricciones con otras entidades. La ficha cumple las cinco condiciones: el enunciado establece que su cantidad y tipo de atributos varía de un alimento a otro y que la de un plato es considerablemente más extensa y anidada (líneas 51–55), y ninguna de las seis consultas usa su contenido. Ningún otro dato del dominio cumple estas condiciones, de modo que las seis consultas se resuelven íntegramente en PostgreSQL. Las alternativas relacionales (tabla ancha, tablas normalizadas por bloque, entidad–atributo–valor o columna JSONB) se descartan porque imponen una estructura fija, multiplican los nulos o los joins, o mantienen la ficha junto a las tablas, contra lo que exige el enunciado (líneas 55–57). La ficha referencia a su alimento o plato mediante la clave primaria de PostgreSQL, con un índice único que garantiza a lo sumo una ficha por fila; la ausencia de transacciones y de claves foráneas entre ambos gestores se compensa con un orden de escritura fijo, comprobaciones en la capa de aplicación y una conciliación periódica.
