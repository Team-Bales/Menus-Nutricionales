# ADR-011: RS-02 también alcanza la composición del plato

**Estado:** Aceptado  
**Fecha:** 2026-11-07 (PR #231, a pedido de la revisión; aceptado al aprobarse el PR)

## Contexto

RS-02 dice: «Un nutricionista solo clasifica los ítems que él mismo describió» (informe, §4.2; R5-17 en la numeración vieja). La clasificación de un ítem es su grupo nutricional, su tipo de preparación y su nivel calórico. El dominio la protege en `ItemAlimentario.Reclasificar`, que recibe al nutricionista y rechaza a cualquiera que no sea el descriptor.

Un plato tiene además una **composición**: los alimentos que lo forman y sus cantidades (relación `Compone`). El #62 agrega dos casos de uso que la cambian, `AgregarIngrediente` y `QuitarIngrediente`. Ni el informe ni el dominio dicen quién puede hacerlo:
- RS-02 nombra solo la clasificación;
- `Plato.AgregarIngrediente` y `Plato.QuitarIngrediente` no reciben al actor.

Sin una decisión, cualquier nutricionista podría cambiar los ingredientes de un plato que describió otro, aunque no pudiera cambiar su clasificación.

## Decisión

**Solo el nutricionista que describió el plato puede cambiar su composición.** Es la misma regla que RS-02 aplica a la clasificación.

- **Por qué:** la composición es parte de la descripción del plato, tanto como su clasificación. El descriptor es el autor del ítem (relación `Describe`), y es quien responde por lo que el plato contiene. Además, la composición determina el aporte del plato: si otro la cambia, cambia lo que se sirve con el nombre del autor.
- **Dónde se comprueba:** en el caso de uso (`ComposicionDelPlato.ExigirDescriptor`), antes de tocar el plato. El dominio no lo hace porque sus métodos de composición no reciben al actor, y RS-02 ya está asignada a la capa de aplicación (Equivalencias de numeración, R5-17).
- **Respuesta:** 409 con `regla: RS-02`, como al reclasificar (ADR-010, §4).
- **Alcance:** agregar y quitar ingredientes. Registrar un plato no se ve afectado, porque quien lo registra es su descriptor. Eliminarlo tampoco: el informe no restringe quién elimina un ítem, y el #61 tampoco lo hace con los alimentos.

## Alternativas consideradas

| Alternativa | Razón de descarte |
|---|---|
| Cualquier nutricionista cambia la composición | Contradice el espíritu de RS-02: se podría cambiar lo que contiene un plato ajeno, que pesa más que su clasificación |
| Llevar la comprobación al dominio (que `AgregarIngrediente` y `QuitarIngrediente` reciban al actor) | Cambia la firma del dominio y sus pruebas para una regla que ya está asignada a la capa de aplicación; se puede hacer más adelante sin cambiar el comportamiento |
| Exigir que sea jefe | No tiene apoyo en el informe: `EsJefe` solo habilita a dictaminar menús (RS-03) |

## Consecuencias

- ✅ La clasificación y la composición de un plato siguen la misma regla, y el cliente recibe la misma respuesta.
- ⚠️ RS-02 queda más amplia que su texto en el informe. Si se vuelve a editar el informe, conviene decir «clasifica o compone».
