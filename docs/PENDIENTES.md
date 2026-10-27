# Pendientes y decisiones

## Reglas que requieren confirmación

- Emprendedores: aclarado el 26/10/2026. CSF afín o invitación de expositor o notas válidas suficientes → comprador; sólo notas bajas + evidencia adicional afín → sugerencia de emprendedor con revisión humana. Sin evidencia ni actividad no se registra. No fijar otro mínimo. Pendiente precisar los casos sin notas ni CSF/invitación, pero con evidencia digital: quedan en revisión manual.
- Comprador previamente registrado: responder con su número sólo después de verificar estado, identidad y número. El flujo específico todavía no está habilitado y no debe inventar números.
- Internacionales: cuáles documentos son obligatorios y cuáles alternativos; moneda/conversión si aplica. No inferir conjunción.
- Fechas exactas de Expo Joya Abril 2027.
- Horarios y costo del evento; contenido autorizado para visa, expositores, vendedores/distribuidores, casa de empeño y acceso no autorizado. Catálogo preparado, respuestas no redactadas ni habilitadas.
- Texto final de todas las plantillas y traducciones. La demo incluye sólo una base revisable.
- Política de retención y eliminación del historial local; tratamiento de identificadores Outlook.
- Quién confirma nombres, cantidades, folios y cambios de perfil; toda confirmación operativa sigue siendo humana.

## Integraciones

- Outlook: escritorio identificado como cuenta IMAP soporte@camaradejoyeria.com.mx en Bandeja de entrada; lector integrado devuelve cero pre-registros. Pendiente probar borrador con mensaje ficticio y confirmar firma, hilo, categoría, estado de leído, carpeta Enviados y conciliación. Para laptop siguen pendientes bitness, perfil/PST POP, cuenta contacto@expojoya.com.mx y carpetas reales (ver INTEGRACION_OUTLOOK.md).
- Registros: tecnología y propietario, entorno de pruebas, acceso de sólo lectura, claves de búsqueda, significado de estados, duplicados/homonimia, permisos, cifrado, tiempos de respuesta y esquema. No asumir tablas o columnas.
- Carpetas/categorías reales y momento exacto para marcar contestado/mover. No inferir que borrador equivale a respuesta enviada.

## Decisiones técnicas de fase 1

- Windows Forms / .NET 8 conforme a solicitud; revisión de soporte antes de producción. .NET 8 termina soporte el 10/11/2026 según Microsoft.
- Cierre a las 12:00 exactas por «antes de las 12:00»; seis meses calendario, incluidos en el borde, sin aceptar fechas futuras. Documentar y probar.
- Textos comunes en políticas, fechas en evento; condiciones Scriban en plantillas externas.
- Todas las vistas previas necesitan revisión explícita para el borrador simulado. Los indicadores de autoenvío no tienen capacidad de ejecución.
- Las plantillas de aprobación/rechazo/folio están sólo en catálogo pendiente. No se inventan folios ni se confirma registro por elegir un estado.
- La confirmación de acompañantes comprueba cupo y nombres e indica su carácter simulado en la UI.
- SDK local en .tools por ausencia de instalación detectada. No instalar herramientas globales ni realizar commits.

## Entrega y pruebas

Registrar resultados reales en VALIDACION.md. Pruebas en laptop real y validación visual humana son necesarias antes de producción. El ejecutable de esta fase es una demostración; no un reemplazo del proceso oficial.
