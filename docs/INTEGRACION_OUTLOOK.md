# Integración con Outlook clásico

Fase actual: la pestaña «Pre-registros» puede usar Outlook clásico por COM con cuenta y carpeta seleccionadas; las pestañas individuales siguen simuladas. No se leen archivos OST/PST directamente ni se solicitan credenciales. Se leen asuntos coincidentes, se prepara Reply y se guarda el borrador revisado. No existe llamada Send, movimiento ni modificación deliberada del original.

En escritorio se identificó soporte@camaradejoyeria.com.mx, tipo IMAP según Outlook, con Bandeja de entrada. La lectura integrada confirmó cero pre-registros coincidentes al comprobar el perfil. Falta probar con un mensaje ficticio recibido y observar en Outlook el borrador, cuenta de envío, hilo, firma, estado de leído y conciliación tras el envío manual. No se supone que esta configuración equivalga a la laptop. La entrega portable y requisitos están en INICIO_RAPIDO.md.

Destino: Outlook clásico Office Professional Plus 2019, POP/SMTP, almacenamiento local PST, cuenta contacto@expojoya.com.mx. Conservar configuración y flujo de recepción existentes. No migrar cuenta ni conectar Microsoft Graph como sustituto del PST.

Antes de usarla en la laptop, verificar con la persona responsable:

- Arquitectura Office (32/64 bits), versión y compatibilidad con el sistema destino.
- Nombre y ruta/configuración del perfil y PST que contiene los mensajes.
- Carpetas reales autorizadas y estructura posterior a contestar.
- Cuenta asociada a cada almacén; reglas de verificación de pertenencia para PST/POP. No basta comprobar el texto visible del destinatario.
- Firma, codificación, HTML, adjuntos, hilo y asunto de respuesta.
- Envío/recepción, advertencias de seguridad y permisos de automatización.
- Ciclo de vida COM en hilo STA, liberación de referencias, Outlook abierto/cerrado y errores de perfil.

La bandeja actual lee mensajes por asunto, detecta presencia de adjuntos sin abrirlos, crea Reply para las respuestas generales confirmadas, inserta HTML y guarda un borrador. El envío sigue siendo manual desde Outlook. Falta verificar con un mensaje ficticio que Outlook conserva firma, hilo y estado de lectura en este equipo y repetirlo en la laptop.

La lista de cuentas y carpetas reales se obtiene del perfil abierto. ICarpetasService y carpetas.example.json se mantienen para simulación. Categorías y movimientos se diseñarán cuando exista el catálogo real y autorización; no se habilitaron.

La bitness para publicación se resolverá con una prueba en laptop; no afirmar compatibilidad basándose sólo en que Windows sea x64. Preparar una copia de prueba y respaldo del PST con el usuario antes de cualquier validación real.
