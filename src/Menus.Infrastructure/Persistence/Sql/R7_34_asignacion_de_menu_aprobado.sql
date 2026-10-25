-- R7-34: solo se asigna un menú aprobado (final). Lo decidió el equipo el 16/10 y el dominio ya lo exigía; la
-- base no lo garantizaba. Como la validación no se borra (R7-33), alcanza con comprobarlo al asignar. Es
-- diferida porque EF no ordena entre sí la validación y la asignación, que no tienen FK directa, y la regla
-- solo tiene sentido al cierre de la operación. Regla nueva de la revisión del PR 4 (19/10); se agrega al #11
-- en la fase de documentación.
CREATE FUNCTION fn_r7_34() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM validacion WHERE menu_id = NEW.menu_id) THEN
        RAISE EXCEPTION 'R7-34: el menú % no está aprobado y no se puede asignar', NEW.menu_id;
    END IF;
    RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER trg_r7_34 AFTER INSERT OR UPDATE ON asignacion
    DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION fn_r7_34();
