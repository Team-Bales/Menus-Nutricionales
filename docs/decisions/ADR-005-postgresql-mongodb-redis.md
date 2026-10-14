# ADR-005: Tres motores de persistencia: PostgreSQL, MongoDB y Redis

**Estado:** Aceptado  
**Fecha:** 2026-10-09

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
| **MongoDB** | Ficha nutricional ampliada de cada `Alimento` (atributos variables por documento) |
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

- ✅ La ficha nutricional puede tener atributos totalmente distintos por alimento sin migraciones
- ✅ La disponibilidad calórica se sirve desde memoria (Redis), sin tocar PostgreSQL
- ✅ Cumple el requisito de tecnologías NoSQL de BD2
- ⚠️ Tres motores implican tres conexiones, tres contenedores Docker y mayor complejidad operativa
- ⚠️ Las transacciones no pueden abarcar PostgreSQL y MongoDB simultáneamente — se maneja con consistencia eventual
