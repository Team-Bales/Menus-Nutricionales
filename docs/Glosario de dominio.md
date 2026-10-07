# Análisis de dominio — Gestión para la generación automática de menús nutricionales

**Asignatura:** Bases de Datos II / Ingeniería de Software — Curso 2026-2027
**Fuente:** enunciado oficial del proyecto de curso, "Gestión para la generación automática de menús nutricionales".
**Alcance de este documento:** glosario de dominio, barrido línea a línea del enunciado, inventario cerrado de entidades / nomencladores / actores / relaciones candidatas, y las decisiones de modelado para fijar el MERX definitivo.

## 1. Glosario de dominio

Términos del enunciado con su definición consensuada en el dominio de la dietética hospitalaria.

| Término | Definición (según el enunciado) | Tipo de concepto |
|---|---|---|
| **Menú nutricional** | Resultado de la *generación automática*: conjunto de alimentos y/o platos (el nutricionista puede agregar ambos directamente al menú; decisión de equipo) servidos a un paciente bajo una dieta. Se almacena con su fecha de creación y los parámetros usados. | Entidad |
| **Alimento** | Unidad básica del banco: se clasifica por grupo nutricional, tipo de preparación y nivel calórico; puede tener ficha nutricional ampliada. | Entidad |
| **Plato** | Elemento del banco administrado junto a los alimentos; el enunciado lo describe como "compuesto por múltiples ingredientes". | Entidad separada, relacionada con Alimento por composición |
| **Ingrediente** | Alimento que entra en la composición de un plato. | Rol de Alimento en la composición |
| **Banco (de alimentos y platos)** | Catálogo agregado de alimentos y platos disponibles sobre el que trabajan los nutricionistas. | Concepto agregado (no entidad) |
| **Dieta** | Plan con identificador único, nombre, programa de atención al que corresponde, y restricciones y grupos nutricionales a cubrir durante el tratamiento | Entidad |
| **Plan de alimentación / tratamiento** | Horizonte temporal de la distribución de grupos nutricionales cubiertos; usado como sinónimo del ciclo de la dieta. | Sinónimo de Dieta (no entidad aparte) |
| **Grupo nutricional** | Catálogo de clasificación de alimentos; el enunciado lo llama explícitamente **"nomenclador del sistema"**. | Nomenclador |
| **Tipo de preparación** | Categoría del alimento/plato; valores explícitos: *entrante, plato fuerte, postre, bebida*. | Nomenclador (valores dados) |
| **Nivel calórico** | Categoría del alimento/plato; valores explícitos: *bajo, medio, alto*. | Nomenclador (valores dados) |
| **Restricción alimentaria** | Criterio al que se adapta el menú; cada dieta lleva una lista de restricciones. | Nomenclador |
| **Programa de atención** | Programa institucional al que corresponde cada dieta. | Nomenclador |
| **Especialidad** | Área de formación del nutricionista; el enunciado la enumera junto al identificador y el nombre como dato propio de cada nutricionista. | Nomenclador |
| **Nutricionista** | Profesional que ingresa alimentos/platos al banco, los clasifica y nivela, crea menús, los valida y ejecuta revalorizaciones. Almacena id, nombre, especialidad y dietas autorizadas. | Entidad + actor |
| **Jefe de nutrición** | Responsable de revisar y aprobar el menú generado; puede ordenar la confección de otro bajo sus criterios. | Rol de Nutricionista |
| **Revisor** | Quien valida un menú (F3, con fecha y observaciones); asimilable al jefe/nutricionista validador. | Rol |
| **Administrador del sistema** | Encargado de evitar alimentos duplicados e impedir la asignación doble de un menú a pacientes. | Actor (rol de sistema, fuera del MERX) |
| **Paciente** | Destinatario de los menús: accede a sus menús, consulta valoraciones, solicita revalorizaciones. Almacena id, nombre, edad y sala. | Entidad + actor |
| **Sala** | Unidad de hospitalización a la que pertenece el paciente; se usa como agregado en reportes (promedio por sala). | Entidad |
| **Valoración nutricional** | Evaluación del estado del paciente para un menú asignado, ejecutada por un nutricionista; lleva un resultado registrado como referencia al nomenclador `ResultadoNutricional`. Consultable por dieta (navegando Valoración → Asignación → Menú → Dieta). | Entidad |
| **Revalorización nutricional** | Solicitud virtual del paciente que especifica al nutricionista ejecutor. Referencia la valoración que se revisó (`ValoraciónRevisadaId`) y, si ya fue atendida, la nueva valoración generada (`ValoraciónGeneradaId`). | Entidad |
| **ResultadoNutricional** | Catálogo de posibles resultados de una valoración (p. ej. «Adecuado», «Insuficiente»); cada valor tiene un `Nombre` y un `Valor` numérico, ambos únicos. | Nomenclador |
| **Consumo** | Registro de cada alimento consumido de un menú asignado a un paciente (aceptación). La identidad depende de paciente + menú + alimento: un paciente registra su aceptación alimento por alimento dentro de un menú. No hay atributo de fecha en esta tabla. | Agregación |
| **Ficha nutricional ampliada** | Documento de macronutrientes, micronutrientes, alérgenos y modo de preparación; estructura **variable por alimento**. Se gestiona de forma independiente del modelo relacional. | Atributo-documento (NoSQL) |
| **Parámetros de generación** | Criterios almacenados junto al menú: proporción de alimentos por tipo de preparación y cobertura de grupos nutricionales (tablas `Distribución` y `CubreMenú`), más la cantidad total de alimentos (atributo escalar `CantidadTotalAlimentos` de Menú) y si el menú fue generado automáticamente (`EsAutomático`). | Concepto compuesto sin representación única — 2 atributos + 2 relaciones (el MERX no admite atributos multivaluados/compuestos) |
| **Disponibilidad calórica** | Dato consultado de forma reiterada sobre la dieta; derivado de niveles calóricos de los alimentos. | Dato derivado |
| **Desempeño nutricional del paciente** | Indicador por menú/dieta/sala; base de las tasas de aceptación y rechazo. | Indicador derivado |
| **Tasa de aceptación / rechazo** | Proporción sobre el atributo `Aceptación` del consumo. | Indicador derivado |

## 2. Barrido línea a línea del enunciado

Leyenda: **[E]** entidad fuerte · **[Débil]** entidad débil · **[N]** nomenclador · **[A]** actor · **[At]** atributo · **[R]** relación · **[Doc]** atributo documento · **[Der]** derivado/analítico · **[Fuera]** fuera de alcance del MERX.

### 2.1 Descripción del problema — Párrafo 1 (líneas 22-28)
| # | Fragmento | Línea(s) | Candidatos detectados |
|---|---|---|---|
| 1 | "sistema para gestionar la generación automática de menús nutricionales dentro de una institución de salud" | 22-23 | **[E]** Menú · **[Fuera]** institución (contexto) |
| 2 | "administrar los alimentos y platos disponibles" | 23-24 | **[E]** Alimento · **[E]** Plato |
| 3 | "las dietas para las cuales se utilizan" | 24 | **[E]** Dieta |
| 4 | "el grupo nutricional (nomenclador del sistema)" | 24-25 | **[N]** Grupo nutricional — nomenclador explícito en el propio enunciado |
| 5 | "y tipo de preparación que abordan" | 25 | **[N]** Tipo de preparación |
| 6 | "los nutricionistas encargados de crear y validar los menús" | 25 | **[A]+[E]** Nutricionista · **[R]** Crea · **[R]** Valida |
| 7 | "los pacientes que recibirán dichos menús" | 26 | **[A]+[E]** Paciente · **[R]** Asigna (destinatario) |
| 8 | "que los menús se adapten a ciertos criterios preestablecidos, como el nivel calórico" | 26-27 | **[N]** Nivel calórico |
| 9 | "el tipo de restricción alimentaria" | 27 | **[N]** Restricción alimentaria |
| 10 | "distribución de los grupos nutricionales cubiertos durante el plan de alimentación" | 27-28 | **[N]** Grupo nutricional · "plan de alimentación" = dieta/tratamiento (sinónimo) |

### 2.2 Descripción del problema — Párrafo 2 (líneas 29-32)
| # | Fragmento | Línea(s) | Candidatos detectados |
|---|---|---|---|
| 11 | "Cada nutricionista … podrá ingresar alimentos y platos al banco existente" | 29 | **[R]** Describe/Ingresa (Nutricionista → Alimento/Plato) · "banco" = concepto agregado |
| 12 | "clasificarlos por grupo nutricional y por tipo de preparación (entrante, plato fuerte, postre o bebida)" | 30-31 | **[N]** Tipo de preparación **con valores cerrados: entrante / plato fuerte / postre / bebida** · **[R]** Clasifica |
| 13 | "otorgarle el nivel calórico (bajo, medio o alto)" | 31 | **[N]** Nivel calórico **con valores cerrados: bajo / medio / alto** · **[R]** Nivela |
| 14 | "generar menús de forma automática a partir de los alimentos seleccionados" | 31 | **[R]** Crea (Nutricionista → Menú) |
| 15 | "ver menús de otros nutricionistas que atienden la misma dieta" | 32 | **[R]** Autoriza/Atiende (Nutricionista ↔ Dieta) |

### 2.3 Descripción del problema — Párrafo 3 (líneas 33-34)
| # | Fragmento | Línea(s) | Candidatos detectados |
|---|---|---|---|
| 16 | "El administrador del sistema se encargará de evitar la existencia de alimentos duplicados, así como impedir la asignación doble de un menú a los pacientes" | 33-34 | **[A]** Administrador del sistema |

### 2.4 Descripción del problema — Párrafo 4 (líneas 35-37)
| # | Fragmento | Línea(s) | Candidatos detectados |
|---|---|---|---|
| 17 | "Los pacientes podrán acceder a los menús que le son asignados" | 35 | **[R]** Asigna (Menú → Paciente) |
| 18 | "el sistema debe garantizar el almacenamiento de cada consumo registrado ante un menú servido" | 35 | **[Débil]** Consumo (paciente + menú + fecha) |
| 19 | "Las valoraciones nutricionales pueden ser consultadas por los nutricionistas y los pacientes" | 35-36 | **[E]** Valoración nutricional · consulta compartida |
| 20 | "los pacientes pueden solicitar de forma virtual una revalorización nutricional y especificar el nutricionista que ejecute esta tarea" | 36 | **[E]** Revalorización · **[R]** Solicita (Paciente → Revalorización) · **[R]** Ejecuta (Nutricionista → Revalorización) |
| 21 | "el resultado de la revalorización manual" [se registra en la base de datos] | 36-37 | **[At]** Resultado de Revalorización |

### 2.5 Descripción del problema — Párrafo 5 (líneas 38-41)
| # | Fragmento | Línea(s) | Candidatos detectados |
|---|---|---|---|
| 22 | "No siempre aquellos nutricionistas que agregan alimentos son los que crean el menú, esta labor es desempeñada por un nutricionista específico" | 38 | Confirma textualmente que el rol de "quien registra alimentos" y el de "quien crea el menú" son roles distintos, no solo una posibilidad inferida |
| 23 | "criterios como la proporción de alimentos por tipo de preparación, la cobertura de los grupos nutricionales y la cantidad total de alimentos, cuya parametrización es almacenada junto al menú generado" | 38-40 | Parámetros de generación — no es un atributo válido en este MERX (prohíbe multivaluados/compuestos): `CantidadTotalAlimentos` **[At]** escalar de Menú; proporción y cobertura pasan a **[R]** `Distribuye(Menú,TipoPreparación;Proporción)` y `Cubre(Menú,GrupoNutricional)` |
| 24 | "el jefe de nutrición es quien revisa y aprueba el menú generado, aunque también puede indicar la confección de otro bajo sus criterios" | 40-41 | **[A]** Jefe de nutrición· **[R]** Valida/Aprueba · indicio de que puede existir **más de un menú generado para la misma dieta** cuando el primero se rechaza |

### 2.6 Descripción del problema — Párrafo 6, atributos de Dieta y Nutricionista (líneas 42-45)
| # | Fragmento | Línea(s) | Candidatos detectados |
|---|---|---|---|
| 25 | "Cada dieta tendrá un identificador único, su nombre" | 42 | **[At]** Dieta(id, nombre) |
| 26 | "el programa de atención al que corresponde" | 42 | **[N]** Programa de atención · **[R]** Corresponde |
| 27 | "la lista de restricciones o grupos nutricionales a cubrir a lo largo del tratamiento" | 42-43 | Restricciones y grupos a cubrir — el enunciado los redacta como dos listas separadas, mantenidas como dos relaciones independientes; ambas se modelan como **[R]** N:M, no como atributos (el MERX no admite multivaluados/compuestos): `Restringe(Dieta,RestricciónAlimentaria)` y `Cubre(Dieta,GrupoNutricional)` |
| 28 | "De cada nutricionista se almacenará su identificador único, nombre, especialidad, y las dietas para las cuales está autorizado a generar o validar menús" | 43-45 | **[At]** Nutricionista(id, nombre, especialidad) · **[N]** Especialidad · **[R]** Autoriza (Nutricionista ↔ Dieta) |

### 2.7 Descripción del problema — Párrafo 7, atributos de Paciente (líneas 49-50)
| # | Fragmento | Línea(s) | Candidatos detectados |
|---|---|---|---|
| 29 | "Cada paciente tendrá un identificador único, su nombre, edad, sala a la que pertenece" | 49 | **[At]** Paciente(id, nombre, edad) · **[E]** Sala · **[R]** Pertenece |
| 30 | "y las dietas en las que está inscrito" | 49-50 | **[R]** Inscribe (Paciente ↔ Dieta) |
| 31 | "Estos pueden consultar todas sus valoraciones nutricionales por dieta recibida" | 50 | **[R]** Evalúa (Valoración ligada a Dieta) |

### 2.8 Descripción del problema — Párrafo 8, ficha nutricional (líneas 51-57)
| # | Fragmento | Línea(s) | Candidatos detectados |
|---|---|---|---|
| 32 | "Cada alimento puede además contener una ficha nutricional ampliada (macronutrientes, micronutrientes, alérgenos, modo de preparación)" | 51-52 | **[Doc]** Ficha nutricional ampliada (NoSQL, estructura variable) |
| 33 | "cuya cantidad y tipo de atributos varía de un alimento a otro y no se ajusta a una estructura fija ni común para todos los casos: un alimento simple puede describirse con pocos campos, mientras que un plato compuesto por múltiples ingredientes requiere una ficha considerablemente más extensa y anidada" | 52-55 | Justifica el atributo-documento no relacional. El enunciado contrapone "alimento simple" con "plato compuesto por múltiples ingredientes" como dos variantes del mismo fenómeno (la ficha); el equipo consideró esta evidencia y aun así mantuvo a Plato como entidad separada por composición |
| 34 | "dicha ficha nutricional no debe modelarse junto al resto de las tablas de la solución, sino gestionarse de forma independiente, permitiendo que cada alimento almacene únicamente los atributos que le correspondan" | 55-57 | **[Doc]** Ficha gestionada aparte (store NoSQL), con esquema flexible por documento |

### 2.9 Descripción del problema — Párrafos 9 y 10, requisitos de consulta y reportes (líneas 58-63)
| # | Fragmento | Línea(s) | Candidatos detectados |
|---|---|---|---|
| 35 | "la disponibilidad calórica de las dietas y las valoraciones más recientes son consultadas de forma reiterada … el sistema debe garantizar respuestas prácticamente inmediatas" | 58-60 | **[Der]** Disponibilidad calórica — requisito no funcional de rendimiento (índices/caché), no es una entidad |
| 36 | "reportes detallados sobre la creación de menús, los alimentos más utilizados, la distribución de niveles calóricos y el desempeño nutricional de los pacientes" | 61-63 | **[Der]** Reportes/indicadores (vistas analíticas) |

### 3. Funcionalidades (F1-F6) — líneas 68-85
| # | Fragmento | Línea(s) | Candidatos detectados |
|---|---|---|---|
| F1 | "listado de menús generados automáticamente para una dieta específica, indicando el nombre del creador, la fecha de creación y los parámetros utilizados" | 68-69 | **[R]** Crea · **[At]** Menú(FechaCreación, Parámetros) · confirma que Menú se asocia a una Dieta específica |
| F2 | "los alimentos más utilizados en los **menús finales** de una dieta, clasificados por nivel calórico y grupo nutricional" | 70-71 | **[Der]** ranking de alimentos · nomencladores confirmados · la expresión **"menús finales"** sugiere que no todo menú generado llega a ese estado (se deriva de `Valida`, sin atributo `Estado` propio en Menú) |
| F3 | "los menús que fueron validados por un revisor determinado, indicando la fecha de validación y las observaciones hechas durante el proceso" | 72-73 | **[R]** Valida **con atributos propios: FechaValidación, Observaciones** · **[A/rol]** revisor (≈ jefe/nutricionista validador) |
| F4 | "desempeño nutricional de los pacientes ante un menú, clasificando los alimentos por nivel calórico y comparando las tasas de aceptación" | 74-75 | **[At]** Consumo.Aceptación · **[Der]** tasa de aceptación |
| F5 | "comparar los menús generados para diferentes dietas … si los criterios de equilibrio fueron cumplidos" | 76-77 | **[Der]** verificación de parámetros de generación vs. composición real del menú |
| F6 | "correlación entre el nivel calórico … los 10 alimentos con la tasa de rechazo más alta, el nutricionista que los creó y la dieta a la que pertenecen" | 78-80 | **[R]** Describe (Nutricionista → Alimento) confirmada como consultable · **[Der]** correlaciones · pertenencia de un alimento a una dieta, siempre indirecta (vía Menú) |
| F6b | "comparar el desempeño … de los pacientes que solicitaron una revalorización con el promedio general de valoraciones de sus respectivas salas" | 80-82 | **[E]** Sala como agregado en reportes · **[At]** Revalorización ligada a Valoración por sala |
| — | "exportar la información mostrada a ficheros con formato PDF … poder ordenar cada columna de los resultados" | 83-85 | **[Fuera]** requisitos de interfaz/UI, no de dominio |

## 4. Inventario de entidades, nomencladores y actores candidatos (lista cerrada)

Resultado del barrido: todo candidato del enunciado cae en una de las cuatro listas siguientes. No hay candidatos adicionales con evidencia textual.

### 4.1 Entidades

| # | Entidad | Atributos principales (según el enunciado) | Evidencia (líneas) | Naturaleza |
|---|---|---|---|---|
| E1 | **Alimento** | Id, Nombre, GrupoNutricional, TipoPreparación, NivelCalórico | 23-24, 29-31, 51-57 | Fuerte. Núcleo del sistema. |
| E2 | **Plato** | mismos atributos base que Alimento | 23-24, 29, 54-55 | Fuerte, con composición (`Compone(Plato,Alimento,Cantidad)`). |
| E3 | **Dieta** | Id, Nombre | 24, 42-43 | Fuerte. |
| E4 | **Menú** | Id, FechaCreación, CantidadTotalAlimentos, EsAutomático | 22-23, 31, 35, 38-40, 68-69 | Fuerte (generado, no necesariamente servido aún). Proporción por tipo de preparación y cobertura de grupos nutricionales son relaciones (`Distribución`, `CubreMenú`), no atributos (el MERX no admite multivaluados/compuestos). `EsAutomático` distingue los menús generados por el sistema de los confeccionados manualmente. |
| E5 | **Nutricionista** | Id, Nombre, Especialidad | 25, 29-32, 38, 40-41, 43-45 | Fuerte + actor. |
| E6 | **Paciente** | Id, Nombre, Edad | 26, 35-36, 49-50 | Fuerte + actor. |
| E7 | **Sala** | Id, Nombre | 49, 80-82 | Fuerte (mínima). |
| E8 | **Valoración nutricional** | ValoraciónId, MenúId, PacienteId, NutricionistaId, ResultadoNutricionalId | 35-36, 50 | Fuerte; ligada a la asignación (Menú + Paciente) y ejecutada por un nutricionista; el resultado se referencia al nomenclador `ResultadoNutricional`. |
| E9 | **Revalorización nutricional** | RevalorizaciónId, FechaSolicitud, PacienteId, NutricionistaId, ValoraciónRevisadaId, ValoraciónGeneradaId (nullable) | 36-37 | Fuerte; solicitada por un paciente, ejecutada por un nutricionista; referencia la valoración que se revisó y la nueva valoración generada (esta última puede estar pendiente). |
| E10 | **Consumo** | (MenúId, PacienteId, AlimentoId), Aceptación | 35, 74-75 | **Agregación** — identificada por el trío (Menú, Paciente, Alimento). No hay atributo de fecha: la fecha de servicio se obtiene de `Asignación`; `Aceptación` es el único atributo propio. |

### 4.2 Nomencladores (catálogos cerrados)

| # | Nomenclador | Valores | Evidencia (líneas) |
|---|---|---|---|
| N1 | **Grupo nutricional** | no enumerados (catálogo institucional) | 24-25 (explícito: *"nomenclador del sistema"*), 27-28, 70-71 |
| N2 | **Tipo de preparación** | entrante, plato fuerte, postre, bebida | 25, 30-31 |
| N3 | **Nivel calórico** | bajo, medio, alto | 26-27, 31 |
| N4 | **Restricción alimentaria** | no enumerados (p. ej. sin gluten, hiposódica) | 27, 42-43 |
| N5 | **Programa de atención** | no enumerados | 42 |
| N6 | **Especialidad** (de Nutricionista) | no enumerados | 44 |
| N7 | **ResultadoNutricional** | no enumerados (p. ej. Adecuado, Insuficiente); cada entrada lleva `Nombre` (UNIQUE) y `Valor` numérico (UNIQUE) | decisión del equipo |

### 4.3 Actores candidatos

| # | Actor | Acciones según el enunciado | Tratamiento en el MERX |
|---|---|---|---|
| A1 | **Nutricionista** | Ingresa/clasifica/nivela alimentos y platos; crea menús; valida; ejecuta revalorización | **Entidad** E5 (se registran sus datos) |
| A2 | **Paciente** | Accede a menús asignados; consulta valoraciones; solicita revalorización | **Entidad** E6 |
| A3 | **Jefe de nutrición** | Revisa y aprueba menús; ordena confección de otros | Rol de Nutricionista, sin datos propios |
| A4 | **Administrador del sistema** | Evita duplicados de alimentos; impide asignación doble | No es dato de dominio, es un rol de mantenimiento de la aplicación |
| A5 | **Revisor** (F3) | Valida menús con fecha y observaciones | Rol asociado a la relación `Valida` (coincide con A3) |

### 4.4 Relaciones candidatas

> **Convención adoptada:** un atributo se deja en la relación solo cuando el hecho depende intrínsecamente del par de entidades relacionadas (por ejemplo, la cantidad de un ingrediente en un plato, o la fecha y observaciones de una validación puntual). Cuando el hecho es 1:1 respecto a una de las entidades — como la fecha de creación de un menú o la fecha de solicitud de una revalorización — se modela como atributo nativo de esa entidad y no de la relación, para no duplicar información. Por esa razón `FechaCreación` vive en Menú y `FechaSolicitud`/`Resultado` viven en Revalorización, no en las relaciones `Crea` o `Solicita`.

| Relación | Participantes | Evidencia (líneas) | Cardinalidad | Atributos propios |
|---|---|---|---|---|
| Describe/Ingresa | Nutricionista → Alimento/Plato | 29, 78-80 | 1:N (un alimento lo describe un único nutricionista) | — |
| Clasifica | Nutricionista → Alimento (grupo, tipo) | 30-31 | — | — |
| Nivela | Nutricionista → Alimento (nivel calórico) | 31 | — | — |
| Autoriza/Atiende | Nutricionista ↔ Dieta | 32, 44-45 | N:M | — |
| Crea | Nutricionista → Menú | 25, 31, 68-69 | 1:N (un menú lo crea un único nutricionista) | — |
| Valida/Aprueba | Nutricionista (revisor/jefe) → Menú | 25, 40-41, 72-73 | 1:N | **FechaValidación, Observaciones** |
| Pertenece | Menú → Dieta | 68-71 | N:1 (varios menús pueden generarse para una misma dieta; mismo verbo que `Pertenece(Paciente,Sala)`, ocurrencia distinta) | — |
| Incluye | Menú → Alimento | 31 | N:M | — |
| **Incluye** *(decisión de equipo)* | Menú → Plato | — (no está en el enunciado; el nutricionista puede agregar platos directamente al menú) | N:M | — |
| Compone | Plato → Alimento | 54-55 | N:M | **Cantidad** |
| Corresponde | Dieta → Programa de atención | 42 | N:1 | — |
| Cubre | Dieta ↔ Grupo nutricional | 42-43 | N:M | — |
| Restringe | Dieta ↔ Restricción alimentaria | 27, 42-43 | N:M | — |
| Distribuye | Menú ↔ Tipo de preparación | 38-40 | N:M | **Proporción** |
| Cubre | Menú ↔ Grupo nutricional | 38-40 | N:M | — |
| Pertenece | Paciente → Sala | 49 | N:1 | — |
| Inscribe | Paciente ↔ Dieta | 49-50 | N:M | — |
| Asigna | Menú ↔ Paciente | 35 | **N:M** (tabla `Asignación(MenúId, PacienteId)`); la restricción de la línea 33-34 —que un menú no se asigne dos veces al mismo paciente— queda garantizada por la PK compuesta. Un menú puede asignarse a varios pacientes y un paciente puede recibir varios menús. | — |
| Excluye | RestricciónAlimentaria ↔ GrupoNutricional | decisión del equipo | **N:M** | — |
| **Consume** (agregación) | Paciente + Menú + Alimento → Consumo | 35, 74-75 | — | **Aceptación** |
| Tiene | Paciente → Valoración | 35-36, 50 | 1:N | — |
| Evalúa | Dieta → Valoración | 50 | 1:N | — |
| Solicita | Paciente → Revalorización | 36 | 1:N | — |
| Ejecuta | Nutricionista → Revalorización | 36 | 1:N | — |

**Nota sobre la `Cubre` repetida:** aparece dos veces en la tabla con participantes distintos (Dieta↔GrupoNutricional y Menú↔GrupoNutricional). Al materializar el MERX se nombran con distintos nombres para evitar la ambigüedad: `CubreDieta(DietaId, GrupoNutricionalId)` y `CubreMenú(MenúId, GrupoNutricionalId)`. Son dos ocurrencias independientes del mismo verbo relacional aplicado a dos entidades distintas.

**Nota sobre `Incluye` :** la única evidencia textual directa (línea 31) dice que el menú se genera "a partir de los alimentos seleccionados", se decidió que un nutricionista puede agregar **tanto alimentos como platos directamente al menú** — de ahí las dos relaciones `Incluye` del glosario. Al materializar el MERX se nombran con nombres distintos: `IncluyeAlimento(MenúId, AlimentoId)` e `IncluyePlato(MenúId, PlatoId)`. Cuando un menú incluye un plato, sus alimentos componentes se obtienen indirectamente atravesando `Composición(PlatoId, AlimentoId, Cantidad)`, ya que el plato "sabe" los alimentos que lo componen. Esto es relevante para F2 y F6 (que hablan de "alimentos" dentro de los menús): esas consultas deben sumar los alimentos incluidos directamente **más** los alimentos de los platos incluidos (vía `Composición`), para no subcontar el uso real de cada alimento.

## 5. Atributos especiales

> **Corrección aplicada:** el MERX prohíbe los atributos multivaluados y compuestos. Solo la ficha nutricional es un caso especial, porque no es un atributo multivaluado sino un documento externo gestionado en otro motor de almacenamiento (MongoDB).

| Atributo | Modo | Justificación textual |
|---|---|---|
| Ficha nutricional ampliada | **Documento (NoSQL), gestor aparte** | Líneas 51-57: estructura variable, anidada, no normalizable; "no debe modelarse junto al resto de las tablas". |

## 6. Decisiones de modelado

| Tema | Decisión | Por qué |
|---|---|---|
| Plato | Entidad separada de Alimento, relacionada por `Compone(Plato, Alimento, Cantidad)` | El enunciado trata "alimentos y platos" como dos catálogos paralelos (líneas 23-24, 29) |
| Jefe de nutrición | Rol de Nutricionista, sin entidad ni atributos propios | La aprobación ya vive en `Valida(Nutricionista, Menú; FechaValidación, Observaciones)`; no tiene estado propio en el enunciado |
| Administrador del sistema | Fuera del MERX | Es mantenimiento de la aplicación (evitar duplicados, asignación doble); no se consulta como dato en ninguna funcionalidad |
| "Plan de alimentación / tratamiento" | Sin entidad propia | Es sinónimo del ciclo de la dieta (líneas 27-28, 42-43) |
| Estado del menú | Sin atributo `Estado` en Menú | Un menú se considera final/aprobado si existe un registro en `Valida`; no hace falta duplicar el dato |
| Programa de atención | Nomenclador | Sin atributos propios ni consultas independientes en las funcionalidades exigidas |
| Restricción alimentaria | Nomenclador, relacionado con Dieta por `Restringe(Dieta, RestricciónAlimentaria)` | Sin atributos propios; la compatibilidad con alimentos se resuelve contra los alérgenos de la ficha nutricional |
| Restricciones vs. grupos a cubrir | Dos relaciones N:M independientes — `Restringe` y `CubreDieta(Dieta,GrupoNutricional)` | El enunciado las redacta como dos listas separadas (línea 42-43) |
| `Excluye(RestricciónAlimentaria, GrupoNutricional)` | Relación N:M para mapear cada restricción clínica a los grupos nutricionales que excluye | Habilita la verificación de compatibilidad a nivel de grupo en PostgreSQL; la verificación a nivel de alérgeno se hace contra la ficha nutricional en MongoDB |
| `Asignación` es N:M, no 1:1 | Un menú puede asignarse a varios pacientes; la restricción de la línea 33-34 se garantiza con PK compuesta `(MenúId, PacienteId)` | El texto dice que el administrador debe impedir la *asignación doble del mismo menú al mismo paciente*, que es exactamente lo que la PK compuesta garantiza |
| `Consumo` sin `FechaConsumo` | La clave de la agregación es `(MenúId, PacienteId, AlimentoId)`; no hay fecha | La fecha de servicio es un hecho de la asignación, no del consumo individual de un alimento; el equipo decidió no duplicarla |
| `ResultadoNutricional` como nomenclador | Los posibles resultados de una valoración se gestionan como catálogo, con `Nombre` y `Valor` únicos | Evita valores de texto libres en `Valoración` y permite consultas analíticas sobre resultados sin comparaciones de cadenas |
| Menú y platos | Un menú puede incluir alimentos y platos directamente (`IncluyeAlimento`, `IncluyePlato`) | El plato ya conoce sus alimentos componentes vía `Composición`, así que no hay que descomponerlo al armar el menú |

## 7. Resumen ejecutivo de la lista cerrada

| Tipo | Lista final |
|---|---|
| **Entidades (10)** | Alimento, Plato, Dieta, Menú, Nutricionista, Paciente, Sala, Valoración Nutricional, Revalorización, Consumo (agregación) |
| **Nomencladores (7)** | Grupo nutricional, Tipo de preparación, Nivel calórico, Restricción alimentaria*, Programa de atención*, Especialidad, ResultadoNutricional† |
| **Actores (5)** | Nutricionista, Paciente, Jefe de nutrición (rol), Administrador del sistema (externo al dominio), Revisor (rol de `Validación`) |
| **Relaciones (25, incluida la agregación Consumo)** | ver la tabla de relaciones candidatas — incluye `Restringe`, `Distribución`, `CubreDieta`, `CubreMenú` (al corregir atributos multivaluados/compuestos), `IncluyeAlimento` e `IncluyePlato` (decisión de equipo), `Excluye` (decisión del equipo) y `Asignación` (N:M) + `Consumo` como agregación |
| **Atributos especiales** | Ficha nutricional ampliada (documento NoSQL) |
| **Derivados (no entidades)** | Disponibilidad calórica, desempeño nutricional, tasas de aceptación/rechazo, ranking de alimentos, criterios de equilibrio, reportes F1-F6 |

\* Nomenclador, ver sección de decisiones de modelado.
† Nomenclador no mencionado explícitamente en el enunciado; incorporado como decisión del equipo al modelar las valoraciones.

## 8. Conclusiones y siguientes pasos

El barrido cubre íntegramente las secciones "Descripción del problema" y "Funcionalidades" del enunciado; no quedan fragmentos con contenido de dominio sin clasificar. La lista cerrada de 10 entidades, 7 nomencladores, 5 actores y 25 relaciones, junto con las decisiones de modelado ya tomadas por el equipo, puede tomarse como base estable para dibujar el MERX.
