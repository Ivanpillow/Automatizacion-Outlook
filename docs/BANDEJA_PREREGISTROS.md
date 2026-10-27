# Bandeja acumulada de pre-registros

Solicitud del usuario: reunir los mensajes cuyo asunto indica «Pre-registro Comprador» o «Pre-registro Emprendedor Expo Joya», revisar manualmente quién ya está registrado, apartar casos individuales y preparar en un solo lote las respuestas generales confirmadas.

## Estados

- Pendiente: recibido, todavía sin revisión.
- Confirmado general: la persona revisó el caso y la vista previa exacta; entra al lote de borradores.
- Revisión individual: comprador registrado, folio, excepción u otro caso que no corresponde al texto general. Sigue en el acumulado.
- Creando borrador: reserva persistida antes de llamar al proveedor de correo, para evitar duplicados.
- Borrador creado: aún pendiente de envío; no sale del acumulado.
- Error de revisión: creación incierta, borrador no localizable o contenido modificado; no reintentar automáticamente.
- Respondido: el proveedor confirmó un envío relacionado con ese mensaje. Sale de pendientes, pero permanece en historial.

No borrar mensajes ni el historial al responder. No identificar duplicados por persona o asunto: cada mensaje entrante es un caso distinto. La clave combina origen (incluido almacén/cuenta), evento e identificador del mensaje. Releer el mismo mensaje no reinicia su estado.

## Revisión y lote

La detección por asunto sólo determina el tipo de solicitud, no autoriza registro ni respuesta. Se omiten respuestas y reenvíos al no comenzar con los prefijos configurados. Se toleran mayúsculas, espacios y variantes de guion. Los prefijos, plantillas y frecuencia están en config/bandeja.json.

Antes de confirmar, revisar el mensaje y el texto propuesto, y declarar que corresponde la respuesta general (no una respuesta individual con folio). La confirmación guarda sólo una huella del mensaje/destinatario y del texto; no almacena sus cuerpos. Cambios de fecha que alteren el texto, plantillas o datos invalidan la confirmación. Confirmar no significa autorizar un registro.

La app propone el nombre a partir de «Nombre:», «Nombre completo:» o «Name:» si aparece al inicio de una línea; en otro caso usa el nombre mostrado por el remitente. La persona puede corregirlo y ver cómo cambia la respuesta, sin copiar/pegar. El nombre corregido sólo vive en memoria. Si se cierra la app entre la confirmación y el lote, esa confirmación caduca y debe revisarse de nuevo; así no se guarda el nombre en SQLite.

«Crear borradores confirmados» procesa únicamente los casos confirmados. El lote muestra cantidad, progreso y resultados; puede detenerse sin deshacer lo ya creado. Cada caso reserva una clave de operación persistente; doble clic o dos instancias no crean dos borradores del mismo caso. Un fallo incierto conserva la reserva y exige revisión; no se reintenta a ciegas.

## Envíos

La salida de pendientes requiere evidencia de respuesta enviada, no lectura, un borrador, una marca visual de Outlook ni un mensaje todavía en Bandeja de salida. La futura integración deberá relacionar inequívocamente la respuesta con el original y comprobar la cuenta. No cerrar casos sólo por coincidencia de asunto o de remitente.

El usuario también desea poder enviar lotes confirmados. Este incremento implementa la opción de borradores; no contiene Send ni envío real. El envío en lote será una acción separada sobre un lote revisado, con confirmación final, seguimiento de errores y exclusión de respuestas individuales no confirmadas.

## Alcance de la demo

La bandeja se guarda en SQLite, en una tabla separada del historial anterior. No guarda nombre, destinatario, asunto libre, cuerpo, folio ni observaciones personales. Para mostrar mensajes se consulta el proveedor seleccionado: simulación u Outlook clásico. La pestaña tiene refresco periódico, incorporación de pre-registros ficticios sólo en simulación, revisión, creación al confirmar si se elige esa opción, lote y simulación explícita de respuesta enviada sólo en simulación. Ningún modo llama Send.

Al reiniciar, se conservan los estados, pero los borradores simulados desaparecen; un borrador no localizable se señala para revisión y no se recrea automáticamente. Los casos respondidos no reaparecen como pendientes al releerlos. Las filas no localizables permanecen visibles por su identificador, sin inventar contenido.

Conexión Outlook: se elige una cuenta y carpeta en el perfil actual, por Account.DeliveryStore y Store.GetDefaultFolder(olFolderInbox), sin leer archivos de datos directamente. Los mensajes se filtran por asunto antes de leer el cuerpo. Se crea Reply y se guarda un borrador en la cuenta elegida, con marca de operación para conciliar reinicios y evitar duplicados. Se consultan Borradores y Enviados; una respuesta enviada se relaciona con el original mediante la marca de esta app o el identificador de mensaje In-Reply-To. Si no puede probar la relación, el caso permanece pendiente. Las cuentas y carpetas cambian por equipo, sin recompilar. La detección de mensajes movidos o relaciones no conservadas por Outlook se revisará con correos ficticios antes de producción.
