-- R7-30: la valoración que genera una revalorización evalúa la misma asignación que la valoración revisada, y la
-- emite el nutricionista que ejecutó la revalorización. Adaptado del #11, §7.
-- R7-29 (la revalorización revisa una valoración de su solicitante) ya no necesita trigger: el solicitante no se
-- guarda, se deduce de la valoración revisada, así que se cumple por estructura (revisión del 19/10).
CREATE FUNCTION fn_r7_30() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE r valoraciones_nutricionales%ROWTYPE; g valoraciones_nutricionales%ROWTYPE;
BEGIN
    IF NEW.valoracion_generada_id IS NOT NULL THEN
        SELECT * INTO r FROM valoraciones_nutricionales WHERE id = NEW.valoracion_revisada_id;
        SELECT * INTO g FROM valoraciones_nutricionales WHERE id = NEW.valoracion_generada_id;
        IF (g.menu_id, g.paciente_id) <> (r.menu_id, r.paciente_id) OR g.nutricionista_id <> NEW.nutricionista_id THEN
            RAISE EXCEPTION 'R7-30: la valoración generada % no evalúa la misma asignación o no la emitió el ejecutor', NEW.valoracion_generada_id;
        END IF;
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER trg_r7_30 BEFORE INSERT OR UPDATE ON revalorizaciones
    FOR EACH ROW EXECUTE FUNCTION fn_r7_30();
