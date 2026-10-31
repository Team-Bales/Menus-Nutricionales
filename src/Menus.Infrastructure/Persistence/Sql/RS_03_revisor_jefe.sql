-- RS-03 (informe, §4.2), la parte de la decisión del #189: quien registra un dictamen tiene que ser jefe de
-- nutrición (nutricionistas.es_jefe, RD-18). Las otras dos condiciones de RS-03 (que esté autorizado para la
-- dieta y que no sea el creador) ya las comprueba fn_validacion, en el mismo INSERT.
-- Solo al insertar: la comprobación ocurre al dictaminar, y la validación no se modifica después (RS-15). Así,
-- retirar el cargo no invalida los dictámenes ya registrados. Quién puede cambiar es_jefe lo restringen los
-- permisos de la aplicación, no este trigger.
-- Lo aplica la migración AgregarEsJefeANutricionista, que también crea la columna, y no InitialCreate
-- (RestriccionesNoDeclarativas.ScriptsPosteriores). Es idempotente porque las pruebas que crean la base con
-- EnsureCreated lo reciben también con ScriptsDeCreacion.
-- Arranque: la migración deja a todos los nutricionistas existentes con es_jefe = false, así que nadie puede
-- dictaminar hasta designar al menos un jefe. La designación corresponde a la administración (RS-03), con
-- Nutricionista.DesignarJefe() desde la aplicación o, mientras no exista ese caso de uso, directamente en la base:
--     UPDATE nutricionistas SET es_jefe = true WHERE id = '<id del nutricionista>';
CREATE OR REPLACE FUNCTION fn_rs_03() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM nutricionistas WHERE id = NEW.nutricionista_id AND es_jefe) THEN
        RAISE EXCEPTION 'RS-03: el revisor % no es jefe de nutrición', NEW.nutricionista_id;
    END IF;
    RETURN NEW;
END $$;
DROP TRIGGER IF EXISTS trg_rs_03 ON validacion;
CREATE TRIGGER trg_rs_03 BEFORE INSERT ON validacion
    FOR EACH ROW EXECUTE FUNCTION fn_rs_03();