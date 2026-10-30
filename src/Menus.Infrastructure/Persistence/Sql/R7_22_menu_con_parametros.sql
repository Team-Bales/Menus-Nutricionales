-- RK-09: todo menú tiene al menos una proporción por tipo de preparación y al menos un grupo nutricional
-- cubierto. Cardinalidad mínima, comprobada al confirmar la transacción. El #11, §7, indica que sigue el
-- mismo patrón que RK-03.
CREATE FUNCTION fn_r7_22() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE m uuid;
BEGIN
    IF TG_TABLE_NAME = 'menus' THEN m := NEW.id; ELSE m := OLD.menu_id; END IF;
    IF EXISTS (SELECT 1 FROM menus WHERE id = m)
       AND (NOT EXISTS (SELECT 1 FROM menu_distribucion WHERE menu_id = m)
            OR NOT EXISTS (SELECT 1 FROM menu_grupo_nutricional WHERE menu_id = m)) THEN
         RAISE EXCEPTION 'RK-09: el menú % no tiene distribución por tipo de preparación o grupos cubiertos', m;
    END IF;
    RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER trg_r7_22_menu AFTER INSERT ON menus
    DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION fn_r7_22();
CREATE CONSTRAINT TRIGGER trg_r7_22_distribucion AFTER DELETE OR UPDATE OF menu_id ON menu_distribucion
    DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION fn_r7_22();
CREATE CONSTRAINT TRIGGER trg_r7_22_grupos AFTER DELETE OR UPDATE OF menu_id ON menu_grupo_nutricional
    DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION fn_r7_22();