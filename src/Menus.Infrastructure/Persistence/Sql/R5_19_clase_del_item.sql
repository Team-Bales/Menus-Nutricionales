-- RS-13; la clase la fijan RK-12 y RD-17: todo ítem es de exactamente una clase, 'Alimento' o 'Plato', y no la cambia (especialización total y
-- disjunta de ItemAlimentario). Los valores los fija el CHECK de la tabla; este trigger impide el cambio, que
-- dejaría sin sentido a RS-12 (un alimento que ya es ingrediente pasaría a ser un plato) y a RK-03. Regla nueva
-- de la generalización (feedback del profesor, 19/10); se agrega al #11 en la fase de documentación.
CREATE FUNCTION fn_r5_19() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    RAISE EXCEPTION 'RS-13: el ítem % no puede cambiar de clase (de % a %)', OLD.id, OLD.clase, NEW.clase;
END $$;
CREATE TRIGGER trg_r5_19 BEFORE UPDATE OF clase ON items_alimentarios
    FOR EACH ROW WHEN (NEW.clase IS DISTINCT FROM OLD.clase) EXECUTE FUNCTION fn_r5_19();