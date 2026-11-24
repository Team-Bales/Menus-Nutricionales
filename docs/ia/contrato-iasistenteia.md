# Contrato de la integración de IA: `IAsistenteIA`

**Estado:** Aceptado con el ADR-015 (actualización del 2026-11-24). Desarrolla el ADR-015 y amplía su alcance (ver §1).
**Entrega:** la IA entra en v1.0 (informe final: 17-12-2026).

Este documento fija **la interfaz, los datos y la regla de privacidad**, no la implementación. Quien tome una
subtarea de IA debe poder trabajar solo con esto y con el ADR-015.

> Nombre: el puerto se llama `IAsistenteIA`, como en el ADR-015 y en los issues #297 y #299.

## 1. Objetivo y casos de uso

La IA ofrece **explicaciones confiables** de análisis sobre **todos los datos del sistema** y sirve a tres usos:

| Tarea (`TareaIA`) | Para qué | Quién |
|---|---|---|
| `ExplicarAnalisis` | Explicar en lenguaje natural un análisis (una valoración, un reporte, un consumo) | Nutricionista; el paciente solo su propia valoración, por el camino determinista |
| `SugerirAcciones` | Borradores de recomendación para el nutricionista | Nutricionista |
| `GuionParaPaciente` | Guion para que un nutricionista novato explique el caso a un paciente sin formación médica | Nutricionista |

**Principio de diseño:** *todos* los datos deben poder llegar a la IA, pero **no hace falta enviarlos todos**. Una
petición puede llevar toda la información de un paciente y de sus menús relacionados, o mucho menos: un solo bloque
pequeño basta. Cada fuente de datos es un **bloque de contexto** independiente, que se suma por petición (§4). Incorporar una fuente nueva
(p. ej. la ficha nutricional, #283, que se incorpora más adelante) es añadir un proveedor de contexto; el puerto no cambia.

Lo que no cambia del ADR-015: la IA **verbaliza y sugiere sobre datos ya calculados**. No calcula, no modifica la
valoración, no escribe en la base y no genera SQL. El paciente masivo no genera llamadas al LLM.

## 2. Qué datos existen hoy

- `ValoracionNutricional`: **resultado de catálogo** (1 a 5) fijado por el nutricionista sobre un menú y un paciente.
- `Consumo`: **aceptó o rechazó** un ítem del menú en un servicio.
- Niveles calóricos de los ítems, dietas, menús, alérgenos.
- Las seis consultas informacionales (reportes de #90).
- Ficha nutricional (macro y micronutrientes): MongoDB, alta y consulta en #283, **más adelante**.

No hay hoy cifras de cobertura por nutriente; los bloques que las necesiten llegarán con #283.

## 3. Arquitectura: la privacidad se aplica al construir el prompt

```text
datos reales ─► proveedores de contexto ─► ConstructorDePrompt ─► AuditorDePrompt ─► IAsistenteIA ─► texto
 (BD, Mongo)     (transforman: §5)          (único que arma        (red de seguridad,   (solo        │
                                             el texto)             §5.3)                transporte)  ▼
                                                                                      VerificadorDeCifras ─► RespuestaIADto
```

- **Todo lo que decide qué ve el modelo es código determinista del servidor**, probado con pruebas unitarias. El
  modelo nunca recibe un dato personal, ni siquiera «para que lo ignore».
- **El puerto es solo transporte.** No sabe de bloques, roles, caché ni privacidad:

```csharp
public interface IAsistenteIA
{
    /// <exception cref="AsistenteIANoDisponibleException">
    /// Proveedor caído, timeout, respuesta vacía o IA deshabilitada.
    /// </exception>
    Task<string> CompletarAsync(PromptIA prompt, CancellationToken ct = default);
}

/// Texto final listo para enviar. Solo lo crea ConstructorDePrompt (constructor `internal`).
public sealed record PromptIA(string Sistema, string Usuario);
```

- **`PromptIA` solo lo puede crear el `ConstructorDePrompt`** (constructor interno) y este solo acepta
  `BloqueDeContexto` ya transformados. Una entidad, un `Guid` o un DTO crudo **no compilan** como entrada.
- **La garantía no depende del proveedor.** Aunque alguien configure un proveedor externo (OpenAI, NVIDIA NIM) con
  su API key, lo que sale del servidor ya está transformado. La `ApiKey` va solo en la cabecera HTTP del adaptador:
  **nunca en el prompt, ni en los logs**.
- **No se registra el prompt ni la respuesta** (ni en logs ni en la caché de la base); solo la tarea, los bloques
  usados, la latencia y el resultado.

Lo que orquesta el servicio `ExplicadorIA` (en `Menus.Application/IA/`, lo llama el manejador de #304):

```csharp
public enum TareaIA { ExplicarAnalisis, SugerirAcciones, GuionParaPaciente }

public sealed record RespuestaIADto(
    string Resumen,
    IReadOnlyList<SeccionDto> Secciones,        // en GuionParaPaciente: los pasos del guion, en lenguaje llano
    IReadOnlyList<string> Recomendaciones,
    IReadOnlyList<string> Advertencias,
    IReadOnlyList<string> FuentesUsadas,
    bool GeneradoPorIA);

public sealed record SeccionDto(string Titulo, string Texto);
```

Las listas nunca son `null`. `GeneradoPorIA` es `true` solo si el texto salió del LLM.

## 4. Contexto por bloques

```csharp
public sealed record BloqueDeContexto(
    string Fuente,                                  // «valoracion», «aceptacion», «reporte-4», «ficha»…
    int Version,                                    // versión del esquema del bloque
    string Resumen,                                 // forma compacta en texto, la que va al prompt
    IReadOnlyDictionary<string, double> Cifras);    // las cifras que contiene, para verificar (§6)

public interface IProveedorDeContexto
{
    string Fuente { get; }
    Task<BloqueDeContexto?> ConstruirAsync(ContextoDeCaso caso, CancellationToken ct);  // null si no hay datos
}
```

- **Un proveedor por fuente**, registrado por reflexión como los manejadores (ADR-010): sumar una fuente no toca
  el puerto ni el constructor del prompt.
- `ContextoDeCaso` identifica el caso en el servidor (valoración, paciente, menú, dieta). **Nunca sale de
  Application**: el proveedor lo usa para leer y devuelve solo el bloque ya transformado.
- **Cada proveedor aplica las reglas de §5 a su fuente.** Las transformaciones comunes (edad → categoría, código
  opaco, fechas relativas) viven en un único `TransformadorDePaciente` que todos reutilizan, para que la regla sea una sola.

Fuentes previstas:

| Fuente | Contenido | Disponible |
|---|---|---|
| `valoracion` | Resultado (nombre y valor 1 a 5), dieta | Ya |
| `aceptacion` | Tasa de aceptación global y por nivel calórico; ítems más rechazados (máx. 5) | Ya |
| `reporte-1` … `reporte-6` | Resumen de cada consulta informacional (#90) | Con #90 |
| `ficha` | Macro y micronutrientes, alérgenos, preparación | Con #283 |
| `menu` / `dieta` | Composición y niveles calóricos | Ya |

### Qué bloques entran en cada petición

La cantidad de información es **libre**: desde un único bloque hasta todos los disponibles del caso. Enviar menos
reduce coste, latencia y ruido; enviar más da más contexto. No hay una cantidad obligatoria.

1. El **nutricionista elige** los bloques en la pestaña de IA, cualquiera de los disponibles (por defecto, los de la
   tabla siguiente).
2. Un tope técnico, para no pasar el límite del modelo: `IA:MaxBloques` y `IA:MaxCaracteresDeContexto`. Si se pasa,
   se descartan los de menor prioridad y se avisa en `Advertencias`. Se pueden subir según el modelo que se use.
3. Fase 2: un clasificador de intención sugiere los bloques a partir de la pregunta (ADR-015 §6).

| Tarea | Bloques por defecto |
|---|---|
| `ExplicarAnalisis` | `valoracion`, `aceptacion` |
| `SugerirAcciones` | `valoracion`, `aceptacion`, `ficha` (si existe) |
| `GuionParaPaciente` | `valoracion`, `aceptacion` |

La `Pregunta` libre del nutricionista (opcional, solo en `ExplicarAnalisis` y `SugerirAcciones`) pasa por el mismo
`AuditorDePrompt` y **no se cachea**.

## 5. Privacidad

**Regla (decisión del equipo):** *todos* los datos clínicos entran en el prompt; **ningún dato personal**. Es
determinista y se aplica **al construir el prompt**, por código, nunca por instrucción al modelo.

### 5.1 Qué se transforma

| Dato | Tratamiento |
|---|---|
| Nombre, `Guid` de paciente, menú o nutricionista | **Se elimina.** El texto dice «el paciente»; si hay varios en un mismo bloque, un código opaco por petición (`P1`, `P2`) |
| Edad | **Categoría** de rango: 0-12, 13-17, 18-39, 40-64, 65+ |
| Sala de hospitalización | **Se elimina** (identifica por ubicación) |
| Fechas exactas | **Relativas** («hace 3 días», «semana 2 del menú») |
| Dietas, alérgenos, valoración, aceptación y rechazo, niveles calóricos, nutrientes | **Pasan tal cual**: son los datos clínicos relevantes (las combinaciones raras, según el consentimiento: §5.2) |
| Texto libre del paciente (observaciones, notas) | **No entra** en la primera versión: no se puede garantizar que no contenga nombres |

La correspondencia código ↔ paciente solo existe en memoria del servidor durante la petición.

### 5.2 Combinaciones raras: entran con el consentimiento del paciente

Una dieta o un alérgeno muy poco frecuente, junto con una categoría de edad, puede permitir rastrear a un
paciente para quien vea el prompt. Son, a la vez, los datos que más valor dan al apoyo de la IA. Decisión del equipo:
**entran en el prompt solo si el paciente dio su permiso** para el uso de sus datos menos sensibles dentro de la
clínica, en el soporte de IA.

| Paciente | Qué entra en el prompt |
|---|---|
| **Con consentimiento** (`ConsentimientoIA` = sí) | Todos los datos clínicos, incluidas las combinaciones raras (§5.1) |
| **Sin consentimiento** (por defecto) | Los mismos datos, pero las combinaciones que aparecen en menos de `IA:KMinimo` pacientes (propuesta: 5) se generalizan o se omiten, y se anota en `Advertencias` |

- El **consentimiento es un dato del paciente**: sí/no, fecha y quién lo registró. **El defecto es «no»**, y se puede
  revocar; al revocarlo deja de aplicarse desde la siguiente petición (la caché no lo impide, porque su clave es el
  hash del prompt ya construido).
- La decisión la toma el `TransformadorDePaciente` (§4), con el mismo código para todos los proveedores.
- **Esto exige un cambio fuera de la línea de IA:** un campo nuevo en `Paciente` (dominio, migración y endpoint) y
  una pantalla para registrarlo. Es un issue aparte; hasta que exista, todos los pacientes
  cuentan como «sin consentimiento».
- Además, el prompt nunca se registra (§3). Con **Ollama local** no sale del servidor.

### 5.3 Red de seguridad: `AuditorDePrompt`

Después de construir el texto y antes de enviarlo, un auditor determinista comprueba que **no contiene**:
- ningún `Guid`;
- el **nombre real, la sala y la edad exacta del paciente del caso** (el servidor conoce esos valores y busca la
  cadena literal; es la comprobación más fuerte porque usa los datos reales);
- ninguna cadena con forma de clave o token (`sk-…`, cabeceras `Authorization`).

Si encuentra algo, **aborta la llamada** con `AsistenteIANoDisponibleException` y deja una advertencia: no envía.

### 5.4 Pruebas de privacidad

- Por proveedor: serializa el bloque y falla si hay `Guid`, nombre, edad en años o sala.
- Del constructor: con un paciente de prueba cuyos datos se conocen, el prompt completo no contiene ninguno.
- Del auditor: casos que deben abortar (nombre embebido, `Guid`, clave) y casos limpios.
- Enlaza con la protección de datos de la línea de seguridad (#293).

## 6. Confiabilidad: la salida solo puede decir lo que está en la entrada

Es el requisito central de «explicaciones confiables».

1. **Prompt restringido:** el modelo usa solo los bloques dados, no inventa cifras, no diagnostica, y marca como
   «a confirmar por el nutricionista» lo que sea una sugerencia.
2. **`VerificadorDeCifras` (determinista, después del LLM):** extrae los números del texto y comprueba que cada uno
   aparece en `Cifras` de algún bloque (con tolerancia de redondeo). Si alguno no aparece, la respuesta se
   **descarta** y se cae al determinista, o se devuelve con advertencia donde no hay determinista.
3. **Trazabilidad:** `FuentesUsadas` dice qué bloques entraron; la UI los muestra.
4. **Aviso fijo** en la UI: «generado con asistencia de IA, revisado por el nutricionista».
5. Las sugerencias son **borradores**: nada se aplica solo ni se guarda en la base.

## 7. Camino determinista

Existe para `ExplicarAnalisis` sobre `valoracion` + `aceptacion` (plantilla desde los números): es lo que ve el
paciente y el *fallback* del nutricionista. `SugerirAcciones` y `GuionParaPaciente` **no tienen determinista**: con la
IA apagada, caída o descartada por el auditor o el verificador devuelven «IA no disponible», no un texto inventado.

## 8. Configuración: `IAOptions` (sección `IA`)

| Clave | Tipo | Defecto | Nota |
|---|---|---|---|
| `Habilitada` | bool | `false` | Con `false` no se instancia el cliente real |
| `BaseUrl` | string | `http://localhost:11434/v1` | Ollama local |
| `Model` | string | (a fijar al probar) | Modelo de Ollama; se elige según la máquina que lo ejecute |
| `ApiKey` | string | vacío | Solo por `user-secrets` o variable de entorno (#295, #307); solo en la cabecera HTTP |
| `TimeoutSegundos` | int | 30 | Al vencer: `AsistenteIANoDisponibleException` |
| `MaxBloques` | int | 4 | §4 |
| `MaxCaracteresDeContexto` | int | 6000 | §4 |
| `VersionDePrompt` | string | `v1` | Entra en la clave de caché |
| `KMinimo` | int | 5 | §5.2, solo para pacientes sin consentimiento |
| `Terminos:Version` | string | (vacío) | Versión de los términos que se aceptaron (§8.1) |
| `Terminos:AceptadoPor` | string | (vacío) | Persona que configuró el proveedor y asume la responsabilidad |
| `Terminos:Fecha` | fecha | (vacía) | Fecha de aceptación |
| `Terminos:RetencionDesactivada` | bool | `false` | Confirma que desactivó la retención y el entrenamiento en su proveedor |

### 8.1 Términos de uso y condiciones de la configuración de IA

Quien configura una `ApiKey` de un proveedor **externo** (cualquier `BaseUrl` que no sea local) asume la
responsabilidad de ese envío de datos. **Para habilitarlo debe aceptar estos términos por configuración**; el
sistema no arranca la IA externa sin ellos (falla cerrado, con un mensaje claro). Con `BaseUrl` local (Ollama) no
se exigen, porque nada sale del servidor.

- `IA:Habilitada = true` con proveedor externo **exige** `Terminos:Version` igual a la vigente, `Terminos:AceptadoPor`,
  `Terminos:Fecha` y `Terminos:RetencionDesactivada = true`. Si falta uno, la validación de opciones al arrancar falla.
- La aceptación queda en el registro de arranque (quién, cuándo, versión), sin el valor de la clave.
- Si cambia el texto de los términos, sube `Version` y hay que aceptarlos de nuevo.

Texto vigente (versión `1`):

> **Términos de uso de la integración de IA**
>
> 1. Al configurar un proveedor de IA externo, usted autoriza que el sistema envíe a ese proveedor **texto
>    construido a partir de datos clínicos de pacientes**. Los datos personales (nombre, identificadores, edad exacta,
>    sala, fechas exactas) se transforman o se eliminan antes del envío, pero **los datos clínicos no se alteran** y,
>    en pacientes con consentimiento, pueden incluir combinaciones poco frecuentes que permitirían reconocerlos.
> 2. Usted es responsable de **desactivar en su proveedor la retención de datos y el uso de los mensajes para
>    entrenamiento**, y de confirmarlo en `Terminos:RetencionDesactivada`.
> 3. Usted es responsable de la custodia de la clave: no puede incluirse en el repositorio ni en registros, y
>    quien la ponga en una configuración compartida responde de su uso.
> 4. Solo deben usarse con la IA los datos de pacientes que **dieron su consentimiento** o a los que se aplica la
>    generalización de §5.2. No desactive esa regla.
> 5. Las respuestas de la IA son **borradores** que revisa un nutricionista. El sistema no sustituye su criterio ni
>    toma decisiones clínicas.
> 6. Este sistema es un proyecto académico, desarrollado con las prácticas de un despliegue real. En él se usan
>    datos ficticios; con datos reales, este texto aplica en su totalidad.

## 9. Caché (Redis, vía el `ICache` existente)

- **Clave:** `ia:{tarea}:{modo}:{VersionDePrompt}:{sha256(prompt final)}`, con `modo` = `determinista` o `llm`.
- La clave es un hash del prompt ya transformado: si cambian los datos cambia la clave y no hace falta invalidar
  a mano. El TTL (propuesto: 24 h) solo limpia claves huérfanas.
- Lo que se guarda es la `RespuestaIADto` (texto ya verificado), nunca el prompt. No se cachean las respuestas con `Pregunta` libre.
- Un fallo de Redis no impide responder (contrato de `ICache`). La clave no contiene ningún identificador.

## 10. Pruebas

- Manejadores y endpoint con un `IAsistenteIA` falso. **Cero llamadas reales al proveedor en CI.**
- Adaptador con `HttpClient` simulado: éxito, timeout, respuesta vacía, 5xx; y que la `ApiKey` va solo en la cabecera.
- `VerificadorDeCifras`: cifra inventada (rechaza) y cifra redondeada (acepta).
- Las pruebas de privacidad de §5.4, con y sin consentimiento (la combinación rara entra solo con él).
- Validación de opciones: proveedor externo sin términos aceptados no arranca; con Ollama local sí.
- La demo y las pruebas manuales usan **Ollama**, que se ejecuta en otra máquina (`IA:BaseUrl`).
- **Spike (#297):** con 2 o 3 valoraciones reales se mide la latencia, el porcentaje de respuestas que pasan el
  `VerificadorDeCifras` y una nota de calidad de 1 a 5 que pone un nutricionista del equipo.

## Dependencias fuera de este documento

- **Consentimiento del paciente (§5.2):** campo nuevo en `Paciente`, con su migración, endpoint y pantalla. Es un issue
  aparte. Hasta que exista, todos los pacientes cuentan como «sin consentimiento».
- Los términos de uso (§8.1) son una propuesta: no son asesoría jurídica.
