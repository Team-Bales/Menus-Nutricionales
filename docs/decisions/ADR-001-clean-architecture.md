# ADR-001: Clean Architecture como estructura de capas

**Estado:** Aceptado  
**Fecha:** 2026-10-09

## Contexto

El sistema gestiona lógica de negocio compleja (generación automática de menús,
valoraciones nutricionales, múltiples actores con distintos permisos) y debe ser
evaluado bajo los criterios de Ingeniería de Software, que exige explícitamente
"una arquitectura que permita a la aplicación ser desacoplada, extensible en
funcionalidades y mantenible" (requisito 7 del enunciado).

Necesitábamos una arquitectura que:
- Separe las reglas de negocio de los detalles de infraestructura (BD, HTTP, UI)
- Permita cambiar el motor de persistencia o el framework de UI sin tocar el dominio
- Sea verificable mediante tests unitarios sin levantar base de datos

## Decisión

Adoptamos **Clean Architecture** (también llamada Onion Architecture) con cuatro capas:

```
Menus.Domain          — entidades, value objects, interfaces de repositorio
Menus.Application     — casos de uso, DTOs, orquestación
Menus.Infrastructure  — EF Core, MongoDB, Redis, implementaciones de repositorios
Menus.Api             — controladores HTTP, configuración DI
Menus.Client          — Blazor WebAssembly (UI)
```

La regla de dependencia es estricta: **las capas internas no conocen las externas**.
`Domain` no referencia `Infrastructure`; `Application` no referencia `Api`.

## Alternativas consideradas

| Alternativa | Razón de descarte |
|-------------|-------------------|
| N-tier clásico (presentación / negocio / datos) | Acopla el dominio a la base de datos; dificulta tests unitarios |
| Arquitectura monolítica sin capas | No cumple el requisito 7 de IS sobre desacoplamiento |
| Microservicios | Sobre-ingeniería para un equipo de 5 estudiantes con un semestre de plazo |

## Consecuencias

- ✅ El dominio es testeable sin infraestructura (xUnit puro, sin EF Core en tests unitarios)
- ✅ Se puede cambiar de PostgreSQL a otro motor sin tocar `Domain` ni `Application`
- ✅ Cumple el requisito 7 de IS de forma explícita y demostrable
- ⚠️ Más archivos y carpetas que una arquitectura plana — requiere disciplina del equipo
- ⚠️ El mapeo entre capas (Domain → DTO → Response) genera código de conversión adicional
