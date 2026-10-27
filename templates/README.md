# Plantillas externas

Editar archivos UTF-8 .sbn y metadatos en catalogo.json con la aplicación cerrada. Sintaxis: `{{ nombre }}` y `{{ if condicion }}...{{ else }}...{{ end }}`. Motor Scriban con variables estrictas. No introducir fechas, sedes, montos, requisitos ni enlaces en cada archivo: utilizar variables del generador y textos comunes de politicas.json.

El catálogo conserva los 27 casos solicitados y añade dos variantes inglesas. `disponible: false` significa pendiente de texto y validación; no es una respuesta generable. La plantilla «acompanante-encontrado» incluye las acciones continuar/cambiar/corregir/revisar mediante condiciones. «confirmacion-acompanantes» cubre uno o dos acompañantes con cálculo de cupo; los IDs futuros específicos se mantienen como catálogo pendiente.

Variables comunes: nombre, titular, folio (nunca inventado), observaciones, respuesta_manual, evento, inicio, fin, recinto, ciudad, estado, correo, registro_url, fecha_limite, hora_limite, siguiente_evento, siguiente_recinto, siguiente_ciudad, siguientes_fechas, requisitos_comprador, requisitos_emprendedor, texto_documentacion, acompanantes_reglas, total_acompanantes, nombres_acompanantes, texto_cupo y accion.

Los datos del usuario se insertan como texto, sin volver a evaluarlos como plantillas; todo el resultado se escapa para HTML. Las plantillas son archivos locales de confianza: revisarlas antes de instalar una edición. No descargar ni ejecutar plantillas desde correos.

`elegibleAutoenvioFuturo` sólo documenta potencial; no habilita envío. Cambiar metadatos no habilita operaciones reales. Las plantillas de confirmación no deben utilizarse con datos no verificados.
