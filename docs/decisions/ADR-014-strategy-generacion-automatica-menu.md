# ADR-014: Patrón Strategy para la selección de ítems en la generación automática de menús

**Estado:** Aceptado
**Fecha:** 2026-11-12 (#98)

## Contexto

RF-10 pide que el sistema soporte la generación automática de un menú aplicando tres parámetros inmutables: proporción de ítems por tipo de preparación, cobertura de grupos nutricionales y cantidad total de ítems (informe, §2.1). El criterio de evaluación de la generación (informe, §4.4) agrega que no debe incluir ítems de grupos prohibidos por la dieta.

Ninguno de los dos fija **cómo** elegir, entre los ítems compatibles, cuáles entran en el menú — solo qué debe respetar el resultado. El issue #98 pide explícitamente que ese criterio de selección sea intercambiable (`IEstrategiaGeneracionMenu` o equivalente), y el checklist de cierre (#96) exige al menos dos patrones de diseño implementados en el proyecto.

## Decisión

**El caso de uso `GenerarMenuAutomaticamente` delega la selección de ítems en una interfaz `IEstrategiaGeneracionMenu` (patrón *Strategy*)**, inyectada por DI. El manejador se limita a:

1. comprobar que la dieta exista y que el nutricionista actor esté autorizado para ella (RF-06);
2. construir los *value objects* de los parámetros de generación, que ya validan sus propias reglas (#79);
3. calcular los grupos prohibidos de la dieta (reutilizando `IDietaRepository.GetGruposProhibidosAsync` del #101, la misma fórmula que pide el informe) y filtrar el banco para excluirlos, comparando contra `ItemAlimentario.GrupoNutricionalId` directamente;
4. pedirle a la estrategia que seleccione los ítems sobre ese banco ya filtrado;
5. crear el `Menu` con los ítems devueltos y confirmar.

La estrategia por defecto, `EstrategiaGeneracionAleatoria`, reparte la cantidad total entre los tipos de preparación según sus proporciones (método del resto mayor, para que la suma coincida exactamente aunque las proporciones no den números enteros) y, dentro de cada tipo, elige al azar entre los ítems compatibles, priorizando los que pertenecen a algún grupo que el menú deba cubrir.

- **Por qué Strategy y no un método privado del manejador:** el issue pide explícitamente que el criterio sea intercambiable, y el curso evalúa el uso de patrones de diseño. Un método privado cumpliría RF-10 igual, pero no deja lugar a una segunda estrategia (por ejemplo, una que maximice variedad, o una determinista para pruebas) sin tocar el caso de uso.
- **Qué no decide la estrategia:** no decide si la dieta existe, si el nutricionista está autorizado, ni qué ítems son compatibles (grupos prohibidos). Esas reglas son del caso de uso, no de el algoritmo de selección, y no cambian aunque la estrategia cambie.
- **Garantía de cobertura:** la estrategia prioriza, pero no garantiza matemáticamente, que todos los grupos pedidos en `cobertura` terminen representados en el menú — depende de qué ítems haya disponibles por tipo de preparación. Esto es aceptable porque la generación automática es, por diseño, un punto de partida: todo menú queda sujeto a validación (RF-12) antes de asignarse a un paciente (RF-13), y el jefe de nutrición puede rechazarlo si la cobertura no le resulta adecuada.
- **Dónde vive:** `Menus.Application.GeneracionDeMenus`, junto al caso de uso. No en `Menus.Domain` porque no es una regla del agregado `Menu` (el agregado no sabe de dónde salen sus ítems, solo que no se repiten); no en `Menus.Infrastructure` porque no depende de ningún motor de persistencia. La carpeta no se llama `Menus` (y el namespace no es `Menus.Application.Menus`): le haría sombra al espacio de nombres raíz `Menus` para cualquier referencia totalmente calificada dentro de `Menus.Application.*` (mismo criterio que adoptó el #99 con `GestionDeMenus`).

## Alternativas consideradas

| Alternativa | Razón de descarte |
|---|---|
| Selección embebida en el manejador, sin interfaz | Cumple RF-10, pero no es intercambiable y no cuenta como patrón de diseño para el checklist #96 |
| Garantizar matemáticamente la cobertura de todos los grupos (backtracking o programación entera) | Sobre-ingeniería para lo que pide el informe: la generación es un punto de partida sujeto a validación humana, no una optimización que deba ser óptima |
| Mover la selección a `Menus.Domain` (un método de `Menu`) | El agregado no conoce el banco de ítems ni las consultas de disponibilidad; mezclaría una decisión algorítmica con las invariantes estructurales del agregado |

## Consecuencias

- ✅ Cambiar el algoritmo de selección (por ejemplo, para favorecer variedad entre menús de la misma dieta) no toca el manejador ni sus pruebas de orquestación, solo la implementación de la estrategia.
- ✅ El manejador se prueba con una estrategia falsa que registra con qué la llamaron, sin depender del azar.
- ⚠️ Con pocos ítems compatibles por tipo de preparación, la generación puede fallar con 409 (no hay suficientes). Es una limitación real del banco en ese momento, no un error de la estrategia.
- ⚠️ **El filtro de grupos prohibidos no baja a los componentes de un plato.** El informe (§4.4) pide que un ítem de grupo prohibido no entre al menú ni siquiera como ingrediente de un plato, pero tanto este filtro como `IItemAlimentarioRepository.ContarPorNivelCaloricoAsync` (#101) comparan solo el `GrupoNutricionalId` propio del ítem — un plato cuyo grupo propio es válido pero que incluye un alimento de un grupo prohibido puede colarse. Es una limitación conocida y compartida con el #101, no exclusiva de esta generación; resolverla implica recorrer la composición de cada plato y consultar el grupo de cada alimento componente, en ambos lugares a la vez para no dejarlos inconsistentes. Se deja pendiente, fuera del alcance del #98.
