# ADR-015: Autenticación en el cliente Blazor — JWT en `localStorage`, cierre de sesión ante 401 y roles alineados con la API

**Estado:** Aceptado
**Fecha:** 2026-11-20 (#75, PR #280)

## Contexto

El #74 protegió la API con JWT y políticas por rol (`SoloAdmin`, `SoloNutricionista`, `NutricionistaOAdmin`, `PacientePropioOPersonal`; ver ADR-011). El cliente Blazor WebAssembly todavía mandaba un actor fijo en `X-Actor-Id` (decisión 2 del ADR-013), así que no podía iniciar sesión ni distinguir roles. El #75 agrega login, perfil y cierre de sesión. Eso obliga a decidir dónde vive el token en el navegador, qué pasa cuando la API deja de aceptarlo y cómo se reflejan en la UI las reglas de autorización de la API.

## Decisiones

1. **El JWT se guarda en `localStorage` y se mantiene en memoria.** `AlmacenDeSesion` lo lee de `localStorage` la primera vez y después lo sirve desde un campo, así que las peticiones no pasan por la interop de JavaScript.
   - *Por qué `localStorage`:* la sesión sobrevive a recargar la página y a abrir otra pestaña, que es lo que espera el personal del centro.
   - *Riesgo aceptado:* si la aplicación tuviera un XSS, un script podría leer el token. El cliente no inserta HTML sin escapar (Blazor escapa por defecto y no se usa `MarkupString` con datos de usuario), y el token caduca. La alternativa robusta es una cookie `HttpOnly` emitida por la API, pero cambia el contrato de autenticación del ADR-011 y añade CSRF. Queda como trabajo futuro.

2. **`AutorizacionHandler` reemplaza a `ActorHeaderHandler`** (supera la decisión 2 del ADR-013). Es un `DelegatingHandler` que:
   - agrega `Authorization: Bearer` solo si el token sigue vigente; si caducó o está mal formado, lo borra y no lo manda;
   - si la API responde **401 a una petición que llevaba token**, cierra la sesión y lleva a `/login?returnUrl=…`. Un 401 sin token (credenciales incorrectas en el login) no cierra nada: lo muestra la pantalla de login;
   - resuelve `ISesionAutenticada` y `NavigationManager` en el momento de usarlos, no en el constructor, para no crear una dependencia circular `HttpClient` → handler → proveedor de estado.

3. **Las páginas exigen los mismos roles que la API** (`RolesDeUsuario`):

   | Pantalla | Rol en el cliente | Política de la API que refleja |
   |---|---|---|
   | Alimentos, platos y dietas (listado y detalle) | `Admin` o `Nutricionista` | Lectura: cualquier usuario autenticado |
   | Botones para crear, editar o borrar alimentos y platos, y formularios | `Nutricionista` | `SoloNutricionista` |
   | Botones para editar grupos y restricciones de una dieta | `Admin` | `SoloAdmin` |
   | `/admin/nomencladores` | `Admin` | `SoloAdmin` |

   Los botones de escritura se ocultan con `AuthorizeView Roles=…`. La API sigue siendo la que protege: esto solo evita botones que siempre darían 403.

4. **Sin sesión → login; con sesión pero sin rol → «Sin permisos».** `App.razor` distingue los dos casos en `NotAuthorized`, así un usuario conectado no termina en el login por entrar a una página que no le corresponde. El login vuelve a la página pedida (`returnUrl`), y solo acepta rutas locales para evitar redirecciones abiertas.

## Alternativas consideradas

| Alternativa | Razón de descarte |
|---|---|
| `sessionStorage` | Menos expuesto en el tiempo, pero cierra la sesión al cerrar la pestaña y no se comparte entre pestañas; el XSS lo puede leer igual |
| Cookie `HttpOnly` emitida por la API | La más segura frente a XSS, pero exige cambiar el login del ADR-011, configurar CSRF y `SameSite`, y no hay tiempo antes de la entrega |
| Solo `[Authorize]` en las páginas, sin roles | Es lo que había: el usuario veía botones que la API rechaza con 403, y un paciente podía abrir `/admin/nomencladores` |
| Comprobar la caducidad solo al cargar la app | Si el token caducaba con la pestaña abierta, la UI seguía mostrando la sesión y cada llamada fallaba con 401 sin llevar al login |

## Consecuencias

- ✅ La sesión caducada se detecta en la primera llamada que la API rechaza, y el usuario vuelve al login sin perder la página en la que estaba.
- ✅ Los roles de la UI se cambian en un solo sitio (`RolesDeUsuario`), cuyo comentario remite a `Politicas` de la API.
- ⚠️ `RolesDeUsuario` duplica los valores de `Menus.Domain.RolesUsuario`, porque el cliente no referencia el dominio. Si se agrega un rol o cambia una política, hay que tocar los dos.
- ⚠️ El token en `localStorage` sigue siendo legible ante un XSS (decisión 1).
