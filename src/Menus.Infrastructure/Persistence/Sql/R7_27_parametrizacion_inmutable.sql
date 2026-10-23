-- R7-27: la parametrización de un menú (cantidad total, si es automático, distribución y grupos cubiertos)
-- no cambia después de creado el menú; solo desaparece con el menú. Adaptado del #11, §7.
CREATE FUNCTION fn_r7_27_menu() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF (NEW.cantidad_total_alimentos, NEW.es_automatico) IS DISTINCT FROM (OLD.cantidad_total_alimentos, OLD.es_automatico) THEN
        RAISE EXCEPTION 'R7-27: la parametrización del menú % no se modifica', OLD.id;
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER trg_r7_27_menu BEFORE UPDATE ON menus FOR EACH ROW EXECUTE FUNCTION fn_r7_27_menu();

-- Las filas de distribución y de grupos cubiertos no se modifican nunca, y solo se borran junto con su menú.
-- Cambio respecto del #11: el borrado se comprueba al confirmar la transacción, y no con pg_trigger_depth().
-- EF Core, al borrar un menú cargado, borra primero esas filas con sentencias propias (no en cascada desde
-- menus), y el SQL original las rechazaba. Al confirmar, si el menú ya no existe, se borró el menú entero.
CREATE FUNCTION fn_r7_27_parametros_actualizacion() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    RAISE EXCEPTION 'R7-27: la parametrización del menú % no se modifica', OLD.menu_id;
END $$;
CREATE TRIGGER trg_r7_27_distribucion_actualizacion BEFORE UPDATE ON menu_distribucion
    FOR EACH ROW EXECUTE FUNCTION fn_r7_27_parametros_actualizacion();
CREATE TRIGGER trg_r7_27_grupos_actualizacion BEFORE UPDATE ON menu_grupo_nutricional
    FOR EACH ROW EXECUTE FUNCTION fn_r7_27_parametros_actualizacion();

CREATE FUNCTION fn_r7_27_parametros_borrado() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF EXISTS (SELECT 1 FROM menus WHERE id = OLD.menu_id) THEN
        RAISE EXCEPTION 'R7-27: la parametrización del menú % no se modifica', OLD.menu_id;
    END IF;
    RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER trg_r7_27_distribucion_borrado AFTER DELETE ON menu_distribucion
    DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION fn_r7_27_parametros_borrado();
CREATE CONSTRAINT TRIGGER trg_r7_27_grupos_borrado AFTER DELETE ON menu_grupo_nutricional
    DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION fn_r7_27_parametros_borrado();
