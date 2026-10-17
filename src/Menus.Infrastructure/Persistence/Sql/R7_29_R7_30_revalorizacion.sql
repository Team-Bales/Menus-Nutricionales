-- R7-29 y R7-30: la revalorización revisa una valoración de su solicitante, y la valoración que genera evalúa
-- la misma asignación y la emite el nutricionista que la ejecutó. Adaptado del #11, §7.
CREATE FUNCTION fn_revalorizacion() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE r valoraciones_nutricionales%ROWTYPE; g valoraciones_nutricionales%ROWTYPE;
BEGIN
    SELECT * INTO r FROM valoraciones_nutricionales WHERE id = NEW.valoracion_revisada_id;
    IF r.paciente_id <> NEW.paciente_id THEN
        RAISE EXCEPTION 'R7-29: la valoración revisada % no es del paciente %', NEW.valoracion_revisada_id, NEW.paciente_id;
    END IF;
    IF NEW.valoracion_generada_id IS NOT NULL THEN
        SELECT * INTO g FROM valoraciones_nutricionales WHERE id = NEW.valoracion_generada_id;
        IF (g.menu_id, g.paciente_id) <> (r.menu_id, r.paciente_id) OR g.nutricionista_id <> NEW.nutricionista_id THEN
            RAISE EXCEPTION 'R7-30: la valoración generada % no evalúa la misma asignación o no la emitió el ejecutor', NEW.valoracion_generada_id;
        END IF;
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER trg_revalorizacion BEFORE INSERT OR UPDATE ON revalorizaciones
    FOR EACH ROW EXECUTE FUNCTION fn_revalorizacion();
