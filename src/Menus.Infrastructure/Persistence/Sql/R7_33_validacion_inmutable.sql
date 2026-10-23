-- R7-33: la validación de un menú no se modifica ni se borra. El primer dictamen cierra el menú (decisión del
-- equipo, 16/10) y la validación es la aprobación: si se pudiera borrar, un menú ya asignado o servido volvería
-- a estar pendiente; si se pudiera cambiar la fecha, quedaría después de servicios ya registrados (R7-31).
-- Regla nueva de la revisión del PR 4 (19/10); se agrega al #11 en la fase de documentación.
CREATE FUNCTION fn_r7_33() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    RAISE EXCEPTION 'R7-33: la validación del menú % no se modifica ni se borra', OLD.menu_id;
END $$;
CREATE TRIGGER trg_r7_33 BEFORE UPDATE OR DELETE ON validacion
    FOR EACH ROW EXECUTE FUNCTION fn_r7_33();
