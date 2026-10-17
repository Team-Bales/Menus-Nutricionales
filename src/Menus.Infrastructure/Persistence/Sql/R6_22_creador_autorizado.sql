-- R6-22: quien crea un menú está autorizado para su dieta. Adaptado del #11, §7.
CREATE FUNCTION fn_r6_22_creador() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM autoriza a
                   WHERE a.nutricionista_id = NEW.nutricionista_creador_id AND a.dieta_id = NEW.dieta_id) THEN
        RAISE EXCEPTION 'R6-22: el nutricionista % no está autorizado para la dieta %', NEW.nutricionista_creador_id, NEW.dieta_id;
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER trg_r6_22_creador BEFORE INSERT OR UPDATE OF nutricionista_creador_id, dieta_id ON menus
    FOR EACH ROW EXECUTE FUNCTION fn_r6_22_creador();
