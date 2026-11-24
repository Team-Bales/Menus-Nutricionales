# Análisis de dependencias vulnerables (SCA)

> Documento de trabajo para el §13.3 del informe (prevención de vulnerabilidades, OWASP). Se incorporará al
> informe cuando esa sección exista; hasta entonces vive aquí.

## Alcance

Análisis de composición de software (SCA) de las dependencias NuGet de la solución, con el objetivo de detectar
paquetes con avisos de seguridad conocidos y dejar constancia de los hallazgos y de su remediación.

El análisis cubre las dependencias directas y transitivas de todos los proyectos. No cubre imágenes base de
Docker, paquetes del sistema operativo ni acciones de GitHub: las acciones las vigila Dependabot por separado.

## Cómo reproducirlo

```bash
dotnet list MenusNutricionales.slnx package --vulnerable --include-transitive
```

Entorno de la ejecución:

| Dato | Valor |
|---|---|
| SDK | .NET 10.0.109 |
| Fuente | `https://api.nuget.org/v3/index.json` |
| Fecha | 2026-11-24 6:42 UTC |

El comando del criterio de aceptación, sin `--include-transitive`, solo lista las dependencias directas; los
avisos transitivos pueden aparecer como warnings de NuGet durante el restore, pero no en la tabla. En esta
solución todas las directas están limpias, pero las transitivas de los proyectos de integración no. Por eso el
análisis completo usa `--include-transitive`.

## Resultados

### Dependencias directas

Ningún proyecto declara un paquete con avisos de seguridad conocidos. El comando sin `--include-transitive`
responde «no vulnerable packages» para los 13 proyectos.

### Dependencias transitivas

Hay avisos en los dos proyectos de pruebas de integración. Todos llegan por dos paquetes de pruebas:

- `Respawn 5.0.0` → `Microsoft.Data.SqlClient 4.0.0`.
- `Testcontainers 4.0.0` → `SSH.NET 2023.0.0`.

| Paquete | Versión | Severidad | Advisory | Origen |
|---|---|---|---|---|
| `Azure.Identity` | 1.3.0 | Alta | [GHSA-5mfx-4wcx-rv27](https://github.com/advisories/GHSA-5mfx-4wcx-rv27) | `Respawn` → `Microsoft.Data.SqlClient` |
| `Azure.Identity` | 1.3.0 | Moderada | [GHSA-m5vv-6r4h-3vj9](https://github.com/advisories/GHSA-m5vv-6r4h-3vj9) | `Respawn` → `Microsoft.Data.SqlClient` |
| `Azure.Identity` | 1.3.0 | Moderada | [GHSA-wvxc-855f-jvrv](https://github.com/advisories/GHSA-wvxc-855f-jvrv) | `Respawn` → `Microsoft.Data.SqlClient` |
| `Microsoft.Data.SqlClient` | 4.0.0 | Alta | [GHSA-98g6-xh36-x2p7](https://github.com/advisories/GHSA-98g6-xh36-x2p7) | `Respawn` |
| `SSH.NET` | 2023.0.0 | Alta | [GHSA-q939-rpr3-3284](https://github.com/advisories/GHSA-q939-rpr3-3284) | `Testcontainers` |
| `SSH.NET` | 2023.0.0 | Alta | [GHSA-mggc-4xg6-vcxf](https://github.com/advisories/GHSA-mggc-4xg6-vcxf) | `Testcontainers` |
| `System.Drawing.Common` | 5.0.0 | Crítica | [GHSA-rxg9-xrhp-64gj](https://github.com/advisories/GHSA-rxg9-xrhp-64gj) | `Respawn` → `Microsoft.Data.SqlClient` |

Proyectos afectados:

- `tests/Menus.Infrastructure.IntegrationTests`
- `tests/Menus.Infrastructure.IntegrationTests.Auth`

La cadena completa de cada paquete se puede consultar con `dotnet nuget why <proyecto> <paquete>`; por ejemplo:

```bash
dotnet nuget why tests/Menus.Infrastructure.IntegrationTests/Menus.Infrastructure.IntegrationTests.csproj System.Drawing.Common
```

Ninguno de estos paquetes forma parte de los artefactos de producción (`src/`): son dependencias de las pruebas
que levantan contenedores con Testcontainers y limpian la base con Respawn. Aun así, se ejecutan en CI y en las
máquinas de desarrollo, así que conviene corregirlas.

## Relación con los bumps de Dependabot

| PR | Paquete | Cambio | Estado | Relación con el SCA |
|---|---|---|---|---|
| #271 | `bunit` | 2.14.0 → 2.15.2 | Fusionado (`e476a16`) | No aparece en los hallazgos; mantiene actualizado el cliente de pruebas Blazor. |
| #272 | `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.14 → 10.0.15 | Pendiente de fusionar (`7783811` en la rama de Dependabot) | No aparece en los hallazgos, pero sigue pendiente; conviene no dejarlo atrás. |
| #273 | `coverlet.collector` | 10.1.0 → 10.1.1 | Fusionado (`9a1b703`) | No aparece en los hallazgos; mantiene actualizado el colector de cobertura. |

Dependabot revisa NuGet y GitHub Actions cada lunes (`.github/dependabot.yml`, límite de 5 PR abiertos). Este
análisis es complementario: Dependabot propone actualizaciones, pero no sustituye una revisión periódica de
avisos ni cubre las dependencias transitivas que no se actualizan solas.

## Remediación recomendada

Actualizar los paquetes que arrastran las versiones vulnerables:

| Paquete | Actual | Recomendada | Efecto |
|---|---|---|---|
| `Respawn` | 5.0.0 | 7.0.0 | Deja de depender de `Microsoft.Data.SqlClient`, por lo que desaparecen `Azure.Identity 1.3.0` y `System.Drawing.Common 5.0.0`. |
| `Testcontainers.MongoDb` | 4.0.0 | 4.17.0 | Usa `SSH.NET 2026.0.0`, sin los avisos actuales. |
| `Testcontainers.PostgreSql` | 4.0.0 | 4.17.0 | Igual que el anterior. |
| `Testcontainers.Redis` | 4.0.0 | 4.17.0 | Igual que el anterior. |

`Respawn` 7 cambia la API de `Checkpoint` a `Respawner` (`CreateAsync` y `ResetAsync`), así que la actualización
requiere adaptar `tests/Menus.Infrastructure.IntegrationTests/Fixtures/PostgresFixture.cs`. En un proyecto
aislado se comprobó que `Respawn 7.0.0` y `Testcontainers 4.17.0` no arrastran paquetes vulnerables.

Después de actualizar, repetir:

```bash
dotnet list MenusNutricionales.slnx package --vulnerable --include-transitive
```

El objetivo es que los dos proyectos de integración también respondan «no vulnerable packages».

## Resumen

El análisis de composición de software se ejecutó con `dotnet list package --vulnerable --include-transitive`
sobre la solución completa. No se detectaron dependencias vulnerables en los proyectos de producción en esta
ejecución. En los proyectos de pruebas de integración se detectaron cuatro dependencias transitivas con avisos:
`Azure.Identity 1.3.0`, `Microsoft.Data.SqlClient 4.0.0`, `SSH.NET 2023.0.0` y `System.Drawing.Common 5.0.0`,
introducidas por `Respawn 5.0.0` y `Testcontainers 4.0.0`; no forman parte de los artefactos desplegados. La
remediación es actualizar `Respawn` a 7.0.0 y `Testcontainers` a 4.17.0 y repetir el análisis. Dependabot propone
las actualizaciones de las dependencias directas (bunit #271, JwtBearer #272 y coverlet.collector #273); #271 y
#273 ya están fusionados y #272 sigue pendiente. El SCA añade la revisión de las transitivas y la evidencia para
la sección de prevención de vulnerabilidades (OWASP A06:2021, componentes vulnerables y desactualizados).

## Limitaciones

- El resultado es una foto del momento: los avisos cambian y deben volver a consultarse antes de cada entrega.
- La base de avisos es la de NuGet; no cubre vulnerabilidades de las imágenes Docker ni del sistema operativo.
- Las acciones de GitHub las vigila Dependabot, no este comando.
- El comando sin `--include-transitive` da una falsa sensación de limpieza en la tabla: solo lista las directas,
  aunque el restore pueda emitir warnings de transitivas.
