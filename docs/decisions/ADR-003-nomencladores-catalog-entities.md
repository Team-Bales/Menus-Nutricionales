# ADR-003: Nomencladores como catalog entities con seed data

**Estado:** Aceptado  
**Fecha:** 2026-10-09

## Contexto

El dominio tiene seis nomencladores: `GrupoNutricional`, `RestriccionAlimentaria`,
`ProgramaAtencion`, `Especialidad` (catálogos institucionales abiertos) y
`TipoPreparacion`, `NivelCalorico` (conjuntos cerrados fijos según el enunciado).

Había que decidir cómo modelarlos en C#: como enums, como value objects (sin Id)
o como entidades con Id persistido en base de datos.

## Decisión

Aplicamos dos tratamientos distintos según la naturaleza del nomenclador:

**Enums de C# (`TipoPreparacion`, `NivelCalorico`):**  
Los valores están fijados explícitamente en el enunciado y no deben cambiar sin
modificar las reglas de negocio. Un enum es la representación más directa y
segura — el compilador verifica exhaustividad en `switch`.

**Catalog entities (`GrupoNutricional`, `RestriccionAlimentaria`, `ProgramaAtencion`, `Especialidad`):**  
Son catálogos institucionales *abiertos*: la institución puede añadir nuevos grupos
nutricionales o programas de atención sin modificar el código. Se modelan como
clases que extienden `Entity<Guid>`, se persisten como tablas en PostgreSQL
y se cargan con seed data inicial en la migración.

No son value objects porque tienen identidad (`Guid Id`) y viven como filas
en la base de datos referenciadas por otras entidades mediante clave foránea.

No son `AggregateRoot` porque no tienen repositorio propio — se leen directamente
mediante consultas al `DbContext` o se cargan como include en otras consultas.

## Alternativas consideradas

| Alternativa | Razón de descarte |
|-------------|-------------------|
| Todos como enums | Los catálogos abiertos requieren que el administrador pueda añadir valores sin redeployar |
| Value objects (sin Id, igualdad por valor) | No se pueden persistir como FK en otras tablas sin un Id |
| Todos con repositorio propio | Sobre-ingeniería: los nomencladores son de solo lectura tras el seed |

## Consecuencias

- ✅ Los catálogos abiertos son extensibles sin tocar el código
- ✅ Los conjuntos cerrados (`TipoPreparacion`, `NivelCalorico`) son seguros en tiempo de compilación
- ✅ Las FK en PostgreSQL garantizan integridad referencial
- ⚠️ El seed data debe mantenerse sincronizado entre entornos (dev, test, producción)
