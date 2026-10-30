# ADR-008: Todos los nomencladores son entidades; los catálogos cerrados, con identificadores fijos

**Estado:** Aceptado  
**Fecha:** 2026-10-28  
**Reemplaza a:** [ADR-003](ADR-003-nomencladores-catalog-entities.md)

## Contexto

El ADR-003 dio dos tratamientos a los nomencladores:
- los **catálogos abiertos** (`GrupoNutricional`, `RestriccionAlimentaria`, `ProgramaAtencion`, `Especialidad`) como entidades con tabla y datos iniciales;
- los **catálogos cerrados** que fija el enunciado (`TipoPreparacion`, `NivelCalorico`) como enums de C#, guardados como texto en las columnas que los usaban.

Después cambiaron tres cosas:

1. **El ADR-006** modeló `ResultadoNutricional`, el séptimo nomenclador, como **entidad de catálogo cerrado**. Es una entidad con cinco instancias fijas, un `Guid` fijo cada una, y tabla propia. Lo presentó como una excepción a la regla «catálogos cerrados → enum».
2. **El modelo trata los siete nomencladores como entidades.** En el MERX son entidades independientes referenciadas (patrón «nomenclador», `Convenciones de modelado MERX.md`, §5), y el diseño relacional del informe les da un esquema propio a cada uno (§5.3.1). El informe además garantiza los catálogos cerrados en dos niveles (`docs/informe.md`, §4.1.3): «la FK impide usar un valor ajeno al catálogo, y el CHECK sobre el propio catálogo impide ampliarlo» (RD-01, RD-02 y RD-14). Con un enum no hay tabla, ni FK, ni CHECK sobre el catálogo.
3. **El #142 convirtió `TipoPreparacion` y `NivelCalorico`** al patrón del ADR-006 (commit `f1eac7d`, 15/10). Las migraciones se regeneraron con sus tablas en el #186. El código ya no tiene los enums que describe el ADR-003, y el #142 pedía actualizar el ADR.

Este ADR registra la regla que el código ya sigue y reemplaza al ADR-003.

## Decisión

**Los siete nomencladores son entidades** (`Entity<Guid>`) en `Menus.Domain/Nomenclators/`. Cada uno tiene tabla propia en PostgreSQL, y las tablas que lo usan lo referencian por clave foránea.

| Tipo | Nomencladores | Cómo se modelan |
|---|---|---|
| **Abierto** | `GrupoNutricional`, `RestriccionAlimentaria`, `ProgramaAtencion`, `Especialidad` | Constructor público: la institución puede agregar valores sin cambiar el código. Datos iniciales en la migración. El nombre es obligatorio (RD-04, RD-07) y único dentro de cada nomenclador (RD-08), y RD-03 declara abierto a `GrupoNutricional` |
| **Cerrado** | `TipoPreparacion`, `NivelCalorico`, `ResultadoNutricional` | Constructor privado. Una instancia estática por valor, con un `Guid` fijo, y la lista `Todos`. Los datos iniciales usan esas mismas instancias (`HasData(X.Todos)`). Un `CHECK` sobre la tabla del catálogo impide agregar otros valores |

En los catálogos cerrados, la integridad queda en dos niveles, como pide el informe:
- **En el dominio,** no se pueden crear otras instancias, y `Existe(Guid)` comprueba que un identificador sea del catálogo. `ResultadoNutricional` agrega `DesdeValor(int)`.
- **En la base,** la FK impide referenciar un valor que no está en la tabla, y el `CHECK` impide ampliar la tabla.

**Los dominios de atributo no son nomencladores y siguen como valores.** Los dominios cerrados de un atributo que el MERX no modela como entidad no llevan tabla:
- `Tipo` de `CubreDieta` (RD-16): enum `TipoCobertura`, guardado como texto con `CHECK`;
- `Clase` de `ItemAlimentario` (RD-17): el discriminador de la tabla `items_alimentarios`.

Para ellos sigue valiendo el argumento del ADR-003: el compilador controla los valores, y no hay nada que referenciar por clave foránea.

**Lo que no cambia respecto del ADR-003:** los nomencladores no son `AggregateRoot` y no tienen repositorio propio.

## Alternativas consideradas

| Alternativa | Razón de descarte |
|-------------|-------------------|
| Mantener los enums del ADR-003 para `TipoPreparacion` y `NivelCalorico` | Sin tabla no hay FK ni `CHECK` sobre el catálogo, que es como el informe garantiza RD-01 y RD-02, y el esquema físico no coincidiría con el diseño relacional |
| Catálogos cerrados como entidades sin identificadores fijos (con identificadores generados al sembrar la base) | El código no podría referirse a un valor concreto (`TipoPreparacion.Entrante`) sin consultar la base, y el seed y el dominio podrían quedar desalineados |
| Convertir también los dominios de atributo (`TipoCobertura`, `Clase`) en tablas | No son entidades en el MERX: son valores de un atributo. Una tabla agregaría una FK sin nada que la justifique en el modelo |

## Consecuencias

- ✅ **El código coincide con el modelo:** los siete nomencladores tienen tabla, como en el MERX y en el diseño relacional del informe.
- ✅ **Los catálogos cerrados se garantizan en dos niveles,** en el dominio y en la base.
- ✅ **El ADR-006 deja de ser una excepción:** su patrón es ahora la regla para todos los catálogos cerrados.
- ✅ **Los `Guid` fijos alinean el código y la base,** porque el seed y el dominio usan los mismos identificadores.
- ⚠️ **Cambiar un catálogo cerrado exige tocar varias cosas a la vez:** el código, el `CHECK` y una migración. Es lo esperado, porque sus valores los fija el enunciado.
- ⚠️ **La escritura de los catálogos abiertos queda por resolver.** Se pueden ampliar, pero no tienen repositorio. La gestión de nomencladores (#64) tendrá que decidir cómo se escriben.
