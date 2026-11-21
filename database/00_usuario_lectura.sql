-- Usuario de solo lectura para el Worker de extraccion.
-- Solo puede consultar la vista vw_OpinionesDetalle (no puede insertar, modificar ni borrar).
-- Cambiar la clave antes de ejecutar.

CREATE USER IF NOT EXISTS 'etl_extraccion'@'localhost' IDENTIFIED BY 'CAMBIAR_CLAVE';

GRANT SELECT ON AnalisisOpinionesClientes.vw_OpinionesDetalle TO 'etl_extraccion'@'localhost';

FLUSH PRIVILEGES;
