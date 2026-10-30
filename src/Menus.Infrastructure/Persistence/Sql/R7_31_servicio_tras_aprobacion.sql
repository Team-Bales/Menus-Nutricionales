-- RS-14: ningún servicio es anterior a la aprobación del menú. El servicio es un día y la aprobación un
-- instante, así que se compara con el día de la aprobación en el calendario UTC. Regla nueva del menú servido
-- (feedback del profesor, 19/10); se agrega al #11 en la fase de documentación.
-- Se comprueba al registrar o cambiar un servicio. Del lado de la validación no hace falta: no se modifica ni se
-- borra (RS-15). Que el menú esté aprobado lo garantiza la asignación de la que depende el servicio (RS-16);
-- igual se exige que la validación sea una aprobación, porque con el rechazo registrado (#155) también puede ser
-- un rechazo.
-- Es diferida, porque EF no ordena entre sí inserciones sin FK directa, como la validación y el servicio, y la
-- regla solo tiene sentido al cierre de la operación.
CREATE FUNCTION fn_r7_31() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM validacion v
                   WHERE v.menu_id = NEW.menu_id AND v.aprobado
                     AND (v.fecha_validacion AT TIME ZONE 'UTC')::date <= NEW.fecha_servicio) THEN
         RAISE EXCEPTION 'RS-14: el servicio del menú % es anterior a su aprobación', NEW.menu_id;
    END IF;
    RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER trg_r7_31 AFTER INSERT OR UPDATE ON servicios
    DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION fn_r7_31();