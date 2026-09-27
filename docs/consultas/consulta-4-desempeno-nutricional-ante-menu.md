<!--
Formalización de la consulta informacional 4 — sub-issue #19 del issue #4.
Insumos que alimenta: §3 del informe (docs/informe.md) y la validación del
modelo contra las 6 consultas (issue #10).
La numeración de líneas citada es la del texto plano del enunciado
(pdftotext), la misma empleada en los issues del repositorio.
Los nombres de entidades son los candidatos de los submodelos de Ola 1
(issues #5, #6, #7); quedan sujetos al glosario (#1) y a las convenciones
de modelado (#2).
-->

# Consulta 4. Desempeño nutricional de pacientes ante un menú

| | |
|---|---|
| **Issue** | #19 (sub-issue de #4) |
| **Línea(s) del enunciado a la que responde** | 74–75 |
| **Milestone** | Semana 5 — Diseño de BD |
| **Submodelos de Ola 1 involucrados** | #5 (Alimento) · #6 (Paciente) · #7 (Menu, Consumo, asignación menú–paciente) |
| **Insumo para** | §3 del informe (`docs/informe.md`) · Validación del modelo (#10) |

## 1. Enunciado original

> «Generar un reporte sobre el desempeño nutricional de los pacientes ante un menú, clasificando los alimentos por nivel calórico y comparando las tasas de aceptación.» (líneas 74–75)

Marco general: reportes «en tablas y gráficos» (líneas 65–67), exportables a PDF (línea 83) y con ordenación arbitraria de columnas (líneas 83–85). El párrafo de reportes del enunciado lo anticipa: «el desempeño nutricional de los pacientes según los diferentes niveles calóricos» (líneas 61–63).

## 2. Reformulación enriquecida

Dado un **menú específico** — parámetro de entrada —, el sistema debe generar un reporte sobre el **desempeño nutricional de los pacientes** que lo recibieron. El «desempeño nutricional» se concreta en el dato observable que el enunciado exige registrar: el **consumo ante un menú servido** (línea 35). El reporte:

1. Recupera los **alimentos del menú** clasificados por **nivel calórico** (bajo, medio, alto; línea 31).
2. Calcula, por alimento, la **tasa de aceptación** entre los pacientes a los que se sirvió: aceptaciones ÷ servicios (ver §7-A y §7-B).
3. **Compara las tasas** entre alimentos, entre niveles calóricos y en el agregado del menú, para detectar patrones de aceptación/rechazo por nivel calórico.

Elementos de la consulta:

- **Parámetro de entrada:** un menú concreto.
- **Universo:** los pacientes con ese menú **asignado** (líneas 33–35) y con **consumo registrado** ante el menú servido (línea 35).
- **Métrica:** tasa de aceptación por alimento, agregable por nivel calórico.
- **Ejes de desglose:** alimento y nivel calórico; el grupo nutricional y el tipo de preparación del alimento (líneas 24, 30) enriquecen el reporte como columnas adicionales.
- **Salida:** tabla y gráfico comparativos, ordenables (líneas 83–85), exportables a PDF (línea 83).

Nota sobre «desempeño nutricional»: la lectura adoptada mide el desempeño por el patrón de consumo (tasas de aceptación), que es lo que la propia consulta exige comparar. Bajo la decisión §7-C —valoración emitida por un nutricionista y anclada al menú recibido, pendiente de confirmación en #7—, el resultado de la valoración puede añadirse como columna complementaria sin coste de ventana temporal; la correlación valoración↔nivel calórico es el objeto de la consulta 6 (líneas 78–82).

## 3. Descomposición de la consulta

| Paso | Resultado parcial | Fuente |
|---|---|---|
| D1 | El menú parámetro, identificado unívocamente. | `Menu` (#7) |
| D2 | Los alimentos del menú con su nivel calórico. | `Menu`–`Alimento`, `Alimento.nivelCalorico` (#7, #5) |
| D3 | Los pacientes a los que el menú fue asignado. | Asignación `Menu`–`Paciente` (dueño por decidir, §7-E) |
| D4 | Los registros de consumo (paciente, alimento, estado) ante ese menú servido. | `Consumo` (#7) |
| D5 | Por alimento: servicios, aceptaciones, rechazos y tasa de aceptación. | derivación |
| D6 | Agregado por nivel calórico y comparación; tabla/gráfico/PDF. | derivación + presentación |

## 4. Datos y entidades necesarios (contra los submodelos de Ola 1)

| Dato necesario | Fuente candidata en el modelo | Submodelo | Sustento en el enunciado |
|---|---|---|---|
| Identidad del menú | Entidad `Menu` | #7 | líneas 31, 38 |
| Composición del menú en alimentos | Relación `Menu`–`Alimento` (vía `Plato` si aplica) | #7 (con #5) | líneas 31, 38 |
| Nivel calórico por alimento | `Alimento.nivelCalorico` | #5 | línea 31 |
| Desgloses: grupo nutricional, tipo de preparación | `Alimento`–`GrupoNutricional`, `Alimento`–`TipoPreparacion` | #5 | líneas 24, 30 |
| Pacientes con el menú asignado | Relación de asignación `Menu`–`Paciente` (§7-E: sin dueño explícito en la partición actual de submodelos) | #7 | líneas 33–35 |
| Consumo por (paciente, menú, alimento) con estado de aceptación | Entidad `Consumo` | #7 | línea 35 |
| Ancla temporal del consumo | Sin atributo propio (§7-D): derivable de `Menu.fechaCreación` (línea 69) | #7 | líneas 35, 69 |
| Identidad del paciente (para el reporte) | Entidad `Paciente` | #6 | línea 49 |

## 5. Trayectoria de navegación por el modelo

```
Menu --(compuesto por)--> Alimento [nivelCalorico, GrupoNutricional, TipoPreparacion]

Menu --(asignado a)--> Paciente --(registra)--> Consumo --(sobre)--> Alimento
```

`Consumo` es el punto de unión de ambas ramas: un registro identifica (paciente, menú, alimento) y su estado expresa la aceptación. La tasa de aceptación por nivel calórico se obtiene agregando sobre esa unión.

## 6. Derivaciones y cálculos (no almacenados)

- `servicios(alimento, menú)` = nº de registros de consumo del alimento en ese menú.
- `tasaAceptación(alimento, menú) = aceptados ÷ servicios`.
- `tasaAceptación(nivel, menú) = Σ aceptados(alimentos del nivel) ÷ Σ servicios(alimentos del nivel)`.
- Comparación de tasas entre niveles y alimentos (presentación en tabla/gráfico).
- Todas las magnitudes son derivables; el modelo solo debe garantizar la trayectoria de §5.

## 7. Ambigüedades detectadas y decisiones recomendadas

| # | Ambigüedad | Lecturas posibles | Recomendación | Issue destino |
|---|---|---|---|---|
| A | Semántica del registro de consumo | (a) Estado por ítem (servido/aceptado/rechazado). (b) Cantidad consumida por ítem. (c) Ambos. | **Resuelta: (a).** Estado por (paciente, menú, alimento), coherente con la decisión de la consulta 2 (§7-D): no ponderar cantidades. El conjunto exacto de estados lo fija el submodelo #7; bajo la lectura mínima, la existencia del registro ya expresa «servido» y el estado discrimina aceptado/rechazado. La tasa de aceptación no exige cantidades. | #7 |
| B | Denominador de la tasa | (a) Servicios con consumo registrado. (b) Pacientes asignados. | **Resuelta: (a).** El enunciado ancla el registro al «menú servido» (línea 35): solo cuentan los consumos registrados; un paciente asignado sin registro queda fuera del denominador. | — |
| C | ¿«Desempeño nutricional» incluye resultados de valoraciones? | (a) Solo tasas de aceptación. (b) Además, resultados de `Valoracion`. | **Resuelta: (a) como contenido exigido** — la consulta manda «comparando las tasas de aceptación». La valoración nutricional —emitida por un nutricionista y, bajo la interpretación recomendada, anclada al menú recibido— queda como columna complementaria (enriquecimiento). Precisión textual: el enunciado solo ancla la valoración a la «dieta recibida» (línea 50); el anclaje al menú es decisión estructural de #7, y es la que además vuelve respondible la parte A de la consulta 6 sin ventanas temporales (acoplada a §7-D). Aunque cada valoración es un hecho fijo, «las valoraciones más recientes» (línea 58) exigen que la serie sea ordenable temporalmente — derivable de la fecha de creación del menú (línea 69). | #7, #21 |
| D | ¿Puede repetirse el mismo menú en fechas distintas? | (a) El menú se asigna/sirve una única vez. (b) Puede servirse en varias fechas. | **Resuelta: (a).** El enunciado no menciona fechas de servicio ni de consumo; «impedir la asignación doble de un menú a los pacientes» (líneas 33–34) se lee en su forma más simple como unicidad del par (menú, paciente). Consecuencias: (i) la no-duplicación es una restricción de unicidad para #11; (ii) no se requieren fechas en la asignación ni en el consumo — el ancla temporal del sistema es la fecha de creación del menú (línea 69); (iii) la repetición de comida en el tiempo se modela con nuevos menús, lo que sostiene además la serie «valoraciones más recientes» (línea 58). Acoplada a §7-C: si #7 diese vuelta esta decisión, el anclaje de la valoración al menú se rompe. | #11 |
| E | Dueño de la asignación `Menu`–`Paciente` en la partición de submodelos | Los submodelos #5/#6/#7 no la listan explícitamente; emerge de las líneas 33–35 (menús asignados, sin asignación doble). | **No se resuelve aquí**: la adjudicación la deciden los dueños de los submodelos. Innegociable para esta consulta: la relación debe existir (requisito de las consultas 4 y 6) y, con §7-D resuelta, su no-duplicación queda como restricción de unicidad (→ #11). | #6, #7, #11 |

## 8. Criterio de validación contra el modelo (insumo para #10)

- [ ] Desde `Menu` se llega a sus alimentos (composición) y a los pacientes asignados (asignación).
- [ ] `Consumo` identifica unívocamente (paciente, menú, alimento) y expresa el estado de aceptación.
- [ ] La asignación `Menu`–`Paciente` existe y es única (asignación doble impedida, líneas 33–34; → #11).
- [ ] `Alimento.nivelCalorico` es accesible desde la composición del menú.
- [ ] Las tasas son calculables sin datos derivados almacenados.

## 9. Notas para el conjunto de datos de prueba

- Un menú servido a suficientes pacientes (≥10) para que las tasas tengan sentido estadístico.
- Casos de cobertura: alimento con 100 % de aceptación, alimento con 100 % de rechazo y tasas intermedias en cada nivel calórico.
- Un paciente asignado sin registro de consumo (evidencia del denominador, §7-B), y dos pacientes con estados distintos ante el mismo alimento del mismo menú (evidencia de los estados por ítem, §7-A).
