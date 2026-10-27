# Especificación funcional

## Alcance y entorno

Atención de contacto@expojoya.com.mx con Outlook clásico 2019, versión aproximada 1808 / 10344.20008, POP/SMTP y archivos PST locales. Conservar el perfil existente; no migrar a Exchange, Microsoft 365 o IMAP. Desarrollo en otro equipo; destino Windows 10. Equipo de desarrollo con i5-6200U y 8 GB RAM: evitar servicios, contenedores y modelos locales grandes. No registrar identificadores del dispositivo o producto.

## Flujo previsto

Leer un mensaje, sugerir idioma/nombre/perfil/solicitud/etapa/plantilla, permitir corregir todo, generar respuesta personalizada y vista previa, crear borrador y revisarlo en Outlook. Clasificar estados/categorías, registrar metadatos y, posteriormente, mover mensajes únicamente con autorización. La consulta oficial de registros llegará después.

## Primera fase

Cinco capas/proyectos. Correos y registros ficticios en memoria; lectura JSON; motor de plantillas con variables y condiciones; servicio de fechas; validación documental orientativa; límites de acompañantes; vista previa y borradores simulados. SQLite registra sólo metadatos de la demostración. Sin Interop ni servicios reales. No abre adjuntos ni envía mensajes.

La pantalla inicial contiene remitente, asunto, fecha, cuerpo de texto y aviso de adjuntos; controles para idioma, perfil, solicitud, etapa y estado de registro; variables de persona, titular, folio, acompañantes y observaciones. Las acciones disponibles son análisis local sencillo, aplicar sugerencia, cambiar plantilla, vista previa y borrador simulado. Las operaciones reales y edición en Outlook no están habilitadas. La validación documental se realiza mediante datos manuales, sin OCR ni inspección de archivos.

## Catálogos

Perfiles: comprador, emprendedor, internacional, acompañante, expositor y otro. Estados: no encontrado, comprador registrado, emprendedor registrado, acompañante registrado, registro incompleto y revisión manual. Etapas: previo, durante y después. Los 27 tipos de respuesta se mantienen en templates/catalogo.json; sólo las plantillas con archivo y estado disponible aparecen en la demo.

Cada plantilla declara ID, nombre, idioma, perfil, etapas, asunto, cuerpo/archivo, variables obligatorias, revisión requerida, permiso de borrador, posible elegibilidad futura para autoenvío, consulta de registros y representación de aprobación/rechazo. La elegibilidad es informativa: no existe operación de envío.

## Seguridad y automatización

Niveles: manual, asistido, borrador automático y envío automático futuro. La demo sólo usa asistido con revisión explícita; no ejecuta trabajos en segundo plano. Aprobaciones, rechazos, folios, cambios de perfil, documentos, excepciones y actualizaciones requieren intervención humana. Internacionales conservan política pendiente; emprendedores requieren revisión humana de notas bajas y evidencia adicional afín. CSF afín, invitación válida o notas suficientes tienen prioridad para sugerir comprador. Consultar REGLAS_NEGOCIO.md.

Posibles automatizaciones futuras: fechas, horarios, sede, costo confirmado, enlace, cupo de acompañantes, recibido y fin de plazo. Los horarios y costo no proporcionados permanecen pendientes, nunca se inventan.

## Historial previsto

ID interno, ID Outlook opcional, evento, recepción, respuesta real opcional, idioma, perfil, tipo, plantilla, estado, resultado, destino opcional, intervención manual, cantidad de acompañantes y pre-registro completo. Crear un borrador NO asigna fecha de respuesta ni marca pre-registro completo. No almacenar cuerpo, nombres, email, documentos o folio por defecto.
