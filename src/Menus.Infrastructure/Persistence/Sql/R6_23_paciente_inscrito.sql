-- R6-23: un paciente solo recibe menús de dietas en las que está inscrito. Adaptado del #11, §7.
CREATE FUNCTION fn_r6_23() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM menus m JOIN paciente_dieta i ON i.dieta_id = m.dieta_id
                   WHERE m.id = NEW.menu_id AND i.paciente_id = NEW.paciente_id) THEN
        RAISE EXCEPTION 'R6-23: el paciente % no está inscrito en la dieta del menú %', NEW.paciente_id, NEW.menu_id;
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER trg_r6_23 BEFORE INSERT OR UPDATE ON asignacion
    FOR EACH ROW EXECUTE FUNCTION fn_r6_23();
