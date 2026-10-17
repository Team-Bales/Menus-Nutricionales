-- Deshace lo que crean los demás scripts de esta carpeta. DROP FUNCTION ... CASCADE también borra los
-- triggers que usan cada función.
DROP INDEX IF EXISTS ux_alimentos_r5_16_nombre;
DROP INDEX IF EXISTS ux_platos_r5_16_nombre;
DROP FUNCTION IF EXISTS fn_r5_15() CASCADE;
DROP FUNCTION IF EXISTS fn_r6_21() CASCADE;
DROP FUNCTION IF EXISTS fn_r6_22_creador() CASCADE;
DROP FUNCTION IF EXISTS fn_validacion() CASCADE;
DROP FUNCTION IF EXISTS fn_r6_23() CASCADE;
DROP FUNCTION IF EXISTS fn_r7_22() CASCADE;
DROP FUNCTION IF EXISTS fn_r7_27_menu() CASCADE;
DROP FUNCTION IF EXISTS fn_r7_27_parametros_actualizacion() CASCADE;
DROP FUNCTION IF EXISTS fn_r7_27_parametros_borrado() CASCADE;
DROP FUNCTION IF EXISTS fn_revalorizacion() CASCADE;
DROP FUNCTION IF EXISTS fn_r9_04() CASCADE;
DROP FUNCTION IF EXISTS fn_r11_01() CASCADE;
