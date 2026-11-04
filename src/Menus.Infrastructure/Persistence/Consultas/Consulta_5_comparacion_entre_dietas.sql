-- Consulta 5 (informe §2.2): comparación de los menús de dos o más dietas, en tres dimensiones. Cada una es una
-- función, porque devuelven tablas de forma distinta.
-- - En las tres, un plato incluido cuenta como un ítem con su propia clasificación, sin descomponerse.
-- - Por defecto se comparan todos los menús de cada dieta («los menús generados»); con p_solo_finales, solo los
--   aprobados.
-- - Ningún criterio es una restricción de la base (informe §4.5): por eso se verifica aquí si se cumplió.
-- Trayectoria (informe §5.5): Dieta ← Menú → Incluye → ItemAlimentario → GrupoNutricional, NivelCalórico;
-- requeridos y restringidos por CubreDieta.Tipo; excluidos por Restringe → Excluye; parámetros por Distribuye y
-- CantidadTotalAlimentos.

-- (1) Distribución porcentual de los ítems por grupo nutricional, sobre todos los ítems incluidos en los menús de
-- cada dieta (un ítem que está en dos menús cuenta dos veces).
CREATE OR REPLACE FUNCTION consulta_5_distribucion_por_grupo(p_dietas uuid[], p_solo_finales boolean DEFAULT false)
RETURNS TABLE (
    dieta_id uuid,
    dieta text,
    grupo_nutricional text,
    items bigint,
    porcentaje numeric)
LANGUAGE sql STABLE AS $$
    SELECT d.id, d.nombre::text, g.nombre::text, count(*),
           round(100.0 * count(*) / sum(count(*)) OVER (PARTITION BY d.id), 2)
    FROM menus m
    JOIN dietas d ON d.id = m.dieta_id
    JOIN menu_item mi ON mi.menu_id = m.id
    JOIN items_alimentarios i ON i.id = mi.item_id
    JOIN grupos_nutricionales g ON g.id = i.grupo_nutricional_id
    WHERE m.dieta_id = ANY (p_dietas)
      AND (NOT p_solo_finales OR EXISTS (SELECT 1 FROM validacion v WHERE v.menu_id = m.id AND v.aprobado))
    GROUP BY d.id, d.nombre, g.nombre
    ORDER BY d.nombre, d.id, count(*) DESC, g.nombre;
$$;

-- (2) Distribución porcentual de los ítems por nivel calórico, con la misma base.
CREATE OR REPLACE FUNCTION consulta_5_distribucion_por_nivel(p_dietas uuid[], p_solo_finales boolean DEFAULT false)
RETURNS TABLE (
    dieta_id uuid,
    dieta text,
    nivel_calorico text,
    items bigint,
    porcentaje numeric)
LANGUAGE sql STABLE AS $$
    SELECT d.id, d.nombre::text, n.nombre::text, count(*),
           round(100.0 * count(*) / sum(count(*)) OVER (PARTITION BY d.id), 2)
    FROM menus m
    JOIN dietas d ON d.id = m.dieta_id
    JOIN menu_item mi ON mi.menu_id = m.id
    JOIN items_alimentarios i ON i.id = mi.item_id
    JOIN niveles_caloricos n ON n.id = i.nivel_calorico_id
    WHERE m.dieta_id = ANY (p_dietas)
      AND (NOT p_solo_finales OR EXISTS (SELECT 1 FROM validacion v WHERE v.menu_id = m.id AND v.aprobado))
    GROUP BY d.id, d.nombre, n.nombre
    ORDER BY d.nombre, d.id, CASE n.nombre WHEN 'Bajo' THEN 1 WHEN 'Medio' THEN 2 WHEN 'Alto' THEN 3 END;
$$;

-- (3) Criterios de equilibrio, una fila por menú, en dos niveles:
-- - De la dieta: están todos los grupos que exige cubrir (CubreDieta Requerido) y no hay ninguno prohibido. Los
--   prohibidos son la unión de los que la cobertura marca Restringido y los que excluyen las restricciones de la
--   dieta (Restringe + Excluye; decisión D9-1).
-- - De los parámetros:
--   - la cantidad de ítems incluidos es la declarada (CantidadTotalAlimentos);
--   - cada tipo de preparación tiene la proporción declarada (Distribuye), contando un tipo no declarado como
--     proporción 0. Con n ítems, la proporción real de un tipo es k/n, y no siempre puede ser exactamente la
--     declarada p. Se considera cumplida si k es uno de los enteros más cercanos a p·n, es decir, si
--     |k/n − p| ≤ 1/(2n). Un menú sin ítems no la cumple.
CREATE OR REPLACE FUNCTION consulta_5_criterios_de_equilibrio(p_dietas uuid[], p_solo_finales boolean DEFAULT false)
RETURNS TABLE (
    dieta_id uuid,
    dieta text,
    menu_id uuid,
    estado text,
    items bigint,
    cantidad_declarada integer,
    cantidad_cumple boolean,
    proporciones_cumplen boolean,
    tipos_fuera_de_proporcion text,
    requeridos_cubiertos boolean,
    requeridos_faltantes text,
    prohibidos_ausentes boolean,
    prohibidos_presentes text)
LANGUAGE sql STABLE AS $$
    WITH menus_elegidos AS (
        SELECT m.id, m.dieta_id, m.cantidad_total_alimentos,
               CASE WHEN v.menu_id IS NULL THEN 'Pendiente' WHEN v.aprobado THEN 'Final' ELSE 'Rechazado' END AS estado,
               (SELECT count(*) FROM menu_item mi WHERE mi.menu_id = m.id) AS n
        FROM menus m
        LEFT JOIN validacion v ON v.menu_id = m.id
        WHERE m.dieta_id = ANY (p_dietas) AND (NOT p_solo_finales OR v.aprobado)
    ),
    grupos_del_menu AS (
        SELECT DISTINCT mi.menu_id, i.grupo_nutricional_id
        FROM menu_item mi
        JOIN items_alimentarios i ON i.id = mi.item_id
    ),
    prohibidos AS (
        SELECT cd.dieta_id, cd.grupo_nutricional_id
        FROM cubre_dieta cd
        WHERE cd.tipo = 'Restringido'
        UNION
        SELECT r.dieta_id, e.grupo_nutricional_id
        FROM restringe r
        JOIN excluye e ON e.restriccion_alimentaria_id = r.restriccion_alimentaria_id
    ),
    tipos AS (
        -- Por menú y tipo de preparación: la proporción declarada (0 si no se declaró) y los ítems de ese tipo.
        SELECT me.id AS menu_id, t.id AS tipo_id, t.nombre AS tipo,
               coalesce(md.proporcion, 0) AS declarada,
               (SELECT count(*) FROM menu_item mi
                JOIN items_alimentarios i ON i.id = mi.item_id
                WHERE mi.menu_id = me.id AND i.tipo_preparacion_id = t.id) AS k,
               me.n
        FROM menus_elegidos me
        CROSS JOIN tipos_preparacion t
        LEFT JOIN menu_distribucion md ON md.menu_id = me.id AND md.tipo_preparacion_id = t.id
    ),
    tipos_evaluados AS (
        SELECT menu_id, tipo,
               n > 0 AND abs(k::numeric / nullif(n, 0) - declarada) <= 1.0 / (2 * nullif(n, 0)) AS cumple
        FROM tipos
        WHERE declarada > 0 OR k > 0
    )
    SELECT me.dieta_id, d.nombre::text, me.id, me.estado, me.n, me.cantidad_total_alimentos,
           me.n = me.cantidad_total_alimentos,
           coalesce(bool_and(te.cumple), false) AND me.n > 0,
           (SELECT string_agg(te2.tipo, ', ' ORDER BY te2.tipo) FROM tipos_evaluados te2
            WHERE te2.menu_id = me.id AND NOT coalesce(te2.cumple, false)),
           NOT EXISTS (SELECT 1 FROM cubre_dieta cd
                       WHERE cd.dieta_id = me.dieta_id AND cd.tipo = 'Requerido'
                         AND NOT EXISTS (SELECT 1 FROM grupos_del_menu gm
                                         WHERE gm.menu_id = me.id AND gm.grupo_nutricional_id = cd.grupo_nutricional_id)),
           (SELECT string_agg(g.nombre, ', ' ORDER BY g.nombre)
            FROM cubre_dieta cd JOIN grupos_nutricionales g ON g.id = cd.grupo_nutricional_id
            WHERE cd.dieta_id = me.dieta_id AND cd.tipo = 'Requerido'
              AND NOT EXISTS (SELECT 1 FROM grupos_del_menu gm
                              WHERE gm.menu_id = me.id AND gm.grupo_nutricional_id = cd.grupo_nutricional_id)),
           NOT EXISTS (SELECT 1 FROM prohibidos p JOIN grupos_del_menu gm ON gm.grupo_nutricional_id = p.grupo_nutricional_id
                       WHERE p.dieta_id = me.dieta_id AND gm.menu_id = me.id),
           (SELECT string_agg(g.nombre, ', ' ORDER BY g.nombre)
            FROM prohibidos p
            JOIN grupos_del_menu gm ON gm.grupo_nutricional_id = p.grupo_nutricional_id
            JOIN grupos_nutricionales g ON g.id = p.grupo_nutricional_id
            WHERE p.dieta_id = me.dieta_id AND gm.menu_id = me.id)
    FROM menus_elegidos me
    JOIN dietas d ON d.id = me.dieta_id
    LEFT JOIN tipos_evaluados te ON te.menu_id = me.id
    GROUP BY me.dieta_id, d.nombre, me.id, me.estado, me.n, me.cantidad_total_alimentos
    ORDER BY d.nombre, me.dieta_id, me.id;
$$;
