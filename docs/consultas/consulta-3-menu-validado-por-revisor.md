<!--
Formalización de la consulta informacional 3 — sub-issue #18 del issue #4.
Es la consulta orientada a la trazabilidad, supervisión clínica y auditoría
del flujo de control de calidad de menús por parte del personal facultativo.
Insumos que alimenta: §3 del informe (docs/informe.md) y la validación del
modelo contra las 6 consultas (issue #10).
La numeración de líneas citada es la del texto plano del enunciado
(pdftotext), la misma empleada en los issues del repositorio.
Los nombres de entidades son los candidatos de los submodelos de Ola 1
(issues #5, #6, #7); quedan sujetos al glosario (#1) y a las convenciones
de modelado (#2).
-->

# Consulta 3. Menús validados por revisor

| | |
|---|---|
| **Issue** | #18 (sub-issue de #4) |
| **Línea(s) del enunciado a la que responde** | 72–73 (con apoyo de 40–41, 43–44) |
| **Milestone** | Semana 5 — Diseño de BD |
| **Submodelos de Ola 1 involucrados** | #5 (Nutricionista revisor / Jefe de nutrición) · #6 (Dieta) · #7 (Menu, ValidacionMenu / Revision) |
| **Insumo para** | §3 del informe (`docs/informe.md`) · Validación del modelo (#10) |

## 1. Enunciado original

> «3. Listar los menús que fueron validados por un revisor determinado, indicando la fecha de validación y las observaciones hechas durante el proceso.» (líneas 72–73)

Marco general: resultados «en tablas y gráficos» (líneas 65–67), exportables a PDF (línea 83) y con ordenación arbitraria de columnas (líneas 83–85).

## 2. Reformulación enriquecida

La consulta demanda una herramienta de auditoría médica y control de calidad que permita rastrear todas las intervenciones de revisión y dictamen realizadas por un revisor específico (identificado en el enunciado funcionalmente como el «jefe de nutrición» o un nutricionista facultado para validar menús, según líneas 40–41 y 43–44).

El sistema debe recuperar el historial de revisiones ejecutadas por dicho profesional, garantizando la visibilidad de:
- **Identificación del menú:** código o ID del menú evaluado, junto con la dieta a la que pertenece y el nutricionista autor que lo generó/propuso originalmente (contexto clínico necesario).
- **Identificación del revisor:** confirmación del profesional actuante (nombre, especialidad).
- **Fecha de validación:** estampa cronológica exacta (`TIMESTAMP` / `DATETIME`) en la que se efectuó y asentó el acto de revisión.
- **Observaciones del proceso:** notas cualitativas, retroalimentación clínica o motivos registrados por el revisor durante la inspección (aspecto crítico cuando el revisor solicita ajustes o indica una nueva confección, líneas 40–41).
- **Estado/Resultado del dictamen:** aunque la consulta enfatiza «menús validados», el flujo de negocio del enunciado («el jefe de nutrición es quien revisa y aprueba el menú generado, aunque también puede indicar la confección de otro bajo sus criterios», líneas 40–41) exige registrar si el resultado fue una **aprobación** o un **rechazo con señalamientos**.

### Elementos comunes
- **Parámetros de entrada:** Identificador único del revisor (`revisor_id` / `nutricionista_id`).
- **Salidas:** Tabla con el listado cronológico de revisiones: identificador del menú, dieta asociada, creador del menú, fecha/hora de validación, dictamen (aprobado/rechazado) y campo textual de observaciones. Opciones de gráficos (p. ej., volumen de menús revisados por mes o proporción aprobados vs. observados), ordenación multi-columna y exportación a PDF.

## 3. Descomposición de la consulta

| Paso | Resultado parcial | Fuente |
|---|---|---|
| 1 | Filtrar y verificar la existencia del revisor (`Nutricionista`) en el sistema. | #5 |
| 2 | Obtener los registros de revisión/validación donde dicho profesional actuó como revisor. | #7 |
| 3 | Recuperar para cada evento los atributos propios del acto: `fechaValidacion` y `observaciones` (así como el estado/dictamen). | #7 |
| 4 | Navegar hacia el `Menu` vinculado para obtener su identificador, fecha de confección y autoría. | #7 |
| 5 | Navegar hacia la `Dieta` del menú para dar contexto clínico al reporte. | #6, #7 |
| 6 | Ordenar cronológicamente (o según la columna seleccionada por el usuario) y formatear para tabla / PDF. | Presentación / UX |

## 4. Datos y entidades necesarios (contra los submodelos de Ola 1)

| Dato necesario | Fuente candidata en el modelo | Submodelo | Sustento en el enunciado |
|---|---|---|---|
| Revisor objetivo (filtro) | `Nutricionista` (`id`, `nombre`, `especialidad`) | #5 | líneas 40, 43, 72 |
| Autorización para validar la dieta | Relación `Nutricionista`–`Dieta` (rol validar) | #5, #6 | líneas 43–44 |
| Menú evaluado | `Menu` (`id`, `nombre`/código, `dieta_id`, `creador_id`) | #7 | líneas 38, 72 |
| Fecha de la validación | `Menu.fechaValidacion` o `ValidacionMenu.fecha` | #7 | línea 72 |
| Observaciones del revisor | `Menu.observaciones` o `ValidacionMenu.observaciones` | #7 | líneas 40–41, 73 |
| Resultado del dictamen | `Menu.estado` o `ValidacionMenu.estado` (Aprobado, Rechazado/Observado) | #7 | líneas 40–41 |
| Dieta de contexto | `Dieta` (`id`, `nombre`) | #6 | líneas 42, 68 |

## 5. Trayectorias de navegación por el modelo

```
Nutricionista (Revisor: id, nombre)
   │
   └──< ValidacionMenu (o relación 1:N / 1:1 con Menu)
          ├── fechaValidacion (TIMESTAMP)
          ├── observaciones (TEXT)
          ├── estadoDictamen (Aprobado / Observado)
          │
          └──> Menu (id, fechaCreacion)
                 │
                 ├──> Nutricionista (Creador) ──> nombre
                 │
                 └──> Dieta ──> nombre
```

## 6. Derivaciones y cálculos (no almacenados)

- La consulta es de **recuperación histórica y auditoría directa**.
- Métricas derivadas opcionales para la capa visual/analítica:
  - Tasa de aprobación del revisor: $\frac{\text{Menús Aprobados}}{\text{Total de Menús Revisados}} \times 100$.
  - Tiempo de respuesta de revisión: diferencia entre `Menu.fechaCreacion` y `fechaValidacion`.
- Ninguno de estos valores se almacena; se calculan en la consulta o capa de servicios.

## 7. Ambigüedades detectadas y decisiones recomendadas

| # | Ambigüedad | Lecturas posibles | Recomendación | Issue destino |
|---|---|---|---|---|
| A | Cardinalidad de la validación: ¿Atributos en `Menu` o entidad propia `ValidacionMenu`? | (a) Columnas directas en `Menu` (`revisor_id`, `fechaValidacion`, `observaciones`, `estado`).<br>(b) Entidad separada `ValidacionMenu` / `RevisionMenu` (historial 1:N). | **Resuelta: (b) Entidad histórica recomendada; (a) admisible si solo se conserva la última revisión.** Las líneas 40–41 indican que el revisor puede rechazar/«indicar la confección de otro bajo sus criterios», lo que implica que un menú puede pasar por más de una iteración o revisión antes de ser servido. Modelar una entidad `ValidacionMenu` garantiza histórico completo sin pérdida de datos. Decisión estructural: #7. | #7 |
| B | ¿Quién puede ser revisor? | (a) Cualquier nutricionista autorizado para la dieta.<br>(b) Exclusivamente un usuario con rol «jefe de nutrición». | **Resuelta: (a) respaldada por regla funcional.** La línea 40 menciona que «el jefe de nutrición es quien revisa y aprueba», pero las líneas 43–44 establecen que de cada nutricionista se almacenan «las dietas para las cuales está autorizado a generar **o validar** menús». Por tanto, todo revisor es un nutricionista con habilitación explícita para validar esa dieta en el sistema. | #5, #6 |
| C | Alcance del término «validados» | (a) Solo los menús que resultaron **aprobados**.<br>(b) Todos los menús que fueron objeto de un proceso de validación (aprobados o rechazados/observados). | **Resuelta: (b).** En ingeniería de software y auditoría clínica, las «observaciones hechas durante el proceso» cobran valor precisamente ante rechazos o ajustes requeridos. La consulta debe listar todos los eventos de validación completados por el revisor, reflejando el dictamen final. | Producto / UX |

## 8. Criterio de validación contra el modelo (insumo para #10)

- [ ] Existe un vínculo directo o a través de una entidad de auditoría entre `Nutricionista` (en calidad de revisor) y `Menu`.
- [ ] El modelo permite almacenar la marca temporal exacta de la validación (`fechaValidacion`).
- [ ] Existe un campo textual para registrar las `observaciones` o notas clínicas de la revisión.
- [ ] El modelo contempla el resultado del proceso de validación (aprobación, rechazo u observaciones).
- [ ] A partir del registro de validación es posible navegar hacia el `Menu`, su `Dieta` y el `Nutricionista` que lo confeccionó originalmente.
- [ ] La relación `Nutricionista`–`Dieta` soporta la restricción de autorización para validar menús de esa dieta específica.

## 9. Notas para el conjunto de datos de prueba

- Registrar al menos dos nutricionistas con permisos de validación sobre distintas dietas.
- Incluir revisiones con dictamen de **aprobación** (con observaciones breves de conformidad).
- Incluir revisiones con dictamen de **observación/rechazo** (con observaciones detalladas exigiendo cambios en los criterios de preparación o equilibrio).
- Casos de menús evaluados en diferentes fechas para verificar el funcionamiento del ordenamiento cronológico y filtros temporales en tablas y gráficos.
