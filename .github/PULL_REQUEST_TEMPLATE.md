# Pull Request

## Issue relacionado

Cierra #<!-- número del issue -->

## Descripción

<!-- Qué hace este PR y por qué es necesario. -->

---

## Checklist

### Código

- [ ] El código compila sin errores (`dotnet build`)
- [ ] Todos los tests pasan (`dotnet test`)
- [ ] Cobertura ≥ 80 % en los módulos modificados
- [ ] No hay secretos ni credenciales en el código

### Documentación de código (obligatorio)

- [ ] Todos los tipos públicos nuevos tienen docstring XML (`/// <summary>`)
- [ ] Todos los métodos públicos no triviales tienen docstring XML
- [ ] Los parámetros con restricciones o que lanzan excepciones están documentados (`/// <param>`, `/// <exception>`)

### Decisiones de arquitectura (cuando aplica)

- [ ] Si se introdujo una abstracción nueva, un patrón o una decisión de tecnología → se creó o actualizó el ADR correspondiente en `docs/decisions/`
- [ ] Si no hay decisión nueva que documentar → marcar esta casilla igualmente para confirmar que se revisó

### Buenas prácticas (IS)

- [ ] Commits siguen Conventional Commits (`feat:`, `fix:`, `docs:`, `test:`, etc.)
- [ ] El nombre de la rama sigue el patrón `feat/issue-XX-descripcion`
- [ ] No hay `console.log`, `Debug.WriteLine` ni código comentado innecesario

---

## Notas para el revisor

<!-- Cualquier contexto adicional que facilite la revisión. -->
