# Ficha nutricional — Esquema del documento en MongoDB

**Asignatura:** Bases de Datos II / Ingeniería de Software — Curso 2026-2027
**Issue:** #8 — criterio de aceptación 1: *«Esquema de documento de ejemplo (alimento simple y plato compuesto)»*
**Documentos relacionados:** [2 - Mecanismo de referencia Postgres-Mongo](./2%20-%20Mecanismo%20de%20referencia%20Postgres-Mongo.md) · [3 - Justificación de la partición](./3%20-%20Justificacion%20de%20la%20particion%20Postgres-Mongo.md) · [Submodelo Alimento-Plato](../Submodelos%20Relacionales%20del%20MERX/Submodelo%20Alimento-Plato.md) (issue #5)
**Alcance de este documento:** estructura del documento que almacena la ficha nutricional ampliada, con un ejemplo completo para un alimento simple y otro para un plato compuesto, y las reglas mínimas de validación de la colección.

---

## 1. Qué exige el enunciado

> «Cada alimento puede además contener una ficha nutricional ampliada (macronutrientes, micronutrientes, alérgenos, modo de preparación), cuya cantidad y tipo de atributos varía de un alimento a otro y no se ajusta a una estructura fija ni común para todos los casos: un alimento simple puede describirse con pocos campos, mientras que un plato compuesto por múltiples ingredientes requiere una ficha considerablemente más extensa y anidada.» (líneas 51–55)

| Exigencia | Consecuencia para el documento |
|---|---|
| «puede además contener» (línea 51) | La ficha es **opcional**: un alimento o plato puede no tener documento. |
| Cuatro bloques nombrados (línea 52) | `macronutrientes`, `micronutrientes`, `alergenos`, `modoPreparacion`. Ninguno es obligatorio. |
| «cantidad y tipo de atributos varía» (líneas 52–53) | Dentro de cada bloque, los campos presentes cambian de un documento a otro. |
| Alimento simple con «pocos campos» frente a plato «más extensa y anidada» (líneas 53–55) | Dos formas de documento en la misma colección: `simple` y `compuesto`. |
| «permitiendo que cada alimento almacene únicamente los atributos que le correspondan» (líneas 56–57) | Un campo ausente significa «no aplica o no se conoce»; no se guardan campos vacíos ni `null` de relleno. |

La ficha se aplica también a `Plato`: el propio enunciado usa el plato como caso de ficha extensa (línea 54), y el submodelo #5 ubica en ella el modo de preparación del plato (R5-15).

---

## 2. Colección y estructura común

- **Base de datos:** la definida en `MONGO_DB` (`.env.example`).
- **Colección:** `fichas_nutricionales`, una sola para alimentos y platos.

Campos comunes a todo documento:

| Campo | Tipo | Obligatorio | Significado |
|---|---|---|---|
| `_id` | `ObjectId` | sí (lo genera MongoDB) | Identificador interno del documento. |
| `origen` | objeto `{ tabla, id }` | sí | Fila de PostgreSQL a la que pertenece la ficha: `tabla` ∈ {`Alimento`, `Plato`} e `id` = `AlimentoId` o `PlatoId`. Es la referencia descrita en el documento 2. |
| `tipo` | `string` | sí | `simple` (alimento) o `compuesto` (plato). |
| `porcionReferencia` | objeto `{ cantidad, unidad }` | no | Porción sobre la que se expresan los valores (p. ej., 100 g). |
| `macronutrientes` | objeto | no | Energía y macronutrientes; campos variables. |
| `micronutrientes` | objeto | no | Vitaminas, minerales u otros; subobjetos variables. |
| `alergenos` | arreglo de `string` | no | Alérgenos presentes. Ausente = no registrado; `[]` = registrado sin alérgenos. |
| `modoPreparacion` | objeto | no | Pasos, tiempos y técnica; propio sobre todo de los platos. |
| `ingredientes` | arreglo de objetos | solo en `compuesto` | Desglose nutricional por ingrediente (§4). |
| `actualizadoEn` | `Date` | sí | Fecha de la última modificación de la ficha. |

Convención de nombres: camelCase y sufijo de unidad en los valores numéricos (`energiaKcal`, `proteinasG`, `sodioMg`), para que cada número lleve su unidad sin un campo aparte.

---

## 3. Ejemplo — alimento simple

Ficha breve: solo los campos que se conocen para ese alimento.

```json
{
  "_id": { "$oid": "66f1a2b3c4d5e6f708192a3b" },
  "origen": { "tabla": "Alimento", "id": 42 },
  "tipo": "simple",
  "porcionReferencia": { "cantidad": 100, "unidad": "g" },
  "macronutrientes": {
    "energiaKcal": 52,
    "carbohidratosG": 13.8,
    "proteinasG": 0.3,
    "grasasG": 0.2,
    "fibraG": 2.4
  },
  "alergenos": [],
  "actualizadoEn": { "$date": "2026-09-30T10:15:00Z" }
}
```

El alimento 42 (una manzana, por ejemplo) no tiene `micronutrientes` ni `modoPreparacion`: los campos no aplican, así que no aparecen.

---

## 4. Ejemplo — plato compuesto

Ficha extensa y anidada: bloques con subobjetos, arreglos de pasos y desglose por ingrediente.

```json
{
  "_id": { "$oid": "66f1a2b3c4d5e6f708192a4c" },
  "origen": { "tabla": "Plato", "id": 7 },
  "tipo": "compuesto",
  "porcionReferencia": { "cantidad": 1, "unidad": "ración (350 g)" },
  "macronutrientes": {
    "energiaKcal": 486,
    "carbohidratosG": 58.2,
    "proteinasG": 31.5,
    "grasasG": 12.4,
    "grasasSaturadasG": 3.1,
    "fibraG": 6.8,
    "azucaresG": 4.0
  },
  "micronutrientes": {
    "vitaminas": { "aUg": 310, "cMg": 22, "b12Ug": 0.6 },
    "minerales": { "hierroMg": 3.9, "calcioMg": 64, "sodioMg": 620, "potasioMg": 780 }
  },
  "alergenos": ["apio"],
  "modoPreparacion": {
    "tecnica": "guiso",
    "tiempoTotalMin": 55,
    "pasos": [
      { "orden": 1, "descripcion": "Cocinar la cebolla y el apio a fuego medio.", "tiempoMin": 8 },
      { "orden": 2, "descripcion": "Añadir el pollo troceado y dorar.", "tiempoMin": 10 },
      { "orden": 3, "descripcion": "Incorporar el arroz, cubrir con agua y cocer a fuego lento.", "tiempoMin": 35 },
      { "orden": 4, "descripcion": "Reposar antes de servir.", "tiempoMin": 2 }
    ]
  },
  "ingredientes": [
    { "alimentoId": 15, "nombre": "Pollo",
      "aporte": { "energiaKcal": 248, "proteinasG": 27.0, "grasasG": 9.0 } },
    { "alimentoId": 23, "nombre": "Arroz",
      "aporte": { "energiaKcal": 208, "carbohidratosG": 46.0, "proteinasG": 4.3 } },
    { "alimentoId": 31, "nombre": "Cebolla",
      "aporte": { "energiaKcal": 24, "carbohidratosG": 5.6 } },
    { "alimentoId": 34, "nombre": "Apio",
      "aporte": { "energiaKcal": 6, "fibraG": 0.5 } }
  ],
  "actualizadoEn": { "$date": "2026-09-30T10:20:00Z" }
}
```

**Sobre `ingredientes`.** La composición del plato vive en PostgreSQL (`Compone`, con `Cantidad`, submodelo #5), y esa es la fuente de verdad. El arreglo `ingredientes` no la reemplaza: es el **desglose nutricional** de cada ingrediente dentro del plato, que es justamente lo que vuelve la ficha «más extensa y anidada» (línea 55). Cada elemento lleva `alimentoId` para poder contrastarlo con `Compone` (regla R8-06 del documento 2); `nombre` es solo una copia de lectura para mostrar la ficha sin consultar PostgreSQL. La cantidad de cada ingrediente **no** se repite en la ficha: se lee de `Composición` (`Cantidad`), así que la ficha no puede contradecirla.

---

## 5. Validación de la colección

MongoDB permite validar con `$jsonSchema` sin imponer una estructura fija. El validador exige solo lo que toda ficha comparte, más la coherencia entre la tabla de origen y el tipo, y deja libre el resto (`additionalProperties` no se restringe en los bloques variables). MongoDB implementa el draft 4 de JSON Schema, donde `exclusiveMinimum` es booleano y acompaña a `minimum`:

```javascript
db.createCollection("fichas_nutricionales", {
  validator: {
    $jsonSchema: {
      bsonType: "object",
      required: ["origen", "tipo", "actualizadoEn"],
      properties: {
        origen: {
          bsonType: "object",
          required: ["tabla", "id"],
          properties: {
            tabla: { enum: ["Alimento", "Plato"] },
            id: { bsonType: ["int", "long"], minimum: 1 }
          }
        },
        tipo: { enum: ["simple", "compuesto"] },
        porcionReferencia: {
          bsonType: "object",
          required: ["cantidad", "unidad"],
          properties: { cantidad: { bsonType: ["int", "double"], minimum: 0, exclusiveMinimum: true } }
        },
        macronutrientes: { bsonType: "object" },
        micronutrientes: { bsonType: "object" },
        alergenos: { bsonType: "array", items: { bsonType: "string" }, uniqueItems: true },
        modoPreparacion: { bsonType: "object" },
        ingredientes: {
          bsonType: "array",
          items: {
            bsonType: "object",
            required: ["alimentoId"],
            properties: { alimentoId: { bsonType: ["int", "long"], minimum: 1 } }
          }
        },
        actualizadoEn: { bsonType: "date" }
      },
      // R8-03: un alimento lleva ficha simple y sin desglose; un plato, ficha compuesta
      anyOf: [
        {
          properties: { origen: { properties: { tabla: { enum: ["Alimento"] } } }, tipo: { enum: ["simple"] } },
          not: { required: ["ingredientes"] }
        },
        {
          properties: { origen: { properties: { tabla: { enum: ["Plato"] } } }, tipo: { enum: ["compuesto"] } }
        }
      ]
    }
  },
  validationLevel: "strict",
  validationAction: "error"
});

// Una ficha por fila de PostgreSQL (ver documento 2)
db.fichas_nutricionales.createIndex({ "origen.tabla": 1, "origen.id": 1 }, { unique: true });
```

| # | Regla | Evidencia | Dónde se garantiza |
|---|---|---|---|
| R8-01 | Toda ficha identifica la fila de PostgreSQL a la que pertenece (`origen.tabla`, `origen.id`). | líneas 51, 56–57 | `$jsonSchema` (`required`) |
| R8-02 | `origen.tabla` solo puede ser `Alimento` o `Plato`. | líneas 51, 54 | `$jsonSchema` (`enum`) |
| R8-03 | `tipo` es `simple` o `compuesto`, y es coherente con la tabla: `Alimento` → `simple`, `Plato` → `compuesto`; solo las fichas de plato llevan `ingredientes`. | líneas 53–55 | `$jsonSchema` (`enum` y `anyOf`) |
| R8-04 | Los valores numéricos de la porción son positivos y los alérgenos no se repiten. | decisión del equipo | `$jsonSchema` |
| R8-05 | Los bloques `macronutrientes`, `micronutrientes` y `modoPreparacion` no tienen campos obligatorios. | líneas 52–53, 56–57 | ausencia de `required` en esos bloques |

---

## 6. Decisiones del equipo tomadas en este documento

- Una sola colección para alimentos y platos, distinguidos por `tipo`, en lugar de dos colecciones: la ficha es el mismo concepto con distinto grado de detalle (líneas 53–55).
- Sufijo de unidad en el nombre de cada valor numérico (`…Kcal`, `…G`, `…Mg`, `…Ug`).
- Desglose por ingrediente en los platos, subordinado a la composición de PostgreSQL y sin repetir sus cantidades (§4).
- Coherencia entre la tabla de origen y el tipo declarada en el validador (R8-03).
- Regla R8-04.

Queda abierta la **lista de alérgenos admitidos** (texto libre o catálogo cerrado). Si se adopta un catálogo, bastaría añadir un `enum` a `alergenos.items`. Se decide junto con la correspondencia entre restricciones alimentarias y alérgenos que necesita la comprobación de compatibilidad (documento 2, §5); un catálogo cerrado es lo que hace fiable esa comprobación.
