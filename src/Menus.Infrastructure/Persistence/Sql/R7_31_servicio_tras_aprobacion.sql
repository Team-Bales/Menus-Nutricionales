-- R7-31: solo se sirve un menú aprobado, y ningún servicio es anterior a la aprobación. Regla nueva del menú
-- servido (feedback del profesor, 19/10); se agrega al #11 en la fase de documentación.
-- Relaciona dos tablas, así que se comprueba desde las dos: al registrar o cambiar un servicio, y al cambiar o
-- borrar la validación de la que depende. Es diferida, porque EF no ordena entre sí inserciones sin FK
-- directa, como la validación y el servicio, y la regla solo tiene sentido al cierre de la operación.
CREATE FUNCTION fn_r7_31() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE m uuid;
BEGIN
    IF TG_TABLE_NAME = 'servicios' THEN m := NEW.menu_id; ELSE m := OLD.menu_id; END IF;
    IF EXISTS (SELECT 1 FROM servicios s
               WHERE s.menu_id = m
                 AND NOT EXISTS (SELECT 1 FROM validacion v
                                 WHERE v.menu_id = s.menu_id
                                   AND v.fecha_validacion <= s.fecha_servicio)) THEN
        RAISE EXCEPTION 'R7-31: el menú % tiene un servicio sin aprobación previa', m;
    END IF;
    RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER trg_r7_31_servicio AFTER INSERT OR UPDATE ON servicios
    DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION fn_r7_31();
CREATE CONSTRAINT TRIGGER trg_r7_31_validacion AFTER UPDATE OR DELETE ON validacion
    DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION fn_r7_31();
