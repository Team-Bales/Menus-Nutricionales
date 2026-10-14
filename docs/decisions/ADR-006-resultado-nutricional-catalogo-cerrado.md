# ADR-006: `ResultadoNutricional` como entidad de catálogo cerrado

**Estado:** Aceptado  
**Fecha:** 2026-10-13

## Contexto

El ADR-003 fija dos tratamientos para los nomencladores: los catálogos cerrados por el
enunciado son enums de C# (`TipoPreparacion`, `NivelCalorico`) y los catálogos abiertos
son entidades persistidas con seed (`GrupoNutricional`, `RestriccionAlimentaria`,
`ProgramaAtencion`, `Especialidad`).

`ResultadoNutricional` es el séptimo nomenclador del MERX y quedó fuera del issue #49.
No encaja limpio en ninguno de los dos casos:

- **Es cerrado:** tiene exactamente cinco categorías, *Muy mala*, *Mala*, *Normal*,
  *Buena* y *Excelente*, con valores del 1 al 5, y nombre y valor únicos y obligatorios
  (submodelo #7, R7-19).
- **Tiene un atributo propio además del nombre:** en el MERX consolidado (§0.1) es un
  conjunto de entidades con `ResultadoNutricionalId`, `Nombre` y `Valor`, y la valoración
  lo referencia con `Tiene(Valoración, ResultadoNutricional)`. El esquema del #12 le da
  tabla propia, y la consulta 6 promedia su `Valor`.
- **El código ya lo referencia por identificador:** `ValoracionNutricional` (#55) guarda
  un `ResultadoNutricionalId` de tipo `Guid`.

## Decisión

`ResultadoNutricional` es una **entidad de catálogo cerrado** en `Menus.Domain/Nomenclators/`:

- Hereda `Entity<Guid>`, con `Nombre` y `Valor`.
- Su constructor es privado y expone exactamente cinco instancias estáticas, cada una
  con un `Guid` fijo: `MuyMala`, `Mala`, `Normal`, `Buena` y `Excelente`. No se pueden
  crear otras categorías, así que R7-19 se cumple en el dominio.
- `Todos` lista las cinco categorías y `DesdeValor(int)` busca una por su valor.
- Se persiste como tabla, con seed de las cinco filas que usa los mismos identificadores.

## Alternativas consideradas

| Alternativa | Razón de descarte |
|-------------|-------------------|
| Enum de C# con valores 1 a 5, como dice el ADR-003 para los catálogos cerrados | No conserva el nombre como dato («Muy mala» lleva espacio), no deja tabla ni clave foránea como pide el MERX, y obliga a cambiar `ValoracionNutricional`, que ya referencia un `Guid` |
| Entidad abierta, como `GrupoNutricional` | Permitiría agregar categorías, y R7-19 fija exactamente cinco |

## Consecuencias

- ✅ Coincide con el MERX y con el #12: entidad con identificador, nombre y valor, y tabla propia.
- ✅ R7-19 se cumple en el dominio: no existen más categorías que las cinco.
- ✅ `ValoracionNutricional` no cambia: sigue referenciando un `Guid`.
- ✅ Los `Guid` fijos permiten que el seed y el código usen los mismos identificadores.
- ⚠️ Es una excepción a la regla del ADR-003 («catálogos cerrados → enum»): se aplica porque
  este catálogo tiene un atributo propio (`Valor`) y ya se referencia por identificador.
- ⚠️ La configuración de EF Core y el seed (#57 y #58) deben usar `ResultadoNutricional.Todos`
  con sus identificadores, sin crear otros.
