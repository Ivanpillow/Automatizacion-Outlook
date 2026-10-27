# Flujos de correo

1. Seleccionar mensaje ficticio. Se muestra sólo texto y presencia de adjuntos.
2. Analizar con una heurística local explicable; no ejecuta contenido del correo. Mostrar sugerencia y permitir aplicar/corregir.
3. Seleccionar plantilla disponible, idioma, perfil, etapa y estado manual. Los resultados del repositorio ficticio son ejemplos; no consultas oficiales.
4. Completar variables. Validar requeridas y coherencia de etapa/idioma; no permitir salida incompleta ni confirmaciones de cupo incorrectas.
5. Generar vista previa. La fecha determina el texto común de documentación. HTML se construye escapando el texto final, sin HTML activo aportado por correos.
6. Revisión humana explícita. Cualquier cambio invalida vista previa/revisión.
7. Crear borrador simulado en memoria tras comprobar pertenencia a la cuenta configurada. Guardar metadatos mínimos en SQLite con estado BorradorSimulado. No marcar respondido.

## Según etapa

- Previo: dudas, pre-registros, documentos, acompañantes, cambios, actualizaciones, gafetes, visa y expositores. Casos aún sin plantilla quedan pendientes de redacción/revisión.
- Durante: facilitar información y borradores; autorizaciones, documentos, folios y rechazos humanos.
- Después: disculpa por atención pendiente durante las actividades; invitación a siguiente edición. Pre-registros tardíos no se confirman para el evento finalizado.

## Acompañante encontrado

Seleccionar estado acompañante y titular. Continuar explica no repetir registro y reimpresión. Cambio solicitado incorpora requisitos del perfil sin aprobarlo. Corregir titular exige revisión; no escribe en base oficial. El texto neutral evita inferencias de género.

## Errores

Configuración inválida, variables faltantes, cupo excedido, política pendiente o archivo de plantilla no disponible producen mensajes comprensibles y no crean borrador. No mostrar contenido de excepciones técnicas que pueda contener datos personales. Fallo de historial se informa sin repetir la creación del borrador.
