# Menús Nutricionales

Sistema web para la gestión de la generación automática de menús nutricionales en una institución de salud — administra alimentos y platos, dietas, nutricionistas, pacientes, y el flujo de generación, validación y valoración de menús.

Proyecto conjunto de las asignaturas **Bases de Datos II** e **Ingeniería de Software**, Séptimo Semestre, Ciencias de la Computación — Curso 2026-2027.

## Equipo — Team-Bales

| Integrante | Rol | GitHub |
|---|---|---|
| _(completar)_ | Project Manager | @QuiquiMatCom2004 |
| _(completar)_ | | |
| _(completar)_ | | |
| _(completar)_ | | |
| _(completar)_ | | |

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

## Estructura del repositorio (objetivo)

```
/src
  /Menus.Domain          # Entidades, reglas de negocio, interfaces (Repository, IUnitOfWork, Strategy)
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

> Esta estructura aún no existe en el repositorio — se agrega en el commit de scaffolding inicial del código, posterior a este commit de bootstrap.

## Cómo levantar el sistema

Requisitos previos:
- [.NET SDK](https://dotnet.microsoft.com/download) (versión a confirmar con el scaffolding inicial)
- [Docker](https://docs.docker.com/get-docker/) y Docker Compose

Pasos:

```bash
git clone https://github.com/Team-Bales/Menus-Nutricionales.git
cd Menus-Nutricionales
cp .env.example .env   # completar valores locales, nunca versionar .env
docker compose up --build
```

> El `docker-compose.yml` (servicios `api`, `postgres`, `mongo`, `redis`, `client`) se añade junto con el scaffolding inicial del código — este README se actualiza en ese mismo commit.

## Flujo de trabajo del equipo

Metodología Scrumban, tablero en GitHub Projects, ramas por issue (`tipo/numero-descripcion`), commits en formato [Conventional Commits](https://www.conventionalcommits.org/), Pull Request con al menos 1 revisión antes de mergear a `main` (sin squash, para conservar la evidencia de aporte individual por commit). Detalle completo en el documento de propuesta técnica del equipo.

## Licencia

Todos los derechos reservados — proyecto académico del equipo Team-Bales, sin licencia de reutilización pública.
