-- R6-22 (el revisor está autorizado para la dieta), R7-25 (no valida un menú que creó) y R7-26 (no valida
-- antes de la creación del menú). Adaptado del #11, §7.
CREATE FUNCTION fn_validacion() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE m menus%ROWTYPE;
BEGIN
    SELECT * INTO m FROM menus WHERE id = NEW.menu_id;
    IF m.nutricionista_creador_id = NEW.nutricionista_id THEN
        RAISE EXCEPTION 'R7-25: el nutricionista % creó el menú % y no puede validarlo', NEW.nutricionista_id, NEW.menu_id;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM autoriza a
                   WHERE a.nutricionista_id = NEW.nutricionista_id AND a.dieta_id = m.dieta_id) THEN
        RAISE EXCEPTION 'R6-22: el revisor % no está autorizado para la dieta %', NEW.nutricionista_id, m.dieta_id;
    END IF;
    IF NEW.fecha_validacion < m.fecha_creacion THEN
        RAISE EXCEPTION 'R7-26: la validación es anterior a la creación del menú %', NEW.menu_id;
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER trg_validacion BEFORE INSERT OR UPDATE ON validacion
    FOR EACH ROW EXECUTE FUNCTION fn_validacion();
