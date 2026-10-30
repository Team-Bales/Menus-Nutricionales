-- RS-15 (informe, §4.2): la validación de un menú no se modifica ni se borra: el primer dictamen cierra el menú.
-- R7_33_validacion_inmutable.sql protege la validación; este script protege lo que el dictamen cierra, los ítems
-- incluidos (Incluye, tabla menu_item): a un menú con dictamen no se le agregan, quitan ni cambian ítems. El
-- dominio ya lo exige (Menu.AgregarItem y Menu.QuitarItem, solo mientras el menú está pendiente), y la
-- parametrización no cambia desde la creación (RS-07).
-- La excepción es la transacción que registra el dictamen. La unidad de trabajo puede guardar juntos los ítems
-- que se cambiaron mientras el menú estaba pendiente y el dictamen, y EF no garantiza en qué orden. Por eso se
-- rechaza el cambio solo si el dictamen se registró en una transacción anterior. Para distinguirlo, el alta de
-- la validación deja una marca local a la transacción, como el alta del menú en RS_07_parametrizacion_sin_altas.sql.
-- Lo aplica la migración CerrarParametrizacionEItemsDelMenu, y no InitialCreate
-- (RestriccionesNoDeclarativas.ScriptsPosteriores). Es idempotente porque las pruebas que crean la base con
-- EnsureCreated lo reciben también con ScriptsDeCreacion.
CREATE OR REPLACE FUNCTION fn_rs_15_dictamen_registrado() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    PERFORM set_config('menus.dictaminados_en_transaccion',
        coalesce(current_setting('menus.dictaminados_en_transaccion', true), '') || NEW.menu_id::text || ';', true);
    RETURN NULL;
END $$;
DROP TRIGGER IF EXISTS trg_rs_15_dictamen_registrado ON validacion;
CREATE TRIGGER trg_rs_15_dictamen_registrado AFTER INSERT ON validacion
    FOR EACH ROW EXECUTE FUNCTION fn_rs_15_dictamen_registrado();

CREATE OR REPLACE FUNCTION fn_rs_15_exigir_menu_abierto(p_menu_id uuid) RETURNS void LANGUAGE plpgsql AS $$
BEGIN
    IF EXISTS (SELECT 1 FROM validacion WHERE menu_id = p_menu_id)
       AND strpos(coalesce(current_setting('menus.dictaminados_en_transaccion', true), ''), p_menu_id::text) = 0 THEN
        RAISE EXCEPTION 'RS-15: el menú % ya tiene dictamen: sus ítems no cambian', p_menu_id;
    END IF;
END $$;

-- El borrado en cascada desde menus no se comprueba aquí. Un menú con dictamen no se puede borrar: la validación
-- lo referencia con RESTRICT (RR-19) y no se borra (R7_33_validacion_inmutable.sql). Así, el error es el de la FK,
-- que dice la causa real, y no depende del orden en que PostgreSQL dispara los triggers de las FK. El borrado que
-- hace EF fila por fila, antes de borrar el menú, sí se comprueba.
CREATE OR REPLACE FUNCTION fn_rs_15_items() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF TG_OP = 'DELETE' AND pg_trigger_depth() > 1 THEN
        RETURN OLD;
    END IF;
    IF TG_OP IN ('UPDATE', 'DELETE') THEN
        PERFORM fn_rs_15_exigir_menu_abierto(OLD.menu_id);
    END IF;
    IF TG_OP IN ('INSERT', 'UPDATE') THEN
        PERFORM fn_rs_15_exigir_menu_abierto(NEW.menu_id);
    END IF;
    IF TG_OP = 'DELETE' THEN
        RETURN OLD;
    END IF;
    RETURN NEW;
END $$;
DROP TRIGGER IF EXISTS trg_rs_15_items ON menu_item;
CREATE TRIGGER trg_rs_15_items BEFORE INSERT OR UPDATE OR DELETE ON menu_item
    FOR EACH ROW EXECUTE FUNCTION fn_rs_15_items();
