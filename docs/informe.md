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

El trabajo sigue una metodología progresiva de cuatro etapas. En primer lugar, se analizan y reformulan los requerimientos funcionales e informacionales para precisar las operaciones que el sistema debe soportar y las consultas que debe responder. A partir de ese análisis se construye el Modelo Entidad-Relación Extendido (MERX) consolidado, que integra las entidades, atributos, relaciones y agregaciones del dominio en un único diagrama coherente. Sobre ese modelo se especifican las restricciones de integridad del esquema, clasificadas por su origen y declaradas mediante los mecanismos estándar del motor de base de datos. Finalmente, se verifica la corrección formal del diseño comprobando que el esquema satisface la Forma Normal de Boyce-Codd (BCNF), la Propiedad de Preservación de Dependencias Funcionales (PPDF) y la Propiedad del Join sin Pérdida de Información (PLJ).

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

Cada **alimento** y cada **plato** se clasifican por grupo nutricional, tipo de preparación y nivel calórico, y pueden contar con una **ficha nutricional ampliada** (macronutrientes, micronutrientes, alérgenos, modo de preparación) cuya estructura variable se gestiona de forma independiente en un almacén NoSQL.

Una **dieta** define los grupos nutricionales a cubrir durante el tratamiento (`CubreDieta`), las restricciones alimentarias a respetar (`Restringe`) y el programa de atención institucional al que corresponde. Los **menús** generados para una dieta almacenan los parámetros de generación como datos inmutables: cantidad total de alimentos y origen automático (`EsAutomático`) como atributos escalares, y la proporción por tipo de preparación y cobertura de grupos nutricionales como relaciones propias (`Distribución`, `CubreMenú`).

El **consumo** registra, para cada alimento de un menú asignado a un paciente, si fue aceptado o rechazado; su clave es el trío `(MenúId, PacienteId, AlimentoId)` y no incluye fecha propia. Las **valoraciones nutricionales** documentan el resultado del estado del paciente para un menú recibido, referenciando un valor del nomenclador `ResultadoNutricional`. Las **revalorizaciones** registran la solicitud virtual del paciente de revisión de una valoración previa, identificando el nutricionista ejecutante y, cuando ya fue atendida, la nueva valoración generada.

#### Decisiones de modelado relevantes

- **Plato como entidad separada** de Alimento (`Composición(Plato, Alimento, Cantidad)`). Un menú incluye alimentos y platos directamente; los alimentos de un plato se acceden vía `Composición`.
- **Jefe de nutrición** es un rol de Nutricionista, no una entidad. El estado del menú se deriva de la existencia de un registro aprobado en `Validación`.
- **Asignación** se modela como N:M (`Asignación(MenúId, PacienteId)`); la clave primaria compuesta garantiza la unicidad por par.
- **Compatibilidad alimentaria** se resuelve con dos relaciones independientes: `CubreDieta` (grupos requeridos) y `Restringe + Excluye` (grupos excluidos por restricción clínica).
- **Parámetros de generación**: `CantidadTotalAlimentos` y `EsAutomático` son escalares de Menú; proporción y cobertura son relaciones (`Distribución`, `CubreMenú`). `Cubre` se desambigua en `CubreDieta` y `CubreMenú`.
- **`ResultadoNutricional`** es un nomenclador con `Nombre` y `Valor` únicos, evitando texto libre y habilitando cálculo de promedios.
- **`Consumo`** no incluye fecha; su clave es `(MenúId, PacienteId, AlimentoId)` y el ancla temporal es `Menú.FechaCreación`.

**Catálogo de alimentos y platos**

| # | Requerimiento |
|---|---|
| RF-01 | El sistema permite registrar alimentos con nombre único, grupo nutricional, tipo de preparación, nivel calórico y nutricionista descritor. |
| RF-02 | El sistema impide el registro de alimentos con nombre duplicado (normalizado). |
| RF-03 | El sistema permite registrar platos como composiciones de alimentos con cantidades, clasificados con los mismos atributos que los alimentos. |
| RF-04 | El sistema permite almacenar fichas nutricionales de estructura variable (macronutrientes, micronutrientes, alérgenos, modo de preparación) en un almacén NoSQL referenciado por la clave del alimento o plato. |

**Usuarios y roles**

| # | Requerimiento |
|---|---|
| RF-05 | El sistema gestiona nutricionistas con especialidad y estado en línea, y pacientes con sala de hospitalización asignada. |
| RF-06 | El sistema controla qué nutricionistas están autorizados para crear y validar menús de cada dieta. |
| RF-07 | El sistema registra la inscripción de pacientes en dietas activas. |

**Dietas y menús**

| # | Requerimiento |
|---|---|
| RF-08 | El sistema permite definir dietas con programa de atención, grupos nutricionales a cubrir y restricciones alimentarias a respetar. |
| RF-09 | El sistema soporta la generación automática de menús aplicando parámetros inmutables: proporción por tipo de preparación, cobertura de grupos nutricionales y cantidad total de alimentos. |
| RF-10 | El sistema permite la confección manual de menús bajo criterios del jefe de nutrición. |
| RF-11 | El sistema registra la validación de un menú con fecha, dictamen (aprobado/rechazado) y observaciones, garantizando que el validador no sea el creador. |
| RF-12 | El sistema asigna menús validados a pacientes inscritos en la dieta correspondiente, impidiendo la asignación doble. |

**Registro clínico y valoraciones**

| # | Requerimiento |
|---|---|
| RF-13 | El sistema registra, para cada alimento de un menú asignado, si el paciente lo aceptó o rechazó. |
| RF-14 | El sistema registra valoraciones nutricionales del estado del paciente, referenciando un valor del nomenclador de resultados y el nutricionista emisor. |
| RF-15 | El sistema procesa solicitudes virtuales de revalorización, identificando el nutricionista ejecutante y la valoración revisada, y vincula la nueva valoración cuando se completa. |

### 2.2 Requerimientos informacionales

El sistema debe dar respuesta a seis consultas informacionales. A continuación se presenta el enunciado original de cada una y su reformulación enriquecida.

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

**Reformulación:** Dadas dos o más dietas, el sistema contrasta sus menús en tres dimensiones: (1) distribución porcentual de alimentos por grupo nutricional; (2) distribución por nivel calórico; (3) verificación de criterios de equilibrio en dos niveles: a nivel de dieta, si los grupos requeridos (`CubreDieta`) están presentes en el menú y los grupos excluidos por sus restricciones (`Restringe + Excluye`) están ausentes; a nivel de parámetros de generación, si las proporciones por tipo de preparación y la cantidad total de alimentos respetan lo declarado en `Distribución` y `CantidadTotalAlimentos`. Ningún resultado se almacena: proporciones, tasas y dictámenes de cumplimiento son todos derivados.

---

#### Consulta 6 — Correlación nivel calórico – resultado nutricional

> *«Determinar, para cada dieta, la correlación entre el nivel calórico de los alimentos y el resultado nutricional promedio de los pacientes. La consulta debe identificar los 10 alimentos con la tasa de rechazo más alta, el nutricionista que los creó y la dieta a la que pertenecen. Además, debe comparar el desempeño nutricional de los pacientes que solicitaron una revalorización con el promedio general de valoraciones de sus respectivas salas para esas mismas dietas.»*

**Reformulación:** La consulta tiene tres partes independientes:

- **Parte A — Correlación por dieta:** para cada dieta, une dos ramas del modelo: la rama alimentaria (dieta → menús → alimentos con nivel calórico) y la rama clínica (pacientes con valoraciones ancladas a esos menús). La unión se hace por el par `(MenúId, PacienteId)` de la asignación; el resultado es el promedio del valor numérico de `ResultadoNutricional` segmentado por nivel calórico, sin necesidad de fechas en `Valoración` ni en `Consumo` (el orden temporal se deriva de `Menú.FechaCreación`).

- **Parte B — Top-10 alimentos rechazados:** sobre todos los registros de `Consumo` del sistema, calcula la tasa de rechazo por alimento y selecciona los 10 con mayor tasa. Por cada uno reporta el nutricionista que lo **describió** en el banco (no el creador del menú) y la dieta a la que pertenece de forma indirecta, vía los menús en que aparece el consumo rechazado.

- **Parte C — Revalorizados frente al promedio de su sala:** para cada paciente que solicitó una revalorización, compara el valor numérico de la nueva valoración generada con el promedio de las valoraciones de los demás pacientes de la misma sala inscritos en la misma dieta. La dieta de referencia se obtiene de forma transitiva: `Revalorización → Valoración revisada → Menú → Dieta`.

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

> `Autoriza`, `Inscribe`, `CubreDieta` y `Restringe` son tablas de interconexión sin atributos propios: su identidad es el par de claves primarias de los participantes.

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

> Las cuatro restricciones de borrado marcadas con `ON DELETE RESTRICT` (RR-04, RR-12, RR-19 y RR-21) impiden borrar un hecho del que dependen otros. La política que las coordina entre submodelos se define en §4.4.

#### 4.1.3 Dominio

| # | Restricción |
|---|---|
| RD-01 | `TipoPreparación` toma exactamente los valores *entrante*, *plato fuerte*, *postre* y *bebida*. |
| RD-02 | `NivelCalórico` toma exactamente los valores *bajo*, *medio* y *alto*. |
| RD-03 | `GrupoNutricional` es un catálogo abierto; se exige únicamente que cada nombre sea único. |
| RD-04 | `Nombre` es obligatorio y no vacío en `Alimento`, `Plato` y los tres nomencladores. |
| RD-05 | `Cantidad` de `Composición` es estrictamente positiva. |
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
| RS-11 | Un grupo que una dieta exige cubrir (`CubreDieta`) no puede estar excluido por una de sus restricciones (`Restringe` seguido de `Excluye`). |

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
| RX-02 | Política de borrado entre submodelos: `RESTRICT` en todas las claves foráneas, salvo las que van del dueño a sus propias filas de composición o parametrización, que son `CASCADE`: `Plato → Composición`; `Menú → Distribución`, `CubreMenú`, `IncluyeAlimento` e `IncluyePlato`; `Dieta → CubreDieta` y `Restringe`. |

**Restricción 1 — por qué así:** un hecho del que dependen otros no se puede borrar (esta restricción amplía las políticas de borrado de §4.1.2). Las filas que solo describen a su dueño desaparecen con él, lo que permite borrar un menú nuevo sin validaciones ni asignaciones de forma completa. Con esta política, la inmutabilidad de la parametrización (RS-07) bloquea los cambios sueltos y deja pasar solo el borrado en cascada.

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
| Un menú puede contener alimentos de grupos excluidos por su dieta. |

---


## 5. Valoración de la corrección del diseño de la base de datos

Un diseño relacional se considera teóricamente correcto si y solo si cumple tres condiciones: cada esquema de la descomposición alcanza la Forma Normal de Boyce-Codd (BCNF), se preserva el conjunto completo de dependencias funcionales del dominio (PPDF) y la reconstrucción por join natural no introduce tuplas espurias (PLJ).

### 5.1 Demostración formal de normalización (BCNF), PPDF y PLJ

El flujo metodológico parte de la extracción del cubrimiento minimal a partir de la especificación conceptual, aplica el algoritmo de síntesis para obtener los 29 esquemas relacionales y verifica las tres propiedades sobre la descomposición resultante.

#### 5.1.1 Extracción del cubrimiento minimal

Aplicando el algoritmo de extracción a partir del MERX consolidado:

1. **Entidades:** por cada conjunto de entidades con atributos U y clave K, se añade K → U.
2. **Interrelaciones:** por cada interrelación con clave compuesta K y atributos propios, se añade K → atributos. Para cada entidad en un extremo de cardinalidad máxima 1, se añade K\_muchos → K\_uno.
3. **Reglas de negocio:** se incorporan las dependencias implícitas por unicidades adicionales del dominio (nombres de nomencladores, nombre normalizado de alimentos y platos, par valor–nombre de `ResultadoNutricional`).

#### 5.1.2 Cubrimiento minimal

```
id_alimento →
    nombre_alimento, id_grupo_nutricional, id_tipo_preparacion,
    id_nivel_calorico, id_nutricionista
id_plato →
    nombre_plato, id_grupo_nutricional, id_tipo_preparacion,
    id_nivel_calorico, id_nutricionista
id_grupo_nutricional → nombre_grupo
id_tipo_preparacion → nombre_tipo_preparacion
id_nivel_calorico → nombre_nivel_calorico
id_dieta → nombre_dieta, id_programa_atencion
id_programa_atencion → nombre_programa
id_restriccion_alimentaria → nombre_restriccion
id_especialidad → nombre_especialidad
id_resultado_nutricional → nombre_resultado, valor_resultado
id_sala → nombre_sala
id_nutricionista → nombre_nutricionista, id_especialidad
id_paciente → nombre_paciente, edad, id_sala
id_menu →
    nombre_menu, es_automatico, cant_total_alimentos,
    fecha_creacion, id_dieta, id_nutricionista_creador
id_valoracion →
    id_menu, id_paciente, id_nutricionista_emisor,
    id_resultado_nutricional, fecha_valoracion, observaciones
id_revalorizacion →
    id_paciente_solicitante, id_nutricionista_ejecutor,
    id_valoracion_revisada, id_valoracion_generada
(id_plato, id_alimento) → cantidad
(id_menu, id_tipo_preparacion) → proporcion
(id_nutricionista, id_menu) →
    fecha_validacion, aprobado, observaciones_validacion
(id_menu, id_paciente, id_alimento) → aceptacion
```

Las interrelaciones sin atributos propios — `Autoriza`, `Inscribe`, `CubreDieta`, `Restringe`, `Excluye`, `Asignacion`, `IncluyeAlimento`, `IncluyePlato` y `CubreMenu` — generan únicamente dependencias triviales de la forma K → K.

### 5.2 Descomposición formal en BCNF

La descomposición ρ se obtiene directamente del cubrimiento minimal: por cada dependencia X → Y en F*, se crea R\_i(U\_i, F\_i) con U\_i = X ∪ {Y} y F\_i = Π\_{R\_i}(F). Los esquemas resultantes son:

#### 5.2.1 Esquemas relacionales resultantes

- R1 = (U1, F1) — Alimento
  U1={id\_alimento, nombre\_alimento, id\_grupo\_nutricional, id\_tipo\_preparacion, id\_nivel\_calorico, id\_nutricionista}
  F1={id\_alimento → nombre\_alimento, id\_grupo\_nutricional, id\_tipo\_preparacion, id\_nivel\_calorico, id\_nutricionista}

- R2 = (U2, F2) — Plato
  U2={id\_plato, nombre\_plato, id\_grupo\_nutricional, id\_tipo\_preparacion, id\_nivel\_calorico, id\_nutricionista}
  F2={id\_plato → nombre\_plato, id\_grupo\_nutricional, id\_tipo\_preparacion, id\_nivel\_calorico, id\_nutricionista}

- R3 = (U3, F3) — GrupoNutricional
  U3={id\_grupo\_nutricional, nombre\_grupo}; F3={id\_grupo\_nutricional → nombre\_grupo}

- R4 = (U4, F4) — TipoPreparacion
  U4={id\_tipo\_preparacion, nombre\_tipo\_preparacion}; F4={id\_tipo\_preparacion → nombre\_tipo\_preparacion}

- R5 = (U5, F5) — NivelCalorico
  U5={id\_nivel\_calorico, nombre\_nivel\_calorico}; F5={id\_nivel\_calorico → nombre\_nivel\_calorico}

- R6 = (U6, F6) — Dieta
  U6={id\_dieta, nombre\_dieta, id\_programa\_atencion}; F6={id\_dieta → nombre\_dieta, id\_programa\_atencion}

- R7 = (U7, F7) — ProgramaDeAtencion
  U7={id\_programa\_atencion, nombre\_programa}; F7={id\_programa\_atencion → nombre\_programa}

- R8 = (U8, F8) — RestriccionAlimentaria
  U8={id\_restriccion\_alimentaria, nombre\_restriccion}; F8={id\_restriccion\_alimentaria → nombre\_restriccion}

- R9 = (U9, F9) — Especialidad
  U9={id\_especialidad, nombre\_especialidad}; F9={id\_especialidad → nombre\_especialidad}

- R10 = (U10, F10) — ResultadoNutricional
  U10={id\_resultado\_nutricional, nombre\_resultado, valor\_resultado}
  F10={id\_resultado\_nutricional → nombre\_resultado, valor\_resultado}

- R11 = (U11, F11) — Sala
  U11={id\_sala, nombre\_sala}; F11={id\_sala → nombre\_sala}

- R12 = (U12, F12) — Nutricionista
  U12={id\_nutricionista, nombre\_nutricionista, id\_especialidad}
  F12={id\_nutricionista → nombre\_nutricionista, id\_especialidad}

- R13 = (U13, F13) — Paciente
  U13={id\_paciente, nombre\_paciente, edad, id\_sala}; F13={id\_paciente → nombre\_paciente, edad, id\_sala}

- R14 = (U14, F14) — Menu
  U14={id\_menu, nombre\_menu, es\_automatico, cant\_total\_alimentos, fecha\_creacion, id\_dieta, id\_nutricionista\_creador}
  F14={id\_menu → nombre\_menu, es\_automatico, cant\_total\_alimentos, fecha\_creacion, id\_dieta, id\_nutricionista\_creador}

- R15 = (U15, F15) — Valoracion
  U15={id\_valoracion, id\_menu, id\_paciente, id\_nutricionista\_emisor, id\_resultado\_nutricional, fecha\_valoracion, observaciones}
  F15={id\_valoracion → id\_menu, id\_paciente, id\_nutricionista\_emisor, id\_resultado\_nutricional, fecha\_valoracion, observaciones}

- R16 = (U16, F16) — Revalorizacion
  U16={id\_revalorizacion, id\_paciente\_solicitante, id\_nutricionista\_ejecutor, id\_valoracion\_revisada, id\_valoracion\_generada}
  F16={id\_revalorizacion → id\_paciente\_solicitante, id\_nutricionista\_ejecutor, id\_valoracion\_revisada, id\_valoracion\_generada}

- R17 = (U17, F17) — Composicion
  U17={id\_plato, id\_alimento, cantidad}; F17={(id\_plato, id\_alimento) → cantidad}

- R18 = (U18, F18) — Distribucion
  U18={id\_menu, id\_tipo\_preparacion, proporcion}; F18={(id\_menu, id\_tipo\_preparacion) → proporcion}

- R19 = (U19, F19) — Validacion
  U19={id\_nutricionista, id\_menu, fecha\_validacion, aprobado, observaciones\_validacion}
  F19={(id\_nutricionista, id\_menu) → fecha\_validacion, aprobado, observaciones\_validacion}

- R20 = (U20, F20) — Consumo
  U20={id\_menu, id\_paciente, id\_alimento, aceptacion}; F20={(id\_menu, id\_paciente, id\_alimento) → aceptacion}

- R21 = (U21, F21) — Autoriza; U21={id\_nutricionista, id\_dieta}
- R22 = (U22, F22) — Inscribe; U22={id\_paciente, id\_dieta}
- R23 = (U23, F23) — CubreDieta; U23={id\_dieta, id\_grupo\_nutricional}
- R24 = (U24, F24) — Restringe; U24={id\_dieta, id\_restriccion\_alimentaria}
- R25 = (U25, F25) — Excluye; U25={id\_restriccion\_alimentaria, id\_grupo\_nutricional}
- R26 = (U26, F26) — Asignacion; U26={id\_menu, id\_paciente}
- R27 = (U27, F27) — IncluyeAlimento; U27={id\_menu, id\_alimento}
- R28 = (U28, F28) — IncluyePlato; U28={id\_menu, id\_plato}
- R29 = (U29, F29) — CubreMenu; U29={id\_menu, id\_grupo\_nutricional}

Luego, ρ = {R\_i}\_{1≤i≤29}, tal que cada R\_i(U\_i, F\_i) está en BCNF respecto a Π\_{R\_i}(F), ∀i, 1 ≤ i ≤ 29.

### 5.3 Garantía de cumplimiento de la PPDF y la PLJ

#### 5.3.1 Propiedad de Preservación de Dependencias Funcionales (PPDF)

Por construcción del algoritmo de síntesis, la PPDF se garantiza siempre. Puesto que cada dependencia funcional X → Y ∈ F* dio origen directo a un esquema relacional R\_i con U\_i = X ∪ {Y}, se cumple formalmente que:

F ≡ ∪\_{i=1}^{29} Π\_{R\_i}(F)

Ninguna dependencia del dominio fue fragmentada entre dos tablas distintas, por lo que toda verificación de integridad puede realizarse localmente sin necesidad de joins.

#### 5.3.2 Propiedad del Join sin Pérdida (PLJ) y Lema de Ullman

Para garantizar que el join natural sobre ρ no introduce tuplas espurias, se aplica el Lema de Ullman. La clave candidata global X del esquema universal R(U, F) se compone de la unión de las claves determinantes de los dominios independientes:

```
X = {id_alimento, id_plato, id_grupo_nutricional, id_tipo_preparacion,
     id_nivel_calorico, id_dieta, id_programa_atencion,
     id_restriccion_alimentaria, id_especialidad, id_resultado_nutricional,
     id_sala, id_nutricionista, id_paciente, id_menu,
     id_valoracion, id_revalorizacion,
     (id_plato,id_alimento), (id_menu,id_tipo_preparacion),
     (id_nutricionista,id_menu), (id_menu,id_paciente,id_alimento)}
```

Al añadir el esquema relacional R30(X), si X no estuviera contenido en ninguna tabla individual, la descomposición final σ = ρ ∪ {R30(X)} garantiza matemáticamente tanto la PPDF como la PLJ.

### 5.4 Valoración final del diseño

#### 5.4.1 Completitud respecto a los requerimientos

El modelo cubre de forma integral los requerimientos funcionales e informacionales del sistema:

- Se gestiona el ciclo completo del alimento: ingreso, clasificación, composición de platos y ficha nutricional en MongoDB.
- Se modelan las dietas con parámetros de cobertura y restricción, y los menús con su parametrización de generación e historial de validación.
- Se registran asignaciones, consumos individuales por alimento, valoraciones y revalorizaciones.
- El esquema ofrece trayectoria de navegación directa para cada una de las seis consultas requeridas:

| Consulta | Trayectoria en el esquema |
|---|---|
| F1 — Menús automáticos por dieta | `Dieta` ← `Menu` (filtrado por `EsAutomatico`) → `Nutricionista` · `FechaCreacion` · `CantTotalAlimentos` · `Distribucion` · `CubreMenu` |
| F2 — Alimentos más usados en menús finales | `Dieta` ← `Menu` (aprobado) → `IncluyeAlimento` → `Alimento` ∪ `IncluyePlato` → `Composicion` → `Alimento` → `NivelCalorico`, `GrupoNutricional` |
| F3 — Menús validados por un revisor | `Nutricionista` (revisor) ← `Validacion` (`FechaValidacion`, `Observaciones`, `Aprobado`) → `Menu` → `Dieta`; `Menu` → `Nutricionista` (creador) |
| F4 — Desempeño ante un menú | `Menu` ← `Asignacion` ← `Consumo` (`Aceptacion`) → `Alimento` → `NivelCalorico` |
| F5 — Comparación entre dietas | `Dieta` ← `Menu` → alimentos → `GrupoNutricional`, `NivelCalorico`; requeridos: `Dieta` → `CubreDieta`; excluidos: `Dieta` → `Restringe` → `Excluye` → `GrupoNutricional`; parámetros: `Distribucion`, `CantTotalAlimentos` |
| F6 — Correlación nivel calórico / resultado | (A) `Menu` → alimentos → `NivelCalorico`; `Asignacion` ← `Valoracion` → `ResultadoNutricional.Valor` · (B) `Consumo` → `Alimento` → `Nutricionista`; `Consumo` → `Menu` → `Dieta` · (C) `Revalorizacion` → `ValoRev` → `Valoracion` → `Paciente` → `Sala`; promedio por (sala, dieta) |

Los cálculos — tasas, promedios, correlaciones, el corte del top-10 — no están almacenados; se derivan en la consulta.

### 5.5 Flexibilidad y extensibilidad

El diseño presenta una estructura modular que facilita su evolución ante cambios del dominio nutricional. La independencia entre entidades base (alimentos, platos, grupos nutricionales) y las estructuras operativas (menús, dietas, consumos) permite incorporar nuevos tipos de alimento, criterios dietéticos o modalidades de valoración sin alterar las tablas existentes. La separación entre los parámetros de generación de un menú (`Distribucion`, `CubreMenu`) y su composición efectiva (`IncluyeAlimento`, `IncluyePlato`) garantiza que las reglas de negocio puedan ajustarse sin reestructurar el esquema. El uso de nomencladores abiertos para `GrupoNutricional` y `ProgramaDeAtencion` permite ampliar los catálogos sin cambios estructurales. La externalización de la ficha nutricional a MongoDB ofrece flexibilidad para extender los datos de nutrientes sin migraciones relacionales.

Quedan abiertas cuatro decisiones de modelado: la unidad de `Cantidad` en `Composicion`, la escala de `Proporcion` en `Distribucion` (fracción o porcentaje), la precisión de `Edad` en `Paciente` (años enteros, meses o fecha de nacimiento) y la correspondencia entre las restricciones de una dieta y los alérgenos de los alimentos, que residen en la ficha nutricional fuera del modelo relacional.

## 6. Conclusiones

El informe recorre las cuatro etapas del diseño: análisis de requerimientos, modelo conceptual (MERX consolidado), especificación de 86 restricciones de integridad y valoración formal. El esquema relacional obtenido alcanza BCNF en sus 29 tablas, preserva todas las dependencias funcionales del dominio y garantiza el join sin pérdida, confirmando que la base de datos propuesta es estructuralmente correcta y está preparada para su implementación.
