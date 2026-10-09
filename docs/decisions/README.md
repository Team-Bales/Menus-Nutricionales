# Architecture Decision Records (ADRs)

Documentamos aquí las decisiones de arquitectura y diseño relevantes del proyecto.
Un ADR captura el **por qué** de una decisión: el contexto que la motivó, las
alternativas consideradas y las consecuencias que implica.

## Cuándo crear un ADR

- Al elegir una arquitectura, patrón o tecnología no trivial
- Al definir una abstracción base que otros componentes van a extender
- Al descartar una alternativa razonable en favor de otra
- Al establecer una convención que el equipo debe seguir

## Cuándo NO hace falta un ADR

- Decisiones locales de implementación (nombre de una variable, estructura de un método)
- Convenciones ya documentadas en el Glosario de Dominio o en el enunciado
- Cambios reversibles sin impacto en otras capas

## Estado posible de un ADR

| Estado | Significado |
|--------|-------------|
| Propuesto | En discusión, aún no aceptado |
| Aceptado | Decisión tomada y en vigor |
| Superado | Reemplazado por otro ADR (indicar cuál) |
| Rechazado | Se consideró y se descartó |

## Índice

| # | Título | Estado |
|---|--------|--------|
| [001](ADR-001-clean-architecture.md) | Clean Architecture como estructura de capas | Aceptado |
| [002](ADR-002-entity-aggregateroot-abstractions.md) | Abstracciones base `Entity<TId>` y `AggregateRoot<TId>` | Aceptado |
| [003](ADR-003-nomencladores-catalog-entities.md) | Nomencladores como catalog entities con seed data | Aceptado |
| [004](ADR-004-repository-unit-of-work.md) | Separación de `IRepository<T>` e `IUnitOfWork` | Aceptado |
| [005](ADR-005-postgresql-mongodb-redis.md) | Tres motores de persistencia: PostgreSQL, MongoDB y Redis | Aceptado |
