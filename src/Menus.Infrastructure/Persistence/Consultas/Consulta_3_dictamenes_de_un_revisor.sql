-- Consulta 3 (informe §2.2): los dictámenes que registró un revisor, con el menú evaluado, su dieta y su creador,
-- la fecha del dictamen, las observaciones y el resultado.
-- - Cada menú tiene a lo sumo un dictamen (RC-12), así que hay una fila por menú revisado.
-- - Incluye los rechazos, porque sus observaciones son parte de la auditoría clínica (ADR-007). Si el rechazo
--   originó un menú de reemplazo (Origina), lo muestra; un rechazo origina a lo sumo uno (RK-15).
-- Trayectoria (informe §5.5): Nutricionista (revisor) ← Validación → Menú → Dieta; Menú → Nutricionista (creador);
-- Validación → Origina → Menú de reemplazo.
CREATE OR REPLACE FUNCTION consulta_3_dictamenes_de_un_revisor(p_revisor_id uuid)
RETURNS TABLE (
    menu_id uuid,
    dieta text,
    creador text,
    fecha_creacion timestamptz,
    fecha_validacion timestamptz,
    aprobado boolean,
    observaciones text,
    menu_reemplazo_id uuid)
LANGUAGE sql STABLE AS $$
    SELECT m.id, d.nombre::text, c.nombre::text, m.fecha_creacion, v.fecha_validacion, v.aprobado, v.observaciones,
           r.id
    FROM validacion v
    JOIN menus m ON m.id = v.menu_id
    JOIN dietas d ON d.id = m.dieta_id
    JOIN nutricionistas c ON c.id = m.nutricionista_creador_id
    LEFT JOIN menus r ON r.validacion_origen_id = v.menu_id
    WHERE v.nutricionista_id = p_revisor_id
    ORDER BY v.fecha_validacion DESC, m.id;
$$;
