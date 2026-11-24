# ADR-015: Integración de IA generativa opcional: proveedor OpenAI-compatible y separación por volumen de uso

**Estado:** Aceptado  
**Fecha:** 2026-11-20 (propuesto); aceptado el 2026-11-24. Ampliado el 2026-11-24 (ver «Actualización»).

## Contexto

Se evalúa sumar IA generativa como **extensión opcional** del sistema, sin bloquear ni condicionar la entrega del informe. El proyecto es de Bases de Datos II e Ingeniería de Software: la persistencia (PostgreSQL, MongoDB y Redis, ADR-005 y ADR-009) es la **fuente de verdad**, y tanto la valoración nutricional como las seis consultas informacionales ya se calculan de forma **determinista**.

Integrar un modelo de lenguaje trae riesgos que esta decisión tiene que acotar:

- **Alucinación:** un LLM no puede inventar valores nutricionales ni fijar una valoración; eso es lógica evaluable de BD II.
- **Escalabilidad y coste:** el usuario masivo es el **paciente**. Llamar a un LLM por cada petición de paciente no escala sin servidores grandes.
- **Datos sensibles:** la valoración involucra datos de salud; no pueden salir del sistema sin anonimizar.
- **Dependencia externa:** no se puede acoplar la aplicación —ni la defensa— a la disponibilidad de un proveedor o de la red.

La capa de aplicación (ADR-010) ya es CQRS con puertos y adaptadores y registro por reflexión, de modo que un puerto nuevo encaja sin cambios estructurales.

## Decisión

### 1. La IA complementa; no calcula ni decide

La base de datos sigue siendo la fuente de verdad. La IA solo **verbaliza, sugiere o clasifica** sobre datos ya calculados: no fija valoraciones, no escribe en la BD y no introduce cifras que no estén en su entrada.

### 2. Proveedor: interfaz OpenAI-compatible, conmutable por configuración

- Un puerto `IAsistenteIA` en `Menus.Application`; un adaptador `AsistenteIAOpenAICompatible` en `Menus.Infrastructure`.
- La configuración `IA:BaseUrl`, `IA:Model` e `IA:ApiKey` permite conmutar **sin tocar código** entre **OpenAI** (nube), **Ollama** (local, expone el mismo `/v1`) y **NVIDIA NIM** (para probar).
- **Ollama en local es el valor por defecto en desarrollo:** coste marginal cero y funciona offline.
- La clave API se gestiona por `user-secrets`/variables de entorno, nunca en el repositorio (se apoya en la tarea de secretos de la línea de seguridad, #295).

### 3. Separación por volumen de uso (clave de escalabilidad)

| Audiencia | Volumen | Estrategia |
|---|---|---|
| **Paciente** | Alto (masivo) | **Sin LLM por petición.** Texto determinista a partir de los números de la valoración, **precalculado una vez por valoración** y cacheado en Redis/Mongo. Como la valoración no cambia, se genera una vez y se sirve a todos. Escala y no cuesta. |
| **Nutricionista** | Bajo (pocos) | **Aquí sí LLM generativo:** resúmenes entre pacientes, borradores de recomendación y Q&A sobre fichas. El coste se justifica por el volumen bajo y la concurrencia baja. |

### 4. Feature flag y degradación elegante

`IA:Habilitada` activa o desactiva la función. Si está desactivada o el proveedor falla, se toma el camino determinista: la aplicación corre igual y una demostración nunca depende de la red.

### 5. Privacidad: anonimizar antes de enviar

Al LLM solo se envían **métricas agregadas** (porcentajes de cobertura por nutriente, kcal objetivo frente a consumidas, déficits/excesos, dieta, alérgenos respetados), nunca el nombre ni el identificador del paciente. Esta decisión se apoya en la tarea de protección de datos de la línea de seguridad (#293).

### 6. Lenguaje natural → consulta (fase 2): router de intención, no SQL generado

Para la consulta en lenguaje natural, un **clasificador de intención pequeño** (un modelo local reducido o *embeddings* y similitud) mapea la pregunta a **una de las seis consultas informacionales predefinidas**. El LLM nunca genera SQL. Así no hay inyección ni consultas no validadas, y el camino común es barato y escalable.

### 7. Alcance y fases

- **MVP (spike #297):** camino determinista del paciente + LLM para el nutricionista (explicación de la valoración), detrás del feature flag.
- **Fase 2:** router de intención para la consulta en lenguaje natural.
- **Pruebas:** los manejadores y el endpoint se prueban con un `IAsistenteIA` falso; no hay llamadas reales al proveedor en CI.

## Alternativas consideradas

| Alternativa | Razón de descarte |
|---|---|
| Adaptador atado a un proveedor concreto (SDK de OpenAI o de Anthropic) | Impide conmutar a Ollama o NVIDIA; la interfaz OpenAI-compatible cubre los tres con un solo adaptador |
| Llamar al LLM en cada petición del paciente | No escala sin servidores grandes; el camino determinista precalculado y cacheado sí |
| Que el LLM genere la consulta SQL (opción D ingenua) | Riesgo de inyección y de consultas no validadas; el router a consultas predefinidas lo evita |
| Que el LLM fije o ajuste la valoración | Rompe la fuente de verdad de BD II y arriesga datos clínicos |
| IA siempre activa, sin feature flag | Acopla la aplicación a un servicio externo y a la red; la demo dependería de su disponibilidad |
| Enviar el registro completo del paciente | Expone datos de salud; se envían solo métricas anonimizadas |

## Consecuencias

- ✅ El proveedor (OpenAI, Ollama o NVIDIA) se conmuta por configuración; el desarrollo corre offline con Ollama.
- ✅ El paciente, que es el usuario masivo, no genera coste de LLM: la función escala apoyándose en la caché.
- ✅ Encaja en el CQRS existente (ADR-010) como un puerto y un adaptador más.
- ✅ Aporta material al informe: §10 (integraciones), §13 (seguridad: anonimización), §15.2 (roadmap); y suma el patrón *Adapter*/*Strategy* al checklist de IS (#96).
- ⚠️ Hay que implementar, post-informe: el adaptador OpenAI-compatible, el generador determinista y la caché por valoración.
- ⚠️ Exige gestionar la clave API fuera del repositorio (#295).
- ⚠️ El router de intención (fase 2) necesita un conjunto de ejemplos por cada consulta para clasificar.

## Actualización (2026-11-24)

La decisión de fondo no cambia (§1 a §6). Se precisan cuatro cosas, que **prevalecen sobre el texto anterior**
cuando difieren; el detalle de interfaces, datos y configuración está en
[`docs/ia/contrato-iasistenteia.md`](../ia/contrato-iasistenteia.md).

1. **Alcance y calendario.** La IA **entra en la v1.0** (informe final: 17-12-2026), ya no como extensión posterior
   al informe. Sigue detrás del feature flag `IA:Habilitada`.
2. **Más tareas que explicar la valoración.** El asistente cubre tres usos: explicar análisis sobre cualquier dato del
   sistema, sugerir acciones al nutricionista y dar un guion a un nutricionista novato para explicar el caso a un
   paciente sin formación médica. Los datos entran como *bloques de contexto* independientes, en la cantidad que
   se elija (de uno a todos los del caso); sumar una fuente nueva, como la ficha nutricional, no cambia el puerto.
3. **Privacidad determinista al construir el prompt.** Ningún dato personal llega al modelo: lo garantiza el código
   del servidor (constructor y auditor de prompt), no una instrucción al modelo, y vale con cualquier proveedor.
   Los datos clínicos entran; la edad se convierte en un rango y el nombre, los identificadores y la sala se eliminan.
   Las combinaciones poco frecuentes entran solo con el **consentimiento del paciente**; sin él se generalizan u omiten.
4. **Responsabilidad de quien configura un proveedor externo.** Debe aceptar unos términos de uso y confirmar que
   desactivó la retención y el entrenamiento en su proveedor; sin eso la IA externa no arranca.

Consecuencias añadidas:
- ✅ Una salida solo puede decir lo que está en la entrada: un verificador comprueba las cifras tras el modelo.
- ⚠️ El consentimiento del paciente exige un cambio en el dominio (campo, migración, endpoint y pantalla), que es
  trabajo aparte de la línea de IA.
- ⚠️ Los issues de la línea de IA redactados antes de esta actualización asumen «explicar la valoración» y un puerto
  que genera la explicación; hay que alinearlos con el contrato.
