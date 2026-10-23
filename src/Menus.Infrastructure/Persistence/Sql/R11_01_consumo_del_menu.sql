-- R11-01: solo se registra consumo de alimentos del menú asignado, incluidos directamente o dentro de un plato
-- incluido. Adaptado del #11, §7.
CREATE FUNCTION fn_r11_01() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM menu_alimento ia
                   WHERE ia.menu_id = NEW.menu_id AND ia.alimento_id = NEW.alimento_id)
       AND NOT EXISTS (SELECT 1 FROM menu_plato ip JOIN composicion c ON c.plato_id = ip.plato_id
                       WHERE ip.menu_id = NEW.menu_id AND c.alimento_id = NEW.alimento_id) THEN
        RAISE EXCEPTION 'R11-01: el alimento % no forma parte del menú %', NEW.alimento_id, NEW.menu_id;
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER trg_r11_01 BEFORE INSERT OR UPDATE ON consumo
    FOR EACH ROW EXECUTE FUNCTION fn_r11_01();
