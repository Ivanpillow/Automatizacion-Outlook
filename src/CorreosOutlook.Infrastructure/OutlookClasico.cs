using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using CorreosOutlook.Application;
using CorreosOutlook.Domain;

namespace CorreosOutlook.Infrastructure;

public sealed record CuentaOutlook(string Correo, string Tipo, IReadOnlyList<string> Carpetas);

/// <summary>Usa el perfil abierto de Outlook clásico. Todas las llamadas se ejecutan desde el hilo STA de WinForms.</summary>
public sealed class OutlookClasico : ICorreoBandeja, IEnvioBandeja
{
    private const string IdInternet = "http://schemas.microsoft.com/mapi/proptag/0x1035001F";
    private const string IdRespuesta = "http://schemas.microsoft.com/mapi/proptag/0x1042001F";
    private readonly string cuenta;
    private readonly string carpetaRelativa;
    private readonly DetectorPreregistro detector;
    private readonly dynamic aplicacion;
    private readonly dynamic sesion;
    private readonly dynamic cuentaCom;
    private readonly dynamic almacen;
    private readonly dynamic carpeta;
    private readonly Dictionary<string, Mensaje> cache = [];
    private readonly Dictionary<string, Borrador> cacheBorradores = [];
    private bool borradoresLeidos;
    private readonly Dictionary<string, EvidenciaEnvio> cacheEnvios = [];
    private bool enviadosLeidos;
    public string Origen { get; }

    public static IReadOnlyList<CuentaOutlook> DescubrirCuentas()
    {
        dynamic app = AbrirAplicacion(); dynamic sesion = app.GetNamespace("MAPI");
        var resultado = new List<CuentaOutlook>();
        foreach (dynamic item in sesion.Accounts)
        {
            try
            {
                string direccion = (string)item.SmtpAddress;
                if (string.IsNullOrWhiteSpace(direccion)) continue;
                dynamic store = item.DeliveryStore;
                dynamic inbox = store.GetDefaultFolder(6);
                var carpetas = new List<string> { "Bandeja de entrada" };
                AgregarSubcarpetas(inbox, "", carpetas, 0);
                string tipo = (int)item.AccountType switch { 0 => "Exchange", 1 => "IMAP", 2 => "POP", _ => "Otro" };
                resultado.Add(new(direccion, tipo, carpetas));
            }
            catch (COMException) { /* Una cuenta sin almacén accesible no se ofrece. */ }
        }
        return resultado;
    }
    private static void AgregarSubcarpetas(dynamic raiz, string prefijo, List<string> carpetas, int nivel)
    {
        if (nivel >= 2) return;
        foreach (dynamic sub in raiz.Folders)
        {
            string nombre = (string)sub.Name;
            string ruta = string.IsNullOrEmpty(prefijo) ? nombre : prefijo + "/" + nombre;
            carpetas.Add(ruta); AgregarSubcarpetas(sub, ruta, carpetas, nivel + 1);
        }
    }
    private static dynamic AbrirAplicacion()
    {
        if (!OperatingSystem.IsWindows()) throw new ValidacionException("Outlook clásico sólo está disponible en Windows.");
        var tipo = Type.GetTypeFromProgID("Outlook.Application")
            ?? throw new ValidacionException("No se encontró Outlook clásico en este equipo.");
        return Activator.CreateInstance(tipo) ?? throw new ValidacionException("No se pudo abrir el perfil de Outlook clásico.");
    }
    public OutlookClasico(string cuenta, string carpetaRelativa, ConfiguracionBandeja config)
    {
        this.cuenta = cuenta; this.carpetaRelativa = carpetaRelativa;
        detector = new(config);
        aplicacion = AbrirAplicacion(); sesion = aplicacion.GetNamespace("MAPI");
        var cuentas = new List<object>();
        foreach (dynamic item in sesion.Accounts)
            if (string.Equals((string)item.SmtpAddress, cuenta, StringComparison.OrdinalIgnoreCase)) cuentas.Add(item);
        if (cuentas.Count != 1) throw new ValidacionException("La cuenta indicada no aparece una sola vez en el perfil de Outlook.");
        cuentaCom = cuentas[0]; almacen = cuentaCom.DeliveryStore;
        carpeta = almacen.GetDefaultFolder(6);
        if (carpeta is null) throw new ValidacionException("La cuenta no tiene Bandeja de entrada.");
        if (carpetaRelativa != "Bandeja de entrada")
        {
            foreach (var segmento in carpetaRelativa.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                dynamic? siguiente = null;
                foreach (dynamic candidata in carpeta.Folders)
                    if (string.Equals((string)candidata.Name, segmento, StringComparison.OrdinalIgnoreCase))
                    { siguiente = candidata; break; }
                if (siguiente is null) throw new ValidacionException("No se encontró la carpeta configurada en Outlook.");
                carpeta = siguiente;
            }
        }
        string origen = cuenta.ToLowerInvariant() + "|" + (string)almacen.StoreID + "|" + (string)carpeta.EntryID;
        Origen = "outlook:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(origen)));
    }
    public IReadOnlyList<Mensaje> ObtenerMensajesDisponibles()
    {
        var resultado = new List<Mensaje>(); cache.Clear(); cacheBorradores.Clear(); borradoresLeidos = false;
        cacheEnvios.Clear(); enviadosLeidos = false;
        // Outlook filtra por asunto sin abrir cada cuerpo. El detector de dominio decide los dos prefijos exactos.
        dynamic filtrados = carpeta.Items.Restrict("@SQL=\"http://schemas.microsoft.com/mapi/proptag/0x0037001F\" LIKE 'Pre%'");
        foreach (dynamic item in filtrados)
        {
            try
            {
                if ((int)item.Class != 43) continue; // olMail
                string id = (string)item.EntryID;
                if (string.IsNullOrWhiteSpace(id)) continue;
                string asunto = (string)item.Subject;
                if (detector.Detectar(asunto) is null) continue;
                string remitente = (string)item.SenderEmailAddress;
                var recibido = new DateTimeOffset((DateTime)item.ReceivedTime);
                var mensaje = new Mensaje(id, cuenta, remitente, (string)item.SenderName,
                    asunto, recibido, (string)item.Body, (int)item.Attachments.Count > 0);
                resultado.Add(mensaje); cache[id] = mensaje;
            }
            catch (COMException) { /* Un elemento que cambió o se movió se reintentará en el próximo refresco. */ }
        }
        return resultado;
    }
    public Borrador CrearBorrador(Mensaje mensaje, VistaPrevia vista, bool revisionConfirmada)
    {
        if (!revisionConfirmada) throw new ValidacionException("La vista previa requiere revisión humana.");
        return CrearBorradorConClave(mensaje, vista, Guid.NewGuid().ToString("N"));
    }
    public Borrador CrearBorradorConClave(Mensaje mensaje, VistaPrevia vista, string operacionId)
    {
        if (!string.Equals(mensaje.Cuenta, cuenta, StringComparison.OrdinalIgnoreCase)
            || !cache.TryGetValue(mensaje.Id, out var conocido) || conocido != mensaje
            || vista.MensajeId != mensaje.Id || !vista.PuedeCrearBorrador)
            throw new ValidacionException("Cambió el mensaje o la vista previa. No se creó el borrador.");
        var previo = BuscarBorrador(operacionId);
        if (previo is not null)
        {
            if (previo.MensajeId != mensaje.Id) throw new ValidacionException("La operación ya pertenece a otro mensaje.");
            return previo;
        }
        dynamic original = sesion.GetItemFromID(mensaje.Id, (string)almacen.StoreID);
        if ((int)original.Class != 43 || (string)original.EntryID != mensaje.Id
            || !string.Equals((string)original.Subject, mensaje.Asunto, StringComparison.Ordinal)
            || !string.Equals((string)original.SenderEmailAddress, mensaje.Remitente, StringComparison.OrdinalIgnoreCase))
            throw new ValidacionException("El correo original cambió. Actualice y revíselo de nuevo.");
        dynamic respuesta = original.Reply();
        respuesta.SendUsingAccount = cuentaCom;
        // Reply conserva destinatario, asunto e hilo. Se inserta el texto revisado sobre la cita y firma de Outlook.
        string htmlExistente = (string)respuesta.HTMLBody;
        int apertura = htmlExistente.IndexOf('>', htmlExistente.IndexOf("<body", StringComparison.OrdinalIgnoreCase));
        string contenido = vista.Html;
        int inicio = contenido.IndexOf("<body>", StringComparison.OrdinalIgnoreCase);
        int fin = contenido.IndexOf("</body>", StringComparison.OrdinalIgnoreCase);
        if (apertura < 0 || inicio < 0 || fin <= inicio)
            throw new ValidacionException("No se pudo preservar la estructura de la respuesta de Outlook.");
        contenido = contenido[(inicio + 6)..fin];
        respuesta.HTMLBody = htmlExistente.Insert(apertura + 1, contenido + "<br><br>");
        dynamic propiedad = respuesta.UserProperties.Add("ExpoJoyaOperacion", 1, false);
        propiedad.Value = operacionId;
        dynamic origen = respuesta.UserProperties.Add("ExpoJoyaMensaje", 1, false);
        origen.Value = mensaje.Id;
        dynamic destinatario = respuesta.UserProperties.Add("ExpoJoyaDestinatario", 1, false);
        destinatario.Value = mensaje.Remitente;
        respuesta.Save();
        dynamic carpetaBorradores = almacen.GetDefaultFolder(16);
        if ((string)respuesta.Parent.EntryID != (string)carpetaBorradores.EntryID)
            respuesta = respuesta.Move(carpetaBorradores);
        dynamic huellaHtml = respuesta.UserProperties.Add("ExpoJoyaHtmlHash", 1, false);
        huellaHtml.Value = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes((string)respuesta.HTMLBody)));
        dynamic asuntoGuardado = respuesta.UserProperties.Add("ExpoJoyaAsunto", 1, false);
        asuntoGuardado.Value = (string)respuesta.Subject;
        respuesta.Save();
        string nuevoId = (string)respuesta.EntryID;
        var creado = new Borrador(Guid.NewGuid(), mensaje.Id, (string)respuesta.Subject, (string)respuesta.HTMLBody, false)
        {
            OutlookEntryId = nuevoId
        };
        cacheBorradores[operacionId] = creado;
        return creado;
    }
    public void ValidarBorrador(string mensajeId, string operacionId) => ObtenerBorradorValido(mensajeId, operacionId);
    private dynamic ObtenerBorradorValido(string mensajeId, string operacionId)
    {
        if (!cache.TryGetValue(mensajeId, out var mensaje))
            throw new ValidacionException("El mensaje original ya no está disponible. Actualice la bandeja.");
        var borrador = BuscarBorrador(operacionId);
        if (borrador?.OutlookEntryId is null || borrador.MensajeId != mensajeId)
            throw new ValidacionException("No se encontró el borrador confirmado.");
        dynamic respuesta = sesion.GetItemFromID(borrador.OutlookEntryId, (string)almacen.StoreID);
        dynamic carpetaBorradores = almacen.GetDefaultFolder(16);
        if ((int)respuesta.Class != 43 || (bool)respuesta.Sent
            || (string)respuesta.Parent.EntryID != (string)carpetaBorradores.EntryID)
            throw new ValidacionException("El borrador cambió de estado. Actualice antes de enviar.");
        string? Marca(string nombre)
        {
            dynamic valor = respuesta.UserProperties.Find(nombre);
            return valor is null ? null : (string)valor.Value;
        }
        if (Marca("ExpoJoyaOperacion") != operacionId || Marca("ExpoJoyaMensaje") != mensajeId
            || !string.Equals(Marca("ExpoJoyaDestinatario"), mensaje.Remitente, StringComparison.OrdinalIgnoreCase))
            throw new ValidacionException("El borrador no corresponde al mensaje y destinatario confirmados.");
        string htmlActual = (string)respuesta.HTMLBody;
        string huellaActual = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(htmlActual)));
        if (Marca("ExpoJoyaHtmlHash") != huellaActual)
            throw new ValidacionException("El contenido del borrador cambió. Revíselo en Outlook antes de enviarlo.");
        if (Marca("ExpoJoyaAsunto") != (string)respuesta.Subject)
            throw new ValidacionException("El asunto del borrador cambió. Revíselo en Outlook antes de enviarlo.");
        if ((int)respuesta.Recipients.Count != 1)
            throw new ValidacionException("El borrador debe tener exactamente un destinatario.");
        dynamic receptor = respuesta.Recipients.Item(1);
        string direccion = (string)receptor.Address;
        if (!string.Equals(direccion, mensaje.Remitente, StringComparison.OrdinalIgnoreCase))
        {
            try { direccion = (string)receptor.AddressEntry.GetExchangeUser().PrimarySmtpAddress; }
            catch (COMException) { throw new ValidacionException("No se pudo comprobar la dirección del destinatario."); }
        }
        if (!string.Equals(direccion, mensaje.Remitente, StringComparison.OrdinalIgnoreCase))
            throw new ValidacionException("El destinatario del borrador cambió.");
        return respuesta;
    }
    public void EnviarBorrador(string mensajeId, string operacionId)
    {
        dynamic respuesta = ObtenerBorradorValido(mensajeId, operacionId);
        respuesta.SendUsingAccount = cuentaCom;
        respuesta.Send();
        cacheBorradores.Remove(operacionId);
        enviadosLeidos = false;
    }
    public Borrador? BuscarBorrador(string operacionId)
    {
        if (!borradoresLeidos)
        {
            dynamic carpetaBorradores = almacen.GetDefaultFolder(16);
            foreach (dynamic item in carpetaBorradores.Items)
            {
                try
                {
                    if ((int)item.Class != 43) continue;
                    dynamic prop = item.UserProperties.Find("ExpoJoyaOperacion");
                    dynamic mensaje = item.UserProperties.Find("ExpoJoyaMensaje");
                    if (prop is null || mensaje is null) continue;
                    string operacion = (string)prop.Value, mensajeId = (string)mensaje.Value;
                    if (!cache.ContainsKey(mensajeId) || string.IsNullOrWhiteSpace(operacion)) continue;
                    cacheBorradores[operacion] = new(Guid.NewGuid(), mensajeId, (string)item.Subject, "", false)
                    { OutlookEntryId = (string)item.EntryID };
                }
                catch (COMException) { }
            }
            borradoresLeidos = true;
        }
        return cacheBorradores.GetValueOrDefault(operacionId);
    }
    private string? TryInternetId(string id)
    {
        try
        {
            dynamic original = sesion.GetItemFromID(id, (string)almacen.StoreID);
            return (string)original.PropertyAccessor.GetProperty(IdInternet);
        }
        catch (COMException) { return null; }
    }
    public EvidenciaEnvio? ConsultarRespuestaEnviada(string mensajeId)
    {
        if (!cache.ContainsKey(mensajeId)) return null;
        if (!enviadosLeidos)
        {
            var porInternet = new Dictionary<string, Mensaje>(StringComparer.OrdinalIgnoreCase);
            foreach (var original in cache.Values)
            {
                var internetId = TryInternetId(original.Id);
                if (!string.IsNullOrWhiteSpace(internetId)) porInternet[internetId] = original;
            }
            dynamic enviados = almacen.GetDefaultFolder(5).Items;
            foreach (dynamic item in enviados)
            {
                try
                {
                    if ((int)item.Class != 43 || !(bool)item.Sent) continue;
                    Mensaje? original = null;
                    dynamic marca = item.UserProperties.Find("ExpoJoyaMensaje");
                    if (marca is not null) cache.TryGetValue((string)marca.Value, out original);
                    if (original is null)
                    {
                        try { porInternet.TryGetValue((string)item.PropertyAccessor.GetProperty(IdRespuesta), out original); }
                        catch (COMException) { }
                    }
                    if (original is null) continue;
                    DateTimeOffset fecha = new((DateTime)item.SentOn);
                    if (fecha < original.Recibido) continue;
                    string id = (string)item.EntryID;
                    if (!string.IsNullOrWhiteSpace(id)) cacheEnvios[original.Id] = new(original.Id, cuenta, id, fecha, true);
                }
                catch (COMException) { }
            }
            enviadosLeidos = true;
        }
        return cacheEnvios.GetValueOrDefault(mensajeId);
    }
}
