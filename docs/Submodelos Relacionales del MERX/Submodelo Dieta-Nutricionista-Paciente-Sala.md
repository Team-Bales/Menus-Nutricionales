# Submodelo Dieta-Nutricionista-Paciente-Sala — Restricciones locales

**Asignatura:** Bases de Datos II / Ingeniería de Software — Curso 2026-2027
**Issue:** #6
**Diagrama:** `Submodelo Dieta-Nutricionista-Paciente-Sala.drawio` (misma carpeta): Página-Entidades, Página-Relaciones-1 y Página-Relaciones-2.
**Alcance de este documento:** restricciones de integridad locales del submodelo — las que involucran únicamente sus entidades y relaciones —, cada una con la evidencia del enunciado que la sustenta y el mecanismo previsto para hacerla cumplir. Es insumo directo de la especificación consolidada de restricciones (issue #11), que las recoge junto con las de los demás submodelos.

---

## 0. Contenido del submodelo

| Elemento | Detalle |
|---|---|
| Entidades | `Dieta`, `Nutricionista`, `Paciente`, `Sala` |
| Nomencladores | `ProgramaDeAtención`, `RestricciónAlimentaria`, `Especialidad` |
| Entidad de frontera | `GrupoNutricional` (issue #5), extremo de `Cubre` en `Cobertura` y de `Excluye` |
| Relaciones | `Corresponde` (`Dieta`, `ProgramaDeAtención`); `Restringe` (`Dieta`, `RestricciónAlimentaria`); `Excluye` (`RestricciónAlimentaria`, `GrupoNutricional`); `Autoriza` (`Nutricionista`, `Dieta`); `Inscribe` (`Paciente`, `Dieta`); `Pertenece` (`Paciente`, `Sala`); `Ejerce` (`Nutricionista`, `Especialidad`) |
| Agregación | `Cobertura`: envuelve `Dieta`—`Cubre`—`GrupoNutricional`, hereda su llave (`DietaId`, `GrupoNutricionalId`) y lleva el atributo `Tipo` |

`GrupoNutricional` se incorpora como entidad de frontera del submodelo #5 —donde se declara nomenclador y se le dan sus atributos— porque este submodelo lo necesita como extremo de `Cubre` en la agregación `Cobertura` y también como extremo de `Excluye`; en el diagrama aparece solo el rectángulo, sin atributos duplicados. `ProgramaDeAtención` y `RestricciónAlimentaria` se modelan como nomencladores porque así lo decide el glosario (§6), y `Especialidad` porque el glosario la cataloga como nomenclador (§4.2, N6). En los tres casos se aplica el patrón nomenclador de las convenciones de modelado (§5): ninguno queda como atributo de `Dieta` o de `Nutricionista`.

`Cubre` se eleva a agregación (`Cobertura`) porque la consulta 5 (§7-C, issue #10) exige verificar algorítmicamente si los criterios de equilibrio de una dieta son cumplidos por un menú: para ello el modelo debe distinguir si un grupo nutricional es *requerido* (debe aparecer en el menú) o *restringido* (no debe aparecer). Una relación pura no puede llevar ese dato; la agregación lo expresa con el atributo `Tipo`. `Restringe(Dieta, RestricciónAlimentaria)` coexiste porque modela restricciones clínicas distintas (sin gluten, hiposódica…) cuya verificación opera contra la ficha nutricional ampliada en MongoDB (issue #8), no contra los grupos nutricionales del modelo relacional.

**Correspondencia con el glosario del dominio:**

- El glosario (§4.1, E3, E5, E6, E7) enumera `Dieta(id, nombre)`, `Nutricionista(id, nombre, especialidad)`, `Paciente(id, nombre, edad)` y `Sala(id, nombre)`. Se conservan esos atributos, salvo `especialidad`, que pasa a ser la relación `Ejerce` con el nomenclador `Especialidad`.
- El glosario (§4.4) registra `Corresponde` (`Dieta` → `Programa de atención`, N:1), `Cubre` (`Dieta` ↔ `Grupo nutricional`, N:M), `Restringe` (`Dieta` ↔ `Restricción alimentaria`, N:M), `Autoriza/Atiende` (`Nutricionista` ↔ `Dieta`, N:M), `Inscribe` (`Paciente` ↔ `Dieta`, N:M) y `Pertenece` (`Paciente` → `Sala`, N:1). Se conservan esos nombres y esos participantes. `Ejerce` (`Nutricionista` ↔ `Especialidad`) traduce la mención del enunciado «su especialidad» como dato del nutricionista (línea 44) al patrón nomenclador. `Cubre` se eleva a agregación `Cobertura` con el atributo `Tipo` (ver §0); el resto de relaciones del glosario permanecen sin cambio.
- `Autoriza` se nombra con el verbo del glosario, que recoge la formulación «las dietas para las cuales está autorizado a generar o validar menús» (líneas 44-45). La relación representa ese conjunto de autorizaciones, no la acción de autorizar.
- `Autoriza` y `Inscribe` son N:M en el glosario, y así se dibujan: `(0,*)` en ambos extremos.

**Lo que este submodelo deja fuera, y por qué:**

- **Usuario, Contraseña y Rol** no se modelan. El glosario (§4.3) no incluye al usuario como candidato del dominio; el acceso a la aplicación es una responsabilidad de la capa de aplicación, y las funcionalidades F1-F6 no consultan credenciales ni permisos como dato.
- **Administrador** no se modela. El glosario (§6) lo declara fuera del MERX: es un rol de mantenimiento de la aplicación (evitar alimentos duplicados, impedir la asignación doble de un menú), no un dato de dominio.
- **`Jefe de nutrición`** no se modela como entidad ni como especialización. El glosario (§6) lo declara rol de `Nutricionista` sin datos propios: la aprobación ya vive en `Valida(Nutricionista, Menú; FechaValidación, Observaciones)` (issue #7). La información que aporta este submodelo es un tipo de restricción, no una relación: una aprobación exige que el aprobador sea un `Nutricionista` autorizado en la dieta (§3, R6-22).

---

## 1. Criterio

Se consideran **locales** las restricciones cuyo alcance se agota en las entidades y relaciones de este submodelo. Las que cruzan hacia otros submodelos se listan aparte (§5) para que la consolidación no las pierda.

Se sigue el mismo criterio de rigor del glosario del dominio: cada restricción cita la línea del enunciado que la sustenta; las que no tienen cita directa se declaran expresamente como **decisión del equipo** (§6). La numeración de líneas es la del texto plano del enunciado, la misma empleada en el glosario y en los issues. Los mecanismos usan el vocabulario del issue #11: PK, FK, UNIQUE, CHECK, NOT NULL, trigger y capa de aplicación.

Las cardinalidades se leen con la convención de dirección de las convenciones de modelado (§4): el par `(mínimo, máximo)` junto a una entidad indica cuántas instancias de esa entidad se asocian a **una** instancia de la entidad del otro extremo. Los valores dibujados en el diagrama son los que se traducen a restricciones en §2.4.

---

## 2. Restricciones estructurales

### 2.1 Clave

| # | Restricción | Evidencia | Mecanismo |
|---|---|---|---|
| R6-01 | `DietaId` identifica unívocamente a cada dieta, `NutricionistaId` a cada nutricionista, `PacienteId` a cada paciente y `SalaId` a cada sala. | líneas 42, 43-45, 49 | PK |
| R6-02 | `ProgramaDeAtenciónId`, `RestricciónAlimentariaId` y `EspecialidadId` identifican unívocamente a cada valor de su nomenclador. | líneas 27, 42, 44 | PK |
| R6-03 | El par (`NutricionistaId`, `DietaId`) identifica cada autorización: un nutricionista no aparece dos veces autorizado para la misma dieta. | decisión del equipo | PK compuesta |
| R6-04 | El par (`PacienteId`, `DietaId`) identifica cada inscripción: un paciente no aparece dos veces inscrito en la misma dieta. | decisión del equipo | PK compuesta |
| R6-05 | El par (`DietaId`, `RestricciónAlimentariaId`) identifica cada ocurrencia de `Restringe`. | decisión del equipo | PK compuesta |
| R6-05b | El par (`DietaId`, `GrupoNutricionalId`) identifica cada ocurrencia de la agregación `Cobertura`. | decisión del equipo | PK compuesta heredada |
| R6-05c | El par (`RestricciónAlimentariaId`, `GrupoNutricionalId`) identifica cada ocurrencia de `Excluye`: una restricción no excluye dos veces el mismo grupo. | decisión del equipo | PK compuesta |

> **Convención adoptada (R6-03 a R6-05c):** las relaciones N:M se materializan como tablas de interconexión cuya llave es el par de claves primarias de los participantes. `Cobertura` hereda la misma llave compuesta que la antigua `Cubre` y añade el atributo `Tipo`, siguiendo el patrón de agregación de las convenciones de modelado (§3): el par (`DietaId`, `GrupoNutricionalId`) sigue siendo la PK; no se incluye `Tipo` en la llave porque una dieta solo puede tener una regla por grupo nutricional.

### 2.2 Referenciales

| # | Restricción | Evidencia | Mecanismo |
|---|---|---|---|
| R6-06 | Toda dieta corresponde a un programa de atención existente. | línea 42 | FK |
| R6-07 | Toda ocurrencia de `Cobertura` referencia una dieta y un grupo nutricional existentes. | líneas 24-25, 42-43 | FK |
| R6-08 | Toda ocurrencia de `Restringe` referencia una dieta y una restricción alimentaria existentes. | líneas 27, 42-43 | FK |
| R6-08b | Toda ocurrencia de `Excluye` referencia una restricción alimentaria y un grupo nutricional existentes. No se elimina una restricción alimentaria ni un grupo nutricional mientras tengan ocurrencias en `Excluye`. | issue #8, doc 2, §5; decisión del equipo | FK; FK con `ON DELETE RESTRICT` |
| R6-09 | Toda autorización referencia un nutricionista y una dieta existentes. | líneas 32, 44-45 | FK |
| R6-10 | Toda inscripción referencia un paciente y una dieta existentes. | líneas 49-50 | FK |
| R6-11 | Todo paciente referencia la sala a la que pertenece, la cual debe existir. | línea 49 | FK |
| R6-12 | Todo nutricionista referencia la especialidad que ejerce, la cual debe existir. | línea 44 | FK |
| R6-13 | No se elimina un valor de nomenclador mientras lo use alguna dieta o algún nutricionista, ni una dieta mientras figure en alguna autorización o alguna inscripción. | decisión del equipo | FK con `ON DELETE RESTRICT` |

### 2.3 Dominio

| # | Restricción | Evidencia | Mecanismo |
|---|---|---|---|
| R6-14 | `Edad` es mayor o igual que cero. | línea 49 | CHECK (`Edad >= 0`) |
| R6-15 | `Nombre` es obligatorio y no vacío en `Dieta`, `Nutricionista`, `Paciente`, `Sala` y en los tres nomencladores. | decisión del equipo | NOT NULL + CHECK |
| R6-16 | `Nombre` es único dentro de cada nomenclador: no hay dos programas de atención, dos restricciones alimentarias ni dos especialidades con el mismo nombre. | decisión del equipo | UNIQUE |
| R6-17 | `Edad` se registra como un número entero de años. | decisión del equipo | tipo entero |
| R6-17b | `Tipo` de `Cobertura` toma exactamente los valores *Requerido* y *Restringido*, y es obligatorio. | consulta 5, §7-C; issue #10 | NOT NULL + CHECK (`Tipo IN ('Requerido', 'Restringido')`) |

> **Convención adoptada (R6-17b):** el dominio cerrado de `Tipo` se garantiza con un CHECK directamente sobre la tabla de `Cobertura`, igual que el dominio de `Aceptación` en `Consumo` (submodelo #7, R7-18). No se usa nomenclador porque solo existen dos valores semánticamente opuestos y el enunciado no anticipa que este catálogo crezca.

> **Convención adoptada (R6-16):** la unicidad del nombre en los nomencladores es la contrapartida de usar el patrón nomenclador (convenciones de modelado, §5): el nombre es la clave con que se reconoce un valor del catálogo en el diálogo con el usuario, y su duplicación lo volvería ambiguo. El enunciado no enumera los valores de estos tres catálogos —a diferencia de `TipoPreparación` y `NivelCalórico`— así que no se exige catálogo cerrado, solo unicidad.

### 2.4 Cardinalidad

| # | Restricción | Evidencia | Mecanismo |
|---|---|---|---|
| R6-18 | Cada dieta corresponde a exactamente un programa de atención: `(1,1)` del lado de `ProgramaDeAtención` en `Corresponde`. | línea 42 | FK NOT NULL en `Dieta` |
| R6-19 | Cada paciente pertenece a exactamente una sala: `(1,1)` del lado de `Sala` en `Pertenece`. | línea 49 | FK NOT NULL en `Paciente` |
| R6-20 | Cada nutricionista ejerce exactamente una especialidad: `(1,1)` del lado de `Especialidad` en `Ejerce`. | línea 44 | FK NOT NULL en `Nutricionista` |
| R6-21 | Toda dieta tiene al menos una ocurrencia de `Cobertura` con `Tipo = Requerido`: `(1,*)` del lado de `GrupoNutricional` con ese tipo. | líneas 42-43; el mínimo, decisión del equipo | capa de aplicación + trigger diferido |

**R6-18, R6-19 y R6-20 — carácter obligatorio.** El enunciado presenta el programa de atención, la sala y la especialidad como un dato propio y singular de cada dieta, cada paciente y cada nutricionista: «el programa de atención al que corresponde» (línea 42), «su nombre, edad, sala a la que pertenece» (línea 49), «su identificador único, nombre, especialidad» (líneas 43-45). En el diagrama se expresa con el `(1,1)` dibujado junto al nomenclador o a `Sala`; en la implementación, con una FK NOT NULL en la entidad: la columna no admite nulos y, por ejemplo, una dieta sin programa de atención no llega a existir.

**R6-21 — cobertura mínima con `Tipo = Requerido`.** El enunciado describe la dieta como el conjunto de «restricciones o grupos nutricionales a cubrir a lo largo del tratamiento» (líneas 42-43). Una dieta sin ningún grupo requerido no tendría sentido operativo: no delimitaría qué debe generar la generación automática de menús. El mínimo se aplica solo a las ocurrencias con `Tipo = Requerido`; una dieta puede tener cero grupos restringidos y seguir siendo válida.

La restricción no es expresable con claves foráneas: la dieta se registra antes que sus ocurrencias de `Cobertura`, y la comprobación solo tiene sentido al cierre de la operación. En la capa de aplicación, el alta de una dieta y la de sus grupos cubiertos se registran en una misma unidad de trabajo (patrón Unit of Work del backend). En PostgreSQL puede reforzarse con un trigger de restricción diferido (`DEFERRABLE INITIALLY DEFERRED`) que verifique, al confirmar la transacción, que la dieta conserva al menos una fila de `Cobertura` con `Tipo = 'Requerido'`. La misma verificación aplica al eliminar o actualizar ocurrencias de `Cobertura`.

---

## 3. Restricciones semánticas

Estas restricciones no restringen los datos almacenados por el submodelo, sino **quién puede operar sobre ellos** a través de las entidades del issue #7. Se registran aquí porque ambas partes son necesarias para enunciarlas: la autorización y la inscripción son hechos de este submodelo, y los menús son del #7.

| # | Restricción | Evidencia | Mecanismo |
|---|---|---|---|
| R6-22 | Un nutricionista solo genera o valida menús de las dietas en las que está autorizado. | líneas 43-45 | capa de aplicación |
| R6-23 | Un paciente solo recibe menús de las dietas en las que está inscrito. | líneas 49-50 | capa de aplicación |

**R6-22.** El enunciado acota el alcance de la autoridad del nutricionista: almacena «las dietas para las cuales está autorizado a generar o validar menús» (líneas 43-45), y confirma que puede «ver menús de otros nutricionistas que atienden la misma dieta» (línea 32). Es decir, la autoridad se verifica **por dieta**, no por autor: un nutricionista puede generar y validar menús de cualquier dieta en la que esté autorizado, aunque esos menús los haya creado otro. La base de datos guarda la autorización (`Autoriza`) pero no la acción de generar o validar (`Crea`, `Valida`); por eso la comprobación se hace en la capa de aplicación: antes de crear o de validar un menú, el nutricionista autenticado debe tener una fila en `Autoriza` para la dieta a la que ese menú corresponde. La misma comprobación aplica al rol de jefe de nutrición (R6-22 y §0), que no es una entidad: se valida contra el `Nutricionista` que lo ejerce.

**R6-23.** El enunciado dice que los pacientes «podrán acceder a los menús que le son asignados» (línea 35) y registra «las dietas en las que está inscrito» (líneas 49-50). La asignación de un menú a un paciente es un hecho del #7, pero su corrección depende de este submodelo: un paciente no puede recibir un menú de una dieta en la que no está inscrito. La verificación es análoga a R6-22: antes de asignar un menú a un paciente, debe existir una fila en `Inscribe` para la dieta del menú y para ese paciente. Las demás reglas de la asignación —qué pares (menú, paciente) se admiten y cómo se registra el consumo— las define el #7, conforme a la consulta 4 (§7-A y §7-D).

Ambas restricciones se expresan mejor como una consulta de existencia sobre las tablas de interconexión (`EXISTS` sobre `Autoriza` e `Inscribe`) que como una regla declarativa: el dato que las hace cumplir —el menú— pertenece al #7 y no tiene representación en este submodelo.

---

## 4. Lo que deliberadamente no se restringe

- **Una dieta puede no declarar ninguna restricción alimentaria.** El enunciado presenta «restricciones o grupos nutricionales» como dos listas separadas (líneas 42-43), una dieta que no declara restricciones es válida siempre que declare sus grupos, y el mínimo `0` del lado de `RestricciónAlimentaria` en `Restringe` lo refleja.
- **Un paciente puede no estar inscrito en ninguna dieta y un nutricionista puede no estar autorizado en ninguna.** Ambas relaciones son N:M puras: `(0,*)` en ambos extremos de `Inscribe` y de `Autoriza`. El paciente de alta sin dieta y el nutricionista de alta sin autorización son altas del sistema pendientes de asignación.
- **Los nombres de las entidades no son únicos.** El enunciado fija el identificador como único (líneas 42, 44, 49) pero no el nombre; se admite, por tanto, que dos dietas, dos pacientes, dos nutricionistas o dos salas compartan nombre sin que ello confunda al modelo, que los distingue por su identificador. La unicidad de nombre solo se exige en los nomencladores (R6-16).
- **`Especialidad` no se deriva de `Autoriza` ni al revés.** Un nutricionista puede estar autorizado en una dieta ajena a su especialidad, y su especialidad no acota sus autorizaciones. Ninguna funcionalidad del enunciado (F1-F6) exige esa correspondencia.
- **`Sala` no depende de `Dieta`:** un paciente pertenece a una sala y está inscrito en varias dietas, sin que su sala restrinja las dietas. La sala se usa como agregado en el reporte F6b (líneas 80-82), no como criterio de inscripción.
- **El traslado de un paciente entre salas no se modela como historial.** `Pertenece` es N:1, con FK NOT NULL en `Paciente` (R6-19); un traslado se representa actualizando la fila del paciente, y no queda constancia de la sala anterior.

---

## 5. Restricciones que involucran al submodelo pero no son locales

Se registran para la consolidación del issue #11; su definición corresponde a los submodelos indicados.

| Restricción | Submodelos | Estado |
|---|---|---|
| Generación de menús por dieta: `Crea(Nutricionista, Menú)` y `Generado para(Menú, Dieta)`, con `Incluye` hacia alimentos y platos. | #6, #7 | Documentado en el glosario |
| Validación y aprobación del menú: `Valida(Nutricionista, Menú; FechaValidación, Observaciones)` y el rol de jefe de nutrición que la ejerce. | #6, #7 | Documentado en el glosario (§6) |
| Asignación de un menú a un paciente y registro de su consumo. | #6, #7 | Los define el #7, conforme a la consulta 4 (§7-A y §7-D) |
| Cobertura de grupos: verificar, al generar un menú para una dieta, que los grupos con `Tipo = Requerido` en `Cobertura(Dieta, GrupoNutricional)` estén presentes en el menú y los grupos con `Tipo = Restringido` estén ausentes. | #5, #6, #7 | **Resuelto en este submodelo** (agregación `Cobertura` + R6-17b + R6-21); la verificación en tiempo de generación es responsabilidad del #7. |
| Compatibilidad de las restricciones alimentarias de una dieta con los alérgenos de los alimentos y platos del menú. | #6, #7 | Pendiente: los alérgenos viven en la ficha nutricional ampliada, gestionada fuera del modelo relacional (issue #8) |
| Valoraciones y revalorizaciones por dieta, y el promedio por sala del reporte F6b. | #6, #7 | Fuera de este submodelo: `Valoración` y `Revalorización` se definen en el #7 |
| Eliminación de un paciente o de un nutricionista con inscripciones o autorizaciones, y de una dieta con esas mismas ocurrencias. | #6, #7 | Pendiente: la política de borrado entre submodelos no está decidida (R6-13 la aplica dentro de este submodelo) |

---

## 6. Decisiones del equipo tomadas en este documento

Restricciones sin cita directa en el enunciado, adoptadas por el equipo: R6-03, R6-04, R6-05, R6-05b, R6-05c, R6-08b, R6-13, R6-15, R6-16, R6-17, R6-17b; el mínimo de un grupo requerido en R6-21; la elevación de `Cubre` a la agregación `Cobertura` con `Tipo` (issue #10, consulta 5 §7-C); y la adición de `Excluye(RestricciónAlimentaria, GrupoNutricional)` para mapear cada restricción clínica a los grupos nutricionales que excluye, habilitando la verificación por grupo en PostgreSQL (issue #8, doc 2, §5).

Quedan abiertas dos decisiones:

- **La precisión de `Edad`.** El enunciado nombra la edad sin fijar su unidad ni su formato (línea 49). R6-14 y R6-17 la tratan como un entero en años; si el equipo prefiere meses o fecha de nacimiento, la representación cambia aunque la restricción no. Conviene resolverlo antes de la implementación.
- **Si `ProgramaDeAtención`, `RestricciónAlimentaria` y `Especialidad` requieren atributos descriptivos propios** (por ejemplo, una descripción o un responsable). El glosario (§6) los declara nomencladores sin atributos propios, y este submodelo los dibuja solo con identificador y nombre; cualquier atributo adicional sería una extensión y debe declararse aquí antes de usarse en el diagrama.