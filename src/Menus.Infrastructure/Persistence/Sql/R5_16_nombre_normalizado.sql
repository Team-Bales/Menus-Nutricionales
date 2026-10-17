-- R5-16: no existen alimentos duplicados. El criterio de identidad es el nombre normalizado, sin distinguir
-- mayúsculas ni espacios sobrantes (submodelo #5). La regla se extiende a los platos por decisión del equipo.
-- Va como SQL porque EF no declara índices sobre expresiones.
CREATE UNIQUE INDEX ux_alimentos_r5_16_nombre ON alimentos (lower(btrim(nombre)));
CREATE UNIQUE INDEX ux_platos_r5_16_nombre ON platos (lower(btrim(nombre)));
