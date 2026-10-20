-- R5-15: todo plato se compone de al menos un alimento. Cardinalidad mínima, comprobada al confirmar
-- la transacción, porque el plato se inserta antes que su composición. Adaptado del #11, §7.
-- Los platos son los ítems de clase 'Plato' (generalización ItemAlimentario, una sola tabla).
CREATE FUNCTION fn_r5_15() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE p uuid;
BEGIN
    -- Un IF y no un CASE: la columna se llama distinto en cada tabla, y PostgreSQL valida el CASE entero
    IF TG_TABLE_NAME = 'items_alimentarios' THEN p := NEW.id; ELSE p := OLD.plato_id; END IF;
    IF EXISTS (SELECT 1 FROM items_alimentarios WHERE id = p AND clase = 'Plato')
       AND NOT EXISTS (SELECT 1 FROM composicion WHERE plato_id = p) THEN
        RAISE EXCEPTION 'R5-15: el plato % no tiene ningún alimento', p;
    END IF;
    RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER trg_r5_15_plato AFTER INSERT ON items_alimentarios
    DEFERRABLE INITIALLY DEFERRED FOR EACH ROW WHEN (NEW.clase = 'Plato') EXECUTE FUNCTION fn_r5_15();
CREATE CONSTRAINT TRIGGER trg_r5_15_composicion AFTER DELETE OR UPDATE OF plato_id ON composicion
    DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION fn_r5_15();
