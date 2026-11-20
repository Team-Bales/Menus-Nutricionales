# Plantilla Oficial del Informe Técnico

Esta carpeta contiene la plantilla oficial de entrega del informe técnico en formato LaTeX, lista para rellenar con los datos del proyecto.

## Archivos

- `plantilla-informe-tecnico.tex` — Documento LaTeX principal (clase `article`, A4, español con `babel`, `hyperref` con enlaces en azul, `booktabs` para tablas, `listings` para bloques de código).
- `plantilla-informe-tecnico.pdf` — PDF compilado de referencia (24 páginas).
- `PlanillaInformeTecnicoIS.pdf` — Plantilla original en PDF proporcionada por la escuela.

## Cómo compilar

```bash
pdflatex plantilla-informe-tecnico.tex
pdflatex plantilla-informe-tecnico.tex   # Segunda pasada para el índice
```

O usar `latexmk -pdf plantilla-informe-tecnico.tex` para compilar automáticamente las pasadas necesarias.

Requisitos: TeX Live con `pdflatex` y paquetes estándar (`babel-spanish`, `booktabs`, `hyperref`, `listings`, `xcolor`, `fancyhdr`, `lastpage`, `geometry`, `titlesec`, `caption`, `enumitem`, `float`, `longtable`, `array`). No requiere `lualatex` ni `xelatex`.

## Estructura del documento

La plantilla reproduce fielmente la planilla oficial incluyendo:

- Portada con campos: Proyecto, Organización, Fecha, Clasificación
- Índice automático
- Control de Versiones (Cuadro 1)
- 15 secciones numeradas (Resumen Ejecutivo, Alcance, Requisitos, Contexto, Arquitectura, Tecnologías, Código Fuente, Configuración, Modelo de Datos, API, Pruebas, Rendimiento, Seguridad, Riesgos, Limitaciones)
- 3 Apéndices (Manual de Instalación, Glosario - Cuadro 19, Referencias)
- 19 cuadros/tablas con contenido guía y placeholders entre corchetes