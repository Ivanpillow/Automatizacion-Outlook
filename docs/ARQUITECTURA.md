# Arquitectura

Dependencias: WinForms → Infrastructure y Application; Infrastructure → Application; Application → Domain. Tests prueba las capas sin Outlook. Domain no conoce UI, archivos ni base de datos.

- Domain: modelos, catálogos, políticas, evaluación documental, cupos y decisión temporal.
- Application: interfaces IOutlookService, ICarpetasService, IRegistroExpoRepository, IHistorialRepository, ITemplateEngine; sugerencias, construcción del contexto y vista previa.
- Infrastructure: JSON con validación, Scriban estricto, Outlook simulado y adaptador COM para la bandeja, SQLite mediante Microsoft.Data.Sqlite y consultas parametrizadas. No ORM ni servidor.
- WinForms: una ventana con pestañas, controles editables, avisos y revisión explícita.
- Tests: xUnit, casos de frontera y pruebas de integración de configuración/plantillas/SQLite.

Dependencias de ejecución externas: Scriban y Microsoft.Data.Sqlite. Tests añade xUnit y Microsoft.NET.Test.Sdk. El adaptador de Outlook clásico usa COM del sistema desde WinForms STA, sin paquete Office Interop adicional. No Python, contenedores, IA remota/local, servicios persistentes ni credenciales.

Plantillas externas de texto UTF-8 con sintaxis Scriban. Catálogo JSON con metadatos. Renderizado estricto: variables desconocidas o requeridas vacías fallan. El contexto permitido se construye explícitamente, sin exponer servicios ni objetos de Outlook al motor. El HTML final escapa todo el resultado.

Tiempo: DateTimeOffset de entrada, convertido a TimeZoneInfo de evento. Umbral inclusivo para cierre; inicio/fin del evento por fecha local. UI usa reloj local del evento y permite simular otras fechas. La cultura determina los días de semana.

Persistencia: SQLite local; esquema operativo sin cuerpo ni identidad del remitente. No almacena la relación nombre/folio. Fechas en ISO 8601. Los borradores simulados viven en memoria; el historial conserva únicamente conteos/metadatos. No hay logs de texto libre. Carpeta de datos separada del binario publicado.

Seguridad por capacidad: las interfaces no exponen Send/Delete/Move/MarkRead sobre mensajes originales. La única escritura real de Outlook es guardar borradores revisados; el adaptador puede mover el borrador recién creado a la carpeta Borradores de la cuenta seleccionada. Los indicadores de autoenvío en el catálogo no habilitan acciones.
