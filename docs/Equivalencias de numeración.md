# Equivalencias de numeración

El informe §4 renumeró las reglas de integridad; el código conserva los nombres de objetos antiguos (archivos `.sql`, funciones `fn_*`, triggers `trg_*`, constraints `ck_*`, `PK_*`, `FK_*`, índices `ux_*`, `IX_*`). Esta tabla establece la equivalencia entre la numeración antigua (R5–R11) y la vigente (RC/RR/RD/RK/RS/RM/RX del informe §4), con el objeto de BD que implementa cada regla.

---

## Convenciones de la tabla

«Objeto» es el nombre exacto en PostgreSQL (no cambia). `IC` = `Persistence/Migrations/20261025234333_InitialCreate.cs`. `Sql/` = `Persistence/Sql/`. «App» = capa de aplicación o dominio, sin objeto en la BD. Una regla antigua puede equivaler a varias vigentes y al revés.

---

### Clave (RC)

| Antigua | Vigente | Qué es | Objeto de BD | Dónde |
|---|---|---|---|---|
| R5-01 | RC-01 | `ItemId` identifica cada ítem (alimento o plato) | PK `PK_items_alimentarios` | IC |
| R5-16 (clave candidata) | RC-01 y RS-01 | Nombre normalizado único entre alimentos y platos | índice único `ux_items_alimentarios_r5_16_nombre` | `Sql/R5_16_nombre_normalizado.sql` |
| R5-02 | RC-02 | PK de los nomencladores de ítem | PK `PK_grupos_nutricionales`, `PK_tipos_preparacion`, `PK_niveles_caloricos` | IC |
| R5-03 | RC-03 | (plato, alimento) identifica cada `Compone` | PK `PK_composicion` | IC |
| R6-01 | RC-04 | PK de dieta, nutricionista, paciente y sala | PK `PK_dietas`, `PK_nutricionistas`, `PK_pacientes`, `PK_salas` | IC |
| R6-02 | RC-05 | PK de programa, restricción alimentaria y especialidad | PK `PK_programas_atencion`, `PK_restricciones_alimentarias`, `PK_especialidades` | IC |
| R6-03 | RC-06 | (nutricionista, dieta) identifica cada autorización | PK `PK_autoriza` | IC |
| R6-04 | RC-07 | (paciente, dieta) identifica cada inscripción | PK `PK_paciente_dieta` | IC |
| R6-05, R6-05b | RC-08 | Pares de `CubreDieta` y de `Restringe` | PK `PK_cubre_dieta`, `PK_restringe` | IC |
| R6-05c, R9-01 | RC-16 | (restricción, grupo) identifica cada `Excluye` | PK `PK_excluye` | IC |
| R7-01 | RC-09 | PK de menú, valoración y revalorización | PK `PK_menus`, `PK_valoraciones_nutricionales`, `PK_revalorizaciones` | IC |
| R7-02 | RC-10 | PK de `ResultadoNutricional` | PK `PK_resultados_nutricionales` | IC |
| R7-03 | RC-11 | (menú, tipo de preparación) identifica cada `Distribuye` | PK `PK_menu_distribucion` | IC |
| R7-04 | — (retirada) | Era (revisor, menú); la sustituye R7-32 | — | — |
| R7-05 | RC-13 | (menú, paciente) identifica cada asignación | PK `PK_asignacion` | IC |
| R7-06 | RC-14 (reformulada) | Antes (menú, paciente, alimento); ahora (menú, paciente, fecha, ítem) | PK `PK_consumo` | IC |
| — (nueva) | RC-17 | (menú, paciente, fecha de servicio) identifica cada servicio | PK `PK_servicios` | IC |
| R7-07 | RC-15 | Un ítem y un grupo aparecen una vez por menú | PK `PK_menu_item`, `PK_menu_grupo_nutricional` | IC |
| R7-32 | RC-12 y RK-13 | Un menú recibe a lo sumo un dictamen: `menu_id` es la PK | PK `PK_validacion` | IC |

---

### Referenciales (RR)

| Antigua | Vigente | Qué es | Objeto de BD | Dónde |
|---|---|---|---|---|
| R5-04 | RR-01 | La clasificación del ítem referencia el nomenclador | FK `FK_items_alimentarios_*` (grupo, tipo, nivel) | IC |
| R5-05 | RR-02 | `Compone` referencia plato y alimento | FK de `composicion` hacia `items_alimentarios` | IC |
| R5-06 | RR-03 | El ítem referencia al nutricionista que lo describió | FK `FK_items_alimentarios_nutricionistas_nutricionista_id` | IC |
| R5-07 | RR-04 | `RESTRICT`: no se borra un valor de nomenclador ni un alimento en uso | `ON DELETE RESTRICT` de esas FK | IC |
| R6-06 | RR-05 | Dieta → programa de atención | FK `FK_dietas_programas_atencion_programa_atencion_id` | IC |
| R6-07 | RR-06 | `CubreDieta` → dieta y grupo | FK `FK_cubre_dieta_*` | IC |
| R6-08 | RR-07 | `Restringe` → dieta y restricción | FK de `restringe` | IC |
| R6-08b, R9-02 | RR-20 | `Excluye` → restricción y grupo | FK `FK_excluye_*` | IC |
| R6-08b, R9-03 | RR-21 | `RESTRICT`: no se borra una restricción ni un grupo en `Excluye` | `ON DELETE RESTRICT` de `FK_excluye_*` | IC |
| R6-09 | RR-08 | Autorización → nutricionista y dieta | FK de `autoriza` | IC |
| R6-10 | RR-09 | Inscripción → paciente y dieta | FK de `paciente_dieta` | IC |
| R6-11 | RR-10 | Paciente → sala | FK `FK_pacientes_salas_sala_id` | IC |
| R6-12 | RR-11 | Nutricionista → especialidad | FK `FK_nutricionistas_especialidades_especialidad_id` | IC |
| R6-13 | RR-12 | `RESTRICT` en nomencladores de dieta/nutricionista y en dietas con autorizaciones o inscripciones | `ON DELETE RESTRICT` | IC |
| R7-08 | RR-13 | Menú → dieta y creador | FK `FK_menus_dietas_dieta_id`, `FK_menus_nutricionistas_nutricionista_creador_id` | IC |
| R7-09 | RR-14 | `Incluye`, `CubreMenú`, `Distribuye` → menú y su referente | FK `FK_menu_item_*`, `FK_menu_grupo_nutricional_*`, `FK_menu_distribucion_*` | IC |
| R7-10 | RR-15 | Validación → nutricionista y menú | FK `FK_validacion_menus_menu_id`, `FK_validacion_nutricionistas_nutricionista_id` | IC |
| R7-11 | RR-16 | Asignación, servicio y consumo; el consumo referencia la inclusión (menú, ítem) | FK `FK_asignacion_*`, `FK_servicios_asignacion_menu_id_paciente_id`, `FK_consumo_servicios_*`, `FK_consumo_menu_item_menu_id_item_id` | IC |
| R7-12 | RR-17 | Valoración → asignación, emisor y resultado | FK `FK_valoraciones_nutricionales_*` | IC |
| R7-13 | RR-18 | Revalorización → ejecutor, valoración revisada y generada | FK `FK_revalorizaciones_*` | IC |
| R7-14 | RR-19 | `RESTRICT`: no se borra un menú con validación/asignaciones, ni una asignación con servicios o valoraciones, etc. | `ON DELETE RESTRICT` de esas FK | IC |
| R11-02 (parte) | RR-22 | `RESTRICT`: no se borra un nutricionista/paciente con hechos asociados ni un ítem que figure en un menú | `ON DELETE RESTRICT` de esas FK | IC |
| R7-38 (parte) | RR-23 | Menú de reemplazo → validación que lo originó, `RESTRICT` | FK `FK_menus_validacion_validacion_origen_id` | IC |

---

### Dominio (RD)

| Antigua | Vigente | Qué es | Objeto de BD | Dónde |
|---|---|---|---|---|
| R5-08 | RD-01 | `TipoPreparación` ∈ {Entrante, Plato fuerte, Postre, Bebida} | CHECK `ck_tipos_preparacion_r5_08_nombre` + índice único `IX_tipos_preparacion_nombre` | IC |
| R5-09 | RD-02 | `NivelCalórico` ∈ {Bajo, Medio, Alto} | CHECK `ck_niveles_caloricos_r5_09_nombre` + índice único `IX_niveles_caloricos_nombre` | IC |
| R5-10 | RD-03 | `GrupoNutricional` abierto, nombre único | índice único `IX_grupos_nutricionales_nombre` | IC |
| R5-11 | RD-04 | Nombre no vacío en ítem y nomencladores de ítem | CHECK `ck_items_alimentarios_r5_11_nombre`, `ck_grupos_nutricionales_r5_11_nombre` | IC |
| R5-12 | RD-05 | `Cantidad` de `Compone` > 0 | CHECK `ck_composicion_r5_12_cantidad` | IC |
| R6-14 | RD-06 | `Edad` ≥ 0 | CHECK `ck_pacientes_r6_14_edad` | IC |
| R6-15 | RD-07 | Nombre no vacío en dieta, nutricionista, paciente, sala y nomencladores | CHECK `ck_dietas_r6_15_nombre`, `ck_nutricionistas_r6_15_nombre`, `ck_pacientes_r6_15_nombre`, `ck_salas_r6_15_nombre`, `ck_programas_atencion_r6_15_nombre`, `ck_restricciones_alimentarias_r6_15_nombre`, `ck_especialidades_r6_15_nombre` | IC |
| R6-16 | RD-08 | Nombre único por nomenclador | índice único `IX_programas_atencion_nombre`, `IX_restricciones_alimentarias_nombre`, `IX_especialidades_nombre` (y los de RD-01/RD-02/RD-03) | IC |
| R6-17 | RD-09 | `Edad` entera | tipo `integer` de `pacientes.edad` | IC |
| R7-15 | RD-10 | `CantidadTotalAlimentos` > 0 | CHECK `ck_menus_r7_15_cantidad_total_alimentos` | IC |
| R7-16 | RD-11 | `Proporción` ∈ (0, 1] | CHECK `ck_menu_distribucion_r7_16_proporcion` | IC |
| R7-17 | RD-12 | `EsAutomático` y `Aprobado` booleanos obligatorios | `NOT NULL` de `menus.es_automatico`, `validacion.aprobado` | IC |
| R7-18 | RD-13 | `Aceptación` booleana obligatoria | `NOT NULL` de `consumo.aceptacion` | IC |
| R7-19 | RD-14 | Cinco categorías de `ResultadoNutricional`, valores 1-5 | CHECK `ck_resultados_nutricionales_r7_19_nombre`, `ck_resultados_nutricionales_r7_19_valor` + índices únicos `IX_resultados_nutricionales_nombre`, `IX_resultados_nutricionales_valor` | IC |
| R7-20 | RD-15 | Fechas obligatorias; `FechaServicio` es `date` | `NOT NULL`; `servicios.fecha_servicio` y `consumo.fecha_servicio` de tipo `date` | IC |
| R6-17b | RD-16 | `Tipo` de `CubreDieta` ∈ {Requerido, Restringido} | CHECK `ck_cubre_dieta_r6_17b_tipo` | IC |
| R5-19 (CHECK) | RD-17 | `Clase` ∈ {Alimento, Plato} | CHECK `ck_items_alimentarios_r5_19_clase` | IC |
| — (nueva) | RD-18 | `es_jefe` booleano, obligatorio, falso por defecto | columna `nutricionistas.es_jefe` | `Migrations/20261029003427_AgregarEsJefeANutricionista.cs` |

---

### Cardinalidad (RK)

| Antigua | Vigente | Qué es | Objeto de BD | Dónde |
|---|---|---|---|---|
| R5-13 | RK-01 | Un grupo, un tipo y un nivel por ítem | FK `NOT NULL` de `items_alimentarios` | IC |
| R5-14 | RK-02 | Un nutricionista descriptor por ítem | FK `NOT NULL` | IC |
| R5-15 | RK-03 | Todo plato tiene al menos un alimento | triggers diferidos `trg_r5_15_plato`, `trg_r5_15_composicion` (función `fn_r5_15`) | `Sql/R5_15_plato_con_alimentos.sql` |
| R6-18 | RK-04 | Una dieta, un programa | FK `NOT NULL` | IC |
| R6-19 | RK-05 | Un paciente, una sala | FK `NOT NULL` | IC |
| R6-20 | RK-06 | Un nutricionista, una especialidad | FK `NOT NULL` | IC |
| R6-21 | RK-07 | Toda dieta tiene al menos un grupo `Requerido` | triggers diferidos `trg_r6_21_dieta`, `trg_r6_21_cubre` (función `fn_r6_21`) | `Sql/R6_21_dieta_con_grupo_requerido.sql` |
| R7-21 | RK-08 | Un creador y una dieta por menú | FK `NOT NULL` | IC |
| R7-22 | RK-09 | Todo menú tiene al menos una proporción y un grupo cubierto | triggers diferidos `trg_r7_22_menu`, `trg_r7_22_distribucion`, `trg_r7_22_grupos` (función `fn_r7_22`) | `Sql/R7_22_menu_con_parametros.sql` |
| R7-23 | RK-10 | Una asignación, un emisor y un resultado por valoración | FK `NOT NULL` | IC |
| R7-24 | RK-11 | Revalorización: ejecutor y valoración revisada obligatorios; genera a lo sumo una valoración | FK `NOT NULL`; índice único `IX_revalorizaciones_valoracion_generada_id` | IC |
| R5-19 (especialización) | RK-12 | Especialización total y disjunta de `ItemAlimentario` | discriminador `clase` `NOT NULL` + CHECK `ck_items_alimentarios_r5_19_clase` | IC |
| R7-32 | RK-13 | A lo sumo un dictamen por menú | PK `PK_validacion` | IC |
| — (nueva) | RK-14 | Cada servicio pertenece a una asignación | FK compuesta `FK_servicios_asignacion_menu_id_paciente_id` | IC |
| R7-38 | RK-15 | `Origina` es (0,1) en ambos extremos | índice único `ux_menus_r7_38_validacion_origen` | IC |

---

### Semánticas (RS)

| Antigua | Vigente | Qué es | Objeto de BD | Dónde |
|---|---|---|---|---|
| R5-16 | RS-01 | Sin ítems duplicados por nombre normalizado | índice único `ux_items_alimentarios_r5_16_nombre` | `Sql/R5_16_nombre_normalizado.sql` |
| R5-17 | RS-02 | El nutricionista solo clasifica lo que describió | App (el informe §4 la deja en la capa de aplicación) | — |
| R6-22 | RS-03 | Creador y revisor autorizados para la dieta | triggers `trg_r6_22_creador` (función `fn_r6_22_creador`, sobre `menus`) y `trg_validacion` (función `fn_validacion`, sobre `validacion`) | `Sql/R6_22_creador_autorizado.sql`, `Sql/R6_22_R7_25_R7_26_validacion.sql` |
| — (nueva, #189) | RS-03 (parte «jefe») | El revisor tiene `EsJefe = true` | trigger `trg_rs_03` (función `fn_rs_03`) | `Sql/RS_03_revisor_jefe.sql` |
| R6-23 | RS-04 | El paciente recibe menús de dietas en las que está inscrito | trigger `trg_r6_23` (función `fn_r6_23`) | `Sql/R6_23_paciente_inscrito.sql` |
| R7-25 | RS-05 | Nadie valida un menú que creó | trigger `trg_validacion` | `Sql/R6_22_R7_25_R7_26_validacion.sql` |
| R7-26 | RS-06 | La validación no es anterior a la creación del menú | trigger `trg_validacion` | `Sql/R6_22_R7_25_R7_26_validacion.sql` |
| R7-27 | RS-07 | Parametrización del menú inmutable | triggers `trg_r7_27_menu`, `trg_r7_27_distribucion_actualizacion`, `trg_r7_27_grupos_actualizacion`, `trg_r7_27_distribucion_borrado`, `trg_r7_27_grupos_borrado` | `Sql/R7_27_parametrizacion_inmutable.sql` |
| R7-28 | RS-08 | La suma de proporciones del menú es 1 | App: la comprueba el dominio al crear el menú (informe línea 246). **No hay trigger y no se crea** | — |
| R7-29 | RS-09 | El solicitante es el paciente de la valoración revisada | Estructura: el solicitante no se almacena | — |
| R7-30 | RS-10 | La valoración generada evalúa la misma asignación y la emite el ejecutor | trigger `trg_r7_30` (función `fn_r7_30`) | `Sql/R7_30_valoracion_generada.sql` |
| R9-04 | RS-11 | Un grupo `Requerido` no puede estar excluido por una restricción de la dieta | triggers `trg_r9_04_cubre`, `trg_r9_04_restringe`, `trg_r9_04_excluye` (función `fn_r9_04`) | `Sql/R9_04_grupo_requerido_no_excluido.sql` |
| R5-18 | RS-12 | Composición de un solo nivel | trigger `trg_r5_18` (función `fn_r5_18`) | `Sql/R5_18_composicion_de_un_nivel.sql` |
| R5-19 (trigger) | RS-13 | La clase del ítem no cambia | trigger `trg_r5_19` (función `fn_r5_19`) | `Sql/R5_19_clase_del_item.sql` |
| R7-31 | RS-14 | Ningún servicio es anterior a la aprobación del menú | trigger diferido `trg_r7_31` (función `fn_r7_31`) | `Sql/R7_31_servicio_tras_aprobacion.sql` |
| R7-33 | RS-15 | La validación no se modifica ni se borra | trigger `trg_r7_33` (función `fn_r7_33`) | `Sql/R7_33_validacion_inmutable.sql` |
| R7-34 | RS-16 | Solo se asigna un menú aprobado | trigger diferido `trg_r7_34` (función `fn_r7_34`) | `Sql/R7_34_asignacion_de_menu_aprobado.sql` |
| R7-35 | RS-17 | Solo un rechazo origina un menú de reemplazo | trigger `trg_r7_35_r7_36` (función `fn_r7_35_r7_36`) | `Sql/R7_35_R7_36_menu_de_reemplazo.sql` |
| R7-36 | RS-18 | El reemplazo es de la misma dieta | trigger `trg_r7_35_r7_36` | `Sql/R7_35_R7_36_menu_de_reemplazo.sql` |
| R7-37 | RS-19 | Un menú no es reemplazo de sí mismo | CHECK `ck_menus_r7_37_validacion_origen` | IC |

---

### Ficha nutricional (RM) y consolidación (RX)

| Antigua | Vigente | Qué es | Objeto | Dónde |
|---|---|---|---|---|
| R8-01 … R8-10 | RM-01 … RM-10 | Misma numeración, un a uno: R8-0n → RM-0n y R8-10 → RM-10 | MongoDB (validador `$jsonSchema`, índice único, aplicación, conciliación); ninguno existe aún | `docs/Ficha nutricional MongoDB/` |
| R11-01 | RX-01 (cambió el mecanismo) | El consumo es solo de ítems incluidos en el menú. El #11 proponía un trigger; hoy es una FK compuesta y **no existe trigger** | FK `FK_consumo_menu_item_menu_id_item_id` | IC |
| R11-02 | RX-02 (y RR-22) | Política de borrado: `CASCADE` en seis FK, `RESTRICT` en el resto | FK con `ON DELETE CASCADE`: `composicion`→plato, `menu_item`/`menu_distribucion`/`menu_grupo_nutricional`→menú, `cubre_dieta`/`restringe`→dieta | IC |

---

## Orden de disparo

Los nombres de los archivos, funciones, triggers, constraints e índices conservan la numeración antigua (R5…R9); la equivalencia con la vigente está en esta tabla. No se renombran: PostgreSQL dispara los triggers de un mismo evento y momento en orden alfabético de su nombre (p. ej., en `validacion`, `trg_rs_03` va antes que `trg_validacion`), y `InitialCreate` aplica los scripts en el orden alfabético de sus archivos.

| Tabla / evento | Orden de disparo (alfabético) |
|---|---|
| `validacion` `BEFORE INSERT` | `trg_rs_03` (RS-03, jefe) → `trg_validacion` (RS-03 autorizado, RS-05, RS-06) |
| `validacion` `BEFORE UPDATE` | `trg_r7_33` (RS-15) → `trg_validacion` |
| `menus` `BEFORE UPDATE OF dieta_id` | `trg_r6_22_creador` (RS-03) → `trg_r7_27_menu` (RS-07) → `trg_r7_35_r7_36` (RS-17, RS-18) |