# Validación de la primera fase

## Bandeja de pre-registros y Outlook de escritorio

Se agregó una pestaña con acumulado persistente, detección de dos asuntos configurados, revisión individual, confirmación de respuesta general, borrador al confirmar o lote de borradores, conciliación de enviados y simulación. El botón de envío real sigue ausente. El adaptador COM selecciona cuenta y carpeta por el perfil de Outlook clásico y guarda sólo borradores revisados; ninguna prueba ha creado un borrador en la cuenta real todavía.

La cuenta soporte@camaradejoyeria.com.mx fue identificada en Outlook clásico como IMAP, con Bandeja de entrada. El lector integrado .NET confirmó la conexión y encontró cero pre-registros en esa carpeta el 26/10/2026. La prueba de PowerShell también contó cero asuntos coincidentes. Falta un mensaje ficticio recibido para comprobar visualmente un borrador real, hilo, cuenta de envío, firma, estado de leído y salida del acumulado al enviarlo manualmente. Esta prueba no se sustituye con los simuladores.

La solución compiló sin errores ni advertencias; 107 pruebas automatizadas pasaron, entre ellas duplicados, persistencia, huellas de respuesta, lote de 80 correos, borrador pendiente, enviados, concurrencia, extracción/corrección del nombre y caducidad segura de la confirmación. El ejecutable autocontenido pasó --smoke-test. Las pruebas de integración real sólo leyeron la cuenta: no se envió, movió ni eliminó correo real.

## Entrega de arranque rápido

Se añadió Iniciar Expo Joya.cmd, que ejecuta la publicación con doble clic sin compilar ni descargar. INICIO_RAPIDO.md explica traslado, requisitos y diferencia entre demo y futura integración con Outlook. scripts/package.ps1 prepara el ZIP portable. La primera copia portable verificó 491 archivos mediante SHA-256 y el iniciador extraído completó --smoke-test con salida 0.

En la entrega anterior se implementó la prioridad documental de comprador (CSF afín, invitación válida o notas suficientes), y la sugerencia de emprendedor con notas bajas más evidencia adicional revisada. La interfaz permite declarar esta revisión manual. Se conservan identificación y revisión humana obligatorias. En ese momento hubo **85/85 pruebas xUnit correctas**, incluidas 15 nuevas pruebas de prioridad, evidencia insuficiente e invalidez de notas antiguas. Informe: artifacts/tests/arranque-y-perfiles.trx. El flujo de comunicar el número de un comprador previamente registrado sigue documentado y pendiente; no se habilitó una plantilla de folio.

Fecha de trabajo: 26 de octubre de 2026, zona de referencia America/Mexico_City.

## Inventario inicial

La carpeta raíz estaba vacía. No se borraron ni reemplazaron archivos preexistentes. Se inicializó Git con rama main; no se crearon commits.

Visual Studio no se encontró mediante su instalador/ubicaciones habituales ni en el registro de programas. Sí se encontró Visual Studio Code, que es un editor distinto. .NET no estaba en PATH ni en las ubicaciones habituales. Se instaló el SDK oficial 8.0.427 de forma local en .tools/dotnet, sin instalación global. El repositorio ignora SDK, cachés, binarios y datos locales.

## Resultados de la primera entrega (históricos)

- Compilación Release de los cinco proyectos: correcta, cero errores y cero advertencias.
- xUnit: 70 pruebas correctas, cero fallos y cero omitidas. Informe TRX en artifacts/tests/resultados.trx.
- Publicación autocontenida Windows x64: generada en artifacts/publish/win-x64.
- Ejecutable publicado con --smoke-test: código de salida 0. Prueba de ventana WinForms, carga de datos, análisis, aplicación de sugerencia, vista previa, revisión, borrador simulado, persistencia SQLite y anulación de revisión al editar.
- Arranque adicional de la aplicación publicada: ventana de demostración detectada por Windows.
- Inspección visual: NO completada. La herramienta de captura devolvió `FrameArrived timed out: timed out waiting on channel` y, al reintentar con la ventana activada, `window capture timed out: timed out waiting on channel`. No se afirma validación visual de tamaño, escalado o disposición de controles.
- Outlook real y base oficial: ninguna conexión ni prueba. No se procesaron mensajes/adjuntos reales ni se enviaron correos.

## Cobertura

Fecha límite, día de semana, segundo anterior/cierre/posterior, límites del evento y zona horaria; textos antes/después/durante; siguiente evento con fechas pendientes. Identificación, cada alternativa por separado, importe inferior/igual/superior, suma de comprobantes, seis meses exactos, documentos antiguos/futuros, titularidad, actividad, políticas pendientes. Cupos cero/uno/dos, exceso, cantidades inválidas, edad, nombres, confirmación manual y cinco acciones de acompañante encontrado. Plantillas disponibles, variables requeridas/desconocidas, sintaxis, escape HTML, cambios de configuración, idioma/etapa, cuenta correcta, revisión humana, simuladores y persistencia mínima sin fecha de respuesta.

## Comandos

```powershell
.\scripts\build.ps1
.\scripts\build.ps1 -Publish
```

Equivalentes con SDK disponible en PATH:

```powershell
dotnet restore CorreosOutlook.sln
dotnet build CorreosOutlook.sln -c Release --no-restore
dotnet test CorreosOutlook.sln -c Release --no-build
dotnet publish src/CorreosOutlook.WinForms -c Release -r win-x64 --self-contained true -o artifacts/publish/win-x64
```

## Archivos entregados

- Raíz: AGENTS.md, README.md, CorreosOutlook.sln, Directory.Build.props, global.json, .gitignore y .gitattributes.
- docs/: ESPECIFICACION_FUNCIONAL, REGLAS_NEGOCIO, FLUJOS_CORREO, ARQUITECTURA, INTEGRACION_OUTLOOK, PENDIENTES, PLAN_DESARROLLO y este informe.
- config/: evento.json, politicas.json y carpetas.example.json.
- templates/: catalogo.json, README.md, seis plantillas españolas y dos inglesas. Catálogo de 27 casos y dos variantes en inglés; ocho plantillas disponibles. Casos restantes pendientes, sin texto inventado.
- src/: Domain (modelos y reglas), Application (contratos, generador y analizador), Infrastructure (JSON, Scriban, simuladores, SQLite) y WinForms (ventana, inicio, manifiesto).
- tests/: proyecto xUnit con fixtures y cuatro grupos de pruebas.
- scripts/: build.ps1 para restaurar, compilar, probar y publicar opcionalmente.

## Límites de esta entrega

La heurística de clasificación es deliberadamente básica y corregible. No hay integración Outlook, consulta oficial, instalador, categorías reales, movimiento ni envío. Folios y casos de aprobación/rechazo siguen pendientes de flujos humanos. La demo de documentos introduce un comprobante a la vez; el dominio admite varios y prueba su suma. Las confirmaciones de acompañantes simulan datos confirmados, no crean registros oficiales.

Revisar manualmente las tres pestañas y el escalado en la laptop; validar textos con responsables. Confirmar políticas de emprendedores e internacionales, fechas del siguiente evento, carpetas y datos técnicos de Outlook antes de ampliar funcionalidades. Revisar versión soportada de .NET antes de producción.
