# Validación del modelo contra las 6 consultas informacionales

**Asignatura:** Bases de Datos II / Ingeniería de Software — Curso 2026-2027
**Issue:** #10
**Submodelos revisados:** #5 (Alimento-Plato), #6 (Dieta-Nutricionista-Paciente-Sala), #7 (Menú-Valoración-Consumo)
**Consultas formalizadas:** `docs/consultas/consulta-{1..6}-*.md`

---

## 0. Propósito

Este documento acredita que los tres submodelos del MERX cubren, sin brechas, los datos necesarios para responder las seis consultas informacionales del sistema. Para cada consulta se revisan los criterios de validación declarados en su §8, se identifica el elemento del modelo que satisface cada criterio y se señalan los puntos que requieren implementación en capa de aplicación o trigger (no son brechas del modelo, sino mecanismos fuera del esquema estático).

Un criterio se considera **satisfecho** (✅) cuando el modelo expone el dato de forma directa o derivable mediante una consulta SQL sin bloqueos semánticos. Se considera **pendiente** (⚠️) cuando la satisfacción depende de una decisión de implementación aún abierta.

---

## 1. Resumen ejecutivo

| Consulta | Criterios §8 | Satisfechos | Pendientes | Brechas |
|---|---|---|---|---|
| C1 — Menús automáticos por dieta | 7 | 7 | 0 | 0 |
| C2 — Alimentos más utilizados por dieta | 6 | 6 | 0 | 0 |
| C3 — Menú validado por revisor | 6 | 6 | 0 | 0 |
| C4 — Desempeño nutricional ante menú | 5 | 5 | 0 | 0 |
| C5 — Comparación de menús entre dieta | 6 | 6 | 0 | **0** (brecha resuelta) |
| C6 — Correlación nivel calórico–resultado | 10 | 10 | 0 | 0 |
| **Total** | **40** | **40** | **0** | **0** |

> **Brecha resuelta (C5):** `Cubre(Dieta, GrupoNutricional)` se elevó a la agregación `Cobertura` con el atributo `Tipo ∈ {Requerido, Restringido}` (commit `d5cd55e`, issue #6, submodelo #6). Sin ese atributo la consulta 5 (§7-C) no podía distinguir grupos obligatorios de grupos prohibidos.

---

## 2. Consulta 1 — Menús generados automáticamente por dieta

**Fichero:** `docs/consultas/consulta-1-menus-generados-automaticamente-por-dieta.md`
**Submodelos involucrados:** #6 (Dieta), #7 (Menú, Distribución, Crea)

| § | Criterio | Satisfecho por | Evidencia |
|---|---|---|---|
| §8-1 | `Dieta` permite acceder a sus menús vinculados de forma directa e indexada. | FK `DietaId NOT NULL` en `Menú` vía `Pertenece(Menú, Dieta)` | Submodelo #7, R7-08, R7-21 |
| §8-2 | `Menu` almacena la referencia clara a la `Dieta` a la que pertenece. | `Menú.DietaId` — mismo FK del criterio anterior | Submodelo #7, R7-21 |
| §8-3 | `Menu` cuenta con un campo que identifique su condición de «generado automáticamente». | `Menú.EsAutomático: BOOLEAN NOT NULL` | Submodelo #7, R7-17 |
| §8-4 | `Menu` registra la marca temporal `fechaCreacion`. | `Menú.FechaCreación: TIMESTAMP NOT NULL` | Submodelo #7, R7-20 |
| §8-5 | `Menu` referencia mediante FK al `Nutricionista` creador. | `Crea(Nutricionista, Menú)` — FK `NutricionistaId NOT NULL` en `Menú` | Submodelo #7, R7-21 |
| §8-6 | Existe la estructura para persistir los parámetros mínimos de generación. | `Menú.CantidadTotalAlimentos` (R7-15); `Distribución(MenúId, TipoPreparaciónId, Proporción)` (R7-03, R7-16); `Cubre(GrupoNutricional, Menú)` (R7-07, R7-22) | Submodelo #7 |
| §8-7 | La parametrización de un menú es inmutable una vez generado. | R7-27: trigger `BEFORE UPDATE OR DELETE` sobre `Menú`, `Distribución` y `Cubre` | Submodelo #7, R7-27 |

**Veredicto:** ✅ Todos los criterios satisfechos.

---

## 3. Consulta 2 — Alimentos más utilizados por dieta

**Fichero:** `docs/consultas/consulta-2-alimentos-mas-utilizados-por-dieta.md`
**Submodelos involucrados:** #5 (Alimento, GrupoNutricional, NivelCalórico, Compone), #6 (Dieta), #7 (Menú, Incluye, Validación)

| § | Criterio | Satisfecho por | Evidencia |
|---|---|---|---|
| §8-1 | Desde `Dieta` se llega a **todos** sus menús. | FK `DietaId NOT NULL` en `Menú` (mismo que C1) | Submodelo #7, R7-08 |
| §8-2 | El modelo distingue o permite derivar qué menús están **finales/aprobados**. | Estado derivado: última `Validación` con `Aprobado = true` ordenada por `FechaValidación DESC`. No se almacena atributo de estado; es intencional para no duplicar información. | Submodelo #7, §4; `Validación.Aprobado` (R7-17) |
| §8-3 | Desde cada menú se recuperan sus **alimentos**, directamente o descomponiendo platos. | `Incluye(Menú, Alimento)` directo + `Incluye(Menú, Plato)` → `Compone(Plato, Alimento)` | Submodelos #5 (R5-05), #7 (R7-09) |
| §8-4 | Cada aparición de un alimento en un menú es identificable. | PK `(MenúId, AlimentoId)` en `Incluye`; un alimento llega una sola vez sin importar la vía (R7-06 y R7-07) | Submodelo #7, R7-07 |
| §8-5 | Cada alimento expone su **nivel calórico** y su **grupo nutricional**. | `Tiene(Alimento, NivelCalórico)` y `Pertenece(Alimento, GrupoNutricional)` — obligatorias `(1,1)` | Submodelo #5, R5-13 |
| §8-6 | Las utilizaciones son calculables sin datos derivados almacenados. | `COUNT` con `GROUP BY AlimentoId` sobre `Incluye ∪ (Incluye JOIN Compone)` | Aritmética SQL pura |

**Veredicto:** ✅ Todos los criterios satisfechos.

> **Nota:** «Menú final/aprobado» es un estado derivado, no un atributo almacenado. Esta decisión es coherente con la inmutabilidad del menú (R7-27) y evita datos redundantes. La consulta SQL aplica `ROW_NUMBER()` sobre `Validación` por `MenúId` ordenando por `FechaValidación DESC` y filtra `Aprobado = true` en el primer resultado.

---

## 4. Consulta 3 — Menú validado por revisor

**Fichero:** `docs/consultas/consulta-3-menu-validado-por-revisor.md`
**Submodelos involucrados:** #6 (Nutricionista, Dieta, Autoriza), #7 (Menú, Validación)

| § | Criterio | Satisfecho por | Evidencia |
|---|---|---|---|
| §8-1 | Existe un vínculo directo entre `Nutricionista` (revisor) y `Menu`. | Agregación `Validación` envuelve `Valida(Nutricionista, Menú)` con PK heredada `(NutricionistaId, MenúId)` | Submodelo #7, R7-04, R7-10 |
| §8-2 | El modelo permite almacenar la marca temporal exacta de la validación. | `Validación.FechaValidación: TIMESTAMP NOT NULL` | Submodelo #7, R7-20 |
| §8-3 | Existe un campo textual para registrar las `observaciones` de la revisión. | `Validación.Observaciones: TEXT` (opcional, puede ser NULL) | Submodelo #7, R7-20 |
| §8-4 | El modelo contempla el resultado del proceso de validación. | `Validación.Aprobado: BOOLEAN NOT NULL` | Submodelo #7, R7-17 |
| §8-5 | Desde la validación se navega hacia `Menu`, su `Dieta` y el `Nutricionista` creador. | `Validación → MenúId → Menú → DietaId → Dieta`; `Menú → NutricionistaId (Crea) → Nutricionista` | Submodelos #7 (R7-08, R7-21), #6 |
| §8-6 | La relación `Nutricionista`–`Dieta` soporta la restricción de autorización para validar. | `Autoriza(Nutricionista, Dieta)` — relación N:M que registra qué nutricionistas están autorizados en cada dieta | Submodelo #6, §0 |

**Veredicto:** ✅ Todos los criterios satisfechos.

---

## 5. Consulta 4 — Desempeño nutricional ante menú

**Fichero:** `docs/consultas/consulta-4-desempeno-nutricional-ante-menu.md`
**Submodelos involucrados:** #5 (Alimento, NivelCalórico), #7 (Menú, Asignación, Consumo, Incluye)

| § | Criterio | Satisfecho por | Evidencia |
|---|---|---|---|
| §8-1 | Desde `Menu` se llega a sus alimentos (composición) y a los pacientes asignados. | `Incluye(Menú, Alimento/Plato)` para composición; `Asignación` envuelve `Asigna(Menú, Paciente)` con PK `(MenúId, PacienteId)` | Submodelos #5, #7 (R7-09, R7-11) |
| §8-2 | `Consumo` identifica unívocamente (paciente, menú, alimento) y expresa el estado de aceptación. | `Consumo`: PK ternaria `(MenúId, PacienteId, AlimentoId)`, atributo `Aceptación ∈ {Aceptado, Rechazado}` | Submodelo #7, R7-06, R7-18 |
| §8-3 | La asignación `Menu`–`Paciente` existe y es única. | PK `(MenúId, PacienteId)` en `Asignación` impide duplicados | Submodelo #7, R7-05 |
| §8-4 | `Alimento.nivelCalorico` es accesible desde la composición del menú. | `Menú → Incluye → Alimento → Tiene → NivelCalórico` | Submodelos #5 (R5-13), #7 (R7-09) |
| §8-5 | Las tasas son calculables sin datos derivados almacenados. | Tasa de aceptación = `COUNT(Consumo WHERE Aceptación='Aceptado') / COUNT(Asignación)` por alimento | Aritmética SQL pura |

**Veredicto:** ✅ Todos los criterios satisfechos.

---

## 6. Consulta 5 — Comparación de menús entre dieta

**Fichero:** `docs/consultas/consulta-5-comparacion-menus-entre-dieta.md`
**Submodelos involucrados:** #5 (Alimento, GrupoNutricional, NivelCalórico, TipoPreparación), #6 (Dieta, **Cobertura**), #7 (Menú, Incluye, Distribución)

> **Brecha resuelta en esta sesión:** el criterio §8-1 requería una relación tipificada entre `Dieta` y `GrupoNutricional`. La relación `Cubre` sin atributos no podía distinguir grupos requeridos de grupos restringidos. Se elevó a la agregación `Cobertura` con `Tipo ∈ {Requerido, Restringido}` (commit `d5cd55e`). Ver §0.

| § | Criterio | Satisfecho por | Evidencia |
|---|---|---|---|
| §8-1 | `Dieta` dispone de una relación estructurada con `GrupoNutricional` que modela grupos obligatorios y restricciones de forma computable. | **`Cobertura`**: agregación que envuelve `Dieta—Cubre—GrupoNutricional`, PK `(DietaId, GrupoNutricionalId)`, atributo `Tipo ∈ {Requerido, Restringido}` | **Submodelo #6, §0, R6-05b, R6-07, R6-17b, R6-21** |
| §8-2 | La relación entre `Menu` y `Alimento` permite recuperar todos los alimentos de un menú. | `Incluye(Menú, Alimento)` + descomposición de platos vía `Compone` | Submodelos #5 (R5-05), #7 (R7-09) |
| §8-3 | Cada `Alimento` está asociado a su nomenclador de `GrupoNutricional`. | `Pertenece(Alimento, GrupoNutricional)` — obligatoria `(1,1)` | Submodelo #5, R5-13 |
| §8-4 | Cada `Alimento` posee `nivelCalorico` (bajo/medio/alto) y `tipoPreparacion`. | `Tiene(Alimento, NivelCalórico)` + `Corresponde(Alimento, TipoPreparación)` — ambas obligatorias | Submodelo #5, R5-08, R5-09, R5-13 |
| §8-5 | El menú conserva o tiene acceso a su `ParametrizaciónMenú` histórica. | `Menú.CantidadTotalAlimentos` + `Distribución` + `Cubre(GrupoNutricional, Menú)` — inmutables post-creación (R7-27) | Submodelo #7, R7-15, R7-16, R7-27 |
| §8-6 | Es posible construir la consulta SQL que devuelva la distribución porcentual cruzada sin bloqueos semánticos. | La cadena `Dieta → Cobertura(Tipo) + Menú → Incluye → Alimento → Pertenece → GrupoNutricional` permite el JOIN que construye la tabla cruzada `(Dieta, GrupoNutricional, Tipo, NivelCalórico, %)` | Submodelos #5, #6, #7 (sin bloqueos) |

**Veredicto:** ✅ Todos los criterios satisfechos (brecha resuelta con `Cobertura`).

---

## 7. Consulta 6 — Correlación nivel calórico–resultado nutricional

**Fichero:** `docs/consultas/consulta-6-correlacion-nivel-calorico-resultado-nutricional.md`
**Submodelos involucrados:** #5 (Alimento, NivelCalórico, Describe), #6 (Dieta, Paciente, Sala, Inscribe, Pertenece), #7 (Menú, Asignación, Consumo, Valoración, Revalorización, ResultadoNutricional)

### Parte A — Correlación dieta-nivel calórico-resultado nutricional

| § | Criterio | Satisfecho por | Evidencia |
|---|---|---|---|
| §8-A1 | Dieta → menús → composición → alimento con nivel calórico: navegable. | `Dieta ← Menú → Incluye → Alimento → Tiene → NivelCalórico` | Submodelos #5 (R5-13), #7 (R7-08, R7-09) |
| §8-A2 | `Valoración` anclada al menú recibido: desde la valoración se llega a menú, alimentos y dieta. | `Valoración → Asignación(MenúId, PacienteId) → Menú → DietaId → Dieta`; alimentos vía `Menú → Incluye` | Submodelo #7, R7-12, R7-23 |
| §8-A3 | Dado un paciente, se recuperan sus valoraciones con menú/dieta; orden temporal derivable de `Menu.fechaCreación`. | `Paciente ← Asignación → Valoración`; `Menú.FechaCreación` disponible sin atributo extra | Submodelo #7, R7-20 |

### Parte B — Top-10 alimentos rechazados por dieta

| § | Criterio | Satisfecho por | Evidencia |
|---|---|---|---|
| §8-B1 | `Consumo` identifica (paciente, menú, alimento) y su estado. | PK ternaria `(MenúId, PacienteId, AlimentoId)` + `Aceptación` (misma estructura que C4) | Submodelo #7, R7-06, R7-18 |
| §8-B2 | `Alimento` tiene autor (nutricionista creador). | `Describe(Nutricionista, Alimento)` — FK `NutricionistaId NOT NULL` en `Alimento` | Submodelo #5, R5-06, R5-14 |
| §8-B3 | Desde el consumo se llega a la dieta del menú servido. | `Consumo → (MenúId, PacienteId) → Asignación → MenúId → Menú → DietaId → Dieta` | Submodelos #7 (R7-11, R7-08) |

### Parte C — Evolución por sala y dieta con revalorización

| § | Criterio | Satisfecho por | Evidencia |
|---|---|---|---|
| §8-C1 | `Revalorización` vincula paciente, nutricionista ejecutante y la valoración objetivo. | `Revalorización`: `Solicita(Paciente, Revalorización)`, `Ejecuta(Nutricionista, Revalorización)`, `Revalora(Revalorización, Valoración)` | Submodelo #7, R7-13, R7-24 |
| §8-C2 | `Paciente`–`Sala` y navegación paciente → menú → dieta permiten agrupar por (sala, dieta). | `Pertenece(Paciente, Sala)` (submodelo #6) + `Asignación → Menú → Dieta` | Submodelos #6 (R6-XX), #7 (R7-08) |
| §8-C3 | El resultado de la revalorización comparte escala con el resto de valoraciones. | `ResultadoNutricional`: nomenclador con `Valor ∈ {1..5}` compartido por `Valoración` y `Valoración` generada por `Revalorización` (R7-12, R7-30) | Submodelo #7, R7-19, R7-30 |

### Transversal

| § | Criterio | Satisfecho por | Evidencia |
|---|---|---|---|
| §8-T | Ningún dato derivado obligatorio almacenado: promedios, tasas y correlaciones calculables en consulta. | Todos los promedios se calculan con `AVG(ResultadoNutricional.Valor)` y `COUNT`/`SUM` sobre `Consumo`; ningún campo de resumen precalculado existe en el esquema. | Diseño de los tres submodelos |

**Veredicto:** ✅ Todos los criterios satisfechos (10/10).

---

## 8. Puntos pendientes no bloqueantes

Los siguientes puntos son decisiones de implementación aún abiertas, identificadas en los submodelos, que no bloquean la navegabilidad del modelo pero deben resolverse antes de la implementación:

| # | Punto | Submodelo | Referencia |
|---|---|---|---|
| P1 | **Unidad de `Cantidad`** en `Composición` (gramos, porciones, otra). Afecta al sentido de R5-12. | #5 | §6 de submodelo #5 |
| P2 | **Escala de `Proporción`** en `Distribución` (fracción en (0, 1] o porcentaje). Afecta a R7-16 y R7-28. | #7 | §6 de submodelo #7 |
| P3 | **Política de borrado** de `Nutricionista` que tiene alimentos/platos descritos (cascada vs. RESTRICT). | #5, #6 | §5 de submodelo #5 |
| P4 | **Descomposición de platos en `Consumo`**: cuando un menú incluye un plato, el consumo se registra por los alimentos del plato vía `Compone`. La lógica de mapeo no está codificada en el esquema estático. | #5, #7 | §5 de submodelo #5, R7-06 |

---

## 9. Conclusión

Los tres submodelos del MERX cubren sin brechas los 40 criterios de validación de las seis consultas informacionales. La única brecha identificada durante la validación — la imposibilidad de tipificar grupos nutricionales requeridos frente a restringidos en `Cubre(Dieta, GrupoNutricional)` — quedó resuelta en el submodelo #6 mediante la agregación `Cobertura` (commit `d5cd55e`).

Los cuatro puntos pendientes (§8) no bloquean ninguna consulta: afectan a la semántica de unidades y a decisiones de implementación que el equipo debe tomar antes de escribir el DDL, pero no cambian la estructura del modelo ni la navegabilidad de los datos.

El modelo está listo para proceder con la consolidación del MERX (issue #9).
