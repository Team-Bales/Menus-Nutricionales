#!/usr/bin/env bash
# Carga los datos de ejemplo (#284) en los contenedores de docker compose: PostgreSQL y MongoDB.
#
# Requisitos: `docker compose up -d` en marcha y las migraciones aplicadas (ver README de esta carpeta).
# Lee usuarios y bases del .env de la raíz del repositorio. Variables opcionales:
#   ENV_FILE               otro archivo de variables (por defecto, ../../.env)
#   CONTENEDOR_POSTGRES    por defecto menus_postgres
#   CONTENEDOR_MONGO       por defecto menus_mongo
set -euo pipefail

CARPETA="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENV_FILE="${ENV_FILE:-$CARPETA/../../.env}"
PG="${CONTENEDOR_POSTGRES:-menus_postgres}"
MG="${CONTENEDOR_MONGO:-menus_mongo}"

if [[ ! -f "$ENV_FILE" ]]; then
    echo "No encuentro $ENV_FILE. Copie .env.example a .env y complételo." >&2
    exit 1
fi

# Se leen solo las variables necesarias: el .env tiene valores con ';' que no se pueden cargar con `source`.
leer() { grep -E "^$1=" "$ENV_FILE" | tail -n 1 | cut -d= -f2-; }
POSTGRES_USER="$(leer POSTGRES_USER)"
POSTGRES_DB="$(leer POSTGRES_DB)"
MONGO_USER="$(leer MONGO_USER)"
MONGO_PASSWORD="$(leer MONGO_PASSWORD)"
MONGO_DB="$(leer MONGO_DB)"

echo "PostgreSQL ($PG, base $POSTGRES_DB)"
if ! docker exec "$PG" psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Atc \
        "SELECT to_regclass('public.\"__EFMigrationsHistory\"') IS NOT NULL" | grep -q t; then
    echo "  La base no tiene las migraciones aplicadas. Aplíquelas antes (README, paso 2)." >&2
    exit 1
fi
docker exec -i "$PG" psql -q -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "$POSTGRES_DB" \
    < "$CARPETA/datos-ejemplo.sql"
echo "  Datos cargados."

echo "MongoDB ($MG, base $MONGO_DB)"
# Como archivo y no por la entrada estándar: así mongosh no lo trata como una sesión interactiva.
docker cp -q "$CARPETA/fichas-nutricionales.js" "$MG:/tmp/fichas-nutricionales.js"
docker exec "$MG" mongosh --quiet \
    "mongodb://$MONGO_USER:$MONGO_PASSWORD@localhost:27017/$MONGO_DB?authSource=admin" \
    --file /tmp/fichas-nutricionales.js
docker exec "$MG" rm -f /tmp/fichas-nutricionales.js

echo "Listo. Usuarios de ejemplo: contraseña Demo1234! (ver README)."
