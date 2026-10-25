-- R7-35 y R7-36: el menú de reemplazo (relación Origina, #155) proviene de un rechazo y es de la misma dieta que
-- el menú rechazado. Reglas nuevas del registro del rechazo (decisión del equipo, 24/10); se agregan al #11 en la
-- fase de documentación.
-- R7-35: la validación de origen tiene aprobado = false. Un menú aprobado no se reemplaza; si la validación no
-- existe, no dice nada y la FK rechaza la fila con su propio error. Como la validación no cambia (R7-33), alcanza
-- con comprobarlo desde el menú.
-- R7-36: se comprueba desde los dos lados, porque relaciona dos filas de menus: al registrar el origen, y al
-- cambiar la dieta de un menú que otro reemplaza.
CREATE FUNCTION fn_r7_35_r7_36() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE dieta_origen uuid;
BEGIN
    IF NEW.validacion_origen_id IS NOT NULL THEN
        IF EXISTS (SELECT 1 FROM validacion WHERE menu_id = NEW.validacion_origen_id AND aprobado) THEN
            RAISE EXCEPTION 'R7-35: el menú % fue aprobado y no origina un menú de reemplazo', NEW.validacion_origen_id;
        END IF;
        SELECT dieta_id INTO dieta_origen FROM menus WHERE id = NEW.validacion_origen_id;
        IF dieta_origen <> NEW.dieta_id THEN
            RAISE EXCEPTION 'R7-36: el menú de reemplazo % no es de la dieta del menú rechazado %', NEW.id, NEW.validacion_origen_id;
        END IF;
    END IF;
    IF TG_OP = 'UPDATE' AND NEW.dieta_id IS DISTINCT FROM OLD.dieta_id
       AND EXISTS (SELECT 1 FROM menus r WHERE r.validacion_origen_id = NEW.id AND r.dieta_id <> NEW.dieta_id) THEN
        RAISE EXCEPTION 'R7-36: el menú % tiene un menú de reemplazo de otra dieta', NEW.id;
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER trg_r7_35_r7_36 BEFORE INSERT OR UPDATE OF validacion_origen_id, dieta_id ON menus
    FOR EACH ROW EXECUTE FUNCTION fn_r7_35_r7_36();
