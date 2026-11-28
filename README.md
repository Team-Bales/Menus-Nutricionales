# Menús Nutricionales

[![CI](https://github.com/Team-Bales/Menus-Nutricionales/actions/workflows/build.yml/badge.svg)](https://github.com/Team-Bales/Menus-Nutricionales/actions/workflows/build.yml)
[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=Team-Bales_Menus-Nutricionales&metric=alert_status)](https://sonarcloud.io/project/overview?id=Team-Bales_Menus-Nutricionales)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=Team-Bales_Menus-Nutricionales&metric=coverage)](https://sonarcloud.io/project/overview?id=Team-Bales_Menus-Nutricionales)
[![codecov](https://codecov.io/gh/Team-Bales/Menus-Nutricionales/branch/main/graph/badge.svg)](https://codecov.io/gh/Team-Bales/Menus-Nutricionales)

Sistema web para la gestión de la generación automática de menús nutricionales en una institución de salud — administra alimentos y platos, dietas, nutricionistas, pacientes, y el flujo de generación, validación y valoración de menús.

Proyecto conjunto de las asignaturas **Bases de Datos II** e **Ingeniería de Software**, Quinto Semestre, Ciencias de la Computación — Curso 2026-2027.

## Equipo — Team-Bales

| Integrante | Rol | GitHub |
|---|---|---|
| Enrique Alejandro Gonzalez Moreira | Project Manager | @QuiquiMatCom2004 |
| Javier Fontes Basabe | QA | @FontesHabana|
| Heily Rodriguez Rodriguez | Data Base Engineer | @heilyrodriguez225|
| Ernesto Alejandro Soler Choong | Backend Engineer| @solerch5510|
| Mauricio Bridon Carbo | Frontend Engineer | @Mackandal04 |

## Arquitectura

Clean Architecture (Onion) en 4 capas — Dominio, Aplicación, Infraestructura, Presentación — con persistencia poliglota: PostgreSQL para los datos relacionales del dominio y MongoDB para la ficha nutricional ampliada de cada alimento (estructura variable/anidada, no tabular). Patrones aplicados: Repository, Strategy, Unit of Work. Detalle completo de las decisiones y su justificación en el documento de propuesta técnica del equipo (fuera de este repositorio).

## Stack técnico

| Capa | Tecnología |
|---|---|
| Backend | ASP.NET Core Web API (C#) |
| Frontend | Blazor WebAssembly (hospedado) |
| ORM relacional | Entity Framework Core |
| Base de datos relacional | PostgreSQL |
| Base de datos NoSQL | MongoDB |
| Caché | Redis |
| Contenedores | Docker + Docker Compose |
| Testing | xUnit + Moq/NSubstitute (backend), bUnit (frontend) |

## Estructura del repositorio

```
/src
  /Menus.Domain          # Entidades, reglas de negocio, interfaces (IRepository, IUnitOfWork, Strategy)
  /Menus.Application      # Casos de uso, orquestación
  /Menus.Infrastructure    # Implementación de repositorios, EF Core, driver Mongo, Redis
  /Menus.Api               # Presentación — API REST, endpoints de comando y de consulta (CQRS-lite)
  /Menus.Client            # Blazor WebAssembly
/tests
  /Menus.Domain.Tests
  /Menus.Application.Tests
  /Menus.Client.Tests
docker-compose.yml
```

## Cómo levantar el sistema

Requisitos previos:
- [.NET SDK 10.0](https://dotnet.microsoft.com/download) (LTS)
- [Docker](https://docs.docker.com/get-docker/) y Docker Compose

Pasos:

```bash
git clone https://github.com/Team-Bales/Menus-Nutricionales.git
cd Menus-Nutricionales
cp .env.example .env   # completar valores locales, nunca versionar .env
docker compose up --build
```

Servicios que levanta `docker-compose.yml`: `api` (ASP.NET Core), `client` (Blazor WebAssembly servido por nginx), `postgres`, `mongo`, `redis`.

La API **no aplica las migraciones al arrancar**. La primera vez, y después de cada migración nueva, hay que aplicarlas a la base del contenedor como se explica en [Aplicar las migraciones](#aplicar-las-migraciones). Después, la aplicación queda en http://localhost:5000.

### Aplicar las migraciones

Con la herramienta `dotnet-ef` (una sola vez: `dotnet tool install --global dotnet-ef`) y los valores de `POSTGRES_*` de tu `.env`:

```bash
dotnet ef database update \
  --project src/Menus.Infrastructure --startup-project src/Menus.Infrastructure \
  --connection "Host=localhost;Port=5432;Database=menus_nutricionales;Username=menus_app;Password=change_me"
```

Termina con `Done.`. El mismo comando sirve con Docker y sin él: en los dos casos PostgreSQL escucha en `localhost:5432`.

### Correr la API y el cliente sin Docker (desarrollo)

Para depurar la API o trabajar en el cliente con recarga, se corren los dos con `dotnet run`. Las bases pueden seguir en Docker.

1. **Levantar las bases** y [aplicar las migraciones](#aplicar-las-migraciones):

   ```bash
   docker compose up -d postgres mongo redis
   ```

   Sirve también un PostgreSQL propio. Mongo y Redis son opcionales: sin sus cadenas de conexión, la API arranca sin fichas nutricionales y sin caché.

2. **Correr la API** (se queda en primer plano, en `http://localhost:5119`). Necesita al menos `Jwt__Key` y la cadena de PostgreSQL. Sin la clave se cae al arrancar con «La clave JWT (Jwt:Key) no está configurada».

   ```bash
   export Jwt__Key="clave-local-de-desarrollo-de-al-menos-32-bytes"
   export ConnectionStrings__Postgres="Host=localhost;Port=5432;Database=menus_nutricionales;Username=menus_app;Password=change_me"
   # Opcionales:
   # export ConnectionStrings__Mongo="mongodb://menus_app:change_me@localhost:27017/fichas_nutricionales?authSource=admin"
   # export ConnectionStrings__Redis="localhost:6379"
   # export AuthBootstrap__Email="admin@ejemplo.com" AuthBootstrap__Password="cambiar_por_password_seguro"
   dotnet run --project src/Menus.Api --launch-profile http
   ```

   En PowerShell, cada variable se define con `$env:Jwt__Key = "..."`.

3. **Correr el cliente** en otra terminal:

   ```bash
   dotnet run --project src/Menus.Client --launch-profile http
   ```

   y abrirlo en **http://localhost:5134**. Tiene que ser ese origen: la API solo acepta llamadas del cliente desde ahí (`Cors:OrigenesDelCliente` en `appsettings.Development.json`). Con el perfil `https` (`https://localhost:7290`), CORS bloquea las llamadas y el login muestra «No se pudo conectar con el servidor», lo mismo que si la API no está corriendo.

4. **Iniciar sesión** con el Admin de `AuthBootstrap__*` o con un usuario de los [datos de ejemplo](#datos-de-ejemplo).

El cliente toma la dirección de la API de `src/Menus.Client/wwwroot/appsettings.Development.json` (`http://localhost:5119/`). En Docker usa `appsettings.json`, que apunta al mismo origen, y nginx pasa `/api/` al contenedor de la API.

### Configurar el asistente de IA

La IA queda deshabilitada por defecto. Para desarrollo local con Ollama, configura `IA__Habilitada=true`, una URL local (`IA__BaseUrl=http://localhost:11434/v1`) y el modelo, sin incluir claves en el repositorio. Para una clave local, usa `user-secrets` en vez de archivos versionados:

```bash
dotnet user-secrets set "IA:ApiKey" "tu-clave-local" --project src/Menus.Api
```

Un proveedor externo solo puede habilitarse tras aceptar el texto vigente de [los términos de la integración](docs/ia/contrato-iasistenteia.md#81-términos-de-uso-y-condiciones-de-la-configuración-de-ia) y desactivar en ese proveedor la retención y el entrenamiento. Entonces deben definirse `IA__Terminos__Version=1`, `IA__Terminos__AceptadoPor`, `IA__Terminos__Fecha` e `IA__Terminos__RetencionDesactivada=true`. La API falla al arrancar si falta una de esas confirmaciones. No publiques ni compartas la API key.

### Datos de ejemplo

Para recorrer la aplicación con todos los roles y ejecutar las 6 consultas con resultados, hay un banco de datos completo (dietas, pacientes, menús, consumos, valoraciones, usuarios y fichas de MongoDB) en [`scripts/datos-ejemplo/`](scripts/datos-ejemplo/README.md). Con los contenedores de `postgres` y `mongo` levantados y después de [aplicar las migraciones](#aplicar-las-migraciones):

```bash
scripts/datos-ejemplo/cargar.sh
```

Todos los usuarios de ejemplo usan la contraseña `Demo1234!` (por ejemplo, `admin@menus.demo`). Es solo para entornos de demostración.

### Compilar y probar sin `sudo` ni Docker

Si no tienes permisos de administrador o no quieres instalar Docker, puedes instalar el SDK de .NET en tu directorio personal y ejecutar las pruebas de dominio (las de integración necesitan Docker y corren en CI):

```bash
curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
bash /tmp/dotnet-install.sh --channel 10.0 --install-dir "$HOME/.dotnet"
export DOTNET_ROOT=$HOME/.dotnet PATH=$HOME/.dotnet:$PATH   # añadir a ~/.bashrc
dotnet build MenusNutricionales.slnx
dotnet test MenusNutricionales.slnx --filter "FullyQualifiedName!~IntegrationTests"
```

**Nota:** Las pruebas de `tests/Menus.Infrastructure.IntegrationTests` necesitan Docker (Testcontainers) y se ejecutan en CI; sin Docker se excluyen con el filtro anterior y la ejecución completa (`dotnet test MenusNutricionales.slnx`) falla en ese proyecto.

## Flujo de trabajo del equipo

Metodología Scrumban, tablero en GitHub Projects, ramas por issue (`tipo/numero-descripcion`), commits en formato [Conventional Commits](https://www.conventionalcommits.org/), Pull Request con al menos 1 revisión antes de mergear a `main` (sin squash, para conservar la evidencia de aporte individual por commit). Detalle completo en el documento de propuesta técnica del equipo.

## Licencia

Todos los derechos reservados — proyecto académico del equipo Team-Bales, sin licencia de reutilización pública.
