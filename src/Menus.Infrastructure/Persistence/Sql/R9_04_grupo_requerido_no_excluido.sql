-- RS-11: un grupo que la dieta exige cubrir (Tipo = Requerido) no puede estar excluido por una de sus
-- restricciones (Restringe seguido de Excluye). Adaptado del #11, §7, a los nombres de la base, con una
-- corrección: solo cuenta Tipo = 'Requerido'. Un grupo Restringido que además está excluido repite la
-- prohibición sin contradecirla (MERX, D9-1), y el SQL original lo rechazaba.
CREATE FUNCTION fn_r9_04() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF EXISTS (SELECT 1 FROM cubre_dieta cd
               JOIN restringe r ON r.dieta_id = cd.dieta_id
               JOIN excluye e ON e.restriccion_alimentaria_id = r.restriccion_alimentaria_id
                             AND e.grupo_nutricional_id = cd.grupo_nutricional_id
               WHERE cd.tipo = 'Requerido') THEN
         RAISE EXCEPTION 'RS-11: una dieta exige cubrir un grupo que una de sus restricciones excluye';
    END IF;
    RETURN NULL;
END $$;
CREATE TRIGGER trg_r9_04_cubre     AFTER INSERT OR UPDATE ON cubre_dieta FOR EACH STATEMENT EXECUTE FUNCTION fn_r9_04();
CREATE TRIGGER trg_r9_04_restringe AFTER INSERT OR UPDATE ON restringe   FOR EACH STATEMENT EXECUTE FUNCTION fn_r9_04();
CREATE TRIGGER trg_r9_04_excluye   AFTER INSERT OR UPDATE ON excluye     FOR EACH STATEMENT EXECUTE FUNCTION fn_r9_04();