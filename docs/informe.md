<!-- PORTADA_INICIO -->
**Equipo: Team-Bales**

| Integrante | Grupo |
|---|---|
| Enrique Alejandro Gonzalez Moreira | C312 |
| Javier Fontes Basabe | C312 |
| Heily Rodriguez Rodriguez | C312 |
| Ernesto Alejandro Soler Choong | C312 |
| Mauricio Brindon Carbo | C312 |

**Gestión para la generación automática de
menús nutricionales**

*Proyecto conjunto de las asignaturas Bases de Datos II e Ingeniería de Software. Quinto Semestre, Ciencias de la Computación, Curso 2026-2027.*

<!-- PORTADA_FIN -->

## 1. Introducción

El presente informe describe el diseño de la base de datos para el sistema de gestión de menús nutricionales en un entorno hospitalario. El sistema atiende a cuatro tipos de actores: pacientes que reciben y consumen menús, nutricionistas que los crean y administran alimentos, un jefe de nutrición que supervisa y valida los contenidos, y administradores que gestionan las cuentas del sistema. Su función central es automatizar la generación de menús a partir de dietas definidas institucionalmente, garantizar su validación clínica y registrar el consumo y los resultados nutricionales de cada paciente.

El trabajo sigue una metodología progresiva de cuatro etapas. En primer lugar, se analizan y reformulan los requerimientos funcionales e informacionales para precisar las operaciones que el sistema debe soportar y las consultas que debe responder. A partir de ese análisis se construye el Modelo Entidad-Relación Extendido (MERX) consolidado, que integra las entidades, atributos, relaciones y agregaciones del dominio en un único diagrama coherente. Sobre ese modelo se especifican las restricciones de integridad del esquema, clasificadas por su origen y declaradas mediante los mecanismos estándar del motor de base de datos. Finalmente, se verifica la corrección formal del diseño comprobando que cada esquema de la descomposición alcanza al menos la Tercera Forma Normal (3FN) y que la descomposición satisface la Propiedad de Preservación de Dependencias Funcionales (PPDF) y la Propiedad del Join sin Pérdida de Información (PLJ). La Forma Normal de Boyce-Codd (BCNF) se comprueba después, como verificación adicional.

## 2. Análisis y reformulación enriquecedora de los requerimientos funcionales e informacionales del sistema

### 2.1 Requerimientos funcionales

#### Descripción del dominio

El sistema gestiona la **generación automática de menús nutricionales** dentro de una institución de salud. Su núcleo es un banco de alimentos y platos que los nutricionistas administran, clasifican y usan para confeccionar menús adaptados a las dietas de los pacientes hospitalizados. El menú puede generarse algorítmicamente — aplicando parámetros de proporción por tipo de preparación, cobertura de grupos nutricionales y cantidad total de alimentos — o confeccionarse de forma manual bajo criterios del jefe de nutrición; en cualquier caso queda sujeto a validación antes de ser asignado a un paciente.

#### Actores identificados

| Actor | Rol en el sistema |
|---|---|
| **Nutricionista** | Ingresa y clasifica alimentos y platos en el banco; crea menús; ejecuta revalorizaciones. Actor central del sistema. |
| **Jefe de nutrición** | Rol de Nutricionista con potestad de revisar y aprobar los menús generados, o indicar la confección de uno nuevo bajo sus criterios. |
| **Revisor** | Rol de Nutricionista que actúa en el proceso de validación de un menú: registra fecha, observaciones y dictamen (aprobado/rechazado). Coincide funcionalmente con el Jefe de nutrición; cualquier nutricionista autorizado para la dieta puede ejercerlo. |
| **Paciente** | Recibe los menús asignados; consulta sus valoraciones nutricionales; solicita revalorizaciones de forma virtual especificando al nutricionista ejecutante. |
| **Administrador del sistema** | Rol de mantenimiento externo al dominio: previene la existencia de alimentos duplicados e impide la asignación doble de un menú. |

#### Entidades y nomencladores del dominio

| Tipo | Elementos |
|---|---|
| **Entidades (10)** | Alimento, Plato, Dieta, Menú, Nutricionista, Paciente, Sala, Valoración Nutricional, Revalorización, Consumo |
| **Nomencladores (7)** | Grupo nutricional, Tipo de preparación *(entrante, plato fuerte, postre, bebida)*, Nivel calórico *(bajo, medio, alto)*, Restricción alimentaria, Programa de atención, Especialidad, Resultado nutricional |

Cada **alimento** y cada **plato** se clasifican por grupo nutricional, tipo de preparación y nivel calórico, y pueden contar con una **ficha nutricional ampliada** (macronutrientes, micronutrientes, alérgenos, modo de preparación). El número y el tipo de campos de esa ficha varían de un elemento a otro y no se ajustan a una estructura fija, de modo que cada uno almacena únicamente los atributos que le corresponden: por eso la ficha se gestiona de forma independiente del resto de sus datos. Cómo se implementa esa independencia es una decisión de diseño, no un requerimiento, y se justifica en la §4.3: la ficha reside en MongoDB, referenciada desde el modelo relacional por la clave del alimento o del plato.

Una **dieta** define los grupos nutricionales a cubrir durante el tratamiento (`CubreDieta`), las restricciones alimentarias a respetar (`Restringe`) y el programa de atención institucional al que corresponde. Los **menús** generados para una dieta almacenan los parámetros de generación como datos inmutables: cantidad total de alimentos y origen automático (`EsAutomático`) como atributos escalares, y la proporción por tipo de preparación y cobertura de grupos nutricionales como relaciones propias (`Distribución`, `CubreMenú`).

El **consumo** registra, para cada alimento de un menú asignado a un paciente, si fue aceptado o rechazado; su clave es el trío `(MenúId, PacienteId, AlimentoId)` y no incluye fecha propia. Las **valoraciones nutricionales** documentan el resultado del estado del paciente para un menú recibido, referenciando un valor del nomenclador `ResultadoNutricional`. Las **revalorizaciones** registran la solicitud virtual del paciente de revisión de una valoración previa, identificando el nutricionista ejecutante y, cuando ya fue atendida, la nueva valoración generada.

#### Decisiones de modelado relevantes

- **Plato como entidad separada** de Alimento (`Composición(Plato, Alimento, Cantidad)`). Un menú incluye alimentos y platos directamente; los alimentos de un plato se acceden vía `Composición`.
- **Jefe de nutrición** es un rol de Nutricionista, no una entidad. El estado del menú se deriva de la existencia de un registro aprobado en `Validación`.
- **Asignación** se modela como N:M (`Asignación(MenúId, PacienteId)`); la clave primaria compuesta garantiza la unicidad por par.
- **Compatibilidad alimentaria** se resuelve con dos vías independientes. `CubreDieta` relaciona la dieta con los grupos nutricionales que le conciernen y califica cada uno con el atributo `Tipo`: *Requerido* si la dieta debe cubrirlo a lo largo del tratamiento, *Restringido* si lo prohíbe por un criterio propio de esa dieta. `Restringe + Excluye` aporta la segunda vía: las restricciones alimentarias son un catálogo institucional, y cada una excluye los grupos que le correspondan, una sola vez, para todas las dietas que la declaren. Los **grupos prohibidos** de una dieta son la unión de las dos vías; sus **grupos requeridos**, los de `Tipo` *Requerido*. Que un grupo quede prohibido por ambas vías es válido: la prohibición se repite sin contradecirse. Lo que no se admite es que un grupo sea a la vez requerido y excluido por una restricción de la misma dieta (RS-11).
- **Parámetros de generación**: `CantidadTotalAlimentos` y `EsAutomático` son escalares de Menú; proporción y cobertura son relaciones (`Distribución`, `CubreMenú`). `Cubre` se desambigua en `CubreDieta` y `CubreMenú`.
- **`ResultadoNutricional`** es un nomenclador con `Nombre` y `Valor` únicos, evitando texto libre y habilitando cálculo de promedios.
- **`Consumo`** no incluye fecha; su clave es `(MenúId, PacienteId, AlimentoId)` y el ancla temporal es `Menú.FechaCreación`.

**Catálogo de alimentos y platos**

| # | Requerimiento |
|---|---|
| RF-01 | El sistema permite registrar alimentos con nombre único, grupo nutricional, tipo de preparación, nivel calórico y nutricionista descritor. |
| RF-02 | El sistema impide el registro de alimentos con nombre duplicado (normalizado). |
| RF-03 | El sistema permite registrar platos como composiciones de alimentos con cantidades, clasificados con los mismos atributos que los alimentos. |
| RF-04 | El sistema permite asociar a cada alimento y a cada plato una ficha nutricional ampliada (macronutrientes, micronutrientes, alérgenos, modo de preparación), cuyos campos varían de un elemento a otro, y consultarla o modificarla de forma independiente del resto de sus datos. |

**Usuarios y roles**

| # | Requerimiento |
|---|---|
| RF-05 | El sistema gestiona nutricionistas con especialidad, y pacientes con sala de hospitalización asignada. |
| RF-06 | El sistema controla qué nutricionistas están autorizados para crear y validar menús de cada dieta. |
| RF-07 | El sistema permite a cada nutricionista consultar los menús de las dietas en las que está autorizado, cualquiera sea el nutricionista que los creó. |
| RF-08 | El sistema registra la inscripción de pacientes en dietas activas. |

> **RF-07 no requiere estructura nueva.** «Atender la misma dieta» es estar autorizado en ella, de modo que los menús visibles para un nutricionista se obtienen recorriendo `Nutricionista` → `Autoriza` → `Dieta` ← `Pertenece` ← `Menú`. La autorización, que RF-06 ya exige para crear y validar, delimita también lo que se puede consultar.

**Dietas y menús**

| # | Requerimiento |
|---|---|
| RF-09 | El sistema permite definir dietas con programa de atención, grupos nutricionales a cubrir y restricciones alimentarias a respetar. |
| RF-10 | El sistema soporta la generación automática de menús aplicando parámetros inmutables: proporción por tipo de preparación, cobertura de grupos nutricionales y cantidad total de alimentos. |
| RF-11 | El sistema permite la confección manual de menús bajo criterios del jefe de nutrición. |
| RF-12 | El sistema registra la validación de un menú con fecha, dictamen (aprobado/rechazado) y observaciones, garantizando que el validador no sea el creador. |
| RF-13 | El sistema asigna menús validados a pacientes inscritos en la dieta correspondiente, impidiendo la asignación doble. |

**Registro clínico y valoraciones**

| # | Requerimiento |
|---|---|
| RF-14 | El sistema registra, para cada alimento de un menú asignado, si el paciente lo aceptó o rechazó. |
| RF-15 | El sistema registra valoraciones nutricionales del estado del paciente, referenciando un valor del nomenclador de resultados y el nutricionista emisor. |
| RF-16 | El sistema procesa solicitudes virtuales de revalorización, identificando el nutricionista ejecutante y la valoración revisada, y vincula la nueva valoración cuando se completa. |

**Requerimientos no funcionales**

| # | Requerimiento |
|---|---|
| RNF-01 | Las consultas de disponibilidad calórica de una dieta y de valoraciones más recientes de un paciente responden de forma prácticamente inmediata, incluso en los horarios de mayor concurrencia. |

RNF-01 recoge una exigencia explícita del enunciado: esas dos consultas las repiten nutricionistas y pacientes a lo largo de toda la jornada, de modo que su latencia no puede depender de la carga del motor relacional. Se tratan en §2.2, bajo *Consultas recurrentes*, junto con la estrategia que las sostiene.

### 2.2 Requerimientos informacionales

El sistema debe dar respuesta a seis consultas informacionales y a dos consultas recurrentes. Las primeras son los reportes que el enunciado enumera como funcionalidades; las segundas son las peticiones que se repiten durante la jornada y están sujetas a RNF-01. A continuación se presenta el enunciado original de cada una y su reformulación enriquecida.

---

#### Consulta 1 — Menús generados automáticamente por dieta

> *«Obtener el listado de menús generados automáticamente para una dieta específica, indicando el nombre del creador, la fecha de creación y los parámetros utilizados.»*

**Reformulación:** Dado el identificador de una dieta, el sistema recupera el historial auditable de todos sus menús con `EsAutomático = TRUE`. Por cada uno proyecta: el nombre del nutricionista creador (quien ejecutó la parametrización algorítmica, no necesariamente quien ingresó los alimentos al banco), la marca temporal de creación (`FechaCreación`) y el detalle completo de los parámetros almacenados de forma inmutable junto al menú: cantidad total de alimentos, proporciones por tipo de preparación (`Distribución`) y grupos nutricionales cubiertos (`CubreMenú`). Los resultados son ordenables por cualquier columna y exportables a PDF.

---

#### Consulta 2 — Alimentos más utilizados en los menús finales de una dieta

> *«Obtener los alimentos más utilizados en los menús finales de una dieta, clasificados por nivel calórico y grupo nutricional.»*

**Reformulación:** Dado el identificador de una dieta, el sistema filtra los **menús finales** — aquellos con al menos una validación aprobada — y sobre ellos cuenta cuántos menús distintos contienen cada alimento (presencia, no cantidad servida). Los alimentos que llegan al menú a través de platos (`IncluyePlato` + `Composición`) se suman a los incluidos directamente (`IncluyeAlimento`) para no subcontar el uso real. El resultado es una tabla de alimentos con número de apariciones, nivel calórico y grupo nutricional, ordenable y filtrable por top-N, exportable a PDF.

---

#### Consulta 3 — Menús validados por un revisor determinado

> *«Listar los menús que fueron validados por un revisor determinado, indicando la fecha de validación y las observaciones hechas durante el proceso.»*

**Reformulación:** Dado el identificador de un nutricionista en rol de revisor, el sistema recupera el historial completo de sus registros en `Validación`: por cada acto reporta el menú evaluado (con su dieta y nutricionista creador), la fecha/hora del dictamen, las observaciones registradas y el resultado (aprobado o rechazado). La consulta lista todos los actos de validación completados, no solo las aprobaciones, porque las observaciones de los rechazos son parte del proceso de auditoría clínica. Los resultados son ordenables y exportables a PDF.

---

#### Consulta 4 — Desempeño nutricional de los pacientes ante un menú

> *«Generar un reporte sobre el desempeño nutricional de los pacientes ante un menú, clasificando los alimentos por nivel calórico y comparando las tasas de aceptación.»*

**Reformulación:** Dado un menú, el sistema recupera los alimentos que lo componen (directos e indirectos vía platos) y los registros de consumo de los pacientes que lo recibieron. Por cada alimento calcula: número de servicios con consumo registrado, número de aceptaciones, número de rechazos y **tasa de aceptación** (aceptaciones ÷ servicios registrados). Los resultados se clasifican por nivel calórico del alimento y se presentan en tabla y gráfico comparativo. Los pacientes asignados sin consumo registrado quedan fuera del denominador; las tasas son derivadas y `Aceptación` en `Consumo` es el único dato almacenado.

---

#### Consulta 5 — Comparación de menús entre dietas

> *«Comparar los menús generados para diferentes dietas, verificando la distribución de alimentos por grupo nutricional y nivel calórico y si los criterios de equilibrio fueron cumplidos.»*

**Reformulación:** Dadas dos o más dietas, el sistema contrasta sus menús en tres dimensiones: (1) distribución porcentual de alimentos por grupo nutricional; (2) distribución por nivel calórico; (3) verificación de criterios de equilibrio en dos niveles: a nivel de dieta, si los grupos que exige cubrir (`CubreDieta` con `Tipo` *Requerido*) están presentes en el menú y si los que tiene prohibidos están ausentes; a nivel de parámetros de generación, si las proporciones por tipo de preparación y la cantidad total de alimentos respetan lo declarado en `Distribución` y `CantidadTotalAlimentos`. Los grupos prohibidos de una dieta son la unión de dos vías: los excluidos por sus restricciones (`Restringe + Excluye`) y los que su propia cobertura marca como *Restringido*.

Los cuatro criterios pueden incumplirse: ninguno es una restricción que la base imponga (§4.5), y de ahí que el enunciado pida verificar «si fueron cumplidos». Ningún resultado se almacena: proporciones, tasas y dictámenes de cumplimiento son todos derivados.

---

#### Consulta 6 — Correlación nivel calórico – resultado nutricional

> *«Determinar, para cada dieta, la correlación entre el nivel calórico de los alimentos y el resultado nutricional promedio de los pacientes. La consulta debe identificar los 10 alimentos con la tasa de rechazo más alta, el nutricionista que los creó y la dieta a la que pertenecen. Además, debe comparar el desempeño nutricional de los pacientes que solicitaron una revalorización con el promedio general de valoraciones de sus respectivas salas para esas mismas dietas.»*

**Reformulación:** La consulta tiene tres partes independientes:

- **Parte A — Correlación por dieta:** para cada dieta, une dos ramas del modelo: la rama alimentaria (dieta → menús → alimentos con nivel calórico) y la rama clínica (pacientes con valoraciones ancladas a esos menús). La unión se hace por el par `(MenúId, PacienteId)` de la asignación; el resultado es el promedio del valor numérico de `ResultadoNutricional` segmentado por nivel calórico, sin necesidad de fechas en `Valoración` ni en `Consumo` (el orden temporal se deriva de `Menú.FechaCreación`).

- **Parte B — Top-10 alimentos rechazados:** sobre todos los registros de `Consumo` del sistema, calcula la tasa de rechazo por alimento y selecciona los 10 con mayor tasa. Por cada uno reporta el nutricionista que lo **describió** en el banco (no el creador del menú) y la dieta a la que pertenece de forma indirecta, vía los menús en que aparece el consumo rechazado.

- **Parte C — Revalorizados frente al promedio de su sala:** para cada paciente que solicitó una revalorización, compara el valor numérico de la nueva valoración generada con el promedio de las valoraciones de los demás pacientes de la misma sala inscritos en la misma dieta. La dieta de referencia se obtiene de forma transitiva: `Revalorización → Valoración revisada → Menú → Dieta`.

---

#### Consultas recurrentes

> *«Dado que la disponibilidad calórica de las dietas y las valoraciones más recientes son consultadas de forma reiterada por nutricionistas y pacientes a lo largo de toda la jornada, el sistema debe garantizar respuestas prácticamente inmediatas ante esas peticiones recurrentes, incluso en los horarios de mayor concurrencia.»*

A diferencia de las seis anteriores, estas dos no son reportes que se piden de vez en cuando, sino consultas de fondo que acompañan el trabajo diario. El enunciado no las define, así que el primer paso es precisar qué significan en términos del modelo.

**R1 — Disponibilidad calórica de una dieta.** Para cada nivel calórico, cuántos ítems del banco son compatibles con la dieta y cómo se distribuyen por nivel calórico los ítems de sus menús finales. Un ítem es compatible cuando su grupo nutricional no está prohibido para esa dieta, es decir, cuando no lo excluye ninguna de sus restricciones (`Restringe + Excluye`) ni lo marca como *Restringido* su propia cobertura (`CubreDieta`). Es el dato que un nutricionista consulta antes de confeccionar un menú: con qué cuenta, y en qué franja calórica.

> La disponibilidad se expresa en los tres niveles del nomenclador `NivelCalórico` —bajo, medio y alto—, no en kilocalorías. El modelo relacional no almacena ningún valor energético: los macronutrientes pertenecen a la ficha nutricional (§4.3), cuya estructura es variable y no se presta a agregación. La alternativa de que cada dieta declarase los niveles calóricos que admite, mediante una relación `Dieta`–`NivelCalórico`, se descartó: el enunciado habla de lo que está *disponible* para la dieta, no de un límite que la dieta imponga.

**R2 — Valoraciones más recientes de un paciente.** Las N valoraciones del paciente con mayor `FechaValoración`, con su resultado nutricional y el menú evaluado. Ordenarlas es posible porque la valoración tiene fecha propia; el modelo no depende para ello de la fecha de creación del menú.

**Trayectoria.** R1 recorre `Dieta → Restringe → Excluye → GrupoNutricional` y `Dieta → CubreDieta` para obtener los grupos prohibidos, `ItemAlimentario → NivelCalórico` para clasificar el banco, y `Dieta ← Menú` (final) `→ Incluye → ItemAlimentario → NivelCalórico` para la parte de los menús. R2 recorre `Paciente ← Asignación ← Valoración → ResultadoNutricional`, ordenando por `FechaValoración`. Ninguna de las dos almacena su resultado: ambas son derivadas.

**Cómo se garantiza la respuesta inmediata (RNF-01).** Las dos consultas son costosas en relación con su frecuencia —R1 recorre el banco completo y R2 ordena el historial del paciente— y sus resultados cambian poco entre una petición y la siguiente. Por eso se resuelven con un **caché de tipo clave-valor en memoria, en Redis, por delante de PostgreSQL**, que sigue siendo la única fuente de verdad:

| | Disponibilidad calórica | Valoraciones recientes |
|---|---|---|
| Clave | `dieta:{DietaId}:disponibilidad-calorica` | `paciente:{PacienteId}:valoraciones-recientes` |
| Valor | Conteo de ítems por nivel calórico | Las N valoraciones más recientes, ya ordenadas |
| Se invalida al | Alta o baja de un ítem del banco, o cambio de su nivel calórico o de su grupo; cambio en la cobertura o en las restricciones de la dieta; cambio en los grupos que excluye una restricción, que alcanza a todas las dietas que la declaran; aprobación de un menú suyo | Registrar una valoración de ese paciente, sea la original o la que genera una revalorización |

El patrón es *cache-aside*: la consulta busca primero en el caché y, si no encuentra la clave, la calcula contra PostgreSQL y la guarda. Las claves llevan además un tiempo de vida como respaldo, de modo que un fallo en la invalidación se corrige solo. Si Redis no está disponible, la consulta se resuelve contra PostgreSQL: se pierde la latencia, no la corrección. La elección de Redis y el reparto de responsabilidades entre los tres motores se justifican en el ADR correspondiente del repositorio.

## 3. MERX de la Base de Datos

El Modelo Entidad-Relación Extendido (MERX) consolidado del sistema se presenta a continuación.

![MERX — Entidades y nomencladores](MERX%20consolidado/MERXInforme-Entidades.png)

![MERX — Relaciones 1: clasificación](MERX%20consolidado/MERXInforme-Relaciones-1.png)

![MERX — Relaciones 2: dieta y menú](MERX%20consolidado/MERXInforme-Relaciones-2.png)

![MERX — Relaciones 3: valoración y consumo](MERX%20consolidado/MERXInforme-Relaciones-3.png)

## 4. Especificación de las restricciones de integridad establecidas y/o detectadas

Las restricciones se agrupan por su origen: **clave**, **referenciales**, **dominio**, **cardinalidad** y **semánticas**.

### 4.1 Restricciones estructurales

#### 4.1.1 Clave

| # | Restricción |
|---|---|
| RC-01 | `AlimentoId` identifica unívocamente a cada alimento y `PlatoId` a cada plato. |
| RC-02 | `GrupoNutricionalId`, `TipoPreparaciónId` y `NivelCalóricoId` identifican unívocamente a cada valor de su nomenclador. |
| RC-03 | Un mismo alimento aparece a lo sumo una vez en la composición de un plato: el par (`PlatoId`, `AlimentoId`) identifica cada ocurrencia de `Composición`. |
| RC-04 | `DietaId` identifica unívocamente a cada dieta, `NutricionistaId` a cada nutricionista, `PacienteId` a cada paciente y `SalaId` a cada sala. |
| RC-05 | `ProgramaDeAtenciónId`, `RestricciónAlimentariaId` y `EspecialidadId` identifican unívocamente a cada valor de su nomenclador. |
| RC-06 | El par (`NutricionistaId`, `DietaId`) identifica cada autorización. |
| RC-07 | El par (`PacienteId`, `DietaId`) identifica cada inscripción. |
| RC-08 | El par (`DietaId`, `GrupoNutricionalId`) y el par (`DietaId`, `RestricciónAlimentariaId`) identifican cada ocurrencia de `CubreDieta` y de `Restringe`, respectivamente. |
| RC-09 | `MenúId` identifica unívocamente a cada menú, `ValoraciónId` a cada valoración y `RevalorizaciónId` a cada revalorización. |
| RC-10 | `ResultadoNutricionalId` identifica unívocamente a cada categoría del nomenclador `ResultadoNutricional`. |
| RC-11 | Un menú registra a lo sumo una proporción por tipo de preparación: el par (`MenúId`, `TipoPreparaciónId`) identifica cada ocurrencia de `Distribución`. |
| RC-12 | Un revisor registra a lo sumo una validación por menú: el par (`NutricionistaId`, `MenúId`) identifica cada ocurrencia de `Validación`. |
| RC-13 | Un menú se asigna a lo sumo una vez a un mismo paciente: el par (`MenúId`, `PacienteId`) identifica cada ocurrencia de `Asignación`. |
| RC-14 | Cada alimento tiene a lo sumo un registro de consumo por asignación: la terna (`MenúId`, `PacienteId`, `AlimentoId`) identifica cada ocurrencia de `Consumo`. |
| RC-15 | Un alimento, un plato o un grupo nutricional aparece a lo sumo una vez en un mismo menú: los pares de `IncluyeAlimento` (`MenúId`, `AlimentoId`), `IncluyePlato` (`MenúId`, `PlatoId`) y `CubreMenú` (`MenúId`, `GrupoNutricionalId`) son únicos. |
| RC-16 | El par (`RestricciónAlimentariaId`, `GrupoNutricionalId`) identifica cada ocurrencia de `Excluye`. |

> `Autoriza`, `Inscribe` y `Restringe` son tablas de interconexión sin atributos propios: su identidad es el par de claves primarias de los participantes. `CubreDieta` también se identifica por ese par, pero **sí tiene un atributo propio**, `Tipo`, que califica cada grupo de la dieta como *Requerido* o *Restringido* (§2.1, RD-16).

#### 4.1.2 Referenciales

| # | Restricción |
|---|---|
| RR-01 | Toda clasificación de un alimento o de un plato referencia un valor existente del nomenclador correspondiente. |
| RR-02 | Toda ocurrencia de `Composición` referencia un plato y un alimento existentes. |
| RR-03 | Todo alimento y todo plato referencia al nutricionista que lo describió. |
| RR-04 | No se elimina un valor de nomenclador mientras lo use algún alimento o plato, ni un alimento mientras forme parte de algún plato. |
| RR-05 | Toda dieta corresponde a un programa de atención existente. |
| RR-06 | Toda ocurrencia de `CubreDieta` referencia una dieta y un grupo nutricional existentes. |
| RR-07 | Toda ocurrencia de `Restringe` referencia una dieta y una restricción alimentaria existentes. |
| RR-08 | Toda autorización referencia un nutricionista y una dieta existentes. |
| RR-09 | Toda inscripción referencia un paciente y una dieta existentes. |
| RR-10 | Todo paciente referencia la sala a la que pertenece. |
| RR-11 | Todo nutricionista referencia la especialidad que ejerce. |
| RR-12 | No se elimina un valor de nomenclador mientras lo use alguna dieta o algún nutricionista, ni una dieta mientras figure en alguna autorización o inscripción. |
| RR-13 | Todo menú referencia la dieta a la que pertenece y el nutricionista que lo creó. |
| RR-14 | Toda ocurrencia de `IncluyeAlimento`, `IncluyePlato`, `CubreMenú` y `Distribución` referencia un menú existente y la entidad correspondiente. |
| RR-15 | Toda ocurrencia de `Validación` referencia un nutricionista y un menú existentes. |
| RR-16 | Toda ocurrencia de `Asignación` referencia un menú y un paciente existentes; todo `Consumo` referencia una asignación existente y un alimento existente. |
| RR-17 | Toda valoración referencia la asignación que evalúa, el nutricionista que la emite y una categoría existente de `ResultadoNutricional`. |
| RR-18 | Toda revalorización referencia al paciente que la solicita, al nutricionista que la ejecuta y a la valoración que revisa; la valoración generada, cuando existe, también debe existir. |
| RR-19 | No se elimina un menú mientras tenga validaciones o asignaciones, ni una asignación mientras tenga consumos o valoraciones, ni una valoración mientras una revalorización la revise o la haya generado. |
| RR-20 | Toda ocurrencia de `Excluye` referencia una restricción alimentaria y un grupo nutricional existentes. |
| RR-21 | No se elimina una restricción alimentaria ni un grupo nutricional mientras figuren en `Excluye`. |
| RR-22 | No se elimina un nutricionista ni un paciente mientras tengan hechos asociados: ítems descritos, menús creados, validaciones, valoraciones emitidas, revalorizaciones ejecutadas, autorizaciones o inscripciones. Tampoco se elimina un ítem alimentario que figure en algún menú, ni un valor de nomenclador que use algún menú. |

> RR-04, RR-12, RR-19 y RR-21 son las reglas que impiden borrar un hecho del que dependen otros, y se implementan con `ON DELETE RESTRICT`. No son las únicas claves foráneas del esquema ni agotan la política de borrado: la política completa, clave foránea por clave foránea, está en RX-02 (§4.4).

#### 4.1.3 Dominio

| # | Restricción |
|---|---|
| RD-01 | `TipoPreparación` toma exactamente los valores *entrante*, *plato fuerte*, *postre* y *bebida*. |
| RD-02 | `NivelCalórico` toma exactamente los valores *bajo*, *medio* y *alto*. |
| RD-03 | `GrupoNutricional` es un catálogo abierto; se exige únicamente que cada nombre sea único. |
| RD-04 | `Nombre` es obligatorio y no vacío en `Alimento`, `Plato` y los tres nomencladores. |
| RD-05 | `Cantidad` de `Composición` se expresa en gramos, como un decimal estrictamente positivo. |
| RD-06 | `Edad` es mayor o igual que cero. |
| RD-07 | `Nombre` es obligatorio y no vacío en `Dieta`, `Nutricionista`, `Paciente`, `Sala` y en los tres nomencladores. |
| RD-08 | `Nombre` es único dentro de cada nomenclador. |
| RD-09 | `Edad` se registra como un número entero de años. |
| RD-10 | `CantidadTotalAlimentos` es un entero estrictamente positivo. |
| RD-11 | `Proporción` de `Distribución` está en el intervalo (0, 1]. |
| RD-12 | `EsAutomático` (menú) y `Aprobado` (validación) son booleanos obligatorios. |
| RD-13 | `Aceptación` de `Consumo` toma exactamente los valores *aceptado* y *rechazado*. |
| RD-14 | `ResultadoNutricional` tiene exactamente cinco categorías — *Muy mala*, *Mala*, *Normal*, *Buena* y *Excelente* —, con `Valor` 1, 2, 3, 4 y 5 respectivamente; `Nombre` y `Valor` son únicos y obligatorios. |
| RD-15 | `FechaCreación`, `FechaValidación` y `FechaSolicitud` son obligatorias. `Observaciones` es opcional. |
| RD-16 | `Tipo` de `CubreDieta` es obligatorio y toma exactamente los valores *Requerido* y *Restringido*. |

> Las restricciones de catálogo cerrado (RD-01, RD-02 y RD-14) se garantizan en dos niveles: la FK impide usar un valor ajeno al catálogo, y el CHECK sobre el propio catálogo impide ampliarlo.

#### 4.1.4 Cardinalidad

| # | Restricción |
|---|---|
| RK-01 | Cada alimento y cada plato tiene exactamente un grupo nutricional, un tipo de preparación y un nivel calórico. |
| RK-02 | Cada alimento y cada plato tiene exactamente un nutricionista que lo describió. |
| RK-03 | Todo plato se compone de al menos un alimento: `(1,*)` del lado de `Alimento` en `Composición`. |
| RK-04 | Cada dieta corresponde a exactamente un programa de atención. |
| RK-05 | Cada paciente pertenece a exactamente una sala. |
| RK-06 | Cada nutricionista ejerce exactamente una especialidad. |
| RK-07 | Toda dieta cubre al menos un grupo nutricional: `(1,*)` del lado de `GrupoNutricional` en `CubreDieta`. |
| RK-08 | Cada menú tiene exactamente un creador y una dieta. |
| RK-09 | Todo menú tiene al menos una proporción por tipo de preparación y al menos un grupo nutricional cubierto. |
| RK-10 | Cada valoración evalúa exactamente una asignación, la emite exactamente un nutricionista y tiene exactamente un resultado. |
| RK-11 | Cada revalorización tiene exactamente un solicitante, un ejecutor y una valoración revisada. Genera a lo sumo una valoración, y cada valoración es generada por a lo sumo una revalorización. |

> Las restricciones de cardinalidad mínima ≥ 1 (RK-03, RK-07 y RK-09) no son expresables con claves foráneas: la entidad se registra antes que sus ocurrencias y la comprobación solo tiene sentido al cierre de la operación. En la capa de aplicación, el alta y su detalle se registran en una misma unidad de trabajo; en PostgreSQL se refuerzan con un trigger de restricción diferido (`DEFERRABLE INITIALLY DEFERRED`).

---

### 4.2 Restricciones semánticas

Las restricciones semánticas no acotan los valores que se pueden almacenar, sino **quién puede operar sobre ellos** y bajo qué condiciones.

| # | Restricción |
|---|---|
| RS-01 | No existen alimentos duplicados. |
| RS-02 | Un nutricionista solo clasifica los alimentos que él mismo describió. |
| RS-03 | Un nutricionista solo genera o valida menús de las dietas en las que está autorizado. |
| RS-04 | Un paciente solo recibe menús de las dietas en las que está inscrito. |
| RS-05 | Un nutricionista no valida un menú que él mismo creó. |
| RS-06 | La fecha de validación no es anterior a la fecha de creación del menú validado. |
| RS-07 | La parametrización de un menú (`CantidadTotalAlimentos`, `EsAutomático`, `Distribución`, `CubreMenú`) no cambia después de creado el menú. |
| RS-08 | La suma de las proporciones de un menú es 1. |
| RS-09 | El paciente que solicita una revalorización es el mismo de la valoración que revisa. |
| RS-10 | La valoración generada por una revalorización evalúa la misma asignación que la valoración revisada y la emite el nutricionista que ejecutó la revalorización. |
| RS-11 | Un grupo que una dieta exige cubrir (`CubreDieta` con `Tipo` *Requerido*) no puede estar excluido por una de sus restricciones (`Restringe` seguido de `Excluye`). |

> **RS-11 restringe la definición de la dieta, no el contenido de sus menús.** Una dieta que exige un grupo y a la vez lo prohíbe es incoherente: ningún menú podría cumplirla. Por eso solo cuenta `Tipo` *Requerido*; un grupo marcado *Restringido* que además esté excluido por una restricción repite la prohibición sin contradecirla. Que un **menú** incluya ítems de un grupo prohibido por su dieta es una cuestión distinta, y no es una restricción: se trata en §4.5.

---

### 4.3 Restricciones de la ficha nutricional (MongoDB)

Las restricciones siguientes pertenecen a la ficha nutricional, almacenada en MongoDB. Se listan por referencia, sin duplicar la especificación del documento correspondiente: se conserva el identificador, se resume en una línea y se indica su mecanismo.

| # | Restricción |
|---|---|
| RM-01 | Toda ficha identifica la fila de PostgreSQL a la que pertenece (`origen.tabla`, `origen.id`). |
| RM-02 | `origen.tabla` solo puede ser `Alimento` o `Plato`. |
| RM-03 | `tipo` es `simple` o `compuesto`, coherente con la tabla: `Alimento` → `simple`, `Plato` → `compuesto`; solo las fichas de plato llevan `ingredientes`. |
| RM-04 | Los valores numéricos de la porción son positivos y los alérgenos no se repiten. |
| RM-05 | Los bloques `macronutrientes`, `micronutrientes` y `modoPreparacion` no tienen campos obligatorios. |
| RM-06 | En la ficha de un plato, el conjunto de `ingredientes.alimentoId` coincide con los alimentos del plato en `Composición`. |
| RM-07 | Solo existe ficha para filas existentes de `Alimento` o `Plato`. |
| RM-08 | Cada fila tiene a lo sumo una ficha. |
| RM-09 | Al eliminar un alimento o un plato se elimina su ficha. |
| RM-10 | Periódicamente se eliminan las fichas huérfanas y se reconstruyen las copias de `nombre` desactualizadas. |

---

### 4.4 Restricciones definidas en la consolidación

Dos restricciones quedaron sin especificar en los submodelos individuales y se definen en la consolidación.

| # | Restricción |
|---|---|
| RX-01 | Solo se registra consumo de alimentos que están en el menú asignado, directamente (`IncluyeAlimento`) o dentro de un plato incluido (`IncluyePlato` + `Composición`). |
| RX-02 | Política de borrado entre submodelos: `CASCADE` únicamente en las seis claves foráneas que van de un dueño a sus propias filas de composición o parametrización; `RESTRICT` en todas las demás. El detalle está en la tabla siguiente. |

**Restricción 1 — por qué así:** un hecho del que dependen otros no se puede borrar. Las filas que solo describen a su dueño, en cambio, desaparecen con él: así un menú que todavía no tiene dictamen se borra por completo, con su parametrización y sus ítems. Con esta política, la inmutabilidad de la parametrización (RS-07) bloquea los cambios sueltos y deja pasar solo el borrado en cascada.

El criterio distingue dos situaciones. Una fila **existe por su dueño y carece de sentido sin él** —la composición de un plato, la parametrización de un menú—: se borra con él, en `CASCADE`. Una fila **registra un hecho propio** —una validación, una asignación, un consumo— o **referencia un catálogo compartido**: el borrado se impide, en `RESTRICT`, porque perderla falsearía el historial o dejaría huérfanas a otras filas.

**Las seis claves foráneas en `CASCADE`:**

| Clave foránea | Por qué |
|---|---|
| `Plato → Composición` | Los ingredientes describen al plato; sin él no existen. |
| `Menú → Incluye` | Los ítems incluidos son el contenido del menú. |
| `Menú → Distribución` | Parámetro de generación del menú. |
| `Menú → CubreMenú` | Parámetro de generación del menú. |
| `Dieta → CubreDieta` | La cobertura define a la dieta. |
| `Dieta → Restringe` | Las restricciones definen a la dieta. |

**Las claves foráneas en `RESTRICT`,** agrupadas por lo que protegen:

| Grupo | Claves foráneas | Por qué |
|---|---|---|
| Clasificación de los ítems | `ItemAlimentario` → `GrupoNutricional`, `TipoPreparación`, `NivelCalórico`, `Nutricionista` descriptor | No se borra un valor de nomenclador en uso (RR-04) ni el nutricionista que describió un ítem (RR-22). |
| Composición | `Composición` → `Alimento` | No se borra un alimento que es ingrediente de un plato (RR-04). |
| Uso de un ítem o un nomenclador | `Incluye` → `ItemAlimentario`; `Distribución` → `TipoPreparación`; `CubreMenú` → `GrupoNutricional` | El borrado en cascada del menú no debe arrastrar el ítem ni el nomenclador que la fila referencia (RR-22). |
| Cobertura y restricciones de una dieta | `CubreDieta` → `GrupoNutricional`; `Restringe` → `RestricciónAlimentaria` | Lo mismo del lado de la dieta: su borrado en cascada no arrastra los nomencladores (RR-12). |
| Entidades base y sus nomencladores | `Dieta` → `ProgramaDeAtención`; `Nutricionista` → `Especialidad`; `Paciente` → `Sala` | No se borra un valor de nomenclador en uso (RR-12). |
| Autorizaciones e inscripciones | `Autoriza` → `Nutricionista`, `Dieta`; `Inscribe` → `Paciente`, `Dieta` | No se borra una dieta que figure en alguna autorización o inscripción (RR-12), ni el nutricionista o el paciente del vínculo (RR-22). |
| Exclusión por restricción | `Excluye` → `RestricciónAlimentaria`, `GrupoNutricional` | RR-21. |
| Origen del menú | `Menú` → `Dieta`, `Nutricionista` creador | El menú documenta quién lo creó y para qué dieta. |
| Ciclo de revisión y servicio | `Menú` → `Validación`; `Validación` → `Nutricionista` revisor; `Menú` → `Asignación`; `Asignación` → `Paciente`; `Asignación` → `Servicio`; `Servicio` → `Consumo`; `Consumo` → `Incluye` | Un menú dictaminado, asignado o servido no se borra: su historial clínico depende de él (RR-19). La última impide además quitar del menú un ítem que ya tiene consumos. |
| Menú de reemplazo | `Menú` → `Validación` de origen | No se borra la validación que originó otro menú: se perdería la traza del rechazo. |
| Valoración | `Valoración` → `Asignación`, `Nutricionista` emisor, `ResultadoNutricional` | La valoración documenta un hecho clínico sobre una asignación concreta (RR-19). |
| Revalorización | `Revalorización` → `Nutricionista` ejecutor, `Valoración` revisada, `Valoración` generada | No se borra una valoración que una revalorización revisa o generó (RR-19). |

Ninguna fila de las dos tablas contradice a la otra: protegen claves foráneas distintas. En particular, que `Menú → Incluye` sea `CASCADE` no choca con que `Menú → Asignación` sea `RESTRICT`, porque un menú asignado no llega a borrarse.

---

### 4.5 Lo que deliberadamente no se restringe

| Condición que se admite |
|---|
| La clasificación de un plato no se deriva de sus ingredientes: un plato de nivel calórico bajo puede contener alimentos de nivel alto. |
| Un alimento puede no formar parte de ningún plato, y un valor de nomenclador puede no estar en uso. |
| La composición tiene un solo nivel: un plato se compone de alimentos, no de otros platos. |
| Una dieta puede no declarar ninguna restricción alimentaria. |
| Un paciente puede no estar inscrito en ninguna dieta y un nutricionista puede no estar autorizado en ninguna. |
| Los nombres de las entidades no son únicos: la unicidad de nombre solo se exige en los nomencladores (RD-08). |
| `Especialidad` no se deriva de `Autoriza`: un nutricionista puede estar autorizado en una dieta ajena a su especialidad. |
| El traslado de un paciente entre salas actualiza la fila del paciente, sin historial. |
| La composición real de un menú no se contrasta con su parametrización. |
| El estado final de un menú se deduce de su validación más reciente; no se almacena. |
| No hay fechas en la asignación, el consumo ni la valoración: el ancla temporal es `Menú.FechaCreación`. |
| Una asignación puede no tener consumos ni valoraciones. |
| El consumo no registra cantidades, solo la aceptación por alimento. |
| La correspondencia entre las restricciones alimentarias de una dieta y los alérgenos de sus ítems no se modela. La compatibilidad por grupo nutricional sí está en el esquema (`Restringe` y `Excluye`); la compatibilidad por alérgeno la comprueba la capa de aplicación contra la ficha nutricional, y no es una restricción de integridad: la base no impide el menú, lo advierte el sistema. Saber qué alérgeno excluye cada restricción es configuración de la aplicación (§5.5). |
| Un menú puede incluir ítems de grupos prohibidos por su dieta. Que no los incluya es un **criterio de evaluación**, no una restricción, y la consulta 5 lo verifica. La compatibilidad alimentaria se comprueba en dos niveles, y ninguno de los dos es una restricción de integridad: por **grupo nutricional**, que el modelo relacional resuelve con `Restringe` y `Excluye` sin consultar la ficha; y por **alérgeno**, que la capa de aplicación contrasta contra la ficha nutricional al generar o validar un menú, para advertir al nutricionista (fila anterior). Imponer el primero en la base, además, vaciaría la consulta 5: su propósito es verificar si los criterios de equilibrio fueron cumplidos, y una de sus comprobaciones es precisamente que ningún ítem del menú pertenezca a un grupo prohibido por su dieta. Si la base impidiera esos menús, la comprobación daría siempre el mismo resultado y la consulta no tendría nada que detectar. El control queda en tres filtros previos al paciente: la generación automática no incluye grupos prohibidos, el revisor lo comprueba antes de aprobar y la consulta 5 lo reporta. |

---


## 5. Valoración de la corrección del diseño de la base de datos

Un diseño relacional se considera teóricamente correcto cuando cumple tres condiciones: cada esquema de la descomposición alcanza **al menos** la Tercera Forma Normal (3FN), se preserva el conjunto completo de dependencias funcionales del dominio (PPDF) y la reconstrucción por join natural no introduce tuplas espurias (PLJ).

La Forma Normal de Boyce-Codd no forma parte del criterio: es una condición más fuerte que la 3FN y no siempre es alcanzable junto con la PPDF, de modo que exigirla descartaría diseños correctos. Aquí se verifica al final, esquema por esquema, como propiedad adicional.

### 5.1 Conjunto de dependencias funcionales extraído del MERX

El punto de partida no es el cubrimiento minimal, sino el conjunto **F** de dependencias funcionales que se lee directamente del modelo conceptual. El cubrimiento minimal se obtiene después, aplicando el algoritmo a F (§5.2).

#### 5.1.1 Reglas de extracción

1. **Conjuntos de entidades.** Por cada entidad con clave K y atributos U, se añade `K → U`.
2. **Entidades débiles.** Su clave es la de la entidad de la que dependen más su llave parcial. `Servicio` no tiene atributos fuera de esa clave, de modo que no aporta ninguna dependencia no trivial.
3. **Especialización.** El discriminador de una especialización es un atributo de la superclase, aunque en el diagrama se dibuje sobre la propia especialización y no como un óvalo. El de `ItemAlimentario` se llama `Clase`, distingue las dos subclases y aparece en F como el atributo `clase`, determinado por `id_item`. Las subclases no aportan dependencias propias, porque no tienen atributos que la superclase no tenga: lo que las distingue es la relación `Compone`, en la que un plato se compone de alimentos simples.
4. **Interrelaciones con atributos propios.** Por cada una con clave compuesta K, se añade `K → atributos`.
5. **Cardinalidad máxima 1.** Por cada extremo con cardinalidad máxima 1 se añade la dependencia del lado «muchos» hacia el lado «uno». Cuando el extremo 1 es obligatorio en ambos lados, la dependencia vale en los dos sentidos.
6. **Unicidades del dominio.** Las claves candidatas declaradas con `UNIQUE` aportan la dependencia inversa: el nombre de cada nomenclador y el de `ResultadoNutricional`, su `Valor`, el nombre normalizado de los ítems y las dos relaciones 1:1 opcionales.

#### 5.1.2 Nombres de rol

La teoría de la normalización trabaja sobre un **esquema universal** R(U, F), y eso exige que un mismo nombre de atributo signifique lo mismo en todo U. En este modelo hay identificadores que aparecen con papeles distintos: `id_nutricionista` es a la vez quien describe un ítem, quien crea un menú, quien lo revisa, quien emite una valoración, quien ejecuta una revalorización y quien está autorizado en una dieta. Sin distinguirlos, el algoritmo deduciría dependencias falsas, por ejemplo que el nutricionista que describió un alimento es el que validó el menú que lo contiene.

Por eso, en U cada aparición con un papel distinto es un **atributo distinto**, y se denota con un sufijo de rol:

| Identificador | Papeles en U |
|---|---|
| `id_nutricionista` | `_descriptor` (ítem), `_creador` (menú), `_revisor` (validación), `_emisor` (valoración), `_ejecutor` (revalorización), `_autorizado` (`Autoriza`) |
| `id_grupo` | `_item` (clasificación), `_cubierto` (`CubreDieta`), `_cubierto_menu` (`CubreMenú`), `_excluido` (`Excluye`) |
| `id_dieta` | `_menu` (menú), `_cubierta`, `_restringida`, `_autorizada`, `_inscrita` |
| `id_menu` | `_origen` (`Origina`), `_distribuido`, `_incluyente`, `_cubierto`, `_asignado`, `_servido`, `_valorado` |
| `id_paciente` | `_inscrito`, `_asignado`, `_servido`, `_valorado` |
| `id_item` | `_incluido` (`Incluye`), `_consumido` (`Consume`); en `Compone`, `id_plato_compuesto` e `id_alimento_componente` |
| `id_tipo` | `_item` (clasificación), `_distribuido` (`Distribuye`) |
| `id_valoracion` | `_revisada`, `_generada` (revalorización) |

#### 5.1.3 El conjunto F

**De las entidades:**

```
id_item            → clase, nombre_item, id_grupo_item, id_tipo_item,
                     id_nivel_item, id_nutricionista_descriptor
id_grupo           → nombre_grupo
id_tipo            → nombre_tipo
id_nivel           → nombre_nivel
id_programa        → nombre_programa
id_restriccion     → nombre_restriccion
id_especialidad    → nombre_especialidad
id_resultado       → nombre_resultado, valor_resultado
id_sala            → nombre_sala
id_dieta           → nombre_dieta, id_programa_dieta
id_nutricionista   → nombre_nutricionista, id_especialidad_nutricionista
id_paciente        → nombre_paciente, edad, id_sala_paciente
id_menu            → fecha_creacion, cantidad_total_alimentos, es_automatico,
                     id_dieta_menu, id_nutricionista_creador, id_menu_origen
id_valoracion      → id_menu_valorado, id_paciente_valorado,
                     id_nutricionista_emisor, id_resultado_valoracion,
                     fecha_valoracion
id_revalorizacion  → fecha_solicitud, id_nutricionista_ejecutor,
                     id_valoracion_revisada, id_valoracion_generada
```

**De las interrelaciones con atributos propios:**

```
(id_plato_compuesto, id_alimento_componente)                → cantidad
(id_menu_distribuido, id_tipo_distribuido)                  → proporcion
(id_dieta_cubierta, id_grupo_cubierto)                      → tipo
(id_menu_servido, id_paciente_servido, fecha_servicio,
 id_item_consumido)                                         → aceptacion
id_menu  → id_nutricionista_revisor, fecha_validacion, aprobado, observaciones
```

La última merece atención: la clave de `Validación` **es el menú**, porque un menú recibe a lo sumo un dictamen (RC-12). Su parte izquierda coincide con la de la entidad `Menú`, y eso tiene consecuencias en la síntesis (§5.3.1).

**De las unicidades del dominio:**

```
nombre_grupo          → id_grupo
nombre_tipo           → id_tipo
nombre_nivel          → id_nivel
nombre_programa       → id_programa
nombre_restriccion    → id_restriccion
nombre_especialidad   → id_especialidad
nombre_resultado      → id_resultado
valor_resultado       → id_resultado
nombre_item           → id_item
id_valoracion_generada → id_revalorizacion
id_menu_origen         → id_menu
```

> Las dos últimas provienen de relaciones 1:1 **opcionales**: una revalorización puede no haber generado todavía su valoración, y un menú puede no ser reemplazo de ninguno. La dependencia se cumple sobre las filas donde el atributo está definido, que es lo que garantiza el `UNIQUE` sobre una columna que admite nulos. No afecta al resultado de la síntesis, porque en ambos casos la parte izquierda es equivalente a una clave que ya existe.

**Lo que no aporta dependencias no triviales.** Ocho interrelaciones no tienen atributos propios, y su única dependencia es la trivial K → K: `Autoriza`, `Inscribe`, `Restringe`, `Excluye`, `Incluye`, `CubreMenú`, `Asignación` y, con ella, la entidad débil `Servicio`. En §5.3.2 se explica cómo entran sus atributos en la descomposición, porque **no pueden salir del algoritmo**: una dependencia trivial no forma parte de un cubrimiento minimal.

---

### 5.2 Cubrimiento minimal

Un cubrimiento minimal G de F cumple tres condiciones: toda parte derecha es un único atributo, ninguna parte izquierda tiene atributos extraños y ninguna dependencia es redundante. Se obtiene aplicando los tres pasos en ese orden.

#### 5.2.1 Paso 1 — partes derechas unitarias

Cada dependencia con varios atributos a la derecha se descompone por la regla de descomposición. Por ejemplo:

```
id_paciente → nombre_paciente, edad, id_sala_paciente
```

se convierte en

```
id_paciente → nombre_paciente
id_paciente → edad
id_paciente → id_sala_paciente
```

Aplicado a todo F:

| Dependencia original | DF unitarias |
|---|---|
| `id_item → …` | 6 |
| `id_menu → …` (entidad) | 6 |
| `id_menu → …` (validación) | 4 |
| `id_valoracion → …` | 5 |
| `id_revalorizacion → …` | 4 |
| `id_paciente → …` | 3 |
| `id_dieta → …`, `id_nutricionista → …`, `id_resultado → …` | 2 cada una |
| Las 7 restantes de entidad, las 4 de interrelación y las 11 de unicidad | 1 cada una |

El conjunto queda con **56 dependencias de parte derecha unitaria**.

#### 5.2.2 Paso 2 — atributos extraños en la parte izquierda

Solo puede haberlos en las partes izquierdas compuestas, que son cuatro. Para cada una se calcula el cierre de cada subconjunto propio respecto de F y se comprueba que no contiene la parte derecha.

**`(id_plato_compuesto, id_alimento_componente) → cantidad`**

```
{id_plato_compuesto}⁺    = {id_plato_compuesto}          ∌ cantidad
{id_alimento_componente}⁺ = {id_alimento_componente}     ∌ cantidad
```

Ninguno de los dos basta: un plato tiene varios ingredientes con cantidades distintas, y un alimento aparece en varios platos con cantidades distintas. **No hay atributos extraños.**

**`(id_menu_distribuido, id_tipo_distribuido) → proporcion`** y **`(id_dieta_cubierta, id_grupo_cubierto) → tipo`** se resuelven igual: cada parte izquierda por separado deja la parte derecha fuera de su cierre, porque un menú tiene una proporción por cada tipo de preparación y una dieta califica varios grupos.

**`(id_menu_servido, id_paciente_servido, fecha_servicio, id_item_consumido) → aceptacion`**

Hay que descartar los cuatro subconjuntos de tres atributos:

```
{id_menu_servido, id_paciente_servido, fecha_servicio}⁺  ∌ aceptacion
    el servicio registra la aceptación de cada ítem por separado
{id_menu_servido, id_paciente_servido, id_item_consumido}⁺ ∌ aceptacion
    el mismo ítem puede aceptarse un día y rechazarse otro
{id_menu_servido, fecha_servicio, id_item_consumido}⁺    ∌ aceptacion
    dos pacientes pueden responder distinto ante el mismo ítem
{id_paciente_servido, fecha_servicio, id_item_consumido}⁺ ∌ aceptacion
    el paciente puede recibir dos menús el mismo día
```

**No hay atributos extraños en ninguna de las cuatro.**

#### 5.2.3 Paso 3 — dependencias redundantes

Una dependencia `X → A` es redundante si `A ∈ X⁺` calculado respecto de `G − {X → A}`. Las dependencias de entidad no lo son: su parte izquierda es la única clave que determina esos atributos, y quitarlas dejaría el atributo sin determinante.

Los casos que sí exigen comprobación son los de unicidad, porque conviven con la dependencia inversa:

```
G − {nombre_grupo → id_grupo}:   {nombre_grupo}⁺ = {nombre_grupo}  ∌ id_grupo
```

El nombre no determina nada más por otra vía, así que **no es redundante**. El mismo cálculo vale para los demás nomencladores, para `nombre_item` y para `valor_resultado`. En `ResultadoNutricional` conviven `nombre_resultado → id_resultado` y `valor_resultado → id_resultado`: ninguna deriva de la otra, porque de `nombre_resultado` no se alcanza `valor_resultado` sin pasar por `id_resultado`, y viceversa. Las dos se conservan, y la relación queda con tres claves candidatas.

El caso de la **revalorización** merece mención aparte, porque en el diseño anterior sí había una redundancia aquí. La restricción RS-09 obligaba a que el paciente solicitante fuera el de la valoración revisada, lo que introducía `id_valoracion_revisada → id_paciente_solicitante`: una dependencia transitiva sobre un determinante que no es clave, que dejaba la relación en 2FN. Al dejar de almacenarse el solicitante, esa dependencia desaparece de F y la restricción se cumple por construcción.

Ninguna otra dependencia resulta redundante. **G tiene las mismas 56 dependencias que salieron del paso 1.**

#### 5.2.4 El cubrimiento minimal G

Se presenta agrupado por parte izquierda, solo para abreviar: cada línea representa tantas dependencias de parte derecha unitaria como atributos tenga a la derecha.

```
id_item            → clase | nombre_item | id_grupo_item | id_tipo_item
                   | id_nivel_item | id_nutricionista_descriptor
nombre_item        → id_item
id_grupo           → nombre_grupo          nombre_grupo        → id_grupo
id_tipo            → nombre_tipo           nombre_tipo         → id_tipo
id_nivel           → nombre_nivel          nombre_nivel        → id_nivel
id_programa        → nombre_programa       nombre_programa     → id_programa
id_restriccion     → nombre_restriccion    nombre_restriccion  → id_restriccion
id_especialidad    → nombre_especialidad   nombre_especialidad → id_especialidad
id_resultado       → nombre_resultado | valor_resultado
nombre_resultado   → id_resultado          valor_resultado     → id_resultado
id_sala            → nombre_sala
id_dieta           → nombre_dieta | id_programa_dieta
id_nutricionista   → nombre_nutricionista | id_especialidad_nutricionista
id_paciente        → nombre_paciente | edad | id_sala_paciente
id_menu            → fecha_creacion | cantidad_total_alimentos | es_automatico
                   | id_dieta_menu | id_nutricionista_creador | id_menu_origen
                   | id_nutricionista_revisor | fecha_validacion | aprobado
                   | observaciones
id_menu_origen     → id_menu
id_valoracion      → id_menu_valorado | id_paciente_valorado
                   | id_nutricionista_emisor | id_resultado_valoracion
                   | fecha_valoracion
id_revalorizacion  → fecha_solicitud | id_nutricionista_ejecutor
                   | id_valoracion_revisada | id_valoracion_generada
id_valoracion_generada → id_revalorizacion
(id_plato_compuesto, id_alimento_componente) → cantidad
(id_menu_distribuido, id_tipo_distribuido)   → proporcion
(id_dieta_cubierta, id_grupo_cubierto)       → tipo
(id_menu_servido, id_paciente_servido, fecha_servicio, id_item_consumido)
                                             → aceptacion
```

---

### 5.3 Descomposición por el algoritmo de síntesis

El algoritmo de síntesis construye la descomposición a partir de **G**, no del cierre F\*: agrupa las dependencias de G que comparten parte izquierda, o cuyas partes izquierdas son equivalentes, y crea un esquema por grupo. Formalmente, para cada grupo `X → A₁, …, X → Aₖ` se crea `Rᵢ(Uᵢ, Fᵢ)` con `Uᵢ = X ∪ {A₁, …, Aₖ}` y `Fᵢ = Π_{Uᵢ}(G)`. Por último se eliminan los esquemas contenidos en otro.

#### 5.3.1 Esquemas obtenidos de G

Dos partes izquierdas son **equivalentes** cuando cada una determina a la otra, y en ese caso sus dependencias van al mismo esquema. Es lo que ocurre en los nomencladores, donde `id` y `nombre` se determinan mutuamente: no generan tablas separadas, sino una sola con dos claves candidatas.

| Esquema | Atributos | Claves candidatas |
|---|---|---|
| `ItemAlimentario` | id_item, clase, nombre_item, id_grupo_item, id_tipo_item, id_nivel_item, id_nutricionista_descriptor | id_item, nombre_item |
| `GrupoNutricional` | id_grupo, nombre_grupo | las dos |
| `TipoPreparación` | id_tipo, nombre_tipo | las dos |
| `NivelCalórico` | id_nivel, nombre_nivel | las dos |
| `ProgramaDeAtención` | id_programa, nombre_programa | las dos |
| `RestricciónAlimentaria` | id_restriccion, nombre_restriccion | las dos |
| `Especialidad` | id_especialidad, nombre_especialidad | las dos |
| `ResultadoNutricional` | id_resultado, nombre_resultado, valor_resultado | las tres |
| `Sala` | id_sala, nombre_sala | id_sala |
| `Dieta` | id_dieta, nombre_dieta, id_programa_dieta | id_dieta |
| `Nutricionista` | id_nutricionista, nombre_nutricionista, id_especialidad_nutricionista | id_nutricionista |
| `Paciente` | id_paciente, nombre_paciente, edad, id_sala_paciente | id_paciente |
| `Menú` | id_menu, fecha_creacion, cantidad_total_alimentos, es_automatico, id_dieta_menu, id_nutricionista_creador, id_menu_origen, id_nutricionista_revisor, fecha_validacion, aprobado, observaciones | id_menu, id_menu_origen |
| `Valoración` | id_valoracion, id_menu_valorado, id_paciente_valorado, id_nutricionista_emisor, id_resultado_valoracion, fecha_valoracion | id_valoracion |
| `Revalorización` | id_revalorizacion, fecha_solicitud, id_nutricionista_ejecutor, id_valoracion_revisada, id_valoracion_generada | id_revalorizacion, id_valoracion_generada |
| `Composición` | id_plato_compuesto, id_alimento_componente, cantidad | el par |
| `Distribución` | id_menu_distribuido, id_tipo_distribuido, proporcion | el par |
| `CubreDieta` | id_dieta_cubierta, id_grupo_cubierto, tipo | el par |
| `Consumo` | id_menu_servido, id_paciente_servido, fecha_servicio, id_item_consumido, aceptacion | la cuaterna |

Son **19 esquemas**.

**La partición de `Menú` y `Validación`.** El algoritmo los une en un solo esquema, porque la clave de la validación es el propio menú. Esa unión es correcta pero indeseable: un menú pendiente no tiene dictamen, de modo que `id_nutricionista_revisor`, `fecha_validacion`, `aprobado` y `observaciones` quedarían nulos en todas las filas de menús sin revisar. Se aplica por eso una **partición vertical por la clave**:

```
Menú      (id_menu, fecha_creacion, cantidad_total_alimentos, es_automatico,
           id_dieta_menu, id_nutricionista_creador, id_menu_origen)
Validación(id_menu, id_nutricionista_revisor, fecha_validacion, aprobado,
           observaciones)
```

La partición **no pierde información ni dependencias**: la intersección de los dos esquemas es `id_menu`, que es clave de ambos, de modo que el join natural los reconstruye exactamente (condición suficiente de la PLJ); y cada dependencia de G sigue contenida en uno de los dos, así que la PPDF tampoco se ve afectada. Lo que se gana es que la ausencia de dictamen se representa por **ausencia de fila**, no por nulos, y que el estado del menú se deriva sin ambigüedad: pendiente si no hay fila, final si la hay con `aprobado`, rechazado si la hay sin él.

Con la partición son **20 esquemas**.

#### 5.3.2 Las interrelaciones sin dependencias no triviales

Las ocho interrelaciones de §5.1.3 no aparecen en ningún esquema de los anteriores, y **no pueden aparecer**: sus únicas dependencias son triviales y un cubrimiento minimal no las contiene. Afirmar que «generan dependencias K → K» no las justifica; hay que explicar por dónde entran.

Entran por el esquema universal. **U contiene todos los atributos del MERX**, incluidos los pares de esas interrelaciones con sus nombres de rol. Ninguno de esos atributos aparece en la parte derecha de ninguna dependencia de G: nada determina qué dieta está autorizada a qué nutricionista, ni qué ítem incluye qué menú. Por el criterio del cálculo de claves, **un atributo que no figura en ninguna parte derecha pertenece a toda clave de U**, de modo que todos ellos están en la clave, y es el último paso del algoritmo el que los incorpora (§5.3.3).

#### 5.3.3 El esquema de la llave y la descomposición final

El algoritmo de síntesis termina comprobando si algún `Rᵢ` contiene una clave candidata de U. Si ninguno la contiene, se añade un esquema formado por esa clave.

**Cálculo de la clave.** Se parte de los atributos que no aparecen en ninguna parte derecha de G, que pertenecen a toda clave:

```
K = { id_nutricionista_autorizado, id_dieta_autorizada,
      id_paciente_inscrito,        id_dieta_inscrita,
      id_dieta_restringida,        id_restriccion_dieta,
      id_restriccion_excluyente,   id_grupo_excluido,
      id_menu_incluyente,          id_item_incluido,
      id_menu_cubierto,            id_grupo_cubierto_menu,
      id_menu_asignado,            id_paciente_asignado,
      id_menu_servido,  id_paciente_servido,  fecha_servicio,
      id_item_consumido,
      id_plato_compuesto,          id_alimento_componente,
      id_menu_distribuido,         id_tipo_distribuido,
      id_dieta_cubierta,           id_grupo_cubierto,
      id_item, id_menu, id_valoracion, id_revalorizacion,
      id_dieta, id_nutricionista, id_paciente, id_sala,
      id_grupo, id_tipo, id_nivel, id_programa,
      id_restriccion, id_especialidad, id_resultado }
```

`K⁺ = U`: los identificadores de entidad arrastran todos sus atributos, y los de interrelación ya están en K. Y K es **mínima**, porque ninguno de sus atributos se deriva de los demás: quitar cualquiera lo deja fuera del cierre. Es, por tanto, una clave candidata de U.

Obsérvese que la clave es un **conjunto de atributos**, no un conjunto de pares: los pares son las claves de cada interrelación por separado, no la del esquema universal.

**Ningún `Rᵢ` contiene K**, así que se añade `R_K(K)`. Ese esquema es *todo clave* y, al no proyectarse sobre él ninguna dependencia no trivial, está en BCNF. Pero reúne hechos independientes entre sí: qué nutricionista está autorizado en qué dieta nada tiene que ver con qué ítem compone qué plato.

**Descomposición de `R_K`.** Esa independencia se expresa con **dependencias multivaluadas**: dentro de `R_K` se cumple `∅ ↠ {id_nutricionista_autorizado, id_dieta_autorizada}`, y una análoga por cada interrelación. Por el teorema de Fagin, una dependencia multivaluada no trivial permite descomponer sin pérdida de información, y aplicándolo sucesivamente `R_K` se separa en un esquema por cada hecho independiente:

| Esquema | Atributos |
|---|---|
| `Autoriza` | id_nutricionista_autorizado, id_dieta_autorizada |
| `Inscribe` | id_paciente_inscrito, id_dieta_inscrita |
| `Restringe` | id_dieta_restringida, id_restriccion_dieta |
| `Excluye` | id_restriccion_excluyente, id_grupo_excluido |
| `Incluye` | id_menu_incluyente, id_item_incluido |
| `CubreMenú` | id_menu_cubierto, id_grupo_cubierto_menu |
| `Asignación` | id_menu_asignado, id_paciente_asignado |
| `Servicio` | id_menu_servido, id_paciente_servido, fecha_servicio |

Los demás fragmentos de `R_K` quedan **contenidos** en esquemas ya obtenidos —los pares de `Composición`, `Distribución` y `CubreDieta`, la cuaterna de `Consumo` y los identificadores de entidad— y el último paso del algoritmo los elimina por redundantes. Cada uno de los ocho esquemas resultantes es todo clave y está en BCNF.

**La descomposición final** es ρ = {los 20 esquemas de §5.3.1} ∪ {los 8 de este apartado} = **28 esquemas**, que son exactamente las 28 tablas del esquema implementado.

---

### 5.4 Propiedades de la descomposición

#### 5.4.1 Preservación de dependencias funcionales (PPDF)

La síntesis **garantiza la PPDF por construcción**. Cada dependencia `X → A` de G dio origen a un esquema `Rᵢ` con `X ∪ {A} ⊆ Uᵢ`, de modo que `X → A ∈ Π_{Uᵢ}(G)`. Por tanto:

```
G ⊆ ∪ᵢ Π_{Rᵢ}(G)   y, como G es un cubrimiento de F,
F ≡ ∪ᵢ Π_{Rᵢ}(G)
```

Ninguna dependencia del dominio quedó repartida entre dos esquemas, así que toda comprobación de integridad asociada a una dependencia funcional se hace localmente, sin join. Los dos pasos posteriores lo conservan: la partición de `Menú` y `Validación` deja cada dependencia dentro de uno de los dos esquemas, y la descomposición de `R_K` solo afecta a atributos sobre los que no se proyecta ninguna dependencia no trivial.

#### 5.4.2 Join sin pérdida (PLJ)

La síntesis **no** garantiza por sí sola la PLJ: hay que añadir un esquema que contenga una clave candidata del esquema universal. Es el resultado que justifica el paso de §5.3.3, y el que se conoce como lema de Ullman:

> Si una descomposición ρ de R(U, F) preserva las dependencias y alguno de sus esquemas contiene una clave candidata de R, entonces ρ tiene la propiedad del join sin pérdida.

Las dos condiciones se cumplen: la PPDF quedó demostrada en §5.4.1, y la clave K calculada en §5.3.3 está contenida en `R_K`. La descomposición σ = ρ ∪ {R_K} tiene, por tanto, la PLJ.

El último paso descompone `R_K` por dependencias multivaluadas, y esa descomposición también es sin pérdida por el teorema de Fagin. La composición de dos descomposiciones sin pérdida es sin pérdida, de modo que la propiedad se conserva en la descomposición final de 28 esquemas.

#### 5.4.3 Verificación adicional: BCNF

La BCNF no forma parte del criterio de corrección, pero se comprueba esquema por esquema porque este diseño la alcanza. Un esquema está en BCNF si en toda dependencia funcional no trivial de `Π_{Rᵢ}(G)` la parte izquierda es superclave.

| Grupo | Verificación |
|---|---|
| Los 8 esquemas todo clave de §5.3.3 | Sobre ellos no se proyecta ninguna dependencia no trivial. **BCNF** |
| `Composición`, `Distribución`, `CubreDieta`, `Consumo` | Su único determinante es la clave compuesta completa, como mostró §5.2.2 al descartar los atributos extraños. **BCNF** |
| Los 7 nomencladores y `ResultadoNutricional` | Dos o tres determinantes, y todos son claves candidatas. **BCNF** |
| `Sala`, `Dieta`, `Nutricionista`, `Paciente`, `Valoración` | Único determinante, la clave primaria. **BCNF** |
| `ItemAlimentario` | Dos determinantes, `id_item` y `nombre_item`, los dos claves candidatas. **BCNF** |
| `Menú` y `Validación` | En `Menú`, `id_menu` e `id_menu_origen`, ambos claves. En `Validación`, solo `id_menu`. **BCNF** |
| `Revalorización` | `id_revalorizacion` e `id_valoracion_generada`, ambos claves candidatas. **BCNF** |

**Los 28 esquemas están en BCNF**, que es una condición más fuerte que la 3FN exigida por el criterio. El diseño anterior tenía una excepción, `Revalorización` en 2FN por la dependencia transitiva que introducía el paciente solicitante; al dejar de almacenarse, la excepción desaparece.

---

### 5.5 Valoración final del diseño

#### 5.5.1 Completitud respecto a los requerimientos

El modelo cubre de forma integral los requerimientos funcionales, no funcionales e informacionales:

- Se gestiona el ciclo completo del ítem alimentario: ingreso, clasificación, composición de los platos y ficha nutricional ampliada.
- Se modelan las dietas con su cobertura y sus restricciones, y los menús con su parametrización de generación, su dictamen y, cuando procede, el menú al que reemplazan.
- Se registran las asignaciones, cada servicio de un menú a un paciente, los consumos por ítem en cada servicio, las valoraciones y las revalorizaciones.
- El esquema ofrece trayectoria de navegación directa para las seis consultas informacionales y para las dos recurrentes:

| Consulta | Trayectoria en el esquema |
|---|---|
| F1 — Menús automáticos por dieta | `Dieta` ← `Menú` (filtrado por `EsAutomático`) → `Nutricionista` · `FechaCreación` · `CantidadTotalAlimentos` · `Distribución` · `CubreMenú` |
| F2 — Ítems más usados en menús finales | `Dieta` ← `Menú` (con validación aprobada) → `Incluye` → `ItemAlimentario` → `NivelCalórico`, `GrupoNutricional`; los platos se descomponen por `Composición` |
| F3 — Menús validados por un revisor | `Nutricionista` (revisor) ← `Validación` (`FechaValidación`, `Aprobado`, `Observaciones`) → `Menú` → `Dieta`; y `Validación` → `Origina` → `Menú` de reemplazo |
| F4 — Desempeño ante un menú | `Menú` ← `Asignación` ← `Servicio` ← `Consumo` (`Aceptación`) → `ItemAlimentario` → `NivelCalórico` |
| F5 — Comparación entre dietas | `Dieta` ← `Menú` → `Incluye` → `ItemAlimentario` → `GrupoNutricional`, `NivelCalórico`; requeridos y restringidos: `CubreDieta.Tipo`; excluidos: `Restringe` → `Excluye`; parámetros: `Distribución`, `CantidadTotalAlimentos` |
| F6 — Correlación nivel calórico / resultado | (A) `Menú` → `Incluye` → `NivelCalórico`; `Asignación` ← `Valoración` → `ResultadoNutricional.Valor` · (B) `Consumo` → `ItemAlimentario` → `Nutricionista` descriptor; `Consumo` → `Servicio` → `Menú` → `Dieta` · (C) `Revalorización` → `Valoración` revisada → `Asignación` → `Paciente` → `Sala` |
| R1 — Disponibilidad calórica | `Dieta` → `Restringe` → `Excluye` y `Dieta` → `CubreDieta` (grupos prohibidos); `ItemAlimentario` → `NivelCalórico`; `Dieta` ← `Menú` final → `Incluye` → `ItemAlimentario` → `NivelCalórico` |
| R2 — Valoraciones más recientes | `Paciente` ← `Asignación` ← `Valoración` → `ResultadoNutricional`, ordenado por `FechaValoración` |
| RF-07 — Menús de la misma dieta | `Nutricionista` → `Autoriza` → `Dieta` ← `Menú` |

Los cálculos —tasas, promedios, correlaciones, el corte del top-10 y los conteos de disponibilidad— no están almacenados: se derivan en la consulta. RNF-01 se satisface sirviendo R1 y R2 desde el caché descrito en §2.2, sin que PostgreSQL deje de ser la fuente de verdad.

### 5.6 Flexibilidad y extensibilidad

El diseño presenta una estructura modular que facilita su evolución ante cambios del dominio nutricional. La independencia entre entidades base (alimentos, platos, grupos nutricionales) y las estructuras operativas (menús, dietas, consumos) permite incorporar nuevos tipos de alimento, criterios dietéticos o modalidades de valoración sin alterar las tablas existentes. La separación entre los parámetros de generación de un menú (`Distribucion`, `CubreMenu`) y su composición efectiva (`IncluyeAlimento`, `IncluyePlato`) garantiza que las reglas de negocio puedan ajustarse sin reestructurar el esquema. El uso de nomencladores abiertos para `GrupoNutricional` y `ProgramaDeAtencion` permite ampliar los catálogos sin cambios estructurales. La externalización de la ficha nutricional a MongoDB ofrece flexibilidad para extender los datos de nutrientes sin migraciones relacionales.

No quedan decisiones de modelado abiertas. Las cuatro que el diseño podía dejar en suspenso están fijadas: la `Cantidad` de la composición se expresa en gramos (RD-05); la `Proporción` de la distribución es una fracción en el intervalo (0, 1] y la suma de las de un menú es 1 (RD-11, RS-08); la `Edad` del paciente es un número entero de años mayor o igual que cero (RD-06, RD-09); y la correspondencia entre las restricciones de una dieta y los alérgenos de sus ítems queda deliberadamente fuera del modelo relacional (§4.5).

Esa última merece una palabra, porque es el punto de extensión más claro del diseño. La compatibilidad alimentaria se resuelve hoy en dos niveles: por grupo nutricional dentro de PostgreSQL, y por alérgeno en la capa de aplicación, que contrasta la ficha de cada ítem contra las restricciones de la dieta. Lo que el modelo relacional no registra es **qué alérgeno excluye cada restricción** —que *sin gluten* excluye *gluten*—, una correspondencia que hoy vive en la configuración de la aplicación. Llevarla al esquema, con un nomenclador de alérgenos y una relación hacia `RestricciónAlimentaria`, permitiría comprobar la compatibilidad por alérgeno sin salir del modelo. Es una extensión aditiva: no obliga a reestructurar ninguna tabla existente, porque el mecanismo por grupo seguiría funcionando igual.

## 6. Conclusiones

El informe recorre las cuatro etapas del diseño: análisis de los requerimientos funcionales, no funcionales e informacionales; modelo conceptual (MERX consolidado); especificación de 88 restricciones de integridad, clasificadas por su origen y con el mecanismo que las implementa; y valoración formal de la corrección.

El esquema relacional se obtuvo aplicando el algoritmo de síntesis al cubrimiento minimal extraído del modelo conceptual, y consta de **28 esquemas**. La descomposición cumple las tres condiciones del criterio de corrección: cada esquema alcanza al menos la 3FN, se preservan todas las dependencias funcionales del dominio —garantizado por construcción del algoritmo— y el join natural no introduce tuplas espurias, por contener la descomposición una clave candidata del esquema universal. La verificación adicional muestra que los 28 esquemas alcanzan además la Forma Normal de Boyce-Codd, condición más fuerte que la exigida.

La base de datos propuesta es, por tanto, estructuralmente correcta y está preparada para su implementación.
