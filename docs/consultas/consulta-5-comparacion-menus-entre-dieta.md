<!--
Formalización de la consulta informacional 5 — sub-issue #20 del issue #4.
Es la consulta analítica y comparativa de calidad de diseño de menús:
contrasta la composición de los menús entre distintas dietas y evalúa
el cumplimiento de las reglas clínicas y de equilibrio nutricional.
Insumos que alimenta: §3 del informe (docs/informe.md) y la validación del
modelo contra las 6 consultas (issue #10).
La numeración de líneas citada es la del texto plano del enunciado
(pdftotext), la misma empleada en los issues del repositorio.
Los nombres de entidades son los candidatos de los submodelos de Ola 1
(issues #5, #6, #7); quedan sujetos al glosario (#1) y a las convenciones
de modelado (#2).
-->

# Consulta 5. Comparación de menús entre dietas

| | |
|---|---|
| **Issue** | #20 (sub-issue de #4) |
| **Línea(s) del enunciado a la que responde** | 76–77 (con apoyo de 25–28, 30–31, 38–40, 42–43) |
| **Milestone** | Semana 5 — Diseño de BD |
| **Submodelos de Ola 1 involucrados** | #5 (Alimento, GrupoNutricional, NivelCalorico) · #6 (Dieta, Restriccion/GrupoRequerido) · #7 (Menu, MenuAlimento, ParametrizacionMenu) |
| **Insumo para** | §3 del informe (`docs/informe.md`) · Validación del modelo (#10) |

## 1. Enunciado original

> «5. Comparar los menús generados para diferentes dietas, verificando la distribución de alimentos por grupo nutricional y nivel calórico y si los criterios de equilibrio fueron cumplidos.» (líneas 76–77)

Marco general: resultados «en tablas y gráficos» (líneas 65–67), exportables a PDF (línea 83) y con ordenación arbitraria de columnas (líneas 83–85).

## 2. Reformulación enriquecida

La consulta demanda una herramienta analítica comparativa multidimensional que permita contrastar dos o más dietas (o menús de distintas dietas) en función de la composición interna de sus planes de alimentación y su apego a las normas clínicas establecidas.

Para llevar a cabo esta comparación, el sistema debe evaluar tres dimensiones fundamentales:

1. **Distribución por Grupo Nutricional:** Conteo y proporción porcentual de alimentos pertenecientes a cada grupo nutricional (nomenclador del sistema: carbohidratos, proteínas, lípidos, vegetales, etc., según líneas 24–25 y 30) presentes en los menús de cada dieta comparada.
2. **Distribución por Nivel Calórico:** Conteo y proporción de alimentos clasificados según su densidad energética (`bajo`, `medio`, `alto`, líneas 30–31) en la composición de los menús analizados.
3. **Verificación de Criterios de Equilibrio y Restricciones:** Contraste dinámico entre la composición real del menú y los requisitos/restricciones propios de su dieta correspondiente:
   - **Cobertura requerida por la Dieta:** Cada dieta define una «lista de restricciones o grupos nutricionales a cubrir a lo largo del tratamiento» (líneas 42–43). El sistema debe validar que los grupos obligatorios estén presentes y que los grupos restringidos/excluidos no figuren.
   - **Parámetros de generación cumplidos:** Verificar si el menú respeta los criterios preestablecidos de equilibrio definidos durante su creación algorítmica: proporciones por tipo de preparación (entrante, plato fuerte, postre, bebida) y cantidad total de alimentos estipulada (líneas 38–40).
   - **Evaluación sintética:** Salida de un indicador o semáforo de cumplimiento (`Cumple` / `No Cumple` / `Desviación observada`).

### Elementos comunes
- **Parámetros de entrada:** Lista o selección de dos o más identificadores de dietas (`dieta_id_1`, `dieta_id_2`, ...) y, opcionalmente, un selector de menús específicos o período de tiempo para acotar la comparación.
- **Salidas:** 
  - Matriz / Tabla comparativa enfrentando las dietas seleccionadas (columnas por dieta, filas por grupo nutricional y nivel calórico con sus respectivos conteos y porcentajes relativos).
  - Gráficos comparativos (p. ej., diagramas de barras agrupadas o gráficos de radar/telaraña superpuestos de distribución de grupos y calorías).
  - Panel o tabla de auditoría de cumplimiento de criterios de equilibrio (restricciones de dieta satisfechas vs. violadas).
  - Capacidad de ordenación por columnas y exportación a PDF.

## 3. Descomposición de la consulta

| Paso | Resultado parcial | Fuente |
|---|---|---|
| 1 | Recibir el conjunto de dietas a contrastar y recuperar los menús generados representativos de cada una. | #6, #7 |
| 2 | Obtener la composición de alimentos de cada menú (`Menu` → `MenuAlimento` → `Alimento`). | #7, #5 |
| 3 | Agregar y calcular la distribución por `GrupoNutricional` (frecuencia y porcentaje sobre el total de alimentos) por dieta/menú. | #5, derivación |
| 4 | Agregar y calcular la distribución por `NivelCalorico` (bajo, medio, alto; frecuencia y porcentaje) por dieta/menú. | #5, derivación |
| 5 | Recuperar las reglas/restricciones y grupos a cubrir definidos en cada `Dieta` (líneas 42–43). | #6 |
| 6 | Contrastar la presencia de grupos en el menú vs. las reglas de la dieta (verificar ausencia de alimentos restringidos y presencia de grupos obligatorios). | Derivación lógica |
| 7 | Contrastar las proporciones del menú frente a los criterios de equilibrio guardados en `ParametrizacionMenu` (líneas 38–40). | #7, derivación |
| 8 | Consolidar la matriz comparativa de distribución y emitir el dictamen booleano/porcentual de cumplimiento de equilibrio. | Presentación / UX |

## 4. Datos y entidades necesarios (contra los submodelos de Ola 1)

| Dato necesario | Fuente candidata en el modelo | Submodelo | Sustento en el enunciado |
|---|---|---|---|
| Dietas a comparar | `Dieta` (`id`, `nombre`, `programaAtencion`) | #6 | líneas 42–43, 76 |
| Reglas de equilibrio de la dieta | `Dieta`–`GrupoNutricional` (restricciones y grupos a cubrir) | #6 | líneas 42–43 |
| Menús evaluados | `Menu` (`id`, `dieta_id`, `esAutomatico`) | #7 | líneas 31, 76 |
| Alimentos componentes del menú | Relación `Menu`–`Alimento` (`MenuAlimento` / `ComposicionMenu`) | #7 | líneas 24, 31, 38 |
| Grupo nutricional del alimento | `Alimento.grupo_nutricional_id` (nomenclador) | #5 | líneas 24, 30 |
| Nivel calórico del alimento | `Alimento.nivelCalorico` (`bajo`, `medio`, `alto`) | #5 | línea 31 |
| Tipo de preparación del alimento | `Alimento.tipoPreparacion` (`entrante`, `plato fuerte`, `postre`, `bebida`) | #5 | línea 30 |
| Criterios y metas de equilibrio del menú | `ParametrizacionMenu` (proporciones esperadas, cantidad de alimentos) | #7 | líneas 38–40 |

## 5. Trayectorias de navegación por el modelo

```
Dieta (dieta_id_1, dieta_id_2, ...)
  │
  ├──> DietaRegla / DietaGrupo (restricciones y grupos a cubrir)
  │
  └──< Menu (menús generados)
         │
         ├──> ParametrizacionMenu (metas de proporción y total de alimentos)
         │
         └──< MenuAlimento
                └──> Alimento
                       ├──> GrupoNutricional (clasificación)
                       ├──> Alimento.nivelCalorico (bajo / medio / alto)
                       └──> Alimento.tipoPreparacion (entrante / plato fuerte / ...)
```

## 6. Derivaciones y cálculos (no almacenados)

- **Distribución de Grupos Nutricionales:** 
  $$\% \text{ Grupo}_i = \frac{\text{Conteo de alimentos del grupo } i \text{ en el menú}}{\text{Cantidad total de alimentos del menú}} \times 100$$
- **Distribución de Nivel Calórico:** 
  $$\% \text{ Nivel}_j = \frac{\text{Conteo de alimentos con nivel } j \ (\text{bajo/medio/alto})}{\text{Cantidad total de alimentos del menú}} \times 100$$
- **Verificación de Criterios de Equilibrio:**
  - *Restricciones negativas:* Comprobar que ningún alimento del menú pertenezca a grupos prohibidos por la dieta ($\text{AlimentosRestringidos} \cap \text{AlimentosEnMenu} = \emptyset$).
  - *Cobertura positiva:* Comprobar que todos los grupos requeridos por la dieta estén cubiertos ($\text{GruposRequeridos} \subseteq \text{GruposEnMenu}$).
  - *Equilibrio de preparación:* Comprobar si las proporciones de entrante, plato fuerte, postre y bebida se ajustan al margen de tolerancia estipulado en la parametrización del menú.
- Ningún resultado comparativo se almacena: todas las proporciones, tasas y diagnósticos de cumplimiento se computan al vuelo en la consulta.

## 7. Ambigüedades detectadas y decisiones recomendadas

| # | Ambigüedad | Lecturas posibles | Recomendación | Issue destino |
|---|---|---|---|---|
| A | ¿Qué constituye un «criterio de equilibrio»? | (a) Únicamente las restricciones clínicas definidas en la `Dieta` (líneas 42–43). (b) Las metas de proporción y grupos configuradas en la `ParametrizacionMenu` (líneas 38–40). (c) Ambos niveles complementarios. | **Resuelta: (c).** Una dieta define restricciones médicas estructurales (p. ej., sin azúcares o cubrir grupo vegetales), mientras que la generación del menú parametrizó cuotas de equilibrio funcional (proporción de platos y balance calórico). Validar el equilibrio implica contrastar contra ambos para una auditoría rigurosa. | #6, #7 |
| B | Nivel de agregación de la comparación | (a) Comparar dieta contra dieta agregando todos sus menús históricos. (b) Comparar menús individuales específicos seleccionados de distintas dietas. | **Resuelta: Soportar ambas vistas (menú representativo o promedio de dieta).** La UI debe permitir comparar un menú concreto de la Dieta A contra un menú de la Dieta B, o bien el perfil agregado promedio de los menús activos de cada dieta. La estructura de navegación es idéntica en ambos casos. | Producto / UX |
| C | Modelado de las restricciones de la dieta | (a) Lista de texto libre en `Dieta`. (b) Relación normalizada `Dieta` ↔ `GrupoNutricional` tipificada (obligatorio / prohibido). | **Resuelta: (b) Normalizada.** Si fuera texto libre, la consulta no podría verificar algorítmicamente si los criterios de equilibrio «fueron cumplidos» (línea 77). La relación debe permitir marcar si un grupo nutricional es «Requerido» o «Restringido/Excluido». | #6 |

## 8. Criterio de validación contra el modelo (insumo para #10)

- [ ] La entidad `Dieta` dispone de una relación estructurada con `GrupoNutricional` que permite modelar tanto grupos obligatorios como restricciones/exclusiones de forma computable.
- [ ] La relación entre `Menu` y `Alimento` (`MenuAlimento`) permite recuperar todos los alimentos que integran un menú determinado.
- [ ] Cada `Alimento` está asociado a su nomenclador de `GrupoNutricional`.
- [ ] Cada `Alimento` posee su atributo `nivelCalorico` (con dominio restringido: bajo, medio, alto) y su `tipoPreparacion`.
- [ ] El menú conserva o tiene acceso a su `ParametrizacionMenu` histórica con las proporciones y metas de generación para evaluar desviaciones.
- [ ] Es posible construir la consulta SQL/agregación que devuelva en una única tabla la distribución porcentual cruzada (dieta, grupo, nivel calórico) sin bloqueos semánticos.

## 9. Notas para el conjunto de datos de prueba

- Crear al menos dos dietas con objetivos claramente diferenciados (p. ej. Dieta Hipocalórica vs. Dieta Hiperproteica).
- Definir reglas explícitas en las dietas: una dieta que prohíba un grupo específico (p. ej., azúcares/lípidos) y exija otro obligatorio (p. ej., vegetales).
- Generar menús donde:
  - Al menos un menú **cumpla rigurosamente** todos los criterios de equilibrio y restricciones de su dieta.
  - Al menos un menú presente una **violación o desviación intencionada** (p. ej., omisión de un grupo nutricional requerido o desbalance en las proporciones de tipo de preparación) para verificar que el reporte detecte y visualice el incumplimiento.
- Datos con suficientes alimentos de los 3 niveles calóricos (`bajo`, `medio`, `alto`) y de los 4 tipos de preparación para alimentar correctamente los gráficos de barras y de distribución porcentual.
