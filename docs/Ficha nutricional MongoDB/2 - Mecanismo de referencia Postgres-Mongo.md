# Ficha nutricional — Mecanismo de referencia PostgreSQL → MongoDB

**Asignatura:** Bases de Datos II / Ingeniería de Software — Curso 2026-2027
**Issue:** #8 — criterio de aceptación 2: *«Mecanismo de referencia Postgres → Mongo documentado»*
**Documentos relacionados:** [1 - Esquema del documento](./1%20-%20Esquema%20del%20documento%20de%20ficha%20nutricional.md) · [3 - Justificación de la partición](./3%20-%20Justificacion%20de%20la%20particion%20Postgres-Mongo.md) · [Submodelo Alimento-Plato](../Submodelos%20Relacionales%20del%20MERX/Submodelo%20Alimento-Plato.md) (issue #5)
**Alcance de este documento:** cómo se localiza, desde una fila de `Alimento` o `Plato` en PostgreSQL, su ficha nutricional en MongoDB; cómo se mantienen consistentes ambos almacenes sin una transacción común; y qué reglas de integridad cubren la referencia.

---

## 1. Qué hay que resolver

El enunciado separa la ficha del resto de las tablas (líneas 55–57), pero la ficha sigue perteneciendo a un alimento o a un plato concreto (línea 51). Hace falta, por tanto, un vínculo entre dos gestores que no comparten claves foráneas ni transacciones:

- **Dirección:** la entidad dueña está en PostgreSQL (`Alimento`, `Plato`); el documento dependiente está en MongoDB. Se navega de PostgreSQL a MongoDB: dado un `AlimentoId` o `PlatoId`, obtener su ficha.
- **Cardinalidad:** cada fila tiene **cero o una** ficha (la ficha es opcional, línea 51), y cada ficha pertenece a **exactamente una** fila.

---

## 2. Alternativas consideradas

| | Alternativa | Dónde vive la referencia | Ventajas | Inconvenientes |
|---|---|---|---|---|
| A | Columna `FichaId` (el `ObjectId` como texto) en `Alimento` y `Plato` | PostgreSQL | La referencia se ve en la tabla. | Añade un atributo al MERX del submodelo #5, que no lo tiene. Obliga a escribir en los dos almacenes para dar de alta una ficha (primero MongoDB para obtener el `_id`, luego actualizar PostgreSQL), con más puntos de fallo. |
| **B** | **La ficha guarda la clave primaria de su fila (`origen.tabla`, `origen.id`), con índice único** | MongoDB | No cambia el MERX. Cada alta o baja de ficha escribe en un solo almacén. La clave que se usa para buscar es la misma PK que ya existe en PostgreSQL. | La referencia no se ve desde la tabla; la existencia de la ficha se comprueba consultando MongoDB. |
| C | Columna `JSONB` en PostgreSQL | — | Sin segundo gestor. | No es una referencia: guarda la ficha junto a las tablas, que es lo que el enunciado descarta (líneas 55–57). Ver documento 3. |

**Se adopta la alternativa B.** La PK de PostgreSQL es la clave de referencia: funciona como una clave foránea lógica cuyo lado dependiente está en MongoDB. El sentido de la navegación sigue siendo PostgreSQL → MongoDB, porque se parte siempre de la fila relacional:

```javascript
// Ficha del alimento 42
db.fichas_nutricionales.findOne({ "origen.tabla": "Alimento", "origen.id": 42 })
```

El índice único sobre (`origen.tabla`, `origen.id`) (documento 1, §5) hace que esta búsqueda sea directa y garantiza que no haya dos fichas para la misma fila.

---

## 3. Operaciones

La interfaz `IUnitOfWork` del backend coordina solo la transacción de PostgreSQL; la escritura en MongoDB se trata como una operación independiente (`src/Menus.Domain/Abstractions/IUnitOfWork.cs`). Por eso el orden de las escrituras está fijado para que un fallo a mitad deje siempre un estado válido o recuperable.

| Operación | Orden | Si falla el segundo paso |
|---|---|---|
| **Consultar** la ficha de un alimento o plato | 1. Leer la fila en PostgreSQL. 2. `findOne` por `origen` en MongoDB. | Se muestra el alimento sin ficha; la ficha es opcional. |
| **Alta** de alimento o plato con ficha | 1. Confirmar la fila en PostgreSQL (obtiene su `Id`). 2. Insertar la ficha con ese `origen`. | La fila queda sin ficha, que es un estado válido. Se reintenta la inserción o se deja para después. |
| **Alta o edición** de la ficha de una fila existente | 1. Comprobar que la fila existe en PostgreSQL. 2. `replaceOne` con `upsert: true` filtrando por `origen`. | Sin efecto en PostgreSQL; se reintenta. |
| **Baja** de alimento o plato | 1. Eliminar la fila en PostgreSQL (sujeto a R5-07). 2. `deleteOne` por `origen`. | Queda una ficha huérfana. No afecta a ninguna consulta, porque se llega a la ficha siempre desde la fila; la conciliación (F-10) la elimina. |
| **Cambio de nombre** de un alimento | 1. Actualizar `Nombre` en PostgreSQL. 2. Actualizar la copia `nombre` en los desgloses de los platos que lo usan. | La ficha del plato muestra el nombre anterior hasta el siguiente reintento o conciliación. |

El paso 2 de la última operación, en MongoDB:

```javascript
db.fichas_nutricionales.updateMany(
  { "ingredientes.alimentoId": 42 },
  { $set: { "ingredientes.$[i].nombre": "Manzana roja" } },
  { arrayFilters: [{ "i.alimentoId": 42 }] }
)
```

**Regla de orden:** en las altas se escribe primero en PostgreSQL y en las bajas también primero en PostgreSQL. Así, el único estado intermedio posible es «fila sin ficha» (válido) o «ficha sin fila» (huérfana, invisible y recuperable), y nunca una fila que apunte a una ficha inexistente.

---

## 4. Reglas de integridad de la referencia

| # | Regla | Evidencia | Mecanismo |
|---|---|---|---|
| F-06 | En la ficha de un plato, el conjunto de `ingredientes.alimentoId` coincide con los alimentos del plato en `Compone`. | líneas 54–55; composición del submodelo #5 | capa de aplicación (al guardar la ficha y al modificar `Compone`) |
| F-07 | Solo existe ficha para filas existentes de `Alimento` o `Plato`: `origen.id` corresponde a una PK vigente de la tabla indicada en `origen.tabla`. | líneas 51, 56–57 | capa de aplicación (comprobación previa) + conciliación (F-10) |
| F-08 | Cada fila tiene a lo sumo una ficha. | línea 51 | índice único (`origen.tabla`, `origen.id`) |
| F-09 | Al eliminar un alimento o un plato se elimina su ficha. | decisión del equipo | capa de aplicación (paso 2 de la baja) |
| F-10 | Periódicamente se eliminan las fichas huérfanas (su `origen` ya no existe en PostgreSQL) y se reconstruyen las copias de `nombre` desactualizadas. | decisión del equipo | tarea programada del backend |

**F-06.** Como un alimento no se elimina mientras forme parte de un plato (R5-07, submodelo #5), el desglose de un plato nunca referencia un alimento inexistente; la regla solo cubre altas y bajas de ingredientes en `Compone`.

**F-07 y F-10.** Son la parte que PostgreSQL resolvería con una clave foránea y que entre dos gestores no se puede declarar. La combinación «comprobación al escribir + conciliación periódica» es la forma habitual de mantener integridad referencial entre almacenes distintos: la primera evita crear huérfanas, la segunda limpia las que deja un fallo a mitad de una baja.

---

## 5. Relación con el MERX

La ficha **no aparece en el MERX**: es un documento externo gestionado en otro motor, no una entidad ni un atributo del modelo relacional (líneas 55–57). Con la alternativa B, el MERX del submodelo #5 queda sin cambios: `Alimento` y `Plato` conservan sus atributos, y su clave primaria es la que usa MongoDB como referencia.

En el informe (§4, MERX) basta una nota junto a `Alimento` y `Plato`: *«pueden tener una ficha nutricional ampliada en MongoDB, localizada por su clave primaria (ver issue #8)»*.

---

## 6. Decisiones del equipo tomadas en este documento

- Alternativa B como mecanismo de referencia.
- Orden de escritura PostgreSQL primero, en altas y en bajas.
- Reglas F-09 y F-10.

Queda abierta la **frecuencia de la conciliación** (F-10), que depende del volumen de altas y bajas; puede fijarse al implementar el backend.
