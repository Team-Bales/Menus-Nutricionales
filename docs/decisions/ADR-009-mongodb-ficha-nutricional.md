# ADR-009: Ficha nutricional en MongoDB: conciliación RM-10, configuración restricción–alérgeno y mantenimiento de RM-06

**Estado:** Propuesto  
**Fecha:** 2026-10-30

## Contexto

La ficha nutricional es un documento opcional que vive en MongoDB y se localiza por la pareja (`origen.tabla`, `origen.id`). Se escribe después de confirmar la fila correspondiente en PostgreSQL; al no existir una transacción común entre ambos almacenes, pueden divergir. Las reglas RM-06 a RM-10 no pueden ser declaradas ni garantizadas por el motor de base de datos, por lo que requieren lógica en la capa de aplicación.

El informe de diseño (línea 247) establece que RM-06 a RM-10 se implementan «en la capa de aplicación» e incluye una tarea programada para la conciliación RM-10. El documento «Mecanismo de referencia Postgres-Mongo» (§4 y §7) deja abierta la frecuencia de dicha conciliación. La línea 469 del informe indica que «qué alérgenos excluye cada restricción se registra en la **configuración de la aplicación**, por decisión de diseño».

Este ADR complementa al ADR-005 (que justifica el uso de MongoDB) y fija tres puntos que hoy están abiertos y bloquean la implementación de MongoDB (issues 09, 10 y 11).

## Decisión

#### (a) Frecuencia y disparador de la conciliación RM-10

| Opción | Descripción | A favor | En contra |
|---|---|---|---|
| A1 **(recomendada)** | Tarea periódica en segundo plano (`BackgroundService`) cada 24 h, con la primera ejecución unos minutos después de arrancar el proceso, y un método `ConciliarAsync()` invocable a mano | Simple; el volumen de altas/bajas de ítems es bajo (catálogo de un servicio de nutrición); la huérfana no afecta a ninguna consulta (doc. 2, §3, «Baja»); la operación es idempotente, así que varias instancias del backend no se estorban | Una ficha huérfana o un `nombre` copiado desactualizado pueden vivir hasta 24 h |
| A2 | Cada hora | Ventana más corta | Recorre toda la colección 24 veces al día sin que cambie nada casi siempre |
| A3 | Por evento: tras un fallo en el paso 2 de una baja o de un cambio de nombre, registrar la pendiente y reintentar | Reacciona en segundos | Exige una cola o tabla de pendientes que no existe; más piezas para un riesgo que el informe define como «invisible y recuperable» |
| A4 | Manual (comando administrativo) | Cero infraestructura | Depende de que alguien se acuerde |

La recomendación incluye: intervalo configurable (`Conciliacion:Intervalo`, por defecto 24 h) y un interruptor `Conciliacion:Habilitada` para desactivarla en pruebas.

#### (b) Dónde vive la configuración restricción–alérgeno

| Opción | Descripción | A favor | En contra |
|---|---|---|---|
| B1 **(recomendada)** | Sección `AlergenosPorRestriccion` en la configuración de la aplicación (`appsettings.json`, `IOptions<…>`): diccionario `restricciónAlimentariaId (GUID) → lista de alérgenos` con el nombre de la restricción como comentario | Es lo que dice el informe (línea 469: «configuración de la aplicación»); los GUID de las restricciones sembradas son fijos (`22222222-0000-0000-0000-00000000000n`); versionable en Git; sin tabla ni migración | Cambiarla exige redesplegar o recargar configuración; una restricción nueva creada en la base no tiene entrada hasta que alguien la añada (ver Consecuencias) |
| B2 | Archivo JSON propio cargado como fuente de configuración | Se puede editar sin tocar `appsettings` | Un archivo más que desplegar; mismo contenido que B1 |
| B3 | Colección propia en MongoDB | Editable en caliente | Contradice el informe (la configuración no es una ficha), y añade escritura y validación nuevas |
| B4 | Tabla en PostgreSQL o columna en `restricciones_alimentarias` | Editable en caliente, relacional | Descartada por el informe (línea 469 y §5.6): ningún nomenclador representa alérgenos y se necesitaría una migración de esquema (P-1) |

Condiciones comunes a la recomendación: los términos se normalizan (minúsculas, sin acentos, recortados) antes de comparar con `alergenos` de la ficha, que es texto libre (doc. 1, §6); una restricción sin entrada se trata como «compatibilidad desconocida» y **advierte, no bloquea** (informe línea 472).

### (c) Cómo se mantiene RM-06 (ingredientes de la ficha ↔ `Compone`)

| Opción | Descripción | A favor | En contra |
|---|---|---|---|
| C1 **(recomendada)** | Validación en los dos puntos que nombra el doc. 2: **(i)** al guardar la ficha de un plato, el conjunto `ingredientes.alimentoId` debe ser igual al de `composicion` en PostgreSQL, o se rechaza la ficha; **(ii)** al cambiar la composición de un plato que ya tiene ficha con `ingredientes`, el caso de uso rechaza el cambio salvo que el mismo comando traiga la ficha actualizada (se escribe PostgreSQL primero y la ficha después, como en el resto de operaciones). La conciliación RM-10 **no corrige** RM-06: solo registra una advertencia por cada divergencia que encuentre | Cubre la regla donde nace la divergencia; coherente con la «regla de orden» del doc. 2; no hay un segundo mecanismo que corrija en silencio | El caso de uso de editar composición tiene que conocer la ficha |
| C2 | La aplicación deriva `ingredientes.alimentoId` de `composicion` al guardar (el cliente solo aporta `aporte` por ingrediente) | Imposible que diverjan al guardar | Cambia el contrato de la ficha del doc. 1 (§4, `alimentoId` aportado por quien escribe); no resuelve el cambio posterior de `Compone` |
| C3 | Solo detectar en la conciliación | Sin lógica en los casos de uso | Deja la regla incumplida hasta 24 h; no es «mantener» RM-06 |

Para las tres opciones: **el informe no pide reglas entre bloques nutricionales** (RM-05: esos bloques no tienen campos obligatorios).

**Alcance de código.** Este ADR no toca código. La implementación de (b) y (c) pertenece a la capa de aplicación (próximas versiones). Los issues 09 (#203), 10 y 11 solo usan (a), y solo para elegir el mecanismo de planificación de la conciliación.

## Alternativas consideradas

Las alternativas se presentan en las tablas de cada punto de la decisión.

## Consecuencias

- Con A1, la huérfana o el nombre desactualizado pueden persistir hasta 24 h (o el intervalo configurado).
- Con B1, mantener la configuración al crear restricciones nuevas es una tarea de administración, no de la base de datos.
- Con C1, el caso de uso «editar composición» depende del repositorio de fichas.
- No se añaden reglas de coherencia entre macronutrientes y micronutrientes ni de otro tipo; el informe no las pide.