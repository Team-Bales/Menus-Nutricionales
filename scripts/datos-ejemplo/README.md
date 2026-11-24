# Datos de ejemplo

Banco de datos completo para recorrer la aplicación con todos los roles y para ejecutar las 6 consultas
informacionales con resultados con sentido (#284). Cubre todo el modelo: catálogos, dietas, nutricionistas,
pacientes, banco de alimentos y platos, menús en todos sus estados, asignaciones, servicios, consumo,
valoraciones, revalorizaciones, usuarios y las fichas nutricionales de MongoDB.

| Archivo | Qué es |
|---|---|
| `datos-ejemplo.sql` | Datos de PostgreSQL, en una sola transacción. **Generado**: no se edita a mano. |
| `fichas-nutricionales.js` | Fichas nutricionales de MongoDB (mongosh). **Generado**: no se edita a mano. |
| `generar.py` | El generador. Es determinista (semilla fija): siempre produce los mismos datos y los mismos identificadores. |
| `cargar.sh` | Carga los dos archivos en los contenedores de `docker compose`. |

> **Solo para entornos de demostración.** Todos los usuarios comparten la contraseña `Demo1234!`.

## Qué contiene

| Entidad | Cantidad | Detalle |
|---|---|---|
| Salas | 6 | Pediatría, Oncología, Geriatría, Medicina Interna A y B, Cirugía General |
| Nutricionistas | 10 | 3 jefes de nutrición; 1 recién incorporado, sin dietas autorizadas |
| Dietas | 8 | 2 pediátricas, 3 oncológicas, 3 geriátricas; una recién creada, sin menús ni pacientes |
| Pacientes | 48 | De 2 a 93 años; algunos inscritos en dos dietas y 4 recién ingresados, sin dieta |
| Alimentos | 66 | De los 5 grupos, los 4 tipos de preparación y los 3 niveles calóricos |
| Platos | 27 | Cada uno con su composición (de 1 a 3 alimentos, con cantidad) |
| Menús | 57 | 42 aprobados, 8 rechazados (cada uno con su menú de reemplazo), 7 pendientes de revisión |
| Asignaciones | 178 | Solo de menús aprobados, a pacientes inscritos en la dieta del menú |
| Servicios | 365 | De 1 a 3 por asignación, siempre después de la aprobación |
| Consumos | 1534 | Por ítem servido, con su aceptación |
| Valoraciones | 188 | Algunas asignaciones valoradas dos veces (seguimiento) |
| Revalorizaciones | 10 | 7 atendidas (con la valoración que generaron) y 3 pendientes |
| Usuarios | 59 | 1 administrador, los 10 nutricionistas y los 48 pacientes |
| Fichas (MongoDB) | 84 | Simples y compuestas; algunos ítems sin ficha, porque la ficha es opcional |

Además, el catálogo de **Excluye** gana una fila: la restricción *Sin lactosa* excluye el grupo *Lácteos*.

### Casos que se pueden ver a propósito

- **Menús rechazados con reemplazo:** cada dieta tiene uno (la hipoglucídica, dos), con observaciones del jefe y un
  menú de reemplazo aprobado de la misma dieta. Aparecen en la consulta 3.
- **Menús manuales que no cumplen sus parámetros:** el menú 3 declara 6 ítems y tiene 5; el 19 incluye una bebida que
  su distribución no prevé; el 43 incluye arroz blanco en una dieta con los cereales restringidos (y por eso se
  rechazó). La consulta 5 los marca.
- **Restricciones respetadas al elegir ítems:** la dieta sin gluten no tiene ítems con gluten; la sin lactosa, ninguno
  con leche; la hiposódica, ninguno con más de 300 mg de sodio; la hipoglucídica, ningún dulce.
- **Aceptación con patrón:** algunas verduras y legumbres (brócoli, espinaca, quimbombó, lentejas…) se rechazan más.
  Se ve en las consultas 4 y 6b.
- **Correlación nivel calórico–resultado con signo distinto según la dieta:** positiva en la hiperproteica
  oncológica (≈ 0,63) y en las pediátricas, negativa en la hiposódica (≈ −0,55) y la hipoglucídica (≈ −0,68), y débil
  en las demás. La dieta de transición, sin menús valorados, da NULL (consulta 6a).
- **Fichas variables:** unos alimentos tienen micronutrientes y otros no; en unos los alérgenos no están registrados
  (campo ausente) y en otros están registrados vacíos (`[]`). Los platos tienen ficha compuesta con modo de
  preparación y desglose por ingrediente, que coincide con su composición en PostgreSQL.

## Cómo cargarlos

1. **Levantar los servicios** desde la raíz del repositorio, con el `.env` completo:

   ```bash
   docker compose up -d --build
   ```

2. **Aplicar las migraciones** a la base de PostgreSQL del contenedor (la API no las aplica al arrancar). Con los
   valores de su `.env`:

   ```bash
   dotnet ef database update \
     --project src/Menus.Infrastructure --startup-project src/Menus.Infrastructure \
     --connection "Host=localhost;Port=5432;Database=<POSTGRES_DB>;Username=<POSTGRES_USER>;Password=<POSTGRES_PASSWORD>"
   ```

3. **Cargar los datos:**

   ```bash
   scripts/datos-ejemplo/cargar.sh
   ```

   El script comprueba que las migraciones estén aplicadas y lee usuarios y bases del `.env`. Si los contenedores se
   llaman distinto, use `CONTENEDOR_POSTGRES` y `CONTENEDOR_MONGO`.

**Sin el script**, a mano:

```bash
docker exec -i menus_postgres psql -v ON_ERROR_STOP=1 -U <POSTGRES_USER> -d <POSTGRES_DB> < scripts/datos-ejemplo/datos-ejemplo.sql
mongosh "mongodb://<MONGO_USER>:<MONGO_PASSWORD>@localhost:27017/<MONGO_DB>?authSource=admin" --file scripts/datos-ejemplo/fichas-nutricionales.js
```

### Volver a cargarlos

- El SQL no se carga dos veces: si los datos ya están, se detiene con un mensaje y no toca nada.
- Las fichas sí se pueden volver a cargar: reemplazan la de cada ítem, sin duplicar.
- Para empezar de cero, recree las bases. **Esto borra todos los datos de los volúmenes:**

  ```bash
  docker compose down -v && docker compose up -d
  ```

  y repita los pasos 2 y 3.

## Usuarios

Contraseña de todos: **`Demo1234!`**

| Correo | Rol | Notas |
|---|---|---|
| `admin@menus.demo` | Admin | |
| `laura.mendez@menus.demo` | Nutricionista, **jefa** | Pediátricas y geriátricas |
| `ernesto.valdes@menus.demo` | Nutricionista, **jefe** | Oncológicas y geriátricas |
| `marisol.fuentes@menus.demo` | Nutricionista, **jefa** | Pediátricas y oncológicas |
| `javier.hernandez@menus.demo` | Nutricionista | Pediátricas |
| `ana.rojas@menus.demo` | Nutricionista | Pediátricas |
| `carlos.diaz@menus.demo` | Nutricionista | Oncológicas |
| `yanet.morales@menus.demo` | Nutricionista | Oncológicas, incluida la de transición |
| `raul.fernandez@menus.demo` | Nutricionista | Geriátricas |
| `daniela.suarez@menus.demo` | Nutricionista | Geriátricas |
| `pedro.ramirez@menus.demo` | Nutricionista | Sin dietas autorizadas |
| `gabriela.delgado@menus.demo` | Paciente | 15 años, Pediatría; con menús, consumos y valoraciones |
| `miguel.ortiz@menus.demo` | Paciente | 47 años, Oncología; pidió dos revalorizaciones |
| `lourdes.ramos@menus.demo` | Paciente | 85 años, Geriatría; inscrita en dos dietas |
| `isabel.ramirez@menus.demo` | Paciente | Recién ingresada, sin dieta |

El correo de cada paciente es `nombre.primerapellido@menus.demo`. La lista completa:

```sql
SELECT u.email, u.rol, coalesce(n.nombre, p.nombre) AS nombre
FROM usuarios u
LEFT JOIN nutricionistas n ON n.id = u.entidad_id
LEFT JOIN pacientes p ON p.id = u.entidad_id
ORDER BY u.rol, u.email;
```

## Identificadores

Los identificadores son fijos y legibles: el prefijo indica la entidad y el final, el número.

| Entidad | Forma | Ejemplo |
|---|---|---|
| Dieta | `d1e7a000-0000-4000-8000-00000000000N` | `…0003` = Hiperproteica oncológica |
| Menú | `3e700000-0000-4000-8000-0000000000NN` | `…0035` = el menú con más consumos |
| Nutricionista | `0a7e0000-0000-4000-8000-0000000000NN` | `…0002` = Ernesto Valdés (jefe) |
| Paciente | `9ac1e000-0000-4000-8000-0000000000NN` | |
| Ítem | `17e30000-0000-4000-8000-0000000000NN` | Alimentos del 1 al 66, platos del 67 al 93 |

Dietas: 1 Normocalórica pediátrica · 2 Pediátrica sin gluten · 3 Hiperproteica oncológica · 4 Blanda oncológica sin
lactosa · 5 Geriátrica hiposódica · 6 Hipoglucídica geriátrica · 7 Geriátrica de fácil masticación · 8 Oncológica de
transición.

## Las 6 consultas con estos datos

```sql
-- 1. Menús automáticos de la dieta hiperproteica, con creador y parámetros
SELECT * FROM consulta_1_menus_automaticos_por_dieta('d1e7a000-0000-4000-8000-000000000003');

-- 2. Los 10 alimentos más usados en los menús finales de la dieta pediátrica normocalórica
SELECT * FROM consulta_2_alimentos_mas_usados('d1e7a000-0000-4000-8000-000000000001', 10);

-- 3. Dictámenes del jefe Ernesto Valdés, con rechazos y sus menús de reemplazo
SELECT * FROM consulta_3_dictamenes_de_un_revisor('0a7e0000-0000-4000-8000-000000000002');

-- 4. Desempeño de los pacientes ante el menú con más consumos
SELECT * FROM consulta_4_desempeno_ante_un_menu('3e700000-0000-4000-8000-000000000035');

-- 5. Comparación de las tres dietas geriátricas
SELECT * FROM consulta_5_distribucion_por_grupo(ARRAY['d1e7a000-0000-4000-8000-000000000005',
    'd1e7a000-0000-4000-8000-000000000006', 'd1e7a000-0000-4000-8000-000000000007']::uuid[]);
SELECT * FROM consulta_5_distribucion_por_nivel(ARRAY['d1e7a000-0000-4000-8000-000000000005',
    'd1e7a000-0000-4000-8000-000000000006', 'd1e7a000-0000-4000-8000-000000000007']::uuid[]);
SELECT * FROM consulta_5_criterios_de_equilibrio(ARRAY(SELECT id FROM dietas));

-- 6. Nivel calórico y resultado nutricional
SELECT * FROM consulta_6a_correlacion_por_dieta();
SELECT * FROM consulta_6b_items_mas_rechazados(10, 5);
SELECT * FROM consulta_6c_revalorizados_frente_a_su_sala();
```

## Cómo cambiarlos

Se cambia `generar.py` y se vuelve a generar (Python 3.10 o superior, sin dependencias):

```bash
python3 scripts/datos-ejemplo/generar.py
```

El generador comprueba las reglas de la base antes de escribir (por ejemplo: el revisor es jefe, está autorizado y no
creó el menú; un servicio no es anterior a la aprobación; una revalorización atendida evalúa la misma asignación).
Aun así, después de un cambio conviene cargarlo en una base limpia para confirmarlo.

Si cambia el esquema de la ficha en `src/Menus.Infrastructure/Mongo/FichaNutricionalEsquema.cs`, hay que actualizar
el validador que crea `fichas-nutricionales.js` (está en el generador).

La fecha de corte de los datos es el 23/11/2026: nada ocurre después.
