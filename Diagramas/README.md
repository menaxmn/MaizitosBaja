# Diagramas

El modelo (11 entidades, 14 relaciones) está en cinco notaciones. Los PNG/PDF se generaron desde la base real con Graphviz.

| Archivo | Notación |
|---|---|
| `01_DER_CrowsFoot` | Crow's Foot (principal) |
| `02_DER_Classic` | Classic |
| `03_DER_ConnectToColumns` | Connect to Columns |
| `04_DER_UML` | UML |
| `05_DER_IDEF1X` | IDEF1X |

- `fuentes/` contiene los `.dot` (editables), `meta.json` (metadatos del esquema) y `gen_diagramas.py` (`python3 gen_diagramas.py salida/` regenera todo).
- `MaizitosBaja.mwb` es el modelo **anterior** de Workbench y está **desactualizado** respecto a `Sql/01_esquema.sql`. Para un `.mwb` editable: *File → Import → Reverse Engineer MySQL Create Script* con `Sql/01_esquema.sql`, luego *Model → Relationship Notation* para cambiar de notación y guardar el modelo.
