-- RS-07 (informe, §4.2): la parametrización de un menú (CantidadTotalAlimentos, EsAutomático, Distribuye y
-- CubreMenú) no cambia después de creado el menú. R7_27_parametrizacion_inmutable.sql impide modificarla y
-- borrarla suelta; este script cierra lo que faltaba: agregarle filas de distribución o de grupos cubiertos a un
-- menú ya creado.
-- «Ya creado» quiere decir insertado en una transacción anterior. El menú y su parametrización se guardan juntos
-- (EF los inserta en el mismo SaveChanges), y el menú exige al menos una fila de cada una al confirmar (RK-09,
-- R7_22_menu_con_parametros.sql).
-- Para saber si el menú se insertó en la transacción actual, el alta del menú deja una marca local a la
-- transacción (set_config con is_local = true). La marca desaparece al confirmar o al deshacer la transacción, y
-- también al deshacer un savepoint, así que sigue la suerte de la fila del menú. No se compara xmin con la
-- transacción actual porque, cuando la unidad de trabajo abre una transacción explícita, EF guarda dentro de un
-- savepoint, y entonces xmin es el de la subtransacción.
-- Lo aplica la migración CerrarParametrizacionEItemsDelMenu, y no InitialCreate
-- (RestriccionesNoDeclarativas.ScriptsPosteriores). Es idempotente porque las pruebas que crean la base con
-- EnsureCreated lo reciben también con ScriptsDeCreacion.
CREATE OR REPLACE FUNCTION fn_rs_07_menu_creado() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    PERFORM set_config('menus.creados_en_transaccion',
        coalesce(current_setting('menus.creados_en_transaccion', true), '') || NEW.id::text || ';', true);
    RETURN NULL;
END $$;
DROP TRIGGER IF EXISTS trg_rs_07_menu_creado ON menus;
CREATE TRIGGER trg_rs_07_menu_creado AFTER INSERT ON menus
    FOR EACH ROW EXECUTE FUNCTION fn_rs_07_menu_creado();

CREATE OR REPLACE FUNCTION fn_rs_07_alta_parametro() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF strpos(coalesce(current_setting('menus.creados_en_transaccion', true), ''), NEW.menu_id::text) = 0 THEN
        RAISE EXCEPTION 'RS-07: la parametrización del menú % no se amplía después de creado', NEW.menu_id;
    END IF;
    RETURN NEW;
END $$;
DROP TRIGGER IF EXISTS trg_rs_07_distribucion_alta ON menu_distribucion;
CREATE TRIGGER trg_rs_07_distribucion_alta BEFORE INSERT ON menu_distribucion
    FOR EACH ROW EXECUTE FUNCTION fn_rs_07_alta_parametro();
DROP TRIGGER IF EXISTS trg_rs_07_grupos_alta ON menu_grupo_nutricional;
CREATE TRIGGER trg_rs_07_grupos_alta BEFORE INSERT ON menu_grupo_nutricional
    FOR EACH ROW EXECUTE FUNCTION fn_rs_07_alta_parametro();
