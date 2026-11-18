# ADR-013: Cliente Blazor del catálogo — contrato `ICatalogoApi`, cabecera `X-Actor-Id` provisional y proxy Nginx

**Estado:** Aceptado
**Fecha:** 2026-11-12 (#65, PR #247)

## Contexto

El issue #65 pide la interfaz del catálogo nutricional (alimentos y platos) en el cliente Blazor WebAssembly. El cliente corre en un origen distinto de la API (en desarrollo, `dotnet run` levanta cliente y API en puertos separados; en Docker, el cliente se sirve como archivos estáticos). Mientras el #74 no agregue autenticación, el backend sigue exigiendo un actor identificado (`ActorNoIdentificadoException`, 401) en las operaciones de alta/edición/borrado. El PR #247 introduce tres piezas de infraestructura nuevas para resolver esto, y la plantilla de PR pide un ADR para decisiones de este tipo.

## Decisiones

1. **`ICatalogoApi` como contrato único del catálogo.** Las páginas Blazor no llaman a `HttpClient` directamente: dependen de la interfaz `ICatalogoApi` (implementada por `CatalogoApi`), inyectada por DI. Esto aísla las páginas de los detalles HTTP/JSON y permite reemplazarla en las pruebas bUnit por un falso en memoria, sin levantar servidor.
   - *Por qué no `HttpClient` directo en las páginas:* haría las pruebas de UI depender de un servidor HTTP real o de mocks de `HttpMessageHandler`, más frágiles y más lentas.

2. **`ActorHeaderHandler` como `DelegatingHandler` centralizado para `X-Actor-Id`.** En vez de que cada llamada de `CatalogoApi` agregue la cabecera, un `DelegatingHandler` se inserta una sola vez en la cadena del `HttpClient` (`Program.cs`) y la agrega a toda solicitud saliente, leyendo `Catalogo:ActorId` de la configuración. Si el valor no es un `Guid` válido, no agrega la cabecera — la API responde 401, que es el comportamiento esperado mientras no exista autenticación real.
   - *Por qué un handler y no un parámetro explícito en cada método de `ICatalogoApi`:* la cabecera es transversal a toda petición, no un dato del caso de uso; agregarla por método ensuciaría la interfaz con un concepto temporal que el #74 va a eliminar.
   - *Qué no resuelve:* no identifica al actor de verdad, solo simula uno fijo por configuración. `appsettings.json` de producción no trae `Catalogo:ActorId`, así que en producción ninguna escritura funciona hasta el #74 — es una limitación esperada, no un defecto de esta PR.

3. **Proxy Nginx en Docker; CORS solo en desarrollo.** La imagen Docker del cliente sirve los archivos estáticos con Nginx, que reenvía `/api/` al contenedor de la API (`nginx.conf`): cliente y API quedan en el mismo origen para el navegador, y no hace falta CORS. En desarrollo (`dotnet run`), cliente y API corren en orígenes distintos, así que la API habilita CORS vía `CorsDelCliente`, con los orígenes tomados de `Cors:OrigenesDelCliente` (solo en `appsettings.Development.json`; sin configurar, la política no admite a ninguno).
   - *Por qué no CORS en todos los entornos:* en Docker agregaría superficie de ataque sin necesidad, ya que el proxy da same-origin gratis.

## Alternativas consideradas

| Alternativa | Razón de descarte |
|---|---|
| CORS habilitado también en Docker/producción, sin proxy Nginx | Expondría la API a cualquier origen configurado innecesariamente; el proxy ya resuelve el problema con same-origin |
| Agregar `X-Actor-Id` a mano en cada método de `CatalogoApi` | Se repite en una docena de métodos y es fácil de olvidar en uno nuevo; un handler lo garantiza para toda solicitud |
| Que las páginas Blazor usen `HttpClient` directamente | Acopla la UI a HTTP/JSON y hace las pruebas bUnit depender de un servidor real |

## Consecuencias

- ✅ Las pruebas bUnit del catálogo no levantan servidor: implementan `ICatalogoApi` directamente (`CatalogoFalso`).
- ✅ Cuando el #74 agregue autenticación real, el cambio se concentra en `ActorHeaderHandler` (o se reemplaza por un handler de `Authorization`) sin tocar `ICatalogoApi` ni las páginas.
- ⚠️ Mientras el #74 no esté, toda escritura desde el cliente en un ambiente sin `Catalogo:ActorId` configurado (como la producción actual) devuelve 401. Es una limitación conocida, no un defecto de esta PR.
