-- RK-07: toda dieta tiene al menos una cobertura con Tipo = Requerido (submodelo #6). Cardinalidad mínima,
-- comprobada al confirmar la transacción. El #11, §7, indica que sigue el mismo patrón que RK-03.
CREATE FUNCTION fn_r6_21() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE d uuid;
BEGIN
    IF TG_TABLE_NAME = 'dietas' THEN d := NEW.id; ELSE d := OLD.dieta_id; END IF;
    IF EXISTS (SELECT 1 FROM dietas WHERE id = d)
       AND NOT EXISTS (SELECT 1 FROM cubre_dieta WHERE dieta_id = d AND tipo = 'Requerido') THEN
         RAISE EXCEPTION 'RK-07: la dieta % no exige cubrir ningún grupo nutricional', d;
    END IF;
    RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER trg_r6_21_dieta AFTER INSERT ON dietas
    DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION fn_r6_21();
CREATE CONSTRAINT TRIGGER trg_r6_21_cubre AFTER DELETE OR UPDATE OF dieta_id, tipo ON cubre_dieta
    DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION fn_r6_21();