-- Consulta 4 (informe §2.2): desempeño de los pacientes ante un menú, por ítem y clasificado por nivel calórico.
-- - Una fila por ítem incluido en el menú, alimento o plato. El plato no se descompone: el consumo se registra por
--   ítem, y el rechazo de un plato completo no se puede atribuir a sus ingredientes.
-- - La tasa se pondera por servicio, no por paciente: cada fila de consumo es un ítem en un servicio. El
--   denominador son los servicios con consumo registrado de ese ítem; un ítem sin consumos tiene la tasa en NULL.
-- - El orden es por nivel calórico (Bajo, Medio, Alto) y, dentro de cada nivel, por tasa de aceptación.
-- Trayectoria (informe §5.5): Menú ← Asignación → Servicio → Consume (Aceptación) → ItemAlimentario → NivelCalórico.
CREATE OR REPLACE FUNCTION consulta_4_desempeno_ante_un_menu(p_menu_id uuid)
RETURNS TABLE (
    item_id uuid,
    item text,
    clase text,
    nivel_calorico text,
    servicios_con_consumo bigint,
    aceptaciones bigint,
    rechazos bigint,
    tasa_aceptacion numeric)
LANGUAGE sql STABLE AS $$
    SELECT i.id, i.nombre::text, i.clase::text, n.nombre::text,
           count(c.item_id),
           count(c.item_id) FILTER (WHERE c.aceptacion),
           count(c.item_id) FILTER (WHERE NOT c.aceptacion),
           round(count(c.item_id) FILTER (WHERE c.aceptacion)::numeric / nullif(count(c.item_id), 0), 4)
    FROM menu_item mi
    JOIN items_alimentarios i ON i.id = mi.item_id
    JOIN niveles_caloricos n ON n.id = i.nivel_calorico_id
    LEFT JOIN consumo c ON c.menu_id = mi.menu_id AND c.item_id = mi.item_id
    WHERE mi.menu_id = p_menu_id
    GROUP BY i.id, i.nombre, i.clase, n.nombre
    ORDER BY CASE n.nombre WHEN 'Bajo' THEN 1 WHEN 'Medio' THEN 2 WHEN 'Alto' THEN 3 END,
             8 DESC NULLS LAST, i.nombre;
$$;
