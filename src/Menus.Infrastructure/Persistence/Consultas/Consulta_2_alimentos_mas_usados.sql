-- Consulta 2 (informe §2.2): alimentos más utilizados en los menús finales de una dieta, con su nivel calórico y
-- su grupo nutricional.
-- - Menú final: su única validación está aprobada (RC-12).
-- - Se cuenta la presencia, no la cantidad servida: cuántos menús finales distintos contienen cada alimento.
-- - Un alimento llega al menú directamente (Incluye) o como ingrediente de un plato incluido (Incluye + Compone).
--   El UNION, a diferencia de UNION ALL, lo cuenta una sola vez por menú aunque llegue por las dos vías.
-- - p_limite acota el resultado a los N primeros (top-N); NULL devuelve todos.
-- Trayectoria (informe §5.5): Dieta ← Menú (aprobado) → Incluye → ItemAlimentario; un Plato → Compone → Alimento;
-- cada alimento → NivelCalórico y GrupoNutricional.
CREATE OR REPLACE FUNCTION consulta_2_alimentos_mas_usados(p_dieta_id uuid, p_limite integer DEFAULT NULL)
RETURNS TABLE (
    alimento_id uuid,
    alimento text,
    nivel_calorico text,
    grupo_nutricional text,
    menus_finales bigint)
LANGUAGE sql STABLE AS $$
    WITH finales AS (
        SELECT m.id
        FROM menus m
        JOIN validacion v ON v.menu_id = m.id AND v.aprobado
        WHERE m.dieta_id = p_dieta_id
    ),
    alimentos_por_menu AS (
        SELECT f.id AS menu_id, i.id AS alimento_id
        FROM finales f
        JOIN menu_item mi ON mi.menu_id = f.id
        JOIN items_alimentarios i ON i.id = mi.item_id AND i.clase = 'Alimento'
        UNION
        SELECT f.id, c.alimento_id
        FROM finales f
        JOIN menu_item mi ON mi.menu_id = f.id
        JOIN composicion c ON c.plato_id = mi.item_id
    )
    SELECT a.id, a.nombre::text, n.nombre::text, g.nombre::text, count(*)
    FROM alimentos_por_menu x
    JOIN items_alimentarios a ON a.id = x.alimento_id
    JOIN niveles_caloricos n ON n.id = a.nivel_calorico_id
    JOIN grupos_nutricionales g ON g.id = a.grupo_nutricional_id
    GROUP BY a.id, a.nombre, n.nombre, g.nombre
    ORDER BY count(*) DESC, a.nombre
    LIMIT p_limite;
$$;
