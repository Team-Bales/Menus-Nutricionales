-- R5-16: no existen alimentos duplicados. El criterio de identidad es el nombre normalizado, sin distinguir
-- mayúsculas ni espacios sobrantes (submodelo #5). La regla se extiende a los platos por decisión del equipo,
-- y con la generalización ItemAlimentario alimentos y platos comparten un solo espacio de nombres: un plato es
-- un alimento compuesto, así que no puede llamarse igual que un alimento. Por eso hay un único índice.
-- Va como SQL porque EF no declara índices sobre expresiones.
CREATE UNIQUE INDEX ux_items_alimentarios_r5_16_nombre ON items_alimentarios (lower(btrim(nombre)));
