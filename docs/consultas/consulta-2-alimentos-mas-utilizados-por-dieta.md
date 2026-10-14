<!--
Formalización de la consulta informacional 2 — sub-issue #17 del issue #4.
Insumos que alimenta: §3 del informe (docs/informe.md) y la validación del
modelo contra las 6 consultas (issue #10).
La numeración de líneas citada es la del texto plano del enunciado
(pdftotext), la misma empleada en los issues del repositorio.
Los nombres de entidades son los candidatos de los submodelos de Ola 1
(issues #5, #6, #7); quedan sujetos al glosario (#1) y a las convenciones
de modelado (#2).
-->

# Consulta 2. Alimentos más utilizados por dieta

| | |
|---|---|
| **Issue** | #17 (sub-issue de #4) |
| **Línea(s) del enunciado a la que responde** | 70–71 |
| **Milestone** | Semana 5 — Diseño de BD |
| **Submodelos de Ola 1 involucrados** | #5 (Alimento/Plato, GrupoNutricional) · #6 (Dieta) · #7 (Menu, Validación/Aprobación) |
| **Insumo para** | §3 del informe (`docs/informe.md`) · Validación del modelo (#10) |

## 1. Enunciado original

> «Obtener los alimentos más utilizados en los menús finales de una dieta, clasificados por nivel calórico y grupo nutricional.» (líneas 70–71)

Marco general de las funcionalidades: los resultados se proveen «en tablas y gráficos» (líneas 65–67), con «la posibilidad de exportar la información mostrada a ficheros con formato PDF» (línea 83) y «poder ordenar cada columna de los resultados» (líneas 83–85). El párrafo de reportes del enunciado ya anticipa esta demanda: «reportes detallados sobre … los alimentos más utilizados …» (líneas 61–63).

## 2. Reformulación enriquecida

Dada una **dieta específica** — parámetro de entrada, identificada por su identificador único o nombre (líneas 42–43) —, el sistema debe producir el listado de los **alimentos** que aparecen con mayor frecuencia en los **menús finales** de esa dieta, con el número de utilizaciones de cada uno y su clasificación por **nivel calórico** (bajo, medio o alto; línea 31) y **grupo nutricional** (nomenclador del sistema; líneas 24 y 30).

Desglose de la lectura:

1. **Universo: menús finales, no todos los menús.** La consulta 1 (líneas 68–69) pide todos los menús generados automáticamente; esta consulta se acota a los menús en estado *final*. Lectura recomendada: menús **aprobados** por el jefe de nutrición (líneas 40–41). Ver §7-A.
2. **Métrica de utilización: presencia en menús.** «Más utilizados» se mide como el número de menús finales de la dieta en cuya composición aparece el alimento. Es un conteo de presencia: no pondera cantidades ni pacientes (§7-D).
3. **Clasificación.** El nivel calórico y el grupo nutricional son clasificaciones del alimento, otorgadas por el nutricionista que lo describe (líneas 30–31).
4. **Salida.** Tabla (alimento, grupo nutricional, nivel calórico, utilizaciones) y gráfico comparativo. El parámetro **N** proviene de la UX, con valor por defecto 10 y rango válido [1, 20]; el corte top-N lo aplica el propio gestor (`LIMIT`; §7-C). Ordenable por cualquier columna (líneas 83–85); exportable a PDF (línea 83).

## 3. Descomposición de la consulta

| Paso | Resultado parcial | Fuente |
|---|---|---|
| D1 | La dieta parámetro, identificada unívocamente. | `Dieta` (#6) |
| D2 | El conjunto de menús **finales** de esa dieta. | `Menu`–`Dieta`, estado/aprobación (#7) |
| D3 | La composición de cada menú final, en alimentos. | `Menu`–`Alimento` (#7, con #5) |
| D4 | Utilizaciones por alimento (nº de menús finales que lo contienen). | derivación |
| D5 | Nivel calórico y grupo nutricional de cada alimento. | `Alimento` (#5) |
| D6 | Reporte: agrupado por (nivel calórico, grupo nutricional), ordenado, con top-N parametrizado desde la UX (§7-C); tabla/gráfico/PDF. | presentación |

## 4. Datos y entidades necesarios (contra los submodelos de Ola 1)

| Dato necesario | Fuente candidata en el modelo | Submodelo | Sustento en el enunciado |
|---|---|---|---|
| Identidad de la dieta (identificador único, nombre) | Entidad `Dieta` | #6 | líneas 42–43 |
| Menús de la dieta | Relación `Menu`–`Dieta` (cada menú se genera para una dieta) | #7 | líneas 31, 38 |
| Estado **final/aprobado** del menú | Relación `Menu`–`Validación/Aprobación`, o estado en `Menu` | #7 | líneas 40–41, 70–71 |
| Composición del menú en alimentos | Relación `Menu`–`Alimento` (detalle de menú; vía `Plato` si el menú integra platos; §7-B) | #7 (con #5) | líneas 31, 38–39 |
| Identidad del alimento | Entidad `Alimento` | #5 | líneas 24, 29 |
| Nivel calórico del alimento | Atributo `nivelCalorico` ∈ {bajo, medio, alto} | #5 | línea 31 |
| Grupo nutricional del alimento | Relación `Alimento`–`GrupoNutricional` | #5 | líneas 24, 30 |
| Fecha de creación del menú (columna de orden complementaria) | Atributo de `Menu` | #7 | línea 69 |

## 5. Trayectoria de navegación por el modelo

```
Dieta --(menús generados para)--> Menu [final/aprobado]
                                        |
                                  (compuesto por)
                                        v
       GrupoNutricional <--(clasificado en)-- Alimento --nivelCalorico--> {bajo, medio, alto}
```

La consulta es navegable si esa cadena se recorre sin requerir datos adicionales almacenados.

## 6. Derivaciones y cálculos (no almacenados)

- `utilizaciones(alimento, dieta) = |{m ∈ menús finales(dieta) : alimento ∈ composición(m)}|`
- Agrupación por (nivel calórico, grupo nutricional) y ordenación por utilizaciones (`GROUP BY` / `ORDER BY`); con top-N, el corte lo aplica el gestor: `ORDER BY utilizaciones DESC LIMIT :n` (§7-C).
- Ninguna de estas magnitudes se almacena: son derivables en consulta. La única exigencia al modelo es la completitud de la trayectoria de §5.

## 7. Ambigüedades detectadas y decisiones recomendadas

| # | Ambigüedad | Lecturas posibles | Recomendación | Issue destino |
|---|---|---|---|---|
| A | «Menús finales» | (a) Menús aprobados por el jefe de nutrición. (b) Menús vigentes más recientes de la dieta. | (a). El enunciado distingue «menús generados automáticamente» (consulta 1, líneas 68–69) de «menús finales», y el flujo descrito culmina en la aprobación (líneas 40–41). El modelo debe permitir filtrar los menús aprobados. | #7 |
| B | Composición: ¿el menú contiene solo alimentos o también platos? | (a) Solo alimentos (línea 31: «a partir de los alimentos seleccionados»). (b) También platos compuestos (líneas 24, 54), lo que exige atravesar la composición del plato hasta los alimentos. | La consulta debe resolverse sobre **alimentos** en cualquier caso: si el menú integra platos, la trayectoria debe descomponerlos (composición recursiva). La decisión Alimento/Plato está abierta en #1 y #5. | #1, #5, #7 |
| C | ¿Cuántos son «los más utilizados»? | (a) Listado completo ordenado. (b) Top-N fijo. (c) Top-N con N como parámetro de la propia consulta. | **Resuelta: (c).** N proviene de la UX, con **valor por defecto 10** y **rango válido [1, 20]** —más de 20 no aporta a un reporte de más utilizados; una dieta con menos alimentos devuelve el ranking completo—. N viaja como parámetro de invocación —un placeholder, no una constante— y el corte lo aplica el gestor: `ORDER BY utilizaciones DESC LIMIT :n` (en EF Core, `Take(n)` se traduce a `LIMIT @p`). El orden de extracción (utilizaciones) es independiente del orden de presentación por columnas exigido en líneas 83–85. Para los empates en el corte, ver la nota de realización a continuación. Sin impacto en el modelo relacional. | Producto |
| D | ¿El conteo pondera cantidades? | (a) Presencia en menús (conteo simple). (b) Suma de cantidades por alimento en cada menú. | (a). La lectura del enunciado es la presencia del alimento en el menú, no la cantidad servida; la consulta no exige almacenar cantidades por alimento en el detalle del menú. Si el modelo llegara a registrarlas por otra demanda, la semántica de esta consulta no cambia. | — (sin requisito de modelo) |

### Nota de realización del top-N (implementación; fuera del alcance del modelo)

- `LIMIT` parametrizado no exige constantes, ni en EF Core ni en SQL directo: `Take(n)` viaja como `DbParameter` (`LIMIT @p`) y `FETCH FIRST @n`/`LIMIT @n` acepta parámetros igualmente (Dapper: `new { n }`). N viene de la UX con valor por defecto 10 y se valida y acota en el endpoint a 1 ≤ N ≤ 20, sin fragmentar la caché de planes del gestor.
- El corte con empates (`FETCH FIRST :n ROWS WITH TIES`, PostgreSQL 13+) no tiene operador LINQ nativo en EF Core ni en el provider Npgsql (verificado en ambos repositorios, sin solicitudes abiertas). Vías de realización:
  1. **Patrón de umbral (LINQ puro, dos consultas):** la 1.ª obtiene las utilizaciones del N-ésimo (`OFFSET :n-1 LIMIT 1`); la 2.ª filtra `>=` ese umbral. Semánticamente equivalente a `WITH TIES`, todo traducido y parametrizado.
  2. **`FromSql`/`SqlQuery`:** fragmento `ORDER BY ... FETCH FIRST {n} ROWS WITH TIES`; n sigue siendo parámetro y el fragmento posee su propio `ORDER BY`.
  3. **Dapper/SQL directo** para la consulta de reporte, acorde al estilo CQRS-lite del backend.

## 8. Criterio de validación contra el modelo (insumo para #10)

Al recorrer esta consulta sobre el MERX consolidado debe verificarse:

- [ ] Desde `Dieta` se llega a **todos** sus menús (relación `Menu`–`Dieta` inequívoca).
- [ ] El modelo distingue (o permite derivar) qué menús están **finales/aprobados**.
- [ ] Desde cada menú se recuperan sus **alimentos**, directamente o descomponiendo platos.
- [ ] Cada aparición de un alimento en un menú es identificable (para contar sin ambigüedad).
- [ ] Cada alimento expone su **nivel calórico** y su **grupo nutricional**.
- [ ] Las utilizaciones son calculables; no se requiere ningún dato derivado almacenado.

## 9. Notas para el conjunto de datos de prueba

- Una dieta con al menos 3 menús aprobados y 1 menú pendiente de aprobar, para evidenciar el filtro de estado.
- Alimentos repetidos en varios menús (para distinguir 1 vs N utilizaciones) y al menos un plato compuesto si #5 decide composición recursiva.
- Cobertura de los tres niveles calóricos y de varios grupos nutricionales, para que la clasificación sea visible en el reporte.
- Una dieta con más de 20 alimentos distintos utilizados en sus menús finales, para ejercitar el corte top-N y su límite superior (§7-C).
