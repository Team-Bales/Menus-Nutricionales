-- Consulta 1 (informe §2.2): el historial auditable de los menús automáticos de una dieta — el nombre del
-- creador, la fecha de creación y el detalle de los parámetros de generación almacenados junto al menú
-- (CantidadTotalAlimentos, Distribuye, CubreMenú).
-- - Una fila por menú. La distribución y la cobertura son relaciones 1:N independientes del menú, así que se
--   agregan en una sola columna de texto cada una (mismo recurso que consulta_5_criterios_de_equilibrio, para no
--   cruzarlas entre sí con un JOIN que multiplicaría las filas).
-- Trayectoria (informe §5.5): Dieta ← Menú (filtrado por EsAutomático) → Crea → Nutricionista (creador) ·
-- FechaCreación · CantidadTotalAlimentos · Distribuye · CubreMenú.
CREATE OR REPLACE FUNCTION consulta_1_menus_automaticos_por_dieta(p_dieta_id uuid)
RETURNS TABLE (
    menu_id uuid,
    creador text,
    fecha_creacion timestamptz,
    cantidad_total_alimentos integer,
    distribucion text,
    grupos_cubiertos text)
LANGUAGE sql STABLE AS $$
    SELECT m.id, c.nombre::text, m.fecha_creacion, m.cantidad_total_alimentos,
           (SELECT string_agg(tp.nombre || ': ' || md.proporcion::text, ', ' ORDER BY tp.nombre)
            FROM menu_distribucion md
            JOIN tipos_preparacion tp ON tp.id = md.tipo_preparacion_id
            WHERE md.menu_id = m.id),
           (SELECT string_agg(gn.nombre, ', ' ORDER BY gn.nombre)
            FROM menu_grupo_nutricional mgn
            JOIN grupos_nutricionales gn ON gn.id = mgn.grupo_nutricional_id
            WHERE mgn.menu_id = m.id)
    FROM menus m
    JOIN nutricionistas c ON c.id = m.nutricionista_creador_id
    WHERE m.dieta_id = p_dieta_id AND m.es_automatico
    ORDER BY m.fecha_creacion DESC, m.id;
$$;
