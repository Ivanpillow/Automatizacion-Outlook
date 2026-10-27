# Abrir Expo Joya con doble clic

## En esta computadora

Abra **Iniciar Expo Joya.cmd**, en la raíz del proyecto, con doble clic. Abre directamente la versión publicada: no compila, no descarga y no necesita abrir una terminal, Codex ni Visual Studio.

También puede abrir directamente **artifacts/publish/win-x64/CorreosOutlook.WinForms.exe**. No confundirlo con createdump.exe, que es un componente del runtime.

La aplicación abre en la pestaña «Bandeja de entrada». Empieza en simulación con mensajes ficticios; pulse «Conectar Outlook clásico» para elegir una cuenta y una carpeta del perfil instalado. La bandeja detecta asuntos, acumula casos, deja revisar respuestas con formato HTML y crea borradores tras confirmación. Con Outlook clásico conectado, «Crear y enviar respuestas confirmadas» muestra los destinatarios y pide confirmación antes de solicitar el envío real. Las demás pestañas de correo individual siguen siendo demostraciones.

Para probar el flujo sin tocar Outlook: en «Pre-registros» pulse «Agregar pre-registros ficticios», elija una fila y «Revisar respuesta general». Revise el nombre del saludo y el texto; marque las dos confirmaciones. Puede activar «Crear el borrador al confirmar cada respuesta» para hacerlo en el mismo paso, o repetir la revisión de varios casos y después pulsar «Crear borradores confirmados». Los borradores permanecen en el acumulado. «Simular respuesta enviada» retira el caso de pendientes y lo conserva en el historial de la demo.

Para probar la lectura real: pulse «Conectar Outlook clásico», elija la cuenta y una carpeta del perfil. En este equipo, la opción identificada es soporte@camaradejoyeria.com.mx → Bandeja de entrada. La selección se recuerda en este equipo; «Volver a simulación» permite cambiar temporalmente. Cuando llegue un pre-registro de prueba, use «Actualizar». Revise y confirme su respuesta antes de crear el borrador. Puede enviarlo manualmente desde Outlook o usar el botón de lote para solicitar el envío desde la aplicación. Después pulse «Actualizar» para conciliar Enviados. Si no se puede comprobar la relación con el original, el caso permanece pendiente de revisión; no repita una solicitud de resultado incierto.

## Pasarla a la laptop

1. Copiar **artifacts/entrega/ExpoJoya-Demo-win-x64.zip** a la laptop.
2. Pulsar «Extraer todo» y conservar todos los archivos y subcarpetas juntos. No ejecutarla desde dentro del ZIP.
3. Abrir la carpeta extraída y hacer doble clic en **Iniciar Expo Joya.cmd** o **CorreosOutlook.WinForms.exe**.
4. Opcional: crear un acceso directo al ejecutable mediante «Enviar a → Escritorio (crear acceso directo)».

No basta copiar el EXE pequeño de forma aislada. El paquete incluye el runtime, bibliotecas, config/ y templates/. No hay que copiar .tools/, el código fuente ni cachés de desarrollo. Esta entrega no es un instalador.

## Qué necesita la laptop

| Uso | Requisitos |
| --- | --- |
| Simulación | Windows de 64 bits compatible, carpeta extraída completa y permiso para guardar datos en el perfil del usuario. No requiere Outlook. |
| Lectura y borradores Outlook | Lo anterior y Outlook clásico instalado, abierto/configurado con cuenta y almacén. Seleccionar cuenta y carpeta desde la app. Verificar arquitectura Office y cuenta de la laptop antes de utilizarla allí. |
| Herramientas de desarrollo | No necesita Visual Studio, Codex, Python, SDK .NET, un servidor SQLite ni instalar Scriban. |
| Runtime .NET | Incluido en el paquete autocontenido; no se descarga aparte. |

La simulación no necesita Internet. Outlook recibe y envía mediante su configuración habitual; la app observa la carpeta seleccionada. No se deben pedir nuevamente contraseñas POP/SMTP a la aplicación.

Microsoft documenta que la publicación autocontenida incluye el runtime: https://learn.microsoft.com/en-us/dotnet/core/deploying/ . Esto no incluye Outlook ni garantiza compatibilidad con cualquier versión de Windows; se comprobará la laptop antes de la entrega final.

La base SQLite local se guarda en %LOCALAPPDATA%/ExpoJoya/CorreosOutlook/demo.db. El ZIP no incluye ni migra ese historial. No borrar esa carpeta al actualizar; cerrar la aplicación antes de sustituir sus binarios y conservar cualquier configuración o plantilla personalizada.

## Probar con Outlook de escritorio

En esta computadora se encontró la cuenta soporte@camaradejoyeria.com.mx y su Bandeja de entrada. Outlook la identifica como IMAP. El lector integrado confirmó la cuenta y encontró cero asuntos de pre-registro al verificar; no se ha probado todavía un borrador real con un mensaje entrante ficticio. Para hacerlo, recibir un mensaje de prueba con el asunto configurado en una carpeta elegida. Revisar el texto en la app antes de crear el borrador; confirmar visualmente en Outlook que permanece sin enviar y en el hilo correcto. No usar datos personales reales en esa prueba.

La conexión se elige por cuenta y carpeta del perfil, y se guarda localmente para el próximo arranque. La cuenta de pruebas es diferente de la futura cuenta productiva contacto@expojoya.com.mx, configurada en la laptop con POP/PST. La selección no se copia en el ZIP; deberá elegirse de nuevo en la laptop. No modificar ni migrar esa cuenta.

## Para desarrollo solamente

`scripts/build.ps1 -Publish` compila, prueba y publica. Después `scripts/package.ps1` prepara el ZIP. El acceso de doble clic ejecuta la última publicación, no recompila al editar el código.

Antes de producción, revisar la versión soportada de .NET y Windows. La entrega actual conserva .NET 8 según la decisión inicial; una publicación autocontenida debe actualizarse para recibir nuevas correcciones del runtime.
