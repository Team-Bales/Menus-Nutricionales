# Análisis de dominio — Gestión para la generación automática de menús nutricionales

## Glosario de dominio

Términos del enunciado con su definición consensuada en el dominio de la dietética hospitalaria.

| Término | Definición (según el enunciado) | Tipo de concepto |
|---|---|---|
| **Menú nutricional** | Resultado de la *generación automática*: conjunto de alimentos (y, a confirmar en D1, platos) servidos a un paciente bajo una dieta. Se almacena con su fecha de creación y los parámetros usados. | Entidad |
| **Alimento** | Unidad básica del banco: se clasifica por grupo nutricional, tipo de preparación y nivel calórico; puede tener ficha nutricional ampliada. | Entidad |
| **Plato** | Elemento del banco administrado junto a los alimentos; el enunciado lo describe como "compuesto por múltiples ingredientes". | Entidad (o subtipo de Alimento — *decisión abierta D1*) |
| **Ingrediente** | Alimento que entra en la composición de un plato. | Rol de Alimento en la composición |
| **Banco (de alimentos y platos)** | Catálogo agregado de alimentos y platos disponibles sobre el que trabajan los nutricionistas. | Concepto agregado (no entidad) |
| **Dieta** | Plan con identificador único, nombre, programa de atención al que corresponde, y restricciones y grupos nutricionales a cubrir durante el tratamiento — estos dos últimos como relaciones N:M, no como atributos (el MERX de la cátedra prohíbe atributos multivaluados/compuestos). | Entidad |
| **Plan de alimentación / tratamiento** | Horizonte temporal de la distribución de grupos nutricionales cubiertos; usado como sinónimo del ciclo de la dieta. | Sinónimo de Dieta (no entidad aparte) |
| **Grupo nutricional** | Catálogo de clasificación de alimentos; el enunciado lo llama explícitamente **"nomenclador del sistema"**. | Nomenclador |
| **Tipo de preparación** | Categoría del alimento/plato; valores explícitos: *entrante, plato fuerte, postre, bebida*. | Nomenclador (valores dados) |
| **Nivel calórico** | Categoría del alimento/plato; valores explícitos: *bajo, medio, alto*. | Nomenclador (valores dados) |
| **Restricción alimentaria** | Criterio al que se adapta el menú; cada dieta lleva una lista de restricciones. | Nomenclador o entidad (*D5*) |
| **Programa de atención** | Programa institucional al que corresponde cada dieta. | Nomenclador o entidad (*D4*) |
| **Especialidad** | Área de formación del nutricionista; el enunciado la enumera junto al identificador y el nombre como dato propio de cada nutricionista. | Nomenclador |
| **Nutricionista** | Profesional que ingresa alimentos/platos al banco, los clasifica y nivela, crea menús, los valida y ejecuta revalorizaciones. Almacena id, nombre, especialidad y dietas autorizadas. | Entidad + actor |
| **Jefe de nutrición** | Responsable de revisar y aprobar el menú generado; puede ordenar la confección de otro bajo sus criterios. | Rol de Nutricionista (*D2*) |
| **Revisor** | Quien valida un menú (F3, con fecha y observaciones); asimilable al jefe/nutricionista validador. | Rol |
| **Administrador del sistema** | Encargado de evitar alimentos duplicados e impedir la asignación doble de un menú a pacientes. | Actor (rol de sistema) (*D3*) |
| **Paciente** | Destinatario de los menús: accede a sus menús, consulta valoraciones, solicita revalorizaciones. Almacena id, nombre, edad y sala. | Entidad + actor |
| **Sala** | Unidad de hospitalización a la que pertenece el paciente; se usa como agregado en reportes (promedio por sala). | Entidad |
| **Valoración nutricional** | Evaluación del estado del paciente (fecha, resultado), consultable por dieta. | Entidad |
| **Revalorización nutricional** | Solicitud virtual del paciente que especifica al nutricionista ejecutor; se registra su resultado manual. | Entidad |
| **Consumo** | Registro de cada menú servido a un paciente (fecha de consumo, aceptación). Un mismo paciente puede recibir el mismo menú en fechas distintas, así que la identidad depende de paciente + menú + fecha, no solo de paciente + menú. | Entidad débil |
| **Ficha nutricional ampliada** | Documento de macronutrientes, micronutrientes, alérgenos y modo de preparación; estructura **variable por alimento**. Se gestiona de forma independiente del modelo relacional. | Atributo-documento (NoSQL) |
| **Parámetros de generación** | Criterios almacenados junto al menú: proporción de alimentos por tipo de preparación y cobertura de grupos nutricionales (relaciones `Distribuye`/`Cubre` de Menú), más la cantidad total de alimentos (atributo escalar de Menú). | Concepto compuesto sin representación única — 1 atributo + 2 relaciones (el MERX no admite atributos multivaluados/compuestos) |
| **Disponibilidad calórica** | Dato consultado de forma reiterada sobre la dieta; derivado de niveles calóricos de los alimentos. | Dato derivado |
| **Desempeño nutricional del paciente** | Indicador por menú/dieta/sala; base de las tasas de aceptación y rechazo. | Indicador derivado |
| **Tasa de aceptación / rechazo** | Proporción sobre el atributo `Aceptación` del consumo. | Indicador derivado |

## Barrido línea a línea del enunciado

Leyenda: **[E]** entidad fuerte · **[Débil]** entidad débil · **[N]** nomenclador · **[A]** actor · **[At]** atributo · **[R]** relación · **[Doc]** atributo documento · **[Der]** derivado/analítico · **[Fuera]** fuera de alcance del MER.

### Descripción del problema — Párrafo 1 (líneas 22-28)
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
| 9 | "el tipo de restricción alimentaria" | 27 | **[N?]** Restricción alimentaria (*D5*) |
| 10 | "distribución de los grupos nutricionales cubiertos durante el plan de alimentación" | 27-28 | **[N]** Grupo nutricional · "plan de alimentación" = dieta/tratamiento (sinónimo) |

### Descripción del problema — Párrafo 2 (líneas 29-32)
| # | Fragmento | Línea(s) | Candidatos detectados |
|---|---|---|---|
| 11 | "Cada nutricionista … podrá ingresar alimentos y platos al banco existente" | 29 | **[R]** Describe/Ingresa (Nutricionista → Alimento/Plato) · "banco" = concepto agregado |
| 12 | "clasificarlos por grupo nutricional y por tipo de preparación (entrante, plato fuerte, postre o bebida)" | 30-31 | **[N]** Tipo de preparación **con valores cerrados: entrante / plato fuerte / postre / bebida** · **[R]** Clasifica |
| 13 | "otorgarle el nivel calórico (bajo, medio o alto)" | 31 | **[N]** Nivel calórico **con valores cerrados: bajo / medio / alto** · **[R]** Nivela |
| 14 | "generar menús de forma automática a partir de los alimentos seleccionados" | 31 | **[R]** Crea (Nutricionista → Menú) · el menú se arma **a partir de alimentos**, no se menciona explícitamente que se arme a partir de platos (relevante para D1 y para la relación `Incluye`, ver la tabla de relaciones candidatas más abajo) |
| 15 | "ver menús de otros nutricionistas que atienden la misma dieta" | 32 | **[R]** Autoriza/Atiende (Nutricionista ↔ Dieta) |

### Descripción del problema — Párrafo 3 (líneas 33-34)
| # | Fragmento | Línea(s) | Candidatos detectados |
|---|---|---|---|
| 16 | "El administrador del sistema se encargará de evitar la existencia de alimentos duplicados, así como impedir la asignación doble de un menú a los pacientes" | 33-34 | **[A]** Administrador del sistema (*D3*) · restricción de integridad sobre Alimento y sobre la relación `Asigna`: un menú no debe terminar asignado a más de un paciente |

### Descripción del problema — Párrafo 4 (líneas 35-37)
| # | Fragmento | Línea(s) | Candidatos detectados |
|---|---|---|---|
| 17 | "Los pacientes podrán acceder a los menús que le son asignados" | 35 | **[R]** Asigna (Menú → Paciente) |
| 18 | "el sistema debe garantizar el almacenamiento de cada consumo registrado ante un menú servido" | 35 | **[Débil]** Consumo (paciente + menú + fecha) |
| 19 | "Las valoraciones nutricionales pueden ser consultadas por los nutricionistas y los pacientes" | 35-36 | **[E]** Valoración nutricional · consulta compartida |
| 20 | "los pacientes pueden solicitar de forma virtual una revalorización nutricional y especificar el nutricionista que ejecute esta tarea" | 36 | **[E]** Revalorización · **[R]** Solicita (Paciente → Revalorización) · **[R]** Ejecuta (Nutricionista → Revalorización) |
| 21 | "el resultado de la revalorización manual" [se registra en la base de datos] | 36-37 | **[At]** Resultado de Revalorización |

### Descripción del problema — Párrafo 5 (líneas 38-41)
| # | Fragmento | Línea(s) | Candidatos detectados |
|---|---|---|---|
| 22 | "No siempre aquellos nutricionistas que agregan alimentos son los que crean el menú, esta labor es desempeñada por un nutricionista específico" | 38 | Confirma textualmente que el rol de "quien registra alimentos" y el de "quien crea el menú" son roles distintos, no solo una posibilidad inferida |
| 23 | "criterios como la proporción de alimentos por tipo de preparación, la cobertura de los grupos nutricionales y la cantidad total de alimentos, cuya parametrización es almacenada junto al menú generado" | 38-40 | Parámetros de generación — no es un atributo válido en este MERX (prohíbe multivaluados/compuestos): `CantidadTotalAlimentos` **[At]** escalar de Menú; proporción y cobertura pasan a **[R]** `Distribuye(Menú,TipoPreparación;Proporción)` y `Cubre(Menú,GrupoNutricional)` |
| 24 | "el jefe de nutrición es quien revisa y aprueba el menú generado, aunque también puede indicar la confección de otro bajo sus criterios" | 40-41 | **[A?]** Jefe de nutrición (*D2*) · **[R]** Valida/Aprueba · indicio de que puede existir **más de un menú generado para la misma dieta** cuando el primero se rechaza (relevante para *D8*) |

### Descripción del problema — Párrafo 6, atributos de Dieta y Nutricionista (líneas 42-45)
| # | Fragmento | Línea(s) | Candidatos detectados |
|---|---|---|---|
| 25 | "Cada dieta tendrá un identificador único, su nombre" | 42 | **[At]** Dieta(id, nombre) |
| 26 | "el programa de atención al que corresponde" | 42 | **[N?]** Programa de atención (*D4*) · **[R]** Corresponde |
| 27 | "la lista de restricciones o grupos nutricionales a cubrir a lo largo del tratamiento" | 42-43 | Restricciones y grupos a cubrir — el enunciado los redacta como dos listas separadas (ver *D6*); ambas se modelan como **[R]** N:M, no como atributos (el MERX no admite multivaluados/compuestos): `Restringe(Dieta,RestricciónAlimentaria)` y `Cubre(Dieta,GrupoNutricional)` |
| 28 | "De cada nutricionista se almacenará su identificador único, nombre, especialidad, y las dietas para las cuales está autorizado a generar o validar menús" | 43-45 | **[At]** Nutricionista(id, nombre, especialidad) · **[N]** Especialidad · **[R]** Autoriza (Nutricionista ↔ Dieta) |

### Descripción del problema — Párrafo 7, atributos de Paciente (líneas 49-50)
| # | Fragmento | Línea(s) | Candidatos detectados |
|---|---|---|---|
| 29 | "Cada paciente tendrá un identificador único, su nombre, edad, sala a la que pertenece" | 49 | **[At]** Paciente(id, nombre, edad) · **[E]** Sala · **[R]** Pertenece |
| 30 | "y las dietas en las que está inscrito" | 49-50 | **[R]** Inscribe (Paciente ↔ Dieta) |
| 31 | "Estos pueden consultar todas sus valoraciones nutricionales por dieta recibida" | 50 | **[R]** Evalúa (Valoración ligada a Dieta) |

### Descripción del problema — Párrafo 8, ficha nutricional (líneas 51-57)
| # | Fragmento | Línea(s) | Candidatos detectados |
|---|---|---|---|
| 32 | "Cada alimento puede además contener una ficha nutricional ampliada (macronutrientes, micronutrientes, alérgenos, modo de preparación)" | 51-52 | **[Doc]** Ficha nutricional ampliada (NoSQL, estructura variable) |
| 33 | "cuya cantidad y tipo de atributos varía de un alimento a otro y no se ajusta a una estructura fija ni común para todos los casos: un alimento simple puede describirse con pocos campos, mientras que un plato compuesto por múltiples ingredientes requiere una ficha considerablemente más extensa y anidada" | 52-55 | Justifica el atributo-documento no relacional. **Importante para D1:** el enunciado contrapone "alimento simple" con "plato compuesto por múltiples ingredientes" como dos variantes del mismo fenómeno (la ficha), lo cual es evidencia a favor de tratar el plato como un **caso compuesto de alimento**, y no solo como un catálogo aparte — se incorpora este matiz a la discusión de D1 |
| 34 | "dicha ficha nutricional no debe modelarse junto al resto de las tablas de la solución, sino gestionarse de forma independiente, permitiendo que cada alimento almacene únicamente los atributos que le correspondan" | 55-57 | **[Doc]** Ficha gestionada aparte (store NoSQL), con esquema flexible por documento |

### Descripción del problema — Párrafos 9 y 10, requisitos de consulta y reportes (líneas 58-63)
| # | Fragmento | Línea(s) | Candidatos detectados |
|---|---|---|---|
| 35 | "la disponibilidad calórica de las dietas y las valoraciones más recientes son consultadas de forma reiterada … el sistema debe garantizar respuestas prácticamente inmediatas" | 58-60 | **[Der]** Disponibilidad calórica — requisito no funcional de rendimiento (índices/caché), no es una entidad |
| 36 | "reportes detallados sobre la creación de menús, los alimentos más utilizados, la distribución de niveles calóricos y el desempeño nutricional de los pacientes" | 61-63 | **[Der]** Reportes/indicadores (vistas analíticas) |

### Funcionalidades (F1-F6) — líneas 68-85
| # | Fragmento | Línea(s) | Candidatos detectados |
|---|---|---|---|
| F1 | "listado de menús generados automáticamente para una dieta específica, indicando el nombre del creador, la fecha de creación y los parámetros utilizados" | 68-69 | **[R]** Crea · **[At]** Menú(FechaCreación, Parámetros) · confirma que Menú se asocia a una Dieta específica |
| F2 | "los alimentos más utilizados en los **menús finales** de una dieta, clasificados por nivel calórico y grupo nutricional" | 70-71 | **[Der]** ranking de alimentos · nomencladores confirmados · la expresión **"menús finales"** sugiere que no todo menú generado llega a ese estado (relevante para *D8*) |
| F3 | "los menús que fueron validados por un revisor determinado, indicando la fecha de validación y las observaciones hechas durante el proceso" | 72-73 | **[R]** Valida **con atributos propios: FechaValidación, Observaciones** · **[A/rol]** revisor (≈ jefe/nutricionista validador, *D2*) |
| F4 | "desempeño nutricional de los pacientes ante un menú, clasificando los alimentos por nivel calórico y comparando las tasas de aceptación" | 74-75 | **[At]** Consumo.Aceptación · **[Der]** tasa de aceptación |
| F5 | "comparar los menús generados para diferentes dietas … si los criterios de equilibrio fueron cumplidos" | 76-77 | **[Der]** verificación de parámetros de generación vs. composición real del menú |
| F6 | "correlación entre el nivel calórico … los 10 alimentos con la tasa de rechazo más alta, el nutricionista que los creó y la dieta a la que pertenecen" | 78-80 | **[R]** Describe (Nutricionista → Alimento) confirmada como consultable · **[Der]** correlaciones · pertenencia de un alimento a una dieta, siempre indirecta (vía Menú) |
| F6b | "comparar el desempeño … de los pacientes que solicitaron una revalorización con el promedio general de valoraciones de sus respectivas salas" | 80-82 | **[E]** Sala como agregado en reportes · **[At]** Revalorización ligada a Valoración por sala |
| — | "exportar la información mostrada a ficheros con formato PDF … poder ordenar cada columna de los resultados" | 83-85 | **[Fuera]** requisitos de interfaz/UI, no de dominio |

## Inventario de entidades, nomencladores y actores candidatos (lista cerrada)

Resultado del barrido: todo candidato del enunciado cae en una de las cuatro listas siguientes. No hay candidatos adicionales con evidencia textual.

### Entidades

| # | Entidad | Atributos principales (según el enunciado) | Evidencia (líneas) | Naturaleza |
|---|---|---|---|---|
| E1 | **Alimento** | Id, Nombre, GrupoNutricional, TipoPreparación, NivelCalórico | 23-24, 29-31, 51-57 | Fuerte. Núcleo del sistema. |
| E2 | **Plato** | mismos atributos base que Alimento (a confirmar en D1) | 23-24, 29, 54-55 | Fuerte con composición (o ISA — *D1*). |
| E3 | **Dieta** | Id, Nombre | 24, 42-43 | Fuerte. |
| E4 | **Menú** | Id, FechaCreación, CantidadTotalAlimentos | 22-23, 31, 35, 38-40, 68-69 | Fuerte (generado, no necesariamente servido aún). Proporción por tipo de preparación y cobertura de grupos nutricionales son relaciones, no atributos (el MERX no admite multivaluados/compuestos). |
| E5 | **Nutricionista** | Id, Nombre, Especialidad | 25, 29-32, 38, 40-41, 43-45 | Fuerte + actor. |
| E6 | **Paciente** | Id, Nombre, Edad | 26, 35-36, 49-50 | Fuerte + actor. |
| E7 | **Sala** | Id, Nombre | 49, 80-82 | Fuerte (mínima). |
| E8 | **Valoración nutricional** | Fecha, Resultado | 35-36, 50 | Fuerte; dependiente de paciente, ligada a dieta. |
| E9 | **Revalorización nutricional** | FechaSolicitud, Resultado | 36-37 | Fuerte; solicitada por un paciente y ejecutada por un nutricionista. |
| E10 | **Consumo** | FechaConsumo (parte de la clave), Aceptación | 35, 74-75 | **Débil** — identificada por Paciente + Menú + FechaConsumo. |

### Nomencladores (catálogos cerrados)

| # | Nomenclador | Valores | Evidencia (líneas) |
|---|---|---|---|
| N1 | **Grupo nutricional** | no enumerados (catálogo institucional) | 24-25 (explícito: *"nomenclador del sistema"*), 27-28, 70-71 |
| N2 | **Tipo de preparación** | entrante, plato fuerte, postre, bebida | 25, 30-31 |
| N3 | **Nivel calórico** | bajo, medio, alto | 26-27, 31 |
| N4 | **Restricción alimentaria** *(decisión D5)* | no enumerados (p. ej. sin gluten, hiposódica) | 27, 42-43 |
| N5 | **Programa de atención** *(decisión D4)* | no enumerados | 42 |
| N6 | **Especialidad** (de Nutricionista) | no enumerados | 44 |

### Actores candidatos

| # | Actor | Acciones según el enunciado | Tratamiento en el MER |
|---|---|---|---|
| A1 | **Nutricionista** | Ingresa/clasifica/nivela alimentos y platos; crea menús; valida; ejecuta revalorización | **Entidad** E5 (se registran sus datos) |
| A2 | **Paciente** | Accede a menús asignados; consulta valoraciones; solicita revalorización | **Entidad** E6 |
| A3 | **Jefe de nutrición** | Revisa y aprueba menús; ordena confección de otros | Rol de Nutricionista (*D2*), sin datos propios |
| A4 | **Administrador del sistema** | Evita duplicados de alimentos; impide asignación doble | No es dato de dominio, es un rol de mantenimiento de la aplicación (*D3*) |
| A5 | **Revisor** (F3) | Valida menús con fecha y observaciones | Rol asociado a la relación `Valida` (coincide con A3) |

### Relaciones candidatas

> **Convención adoptada:** un atributo se deja en la relación solo cuando el hecho depende intrínsecamente del par de entidades relacionadas (por ejemplo, la cantidad de un ingrediente en un plato, o la fecha y observaciones de una validación puntual). Cuando el hecho es 1:1 respecto a una de las entidades — como la fecha de creación de un menú o la fecha de solicitud de una revalorización — se modela como atributo nativo de esa entidad y no de la relación, para no duplicar información. Por esa razón `FechaCreación` vive en Menú y `FechaSolicitud`/`Resultado` viven en Revalorización, no en las relaciones `Crea` o `Solicita`.

| Relación | Participantes | Evidencia (líneas) | Cardinalidad | Atributos propios |
|---|---|---|---|---|
| Describe/Ingresa | Nutricionista → Alimento/Plato | 29, 78-80 | 1:N (un alimento lo describe un único nutricionista) | — |
| Clasifica | Nutricionista → Alimento (grupo, tipo) | 30-31 | — | — |
| Nivela | Nutricionista → Alimento (nivel calórico) | 31 | — | — |
| Autoriza/Atiende | Nutricionista ↔ Dieta | 32, 44-45 | N:M | — |
| Crea | Nutricionista → Menú | 25, 31, 68-69 | 1:N (un menú lo crea un único nutricionista) | — |
| Valida/Aprueba | Nutricionista (revisor/jefe) → Menú | 25, 40-41, 72-73 | 1:N | **FechaValidación, Observaciones** |
| Generado para | Menú → Dieta | 68-71 | N:1 (varios menús pueden generarse para una misma dieta) | — |
| Incluye | Menú → Alimento | 31 | N:M | — |
| Compone | Plato → Alimento | 54-55 | N:M | **Cantidad** |
| Corresponde | Dieta → Programa de atención | 42 | N:1 | — |
| Cubre | Dieta ↔ Grupo nutricional | 42-43 | N:M | — |
| Restringe | Dieta ↔ Restricción alimentaria | 27, 42-43 | N:M | — |
| Distribuye | Menú ↔ Tipo de preparación | 38-40 | N:M | **Proporción** |
| Cubre | Menú ↔ Grupo nutricional | 38-40 | N:M | — |
| Pertenece | Paciente → Sala | 49 | N:1 | — |
| Inscribe | Paciente ↔ Dieta | 49-50 | N:M | — |
| Asigna | Menú → Paciente | 35 | **1:1** (un menú se asigna a un único paciente; restricción explícita en línea 33-34 que el administrador debe hacer cumplir) | — |
| **Consume** (identificadora) | Paciente + Menú → Consumo | 35, 74-75 | — | FechaConsumo, **Aceptación** |
| Tiene | Paciente → Valoración | 35-36, 50 | 1:N | — |
| Evalúa | Dieta → Valoración | 50 | 1:N | — |
| Solicita | Paciente → Revalorización | 36 | 1:N | — |
| Ejecuta | Nutricionista → Revalorización | 36 | 1:N | — |

**Nota sobre la `Cubre` repetida:** aparece dos veces en la tabla con participantes distintos (Dieta↔GrupoNutricional y Menú↔GrupoNutricional) porque ambas expresan el mismo hecho — "cubre este grupo nutricional" — sobre dos entidades distintas del dominio; no es una relación única compartida, son dos ocurrencias independientes del mismo verbo relacional aplicado a dos entidades distintas.

**Nota sobre `Incluye`:** la única evidencia textual directa (línea 31) dice que el menú se genera "a partir de los alimentos seleccionados", sin mencionar platos. Antes se asumía también `Menú → Plato`, pero esa relación no tiene respaldo textual propio; queda condicionada a cómo se resuelva D1 (si el plato termina siendo un alimento compuesto, entra al menú igual que cualquier alimento a través de `Incluye`; si se mantiene como catálogo aparte, habría que decidir si el menú puede incluir platos directamente o solo a través de sus alimentos componentes).

## Atributos especiales

> **Corrección aplicada:** el MERX de la cátedra prohíbe los atributos multivaluados y compuestos (`Atributos.pdf`). De las cuatro filas que esta sección tenía en la versión aprobada por el PR #1, tres no son atributos: son relaciones N:M (`Cubre`, `Restringe`, `Distribuye`, ver la tabla de relaciones candidatas). Solo la ficha nutricional sigue siendo un caso especial, porque no es un atributo multivaluado sino un documento externo gestionado en otro motor de almacenamiento (MongoDB, issue #8) — una categoría distinta a la que esta prohibición no aplica.

| Atributo | Modo | Justificación textual |
|---|---|---|
| Ficha nutricional ampliada | **Documento (NoSQL), gestor aparte** | Líneas 51-57: estructura variable, anidada, no normalizable; "no debe modelarse junto al resto de las tablas". |

## Decisiones abiertas

### D1 — ¿Plato es especialización de Alimento o entidad con composición propia?

- **Composición, Plato como entidad separada (opción actual):** la línea 54-55 dice literalmente *"un plato compuesto por múltiples ingredientes"* → el plato se descompone en alimentos con cantidades. El enunciado administra "alimentos y platos" como dos catálogos paralelos (líneas 23-24, 29): misma operación de alta, pero no se afirma en ningún punto que "un plato sea un alimento".
- **Especialización (ISA de Alimento) — evidencia reforzada:** la línea 53 contrapone explícitamente *"un alimento simple puede describirse con pocos campos, mientras que un plato compuesto por múltiples ingredientes requiere una ficha considerablemente más extensa"*. Esta redacción trata al plato como el caso compuesto de la misma noción de alimento, no como un concepto aparte, lo cual respalda más de lo que se reconocía antes la alternativa de especialización. Bajo esta lectura, el menú incluiría platos con un único `Incluye`, heredando grupo/tipo/nivel, y la composición se modelaría como auto-relación recursiva sobre Alimento.
- **Recomendación:** dado que el resto del enunciado (líneas 23-24, 29) sigue tratando "alimentos y platos" como dos sustantivos distintos en cada enumeración, se mantiene Plato como entidad separada con `Compone(Plato, Alimento, Cantidad)`, pero se deja constancia explícita de que la línea 53 es evidencia real a favor de la alternativa ISA. Se recomienda confirmar con el profesor cuál interpretación espera la cátedra, ya que ambas son defendibles con cita textual.

### D2 — ¿Jefe de nutrición es especialización de Nutricionista o rol/atributo?

**Evidencia:** línea 40-41 (revisa y aprueba, ordena confección de otro) y F3 (menús "validados por un revisor" con fecha y observaciones). El jefe no tiene ningún atributo ni relación propia más allá de validar.

- **Rol/atributo (recomendada):** la aprobación ya vive en `Valida(Nutricionista, Menú; FechaValidación, Observaciones)`. Basta con un atributo o subtipo ligero en Nutricionista, o simplemente interpretar que quien valida ejerce el rol de revisor. Sin atributos propios, una especialización completa sería sobremodelado.
- **Especialización:** solo se justificaría si el jefe tuviera estado propio (por ejemplo, supervisión de nutricionistas o presupuesto), y el enunciado no lo menciona.

### D3 — Administrador del sistema

**Evidencia:** línea 33-34 — tareas de integridad y mantenimiento (eliminar duplicados, impedir asignación doble). No se almacenan sus datos ni sus acciones se consultan en F1-F6; nada indica que sea un usuario autenticado del dominio.

- **Fuera del MER (recomendada):** es un actor de mantenimiento de la aplicación; sus tareas son restricciones de integridad sobre `Alimento` y sobre la relación `Asigna`, no datos de dominio.
- **Entidad/rol Usuario:** solo si la aplicación exige autenticación y auditoría, lo cual es una decisión de Ingeniería de Software más que de Bases de Datos.

### D4 — Programa de atención

**Evidencia:** línea 42 — la dieta "corresponde a" un programa. Sin atributos propios ni consultas sobre programas en F1-F6.

- **Nomenclador (recomendada):** catálogo cerrado de programas institucionales, referenciado por Dieta.
- **Entidad:** se promovería solo si cada programa tuviera responsables, vigencia o recursos propios, y el enunciado no lo pide.

### D5 — Restricción alimentaria

**Evidencia:** línea 27 (criterio de adaptación del menú) y 42-43 ("lista de restricciones o grupos nutricionales a cubrir").

- **Nomenclador (recomendada):** catálogo de tipos (sin gluten, hiposódica, etc.) relacionado con Dieta mediante `Restringe(Dieta, RestricciónAlimentaria)` N:M — no un atributo multivaluado, corregido porque el MERX de la cátedra prohíbe los atributos multivaluados/compuestos. La compatibilidad alimento-restricción se resuelve contrastando la restricción contra los alérgenos de la ficha nutricional (líneas 51-52), no mediante una relación adicional distinta de `Restringe`.
- **Entidad:** solo si la restricción necesitara atributos propios (severidad, contraindicaciones) o se quisiera asociar directamente a Alimento/Plato.

### D6 — "Lista de restricciones" frente a "grupos nutricionales a cubrir"

La línea 42-43 los enuncia como dos listas distintas; conviene confirmar si se mantienen como dos relaciones N:M independientes — `Cubre(Dieta,GrupoNutricional)` y `Restringe(Dieta,RestricciónAlimentaria)` — (más fiel al enunciado) o se fusiona el nomenclador en uno solo (más simple de implementar). Requiere coherencia con el diagrama que finalmente se dibuje.

### D7 — "Plan de alimentación / tratamiento"

Terminología que el enunciado usa como sinónimo del ciclo de la dieta (líneas 27-28, 42-43). Decisión: no crear una entidad nueva; documentar la sinonimia en el glosario de datos del proyecto.

### D8 — ¿Existe un estado o versión del Menú? *(decisión nueva, detectada en esta revisión)*

**Evidencia:** línea 40-41 — el jefe de nutrición "puede indicar la confección de otro [menú] bajo sus criterios", lo que sugiere que un menú puede ser rechazado y sustituido por uno nuevo para la misma dieta. La línea 70-71 (F2) habla de "menús finales de una dieta" para calcular los alimentos más usados, lo cual implica que no todo menú generado alcanza ese estado.

- **Sin atributo explícito (recomendada, más simple):** un menú se considera "final"/aprobado si existe un registro en `Valida` con resultado positivo; no se necesita un atributo `Estado` adicional, basta con derivarlo de la relación de validación.
- **Con atributo `Estado` en Menú:** necesario si el jefe puede rechazar un menú explícitamente (no solo ignorarlo y pedir uno nuevo), y ese rechazo debe quedar registrado como tal para trazabilidad o para las consultas de F2 y F5. El enunciado no dice explícitamente que el rechazo se registre, así que esto queda abierto para confirmar con el equipo.

## Resumen ejecutivo de la lista cerrada

| Tipo | Lista final |
|---|---|
| **Entidades (10)** | Alimento, Plato, Dieta, Menú, Nutricionista, Paciente, Sala, Valoración Nutricional, Revalorización, Consumo (débil) |
| **Nomencladores (6)** | Grupo nutricional, Tipo de preparación, Nivel calórico, Restricción alimentaria*, Programa de atención*, Especialidad |
| **Actores (5)** | Nutricionista, Paciente, Jefe de nutrición (rol), Administrador del sistema (externo al dominio), Revisor (rol de `Valida`) |
| **Relaciones (22, incluida la identificadora)** | ver la tabla de relaciones candidatas — 21 relaciones ordinarias (incluye `Restringe`, `Distribuye` y `Cubre(Menú,·)`, agregadas al corregir los atributos multivaluados/compuestos) + `Consume` como relación identificadora de la entidad débil Consumo |
| **Atributos especiales** | Ficha nutricional ampliada (documento NoSQL, issue #8) — es el único caso; los que antes figuraban aquí como multivaluados (Parámetros de generación, Restricciones, Grupos a cubrir) son relaciones N:M (el MERX no admite atributos multivaluados/compuestos) |
| **Derivados (no entidades)** | Disponibilidad calórica, desempeño nutricional, tasas de aceptación/rechazo, ranking de alimentos, criterios de equilibrio, reportes F1-F6 |

\* Nomenclador condicionado a las decisiones D5/D4.

## Conclusiones y siguientes pasos

El barrido cubre íntegramente las secciones "Descripción del problema" y "Funcionalidades" del enunciado; no quedan fragmentos con contenido de dominio sin clasificar. La lista cerrada de 10 entidades, 6 nomencladores, 5 actores y 22 relaciones puede tomarse como base estable para dibujar el MER, siempre que el equipo resuelva antes las decisiones abiertas (D1-D8) — en particular D1, porque determina si Plato y Alimento son dos entidades con una relación de composición o una jerarquía de especialización, lo cual cambia la forma de varias relaciones (`Incluye`, `Describe`, `Clasifica`, `Nivela`). Se recomienda llevar D1, D2, D3 y D8 a la próxima reunión con el profesor, ya que son las que más impacto tienen sobre el diagrama y las que peor se resuelven por consenso interno del equipo sin una referencia externa.
