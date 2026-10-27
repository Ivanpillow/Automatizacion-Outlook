# Correos Outlook · Expo Joya

Aplicación de escritorio en C# / .NET 8 / Windows Forms para asistir la atención de correos. Incluye simulación y una bandeja de pre-registros que puede leer una cuenta y carpeta elegidas del perfil de Outlook clásico. Después de revisar cada respuesta, puede crear borradores individuales o en lote. No envía correos.

## Inicio

**Para probar sin herramientas de desarrollo:** hacer doble clic en **Iniciar Expo Joya.cmd** en la raíz. Abrir la pestaña «Pre-registros». La simulación está disponible al iniciar; «Conectar Outlook clásico» permite elegir una cuenta y carpeta del perfil. Para trasladarla, extraer el ZIP preparado en `artifacts/entrega/ExpoJoya-Demo-win-x64.zip`. Consulte [INICIO_RAPIDO.md](INICIO_RAPIDO.md) para instrucciones y requisitos de la laptop.

Abrir CorreosOutlook.sln con Visual Studio compatible con .NET 8 y desarrollo de escritorio, o utilizar el SDK de .NET 8:


## Uso de la demostración

Seleccionar un mensaje ficticio, analizarlo y aplicar la sugerencia si procede. Revisar idioma, perfil, etapa calculada, estado simulado, plantilla y variables. La fecha de evaluación es modificable para comprobar el límite documental; se interpreta en la zona horaria del evento. Generar una vista previa, revisar su contenido, marcar la casilla de revisión y crear un borrador simulado. Cambiar cualquier dato invalida la revisión anterior. Los borradores desaparecen al cerrar.

La sección de reglas permite comprobar documentos y acompañantes introducidos manualmente. Un resultado documental favorable sólo indica que cumple criterios conocidos: nunca es una autorización. Ningún adjunto se abre.

La pestaña «Pre-registros» detecta los dos asuntos configurados en `config/bandeja.json`, guarda su acumulado en SQLite y ofrece revisión de la respuesta general, atención individual y lote de borradores. Puede optar por crear el borrador al confirmar cada respuesta. Un borrador sigue pendiente; sólo una respuesta verificada en Enviados lo retira del acumulado. Consulte [BANDEJA_PREREGISTROS.md](docs/BANDEJA_PREREGISTROS.md). En la simulación hay ejemplos ficticios; con Outlook se consulta el perfil instalado, sin abrir `.ost` ni `.pst` directamente.

ón de Outlook real.

## Documentación

- [Especificación](docs/ESPECIFICACION_FUNCIONAL.md)
- [Reglas](docs/REGLAS_NEGOCIO.md)
- [Flujos](docs/FLUJOS_CORREO.md)
- [Arquitectura](docs/ARQUITECTURA.md)
- [Outlook](docs/INTEGRACION_OUTLOOK.md)
- [Pendientes](docs/PENDIENTES.md)
- [Plan](docs/PLAN_DESARROLLO.md)

La solicitud original es la autoridad de negocio; ninguna heurística equivale a una decisión. El catálogo incluye los casos futuros como pendientes y no permite generar respuestas de plantillas aún no redactadas.

