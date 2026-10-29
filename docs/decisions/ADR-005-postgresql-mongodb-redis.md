# ADR-005: Tres motores de persistencia: PostgreSQL, MongoDB y Redis

**Estado:** Aceptado  
**Fecha:** 2026-10-09

> **Actualización del 2026-10-28.** La decisión no cambia; se ajustan dos detalles al diseño del informe (`docs/informe.md`), que remite a este ADR para justificar los tres motores:
> - **MongoDB:** con la generalización `ItemAlimentario` (§2.1), hay ficha para alimentos y para platos, no solo para alimentos. `origen.tabla` guarda la clase del ítem (RM-01 a RM-03, §4.3). Las restricciones de la ficha y cómo se mantiene coherente con PostgreSQL (RM-09, RM-10) están en `docs/Ficha nutricional MongoDB/`.
> - **Redis:** cachea las dos consultas recurrentes, la disponibilidad calórica de una dieta y las valoraciones más recientes de un paciente, con *cache-aside*, invalidación por evento y tiempo de vida como respaldo. Si Redis no responde, la consulta va a PostgreSQL, y se pierde latencia, no corrección (informe, «Consultas recurrentes»).

## Contexto

El enunciado establece dos requisitos de persistencia que no encajan en un único
motor relacional clásico:

1. **Ficha nutricional ampliada**: "la cantidad y tipo de atributos varía de un
   alimento a otro y no se ajusta a una estructura fija ni común para todos los
   casos" — un esquema relacional rígido forzaría columnas nulas o una tabla EAV
   extremadamente difícil de consultar.

2. **Disponibilidad calórica y valoraciones recientes**: "el sistema debe garantizar
   respuestas prácticamente inmediatas ante esas peticiones recurrentes, incluso en
   los horarios de mayor concurrencia" — una consulta a PostgreSQL bajo carga no
   garantiza latencia sub-milisegundo.

Además, la asignatura **Bases de Datos II** evalúa el uso de tecnologías NoSQL,
por lo que incorporar al menos un motor no relacional es un requisito implícito
del curso.

## Decisión

| Motor | Responsabilidad |
|-------|----------------|
| **PostgreSQL** | Datos relacionales: entidades del dominio, relaciones N:M, constraints de integridad, las 6 consultas informacionales |
| **MongoDB** | Ficha nutricional ampliada de cada ítem alimentario, alimento o plato (atributos variables por documento) |
| **Redis** | Caché de disponibilidad calórica de dietas y valoraciones recientes |

Cada motor resuelve el problema para el que está optimizado. PostgreSQL mantiene
la integridad referencial del modelo. MongoDB almacena documentos de esquema flexible
sin columnas nulas ni tablas auxiliares. Redis sirve lecturas frecuentes en memoria.

## Alternativas consideradas

| Alternativa | Razón de descarte |
|-------------|-------------------|
| Solo PostgreSQL con columna JSONB para la ficha | JSONB funciona pero la consulta de atributos individuales es menos expresiva y no justifica el uso de NoSQL para la asignatura BD2 |
| PostgreSQL + MongoDB sin Redis | No cumple el requisito de respuestas inmediatas bajo concurrencia |
| Solo MongoDB | Pierde las garantías ACID de las transacciones relacionales necesarias para la generación de menús |

## Consecuencias

- ✅ La ficha nutricional puede tener atributos totalmente distintos por ítem sin migraciones
- ✅ La disponibilidad calórica y las valoraciones recientes se sirven desde memoria mientras su clave está en Redis; si no está, o si Redis no responde, se calculan en PostgreSQL, que es la única fuente de verdad
- ✅ Cumple el requisito de tecnologías NoSQL de BD2
- ⚠️ Tres motores implican tres conexiones, tres contenedores Docker y mayor complejidad operativa
- ⚠️ Las transacciones no pueden abarcar PostgreSQL y MongoDB simultáneamente — se maneja con consistencia eventual: la aplicación borra la ficha junto con su ítem (RM-09), y una tarea periódica elimina las fichas huérfanas y reconstruye los nombres copiados (RM-10)
