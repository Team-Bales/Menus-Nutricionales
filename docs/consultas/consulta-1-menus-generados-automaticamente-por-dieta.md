<!--
Formalización de la consulta informacional 1 — sub-issue #16 del issue #4.
Es la consulta base del flujo de generación: recupera la traza histórica y la
parametrización de los menús generados algorítmicamente para una dieta dada.
Insumos que alimenta: §3 del informe (docs/informe.md) y la validación del
modelo contra las 6 consultas (issue #10).
La numeración de líneas citada es la del texto plano del enunciado
(pdftotext), la misma empleada en los issues del repositorio.
Los nombres de entidades son los candidatos de los submodelos de Ola 1
(issues #5, #6, #7); quedan sujetos al glosario (#1) y a las convenciones
de modelado (#2).
-->

# Consulta 1. Menús generados automáticamente por dieta

| | |
|---|---|
| **Issue** | #16 (sub-issue de #4) |
| **Línea(s) del enunciado a la que responde** | 68–69 (con apoyo de 31–32, 38–41, 42–44) |
| **Milestone** | Semana 5 — Diseño de BD |
| **Submodelos de Ola 1 involucrados** | #5 (Nutricionista creador) · #6 (Dieta) · #7 (Menu, ParametrizacionMenu) |
| **Insumo para** | §3 del informe (`docs/informe.md`) · Validación del modelo (#10) |

## 1. Enunciado original

> «1. Obtener el listado de menús generados automáticamente para una dieta específica, indicando el nombre del creador, la fecha de creación y los parámetros utilizados.» (líneas 68–69)

Marco general: resultados «en tablas y gráficos» (líneas 65–67), exportables a PDF (línea 83) y con ordenación arbitraria de columnas (líneas 83–85).

## 2. Reformulación enriquecida

La consulta demanda un reporte histórico y auditable del proceso de confección algorítmica de menús para una dieta concreta seleccionada por el usuario. 

Para satisfacer esta necesidad, el sistema debe filtrar el universo de menús bajo dos condiciones deterministas:
1. Pertenencia a la **dieta específica** pasada como parámetro.
2. Origen o modalidad de generación **automática** (el enunciado distingue la generación automática a partir de alimentos preseleccionados —línea 31— de la confección manual o indicación directa bajo criterios del jefe de nutrición —líneas 40–41—).

Por cada menú que cumpla el criterio, deben proyectarse sus metadatos de autoría y configuración:
- **Identidad del menú:** identificador único y/o código descriptivo.
- **Creador:** nombre y apellidos del nutricionista específico que ejecutó la parametrización y generación (líneas 38–40: «No siempre aquellos nutricionistas que agregan alimentos son los que crean el menú, esta labor es desempeñada por un nutricionista específico...»). Debe verificarse además que dicho nutricionista esté autorizado para generar menús en esa dieta (líneas 43–44).
- **Fecha de creación:** marca temporal precisa (`TIMESTAMP` / `DATETIME`) del instante en que se ejecutó la generación y registro del menú en el sistema.
- **Parámetros utilizados:** conjunto exacto de criterios con los que el algoritmo corrió. El enunciado los tipifica explícitamente en las líneas 38–40: «la proporción de alimentos por tipo de preparación, la cobertura de los grupos nutricionales y la cantidad total de alimentos, cuya parametrización es almacenada junto al menú generado».

### Elementos comunes
- **Parámetros de entrada:** Identificador único de la `Dieta` (`dieta_id`).
- **Salidas:** Tabla estructurada con las columnas: identificador de menú, fecha/hora de creación, nombre del nutricionista creador y detalle de los parámetros de generación aplicados. Gráficos opcionales (p. ej., distribución temporal de menús generados por nutricionista para esa dieta). Capacidad de ordenación multi-columna y exportación a PDF.

## 3. Descomposición de la consulta

| Paso | Resultado parcial | Fuente |
|---|---|---|
| 1 | Filtrar y validar la existencia de la `Dieta` indicada por el usuario. | #6 |
| 2 | Seleccionar los registros de `Menu` asociados a dicha dieta cuyo indicador o modalidad de generación sea automática (`esAutomatico = TRUE`). | #7 |
| 3 | Resolver la relación `Menu` → `Nutricionista` creador para extraer el nombre del profesional responsable. | #5, #7 |
| 4 | Proyectar el atributo temporal `fechaCreacion` del menú. | #7 |
| 5 | Recuperar el detalle de la parametrización asociada a cada menú (proporción por preparación, cobertura nutricional y total de alimentos). | #7 |
| 6 | Ordenar y estructurar el conjunto de resultados según la interacción del usuario final. | Presentación / UX |

## 4. Datos y entidades necesarios (contra los submodelos de Ola 1)

| Dato necesario | Fuente candidata en el modelo | Submodelo | Sustento en el enunciado |
|---|---|---|---|
| Dieta objetivo (filtro) | `Dieta` (`id`, `nombre`) | #6 | líneas 42, 68 |
| Menús de la dieta | `Menu` (`id`, `dieta_id`) | #7 | líneas 31, 68 |
| Indicador de generación automática | `Menu.esAutomatico` / `Menu.origen` | #7 | líneas 31, 40–41, 68 |
| Fecha de creación | `Menu.fechaCreacion` | #7 | línea 69 |
| Identidad del creador | `Menu.nutricionista_creador_id` → `Nutricionista.nombre` | #5, #7 | líneas 38–40, 43, 69 |
| Parámetros de generación | Entidad o estructura adosada `ParametrizacionMenu` (proporciones, grupos cubiertos, total de alimentos) | #7 | líneas 38–40, 69 |
| Autorización del nutricionista para la dieta | Relación `Nutricionista`–`Dieta` (integridad/negocio) | #5, #6 | líneas 43–44 |

## 5. Trayectorias de navegación por el modelo

```
Dieta (id) 
   │
   └──< Menu (filtrar por dieta_id AND esAutomatico = TRUE)
         │
         ├──> Nutricionista (creador) ──> Nutricionista.nombre
         │
         ├──> Menu.fechaCreacion
         │
         └──> ParametrizacionMenu (o atributos propios de parametrización)
                ├── proporcionPreparacion
                ├── coberturaGruposNutricionales
                └── cantidadTotalAlimentos
```

## 6. Derivaciones y cálculos (no almacenados)

- La consulta es fundamentalmente de **recuperación estructurada y proyección histórica**.
- No requiere agregaciones analíticas complejas obligatorias; las métricas estadísticas (p. ej., cantidad de menús generados por mes, promedio de alimentos por menú) son derivables de la proyección de los datos.
- Formateo legible de la estructura de parámetros para la vista/PDF (serialización de proporciones o descomposición columnar).

## 7. Ambigüedades detectadas y decisiones recomendadas

| # | Ambigüedad | Lecturas posibles | Recomendación | Issue destino |
|---|---|---|---|---|
| A | Distinción entre menús automáticos y manuales | (a) Todos los menús registrados nacen de un proceso automático y se asume implícito.<br>(b) Existen menús creados o ajustados manualmente (p. ej. por orden del jefe de nutrición, líneas 40–41), requiriendo un discriminador explícito. | **Resuelta: (b).** El enunciado recalca «menús generados automáticamente» (línea 68) y contrapone que el jefe de nutrición puede «indicar la confección de otro bajo sus criterios» (líneas 40–41). El submodelo #7 debe contemplar un atributo booleano (`esAutomatico`) o un discriminador de origen (`tipoGeneracion`: AUTOMATICA / MANUAL). | #7 |
| B | Modelado de los «parámetros utilizados» | (a) Atributos estructurados normalizados (entidad propia o columnas en `Menu`).<br>(b) Estructura semiestructurada (columna `JSON` / clave-valor).<br>(c) Cadena de texto libre. | **Resuelta: (a) o (b) tipada.** El enunciado detalla explícitamente los criterios mínimos en las líneas 38–40: «proporción de alimentos por tipo de preparación, la cobertura de los grupos nutricionales y la cantidad total de alimentos». Deben quedar explícitamente representados para auditoría; una entidad débil `ParametrizacionMenu` o columnas normalizadas garantizan consistencia relacional. Decisión final técnica: #7. | #7 |
| C | Creador vs. Aprobador/Revisor | ¿El creador del menú es el mismo que lo revisa? | **No.** El enunciado separa roles funcionales: el nutricionista específico genera el menú aplicando parámetros (líneas 38–40), mientras que el jefe de nutrición es quien lo revisa y aprueba (línea 40). La entidad `Menu` debe asociar dos claves foráneas distintas hacia `Nutricionista`: creador y revisor/aprobador. | #5, #7 |
| D | Alcance de la parametrización | ¿Los parámetros son plantillas reutilizables o instancias por menú? | **Resuelta:** El enunciado indica «cuya parametrización es almacenada junto al menú generado» (línea 40). Esto exige una relación 1:1 histórica (o embebida) inmutable para garantizar que cambios futuros en criterios no alteren la auditoría del menú ya generado. | #7 |

## 8. Criterio de validación contra el modelo (insumo para #10)

- [ ] La entidad `Dieta` permite acceder a sus menús vinculados de forma directa e indexada.
- [ ] La entidad `Menu` almacena la referencia clara a la `Dieta` a la que pertenece.
- [ ] La entidad `Menu` cuenta con un campo/discriminador que identifique inequívocamente su condición de «generado automáticamente».
- [ ] La entidad `Menu` registra la marca temporal `fechaCreacion`.
- [ ] La entidad `Menu` referencia mediante FK al `Nutricionista` en su rol específico de creador/generador, permitiendo recuperar su nombre.
- [ ] Existe la estructura relacional (columnas o entidad vinculada) para persistir fielmente los parámetros mínimos descritos en el enunciado: proporción por tipo de preparación, cobertura de grupos nutricionales y cantidad total de alimentos.
- [ ] El modelo garantiza que la parametrización de un menú sea inmutable una vez generado.

## 9. Notas para el conjunto de datos de prueba

- Registrar al menos una dieta con menús generados automáticamente y al menos un menú confeccionado manualmente (para verificar que el filtro de generación automática discrimine correctamente).
- Incluir múltiples menús automáticos generados por diferentes nutricionistas autorizados para una misma dieta en diferentes fechas.
- Parametrizaciones variadas (distintas combinaciones de proporciones de entrante/plato fuerte/postre/bebida y diferentes cantidades de alimentos totales) para constatar la correcta persistencia y visualización tabular y en PDF.
