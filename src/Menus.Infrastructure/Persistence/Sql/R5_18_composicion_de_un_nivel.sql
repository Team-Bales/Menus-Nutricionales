-- R5-18: la composición tiene un solo nivel. En Compone, el compuesto es un ítem de clase 'Plato' y el
-- componente, uno de clase 'Alimento': un plato no se compone de otros platos. Regla nueva de la
-- generalización ItemAlimentario (feedback del profesor, 19/10); antes la garantizaban las FK a tablas
-- separadas, y ahora las dos FK apuntan a items_alimentarios. Se agrega al #11 en la fase de documentación.
-- Si el ítem no existe, el trigger no dice nada y la FK rechaza la fila con su propio error.
CREATE FUNCTION fn_r5_18() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF EXISTS (SELECT 1 FROM items_alimentarios WHERE id = NEW.plato_id AND clase <> 'Plato') THEN
        RAISE EXCEPTION 'R5-18: el compuesto % no es un plato', NEW.plato_id;
    END IF;
    IF EXISTS (SELECT 1 FROM items_alimentarios WHERE id = NEW.alimento_id AND clase <> 'Alimento') THEN
        RAISE EXCEPTION 'R5-18: el componente % no es un alimento simple', NEW.alimento_id;
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER trg_r5_18 BEFORE INSERT OR UPDATE ON composicion
    FOR EACH ROW EXECUTE FUNCTION fn_r5_18();
