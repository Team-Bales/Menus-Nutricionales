<!--
Formalización de la consulta informacional 6 — sub-issue #21 del issue #4.
Es la consulta más exigente del enunciado: tres demandas analíticas
encadenadas que recorren prácticamente todo el modelo.
Insumos que alimenta: §3 del informe (docs/informe.md) y la validación del
modelo contra las 6 consultas (issue #10).
La numeración de líneas citada es la del texto plano del enunciado
(pdftotext), la misma empleada en los issues del repositorio.
Los nombres de entidades son los candidatos de los submodelos de Ola 1
(issues #5, #6, #7); quedan sujetos al glosario (#1) y a las convenciones
de modelado (#2).
-->

# Consulta 6. Correlación nivel calórico / resultado nutricional

| | |
|---|---|
| **Issue** | #21 (sub-issue de #4) |
| **Línea(s) del enunciado a la que responde** | 78–82 |
| **Milestone** | Semana 5 — Diseño de BD |
| **Submodelos de Ola 1 involucrados** | #5 (Alimento, Nutricionista como creador) · #6 (Dieta, Paciente, Sala) · #7 (Menu, Consumo, Valoracion, Revalorizacion) |
| **Insumo para** | §3 del informe (`docs/informe.md`) · Validación del modelo (#10) |

## 1. Enunciado original

> «Determinar, para cada dieta, la correlación entre el nivel calórico de los alimentos y el resultado nutricional promedio de los pacientes. La consulta debe identificar los 10 alimentos con la tasa de rechazo más alta, el nutricionista que los creó y la dieta a la que pertenecen. Además, debe comparar el desempeño nutricional de los pacientes que solicitaron una revalorización con el promedio general de valoraciones de sus respectivas salas para esas mismas dietas.» (líneas 78–82)

Marco general: resultados «en tablas y gráficos» (líneas 65–67), exportables a PDF (línea 83) y con ordenación arbitraria de columnas (líneas 83–85).

## 2. Reformulación enriquecida

### Parte A — Correlación nivel calórico ↔ resultado nutricional, por dieta

Para cada dieta, poner en relación el **nivel calórico de los alimentos efectivamente servidos** a los pacientes (a través de los menús de la dieta) con el **resultado nutricional** de esos pacientes. Exige unir dos ramas del modelo:

- rama alimentaria: dieta → menús → composición → `Alimento.nivelCalorico`;
- rama clínica: dieta → pacientes inscritos → valoraciones con resultado;

unidas por la **asignación de menú a paciente** y por el **anclaje de la valoración al menú recibido** (interpretación adoptada en la consulta 4, §7-C, pendiente de confirmación en #7): no se requieren ventanas temporales (§7-F).

Presentación recomendada: resultado nutricional promedio segmentado por nivel calórico (tabla/gráfico comparativo por dieta). El coeficiente de correlación entre dos variables ordinales —el nivel calórico (bajo < medio < alto) y el resultado con su mapeo numérico (§7-G)— es calculable (p. ej., Spearman, pensado justamente para datos ordinales), pero con tres niveles calóricos la comparación de promedios por nivel es la lectura más informativa. El modelo debe soportar ambas; el cálculo es derivado.

### Parte B — Top-10 alimentos con mayor tasa de rechazo, con creador y dieta

Sobre los registros de consumo del sistema, calcular la **tasa de rechazo** de cada alimento (rechazos ÷ servicios) y seleccionar los **10 alimentos con mayor tasa**. Para cada uno reportar: la identidad del alimento, **el nutricionista que lo creó** (líneas 29–31: cada nutricionista ingresa alimentos y clasifica «aquellos alimentos descritos por él»; línea 38 confirma que quien agrega alimentos no es necesariamente quien crea el menú) y **la dieta a la que pertenece** (§7-B: los alimentos no se adscriben directamente a dietas; se usan en menús de dietas, línea 24).

### Parte C — Revalorizados frente al promedio de su sala, por dieta

Para cada paciente que **solicitó una revalorización nutricional** (líneas 35–36), comparar su **desempeño nutricional** — el resultado de la revalorización, que el enunciado exige registrar (líneas 36–37) — con el **promedio general de las valoraciones de su sala**: los demás pacientes de la misma **sala** (línea 49) inscritos en la **misma dieta** («sus respectivas salas para esas mismas dietas», líneas 81–82). Requiere: `Revalorizacion` (solicitud, nutricionista ejecutante, resultado), `Paciente`–`Sala`, `Paciente`–`Dieta` (el paciente puede estar inscrito en varias, línea 49), `Valoracion` por (paciente, dieta) (línea 50) y el vínculo `Revalorizacion`↔`Dieta` (§7-E).

### Elementos comunes

- **Parámetros de entrada:** A no recibe parámetro (barrido por dieta, acotado a pacientes con menús asignados y valoración — §7-A); B sin parámetro: top-10 **global** del sistema (§7-C); C es un barrido por paciente revalorizado.
- **Salidas:** tablas y gráficos por dieta/alimento/sala, ordenables (líneas 83–85), exportables a PDF (línea 83).

## 3. Descomposición de la consulta

**Parte A**

| Paso | Resultado parcial | Fuente |
|---|---|---|
| A1 | Por dieta: menús y composición, con nivel calórico por alimento. | #7, #5 |
| A2 | Pacientes inscritos en la dieta con menús asignados/servidos. | #6, #7 |
| A3 | Valoraciones de esos pacientes para esa dieta (resultado, fecha). | #7 |
| A4 | Unión de ambas ramas por paciente, vía el anclaje de la valoración al menú recibido (sin ventana temporal, §7-F). | `Valoracion` → `Menu` (#7) |
| A5 | Resultado promedio por nivel calórico; correlación opcional. | derivación |

**Parte B**

| Paso | Resultado parcial | Fuente |
|---|---|---|
| B1 | Consumos agrupados por alimento (y dieta de contexto vía menú). | #7 |
| B2 | `tasaRechazo(alimento[, dieta]) = rechazos ÷ servicios`. | derivación |
| B3 | Top-10 por tasa, con autor del alimento y dieta(s) de contexto. | #5, #7 |

**Parte C**

| Paso | Resultado parcial | Fuente |
|---|---|---|
| C1 | Pacientes con solicitud de revalorización y su dieta de referencia. | #7 |
| C2 | Resultado de la revalorización de cada paciente (desempeño del revalorizado). | #7 |
| C3 | Promedio de valoraciones por (sala, dieta): pacientes de la sala inscritos en esa dieta. | #6, #7 |
| C4 | Comparación de (C2) frente a (C3), por paciente y dieta. | derivación |

## 4. Datos y entidades necesarios (contra los submodelos de Ola 1)

| Dato necesario | Fuente candidata en el modelo | Submodelo | Sustento en el enunciado |
|---|---|---|---|
| **A.** Dietas (barrido) | `Dieta` | #6 | líneas 42–43 |
| Menús de cada dieta y su composición | `Menu`–`Dieta`, `Menu`–`Alimento` | #7 | líneas 31, 38 |
| Nivel calórico de los alimentos servidos | `Alimento.nivelCalorico` | #5 | línea 31 |
| Asignación menú–paciente | Relación `Menu`–`Paciente` | #7 | líneas 33–35 |
| Pacientes inscritos por dieta | `Paciente`–`Dieta` | #6 | línea 49 |
| Resultado de las valoraciones por (paciente, menú) — la dieta se deriva del menú; orden temporal derivable de `Menu.fechaCreación` (línea 69) | `Valoracion` | #7 | líneas 35, 50, 69 |
| **B.** Consumos con estado por (paciente, menú, alimento) | `Consumo` | #7 | línea 35 |
| Autor del alimento (creador) | Relación `Alimento`–`Nutricionista` | #5 | líneas 29–31, 38 |
| Dieta de contexto del rechazo | `Menu`–`Dieta` (el rechazo ocurre ante un menú de una dieta) | #7 | líneas 24, 31 |
| **C.** Solicitud de revalorización (paciente, nutricionista ejecutante) | `Revalorizacion` | #7 | líneas 35–36 |
| Resultado de la revalorización | Nueva `Valoracion` vinculada a la valoración revalorizada (§7-D) | #7 | líneas 36–37 |
| Sala del paciente | `Paciente`–`Sala` | #6 | línea 49 |
| Promedio de valoraciones por (sala, dieta) | Agregación sobre `Valoracion` vía `Paciente`–`Sala` y `Paciente`–`Dieta` | #6, #7 | líneas 49–50 |
| Vínculo revalorización–dieta | Transitivo: `Revalorizacion` → `Valoracion` → `Menu` → `Dieta` (§7-E) | #7 | línea 82 |

## 5. Trayectorias de navegación por el modelo

```
A: Dieta ──> Menu ──> Alimento [nivelCalorico]
   Paciente ──(valoración sobre el menú recibido)──> Valoracion ──> Menu ──> Dieta
   unión de ambas ramas: el anclaje de la valoración al menú
   (sin fechas propias; el orden temporal se deriva de Menu.fechaCreación)

B: Consumo ──> Alimento ──> Nutricionista (creador)
   Consumo ──> Menu ──> Dieta

C: Revalorizacion ──> Valoracion (objetivo) ──> Menu ──> Dieta
   Valoracion ──> Paciente ──> Sala ──> promedio por (Sala, Dieta)
```

## 6. Derivaciones y cálculos (no almacenados)

- **Parte A:** promedio del resultado nutricional por nivel calórico, por dieta; coeficiente de correlación por rangos (opcional).
- **Parte B:** `tasaRechazo` por alimento (global y por dieta); top-10 (`ORDER BY tasa DESC LIMIT 10`).
- **Parte C:** promedio de valoraciones por (sala, dieta); diferencia o razón frente al resultado del revalorizado.
- Ninguna de estas magnitudes se almacena. La exigencia al modelo es la completitud de las tres trayectorias, no el almacenamiento de resultados.

## 7. Ambigüedades detectadas y decisiones recomendadas

| # | Ambigüedad | Lecturas posibles | Recomendación | Issue destino |
|---|---|---|---|---|
| A | Ámbito del «resultado nutricional promedio» | (a) Todos los pacientes inscritos en la dieta. (b) Solo los pacientes con menús asignados/servidos y valoración en el ámbito considerado. | **Resuelta por coherencia: (b).** Solo cuentan hechos registrados (consulta 4-§7-B: denominador = consumos registrados) y, bajo el anclaje de la valoración al menú (consulta 4-§7-C), solo quien recibió menús tiene valoraciones que correlacionar; meternos con pacientes inscritos sin menús ni valoración no aporta a la correlación. | — |
| B | «La dieta a la que pertenecen [los alimentos]» | (a) Dieta(s) de los menús donde el alimento fue servido y rechazado (línea 24: los alimentos se usan «para» dietas vía menús). (b) Relación directa `Alimento`–`Dieta`. | **Resuelta por coherencia: (a).** El enunciado no sustenta la adscripción directa (b), y la decisión de la consulta 4-§7-A —el consumo se registra por (paciente, menú, alimento)— encadena alimento → menú → dieta de forma determinista. Un alimento rechazado en menús de varias dietas aporta filas (alimento, dieta) con su tasa propia. | — |
| C | Alcance del top-10 | (a) Global del sistema. (b) Por cada dieta. | **Resuelta: top-10 global.** N = 10 lo fija el enunciado (no es parámetro de UX); el corte lo aplica el gestor (`LIMIT`, consulta 2-§7-C y su nota de realización). Los alimentos viajan con su dieta (§7-B), y el desglose por dieta queda como enriquecimiento opcional —la trayectoria ya lo soporta—. **Desempate obligatorio y determinista** (el enunciado pide exactamente 10; el corte no puede ser arbitrario): a igual tasa de rechazo, gana el mayor número de servicios (más evidencia estadística); a persistir el empate, criterio estable (identificador del alimento). | Producto |
| D | ¿El resultado de la revalorización es una `Valoracion`? | (a) Se registra como una nueva `Valoracion` del paciente. (b) Atributo resultado dentro de `Revalorizacion`. | **Resuelta: (a), confirmada en revisión.** Con la valoración anclada al menú (consulta 4-§7-C), la revalorización opera **sobre una valoración concreta** (`Revalorizacion` → `Valoracion`) y su resultado es una nueva `Valoracion` que hereda la cadena de anclajes (→ menú → dieta → paciente): la parte C compara entonces magnitudes del mismo tipo contra «el promedio general de valoraciones». En (b) habría dos magnitudes heterogéneas. | #7 |
| E | Vínculo `Revalorizacion`↔`Dieta` | (a) Directo: atributo/relación en `Revalorizacion`. (b) Transitivo: vía la `Valoracion` revalorizada (→ `Menu` → `Dieta`). | **Resuelta: (b), confirmada en revisión (vía D).** Si la revalorización opera sobre una valoración concreta, la dieta se deriva de la cadena de anclajes sin atributo adicional; «para esas mismas dietas» (líneas 81–82) queda determinado incluso con pacientes polidietéticos (línea 49), porque cada valoración referencia un menú de una dieta. | #7 |
| F | Relación temporal valoración ↔ menús consumidos | (a) Mínimo: fechas en `Valoracion` y `Consumo`. (b) Robusto: anclar la valoración al menú evaluado. | **Resuelta por estructura — se disuelve la ventana.** Con las decisiones de la consulta 4 (§7-C: valoración anclada al menú; §7-D: asignación única (menú, paciente), sin fechas de servicio/consumo) no hace falta ninguna fecha en valoración ni consumo: la pertenencia de una valoración a unos alimentos (y niveles calóricos) queda determinada por estructura. La serie «las valoraciones más recientes» (línea 58) se ordena por `Menu.fechaCreación` (línea 69). | — (cubierto por la consulta 4-§7-C, que #7 debe confirmar) |
| G | Escala del «resultado nutricional» | (a) Numérica. (b) Calificativa. | **Recomendada: ordinal de 5 categorías con mapeo numérico explícito** (Muy mala = 1, Mala = 2, Normal = 3, Buena = 4, Excelente = 5). Evidencia del enunciado: no define la escala, pero exige «promedios» dos veces (líneas 78–79 y 81–82) — el resultado debe admitir agregación: las categorías solas no la admiten y el numérico puro carece de sustento semántico en el dominio. Con el mapeo documentado, los promedios de esta consulta se derivan de los códigos (nada adicional almacenado); la correlación de la parte A queda bien servida por Spearman (nivel calórico y resultado, ambos ordinales); y el reporte gana riqueza (distribución por categorías además del promedio). La escala y su mapeo se especifican como nomenclador/restricción → #7 y #11. Decisión final: #7. | #7, #11 |

## 8. Criterio de validación contra el modelo (insumo para #10)

**Parte A**

- [ ] Dieta → menús → composición → alimento con nivel calórico: navegable.
- [ ] `Valoracion` anclada al menú recibido (consulta 4-§7-C, a confirmar en #7): desde la valoración se llega a menú, alimentos (con nivel calórico) y dieta.
- [ ] Dado un paciente, se recuperan sus valoraciones con su menú/dieta asociados; el orden temporal es derivable de `Menu.fechaCreación` (línea 69).

**Parte B**

- [ ] `Consumo` identifica (paciente, menú, alimento) y su estado (consulta 4-§7-A/D: sin fechas propias).
- [ ] `Alimento` tiene autor (nutricionista creador).
- [ ] Desde el consumo se llega a la dieta del menú servido.

**Parte C**

- [ ] `Revalorizacion` vincula paciente, nutricionista ejecutante y la valoración objetivo (→ menú → dieta — §7-D/E).
- [ ] `Paciente`–`Sala` y la navegación paciente → menú → dieta permiten agrupar valoraciones por (sala, dieta).
- [ ] El resultado de la revalorización (nueva `Valoracion`, §7-D) comparte escala con el resto de valoraciones (§7-G).

**Transversal**

- [ ] Ningún dato derivado obligatorio almacenado: promedios, tasas y correlaciones son calculables en consulta.

## 9. Notas para el conjunto de datos de prueba

- **Parte A:** varias dietas con menús de distinto perfil calórico; pacientes con valoraciones ancladas a sus menús; y al menos dos pacientes de la misma dieta con valoraciones sobre menús de distinto nivel calórico predominante (para que el contraste por nivel tenga datos).
- **Parte B:** más de 10 alimentos con rechazos (para que el top-10 discrimine), con autores distintos y el mismo alimento apareciendo en varias dietas (evidencia de §7-B).
- **Parte C:** al menos 2 salas; pacientes revalorizados y no revalorizados por sala; un paciente inscrito en varias dietas con una revalorización sobre una valoración de una dieta concreta (evidencia de §7-E); resultado aplicando la escala y el mapeo de §7-G).
