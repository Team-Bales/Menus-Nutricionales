-- R11-01: solo se registra consumo de lo que el menú servido incluye. Con la generalización ItemAlimentario, el
-- menú incluye ítems (alimentos o platos) y se consume el ítem tal como se sirvió: un plato se acepta o se
-- rechaza entero (feedback del profesor, 19/10). Por eso ya no se busca el alimento dentro de los platos.
-- Adaptado del #11, §7.
CREATE FUNCTION fn_r11_01() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM menu_item mi
                   WHERE mi.menu_id = NEW.menu_id AND mi.item_id = NEW.item_id) THEN
        RAISE EXCEPTION 'R11-01: el ítem % no forma parte del menú %', NEW.item_id, NEW.menu_id;
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER trg_r11_01 BEFORE INSERT OR UPDATE ON consumo
    FOR EACH ROW EXECUTE FUNCTION fn_r11_01();
