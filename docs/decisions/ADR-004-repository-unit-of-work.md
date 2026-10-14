# ADR-004: Separación de `IRepository<T>` e `IUnitOfWork`

**Estado:** Aceptado  
**Fecha:** 2026-10-09

## Contexto

La capa `Application` necesita acceder a los datos sin conocer EF Core ni ningún
motor de persistencia concreto (principio de inversión de dependencias). Había que
definir cómo exponer las operaciones de lectura/escritura y cómo coordinar
transacciones que afectan a múltiples agregados en un mismo caso de uso.

Por ejemplo, generar un menú implica crear el `Menú` y posiblemente actualizar
el estado de la `Dieta` asociada — dos agregados distintos que deben persistirse
de forma atómica.

## Decisión

Definimos dos interfaces independientes en `Menus.Domain/Abstractions/`:

**`IRepository<TEntity, TId>`** — operaciones CRUD por agregado:
```csharp
Task<TEntity?> GetByIdAsync(TId id, CancellationToken ct);
Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken ct);
Task AddAsync(TEntity entity, CancellationToken ct);
void Remove(TEntity entity);
```
No expone `SaveChanges` — solo acumula cambios en memoria.

**`IUnitOfWork`** — confirmación atómica de todos los cambios pendientes:
```csharp
Task<int> CommitAsync(CancellationToken ct);
```

Un caso de uso recibe ambas interfaces. Usa los repositorios para preparar
los cambios y llama a `CommitAsync` una sola vez al final, garantizando
que todo se persiste o nada se persiste.

`IUnitOfWork` cubre únicamente PostgreSQL (EF Core). Las escrituras a MongoDB
son operaciones independientes que no participan de esta transacción relacional.

## Alternativas consideradas

| Alternativa | Razón de descarte |
|-------------|-------------------|
| `SaveChanges` dentro de cada repositorio | Impide transacciones que abarcan varios agregados |
| Un solo `IRepository` con `SaveChanges` incluido | Acopla la transacción al repositorio; dificulta tests |
| Usar directamente el `DbContext` en `Application` | Rompe la regla de dependencia: `Application` no debe conocer EF Core |

## Consecuencias

- ✅ Los casos de uso coordinan transacciones multi-agregado sin conocer EF Core
- ✅ Los repositorios son fáciles de sustituir por mocks en tests de `Application`
- ✅ Una sola llamada a `CommitAsync` al final de cada caso de uso reduce errores
- ⚠️ El desarrollador debe recordar llamar a `CommitAsync` — si se olvida, los cambios no persisten
