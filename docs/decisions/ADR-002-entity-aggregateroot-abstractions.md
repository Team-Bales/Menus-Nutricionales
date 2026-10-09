# ADR-002: Abstracciones base `Entity<TId>` y `AggregateRoot<TId>`

**Estado:** Aceptado  
**Fecha:** 2026-10-09

## Contexto

El dominio tiene más de diez tipos con identidad propia (Alimento, Dieta, Menú,
Paciente, etc.). Sin una clase base común, cada entidad implementaría `Equals`,
`GetHashCode` y los operadores `==` / `!=` de forma repetida y potencialmente
inconsistente, violando DRY y abriendo la puerta a bugs sutiles (igualdad por
referencia donde debería ser por identidad).

Además, no todas las entidades son iguales desde el punto de vista del diseño:
algunas son raíces de agregado con repositorio propio, y otras viven solo dentro
de un agregado y no se acceden de forma independiente.

## Decisión

Creamos dos clases base en `Menus.Domain/Abstractions/`:

**`Entity<TId>`** — para cualquier objeto del dominio con identidad:
- La igualdad se determina por `Id`, no por referencia
- Sobreescribe `Equals`, `GetHashCode`, `==` y `!=`
- Es la base de todas las entidades del dominio, incluyendo catalog entities

**`AggregateRoot<TId> : Entity<TId>`** — para las raíces de agregado:
- Hereda toda la lógica de identidad de `Entity<TId>`
- Marca semánticamente que este tipo es el punto de entrada a su clúster
- Solo los `AggregateRoot` tienen un `IRepository` asociado

Entidades que extienden `AggregateRoot<Guid>`: `Alimento`, `Plato`, `Dieta`,
`Menú`, `Nutricionista`, `Paciente`, `ValoraciónNutricional`.

Entidades que extienden `Entity<Guid>`: `GrupoNutricional`, `RestriccionAlimentaria`,
`ProgramaAtencion`, `Especialidad`, y entidades débiles como `Consumo`.

## Alternativas consideradas

| Alternativa | Razón de descarte |
|-------------|-------------------|
| Sin clase base (cada entidad implementa Equals) | Código duplicado, riesgo de inconsistencias |
| Una sola clase base `BaseEntity` sin distinción de agregado | No comunica la semántica de quién tiene repositorio y quién no |
| Usar `record` en lugar de `class` | Los records de C# son value objects por convención; usarlos para entidades con identidad es confuso |

## Consecuencias

- ✅ Igualdad por identidad garantizada en todo el dominio sin repetición
- ✅ La distinción `Entity` / `AggregateRoot` hace explícito el diseño del modelo
- ✅ Solo se crean repositorios para `AggregateRoot`, reduciendo la superficie de la API
- ⚠️ EF Core requiere que las entidades tengan constructores accesibles o usar shadow properties para mapear `TId`
