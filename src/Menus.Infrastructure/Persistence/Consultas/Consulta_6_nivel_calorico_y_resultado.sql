-- Consulta 6 (informe §2.2): correlación entre el nivel calórico y el resultado nutricional. Tiene tres partes
-- independientes, una función cada una.

-- Parte A: correlación de rangos de Spearman por dieta.
-- - Cada menú con asignaciones valoradas aporta una observación:
--   - x: la media de los rangos de nivel calórico de sus ítems (Bajo = 1, Medio = 2, Alto = 3);
--   - y: el promedio del valor del resultado nutricional de sus pacientes.
-- - De cada paciente se toma solo su valoración más reciente de esa asignación dentro del período [p_desde,
--   p_hasta), para que quien tiene varias valoraciones no pese más.
-- - Spearman es la correlación de Pearson entre los rangos, con rango promedio en los empates; Pearson la calcula
--   corr(). El coeficiente es NULL (no calculable) si la dieta tiene menos de dos menús valorados o si alguna de
--   las dos variables es constante.
-- - La media de rangos es un indicador ordinal del menú, no una medida de calorías, y la asociación no prueba
--   causalidad.
-- Trayectoria (informe §5.5): Dieta ← Menú → Incluye → ItemAlimentario → NivelCalórico; Menú ← Asignación ←
-- Valoración → ResultadoNutricional.Valor.
CREATE OR REPLACE FUNCTION consulta_6a_correlacion_por_dieta(p_desde timestamptz DEFAULT NULL, p_hasta timestamptz DEFAULT NULL)
RETURNS TABLE (
    dieta_id uuid,
    dieta text,
    menus_valorados bigint,
    coeficiente_spearman numeric)
LANGUAGE sql STABLE AS $$
    WITH ultima_valoracion AS (
        SELECT DISTINCT ON (v.menu_id, v.paciente_id) v.menu_id, v.paciente_id, r.valor
        FROM valoraciones_nutricionales v
        JOIN resultados_nutricionales r ON r.id = v.resultado_nutricional_id
        WHERE (p_desde IS NULL OR v.fecha_valoracion >= p_desde)
          AND (p_hasta IS NULL OR v.fecha_valoracion < p_hasta)
        ORDER BY v.menu_id, v.paciente_id, v.fecha_valoracion DESC
    ),
    resultado_por_menu AS (
        SELECT menu_id, avg(valor) AS y
        FROM ultima_valoracion
        GROUP BY menu_id
    ),
    nivel_por_menu AS (
        SELECT mi.menu_id, avg(CASE n.nombre WHEN 'Bajo' THEN 1 WHEN 'Medio' THEN 2 WHEN 'Alto' THEN 3 END) AS x
        FROM menu_item mi
        JOIN items_alimentarios i ON i.id = mi.item_id
        JOIN niveles_caloricos n ON n.id = i.nivel_calorico_id
        GROUP BY mi.menu_id
    ),
    observaciones AS (
        SELECT m.dieta_id, m.id AS menu_id, nm.x, rm.y
        FROM menus m
        JOIN nivel_por_menu nm ON nm.menu_id = m.id
        JOIN resultado_por_menu rm ON rm.menu_id = m.id
    ),
    rangos AS (
        SELECT o.dieta_id, o.menu_id,
               rank() OVER (PARTITION BY o.dieta_id ORDER BY o.x) + (count(*) OVER (PARTITION BY o.dieta_id, o.x) - 1) / 2.0 AS rx,
               rank() OVER (PARTITION BY o.dieta_id ORDER BY o.y) + (count(*) OVER (PARTITION BY o.dieta_id, o.y) - 1) / 2.0 AS ry
        FROM observaciones o
    )
    SELECT d.id, d.nombre::text, count(r.menu_id),
           round(corr(r.ry::double precision, r.rx::double precision)::numeric, 4)
    FROM dietas d
    LEFT JOIN rangos r ON r.dieta_id = d.id
    GROUP BY d.id, d.nombre
    ORDER BY d.nombre, d.id;
$$;

-- Parte B: los ítems con la tasa de rechazo más alta, sobre todos los consumos registrados.
-- - Entran alimentos y platos, porque el consumo registra los dos como ítems y puede reflejar el rechazo de un
--   plato completo.
-- - Solo entran los ítems con al menos un rechazo y al menos p_minimo_consumos consumos. Con el mínimo en 1, un
--   ítem consumido una sola vez y rechazado tiene tasa 1.
-- - El nutricionista es el que describió el ítem en el banco, no el creador del menú.
-- - Las dietas son las de los menús en que aparece algún consumo rechazado del ítem. Un ítem puede estar en menús
--   de varias dietas, por eso se listan todas.
-- - El empate en la tasa se resuelve por más rechazos y después por nombre.
-- Trayectoria (informe §5.5): Consume → ItemAlimentario → Nutricionista descriptor; Consume → Servicio →
-- Asignación → Menú → Dieta.
CREATE OR REPLACE FUNCTION consulta_6b_items_mas_rechazados(p_limite integer DEFAULT 10, p_minimo_consumos integer DEFAULT 1)
RETURNS TABLE (
    item_id uuid,
    item text,
    clase text,
    descriptor text,
    consumos bigint,
    rechazos bigint,
    tasa_rechazo numeric,
    dietas text)
LANGUAGE sql STABLE AS $$
    SELECT i.id, i.nombre::text, i.clase::text, nd.nombre::text,
           count(*),
           count(*) FILTER (WHERE NOT c.aceptacion),
           round(count(*) FILTER (WHERE NOT c.aceptacion)::numeric / count(*), 4),
           string_agg(DISTINCT d.nombre::text, ', ' ORDER BY d.nombre::text) FILTER (WHERE NOT c.aceptacion)
    FROM consumo c
    JOIN items_alimentarios i ON i.id = c.item_id
    JOIN nutricionistas nd ON nd.id = i.nutricionista_id
    JOIN menus m ON m.id = c.menu_id
    JOIN dietas d ON d.id = m.dieta_id
    GROUP BY i.id, i.nombre, i.clase, nd.nombre
    HAVING count(*) FILTER (WHERE NOT c.aceptacion) > 0 AND count(*) >= p_minimo_consumos
    ORDER BY 7 DESC, 6 DESC, i.nombre
    LIMIT p_limite;
$$;

-- Parte C: los pacientes con una revalorización ya atendida frente al promedio de su sala.
-- - Por cada revalorización que ya generó su valoración, compara el valor de esa valoración con el promedio de las
--   valoraciones de los demás pacientes de la misma sala sobre menús de la misma dieta.
-- - El paciente es el de la asignación que evalúa la valoración revisada; la dieta es la del menú de esa
--   asignación. La sala es la actual del paciente, porque el traslado no deja historial (informe §4.5).
-- - Las revalorizaciones pendientes no entran. Si nadie más de la sala tiene valoraciones de esa dieta, el
--   promedio y la diferencia son NULL.
-- Trayectoria (informe §5.5): Revalorización → Genera → Valoración generada → ResultadoNutricional.Valor;
-- Revalorización → Revalora → Valoración revisada → Asignación → Paciente → Sala, y Asignación → Menú → Dieta.
CREATE OR REPLACE FUNCTION consulta_6c_revalorizados_frente_a_su_sala()
RETURNS TABLE (
    revalorizacion_id uuid,
    paciente_id uuid,
    paciente text,
    sala text,
    dieta text,
    valor_revalorizacion integer,
    promedio_sala numeric,
    diferencia numeric)
LANGUAGE sql STABLE AS $$
    WITH revalorizados AS (
        SELECT r.id, vr.paciente_id, p.nombre AS paciente, p.sala_id, m.dieta_id, rg.valor
        FROM revalorizaciones r
        JOIN valoraciones_nutricionales vr ON vr.id = r.valoracion_revisada_id
        JOIN valoraciones_nutricionales vg ON vg.id = r.valoracion_generada_id
        JOIN resultados_nutricionales rg ON rg.id = vg.resultado_nutricional_id
        JOIN pacientes p ON p.id = vr.paciente_id
        JOIN menus m ON m.id = vr.menu_id
    ),
    con_promedio AS (
        SELECT rv.*,
               (SELECT avg(r2.valor)
                FROM valoraciones_nutricionales v2
                JOIN resultados_nutricionales r2 ON r2.id = v2.resultado_nutricional_id
                JOIN pacientes p2 ON p2.id = v2.paciente_id
                JOIN menus m2 ON m2.id = v2.menu_id
                WHERE p2.sala_id = rv.sala_id AND m2.dieta_id = rv.dieta_id AND v2.paciente_id <> rv.paciente_id) AS promedio
        FROM revalorizados rv
    )
    SELECT cp.id, cp.paciente_id, cp.paciente::text, s.nombre::text, d.nombre::text, cp.valor,
           round(cp.promedio, 2), round(cp.valor - cp.promedio, 2)
    FROM con_promedio cp
    JOIN salas s ON s.id = cp.sala_id
    JOIN dietas d ON d.id = cp.dieta_id
    ORDER BY s.nombre, d.nombre, cp.paciente;
$$;
