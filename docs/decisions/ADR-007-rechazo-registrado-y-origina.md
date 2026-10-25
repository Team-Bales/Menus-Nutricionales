# ADR-007: El rechazo del menú se registra, y el menú de reemplazo lo referencia (`Origina`)

**Estado:** Aceptado  
**Fecha:** 2026-10-25 (decisión del equipo del 24/10, issue #155)

## Contexto

El 16/10 el equipo decidió que el menú rechazado **se borra físicamente, sin registro**. El argumento era que el
algoritmo de generación es determinista: si se vuelve a generar un menú que ya se rechazó, se rechaza igual, así
que no hace falta guardarlo. El #147 lo implementó (`Menu.Rechazar` solo comprobaba las reglas y el caso de uso
borraba el menú), y el #153 quitó el atributo `Aprobado` de la validación: como solo se guardaban aprobaciones,
valía siempre `true` (la dependencia ∅ → aprobado, que viola la 3FN).

El 19/10 el profesor devolvió el informe de diseño con este señalamiento (P1-5):

> No se modela que el jefe de nutrición «puede indicar la confección de otro [menú] bajo sus criterios»: no hay
> relación de solicitud o reemplazo entre menús ni entre la validación y un nuevo menú.

Con el borrado del rechazo, las dos relaciones que menciona eran imposibles: ni el menú rechazado ni su dictamen
existen después del rechazo. Además, la consulta 3 del enunciado pide los menús validados por un revisor «indicando
la fecha de validación y las observaciones hechas durante el proceso», y el informe entregado sostenía que las
observaciones de los rechazos son parte de la auditoría clínica. Con el borrado, solo quedaban las de las
aprobaciones.

## Decisión

**El rechazo se registra, y el menú que se confecciona en su lugar lo referencia.**

- **`Validacion` recupera `Aprobado`** (booleano obligatorio, R7-17). Con los dos dictámenes registrados toma los
  dos valores y depende de la clave, como cualquier atributo: el argumento de la 3FN del #153 ya no aplica.
- **`Menu.Rechazar` registra la validación** con `Aprobado = false` y las observaciones del revisor. El menú no se
  borra.
- **Tres estados, todos derivados de la validación** (no se almacenan): pendiente (sin validación), final
  (validación aprobada) y rechazado (validación no aprobada). Un menú rechazado no se modifica, no se asigna y no se
  vuelve a revisar: se confecciona otro.
- **Relación `Origina`:** la validación rechazada origina a lo sumo un menú de reemplazo, y un menú reemplaza a lo
  sumo una validación: `Validación (0,1) — Origina — (0,1) Menú`. En el esquema es la columna
  `menus.validacion_origen_id`, una FK anulable y única hacia `validacion(menu_id)`, con `ON DELETE RESTRICT`. En
  el dominio, `Menu.RegistrarComoReemplazo(menuRechazado)`, mientras el menú de reemplazo espera revisión.
- **Reglas nuevas:**

  | Regla | Qué dice | Mecanismo |
  |---|---|---|
  | R7-35 | La validación de origen es un rechazo: un menú aprobado no se reemplaza | Dominio y trigger |
  | R7-36 | El menú de reemplazo es de la misma dieta que el rechazado | Dominio y trigger (desde los dos lados) |
  | R7-37 | Un menú no es reemplazo de sí mismo | Dominio y CHECK |
  | R7-38 | `Origina` es (0,1) en los dos lados | FK anulable con índice único |

- **Reglas que se adaptan:** R7-34 (solo se asigna un menú aprobado) y R7-31 (ningún servicio es anterior a la
  aprobación) exigen ahora `aprobado`, porque la validación también puede ser un rechazo.
- **Lo que se mantiene del #153:** la clave de la validación sigue siendo el menú (R7-32): un menú tiene un solo
  dictamen, así que «menú final» tiene una sola definición (P1-9). La validación sigue sin modificarse ni borrarse
  (R7-33).

## Alternativas consideradas

| Alternativa | Razón de descarte |
|-------------|-------------------|
| Mantener el borrado del rechazo y agregar una relación `Indica` del jefe hacia el menú nuevo, con sus criterios | Guardaba la petición sin el rechazo que la motivó, los criterios quedaban como texto libre que ninguna consulta usa, y la consulta 3 seguía sin las observaciones de los rechazos |
| Sostener que «indicar la confección de otro» ocurre fuera del sistema | El profesor lo señaló como algo que el modelo tiene que representar |
| Registrar el rechazo y permitir que el mismo menú se vuelva a revisar (varias validaciones por menú) | Reintroduce lo que el profesor señaló en P1-9: varias validaciones sin una fecha comparable entre revisores, y dos definiciones de «menú final» |

## Consecuencias

- ✅ P1-5 queda modelado: es la rama del rechazo de la revisión, y el menú de reemplazo sabe qué rechazo lo originó.
- ✅ La consulta 3 recupera los rechazos y sus observaciones.
- ✅ P1-9 sigue resuelto: un menú tiene un solo dictamen y una sola definición de «final».
- ✅ En el MERX, la agregación `Validación` queda justificada, porque participa en otra relación (`Origina`), que es
  el criterio del profesor para usar una agregación (P2-5).
- ⚠️ **Revierte la decisión del 16/10** y dos cambios del #153 (quitar `Aprobado` y el borrado del rechazo). El
  resto del #153 (la generalización, `Servicio`, R11-01 como FK, la revalorización en BCNF) no cambia.
- ⚠️ Los menús rechazados se conservan. El `CASCADE` del menú hacia su parametrización y sus ítems (R11-02) ya no
  se usa para el rechazo; solo actuaría si se borrara un menú pendiente. Un menú con dictamen no se puede borrar.
- ⚠️ Que una validación rechazada origine a lo sumo un menú (R7-38) abarca a varios menús: no lo comprueba el
  agregado, lo garantiza la base con el índice único.
- ⚠️ Hay que actualizar la documentación: el MERX (`Aprobado`, `Origina`, `Validación` como agregación), el #11
  (R7-17, R7-31, R7-34 y R7-35 a R7-38), el #12 y la §5 del informe (las DF nuevas), las consultas 3 y 5, y el
  informe (RF-10, RF-11, la tabla de actores y la §4).
