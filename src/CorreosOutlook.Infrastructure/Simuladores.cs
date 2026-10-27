using CorreosOutlook.Application;
using CorreosOutlook.Domain;

namespace CorreosOutlook.Infrastructure;

public sealed class OutlookSimulado : ICorreoBandeja
{
    private readonly string cuenta;
    private readonly List<Mensaje> mensajes;
    private readonly List<Borrador> borradores = [];
    private readonly Dictionary<string, Borrador> operaciones = [];
    private readonly Dictionary<string, EvidenciaEnvio> envios = [];
    public string Origen => "simulado:" + cuenta.ToLowerInvariant();
    public IReadOnlyList<Borrador> Borradores => borradores.AsReadOnly();
    public OutlookSimulado(string cuenta)
    {
        this.cuenta = cuenta;
        mensajes = new List<Mensaje>
        {
            new Mensaje("demo-001", cuenta, "persona.alfa@example.invalid", "Persona Alfa (ficticia)",
                "Requisitos para comprador", DateTimeOffset.UtcNow, "Deseo conocer los requisitos para comprador. Ejemplo ficticio.", true),
            new Mensaje("demo-002", cuenta, "persona.beta@example.invalid", "Persona Beta (ficticia)",
                "Consulta de acompañantes", DateTimeOffset.UtcNow, "Aparezco como acompañante y deseo información. Ejemplo ficticio.", false),
            new Mensaje("demo-003", cuenta, "visitor.gamma@example.invalid", "Visitor Gamma (fictional)",
                "Event information", DateTimeOffset.UtcNow, "Hello, please share information about the event. Fictional message.", false)
        };
    }
    public IReadOnlyList<Mensaje> ObtenerMensajesDisponibles() => mensajes.AsReadOnly();
    public Borrador CrearBorrador(Mensaje mensaje, VistaPrevia vista, bool revisionConfirmada)
    {
        if (!string.Equals(mensaje.Cuenta, cuenta, StringComparison.OrdinalIgnoreCase)
            || !mensajes.Contains(mensaje) || vista.MensajeId != mensaje.Id)
            throw new ValidacionException("Mensaje ajeno a la cuenta o al simulador, o vista previa de otro mensaje.");
        if (!revisionConfirmada || !vista.PuedeCrearBorrador)
            throw new ValidacionException("Debe revisar y confirmar la vista previa antes de crear el borrador simulado.");
        var borrador = new Borrador(Guid.NewGuid(), mensaje.Id, vista.Asunto, vista.Html, true);
        borradores.Add(borrador);
        return borrador;
    }

    public Borrador CrearBorradorConClave(Mensaje mensaje, VistaPrevia vista, string operacionId)
    {
        if (operaciones.TryGetValue(operacionId, out var existente))
        {
            if (existente.MensajeId != mensaje.Id) throw new ValidacionException("La operación corresponde a otro mensaje.");
            return existente;
        }
        var borrador = CrearBorrador(mensaje, vista, true);
        operaciones.Add(operacionId, borrador); return borrador;
    }
    public Borrador? BuscarBorrador(string operacionId) => operaciones.GetValueOrDefault(operacionId);
    public EvidenciaEnvio? ConsultarRespuestaEnviada(string mensajeId) => envios.GetValueOrDefault(mensajeId);
    public void SimularRespuestaEnviada(string mensajeId, DateTimeOffset? fecha = null)
    {
        if (!mensajes.Any(m => m.Id == mensajeId)) throw new ValidacionException("No existe ese mensaje ficticio.");
        envios[mensajeId] = new(mensajeId, cuenta, "envio-ficticio-" + Guid.NewGuid().ToString("N"), fecha ?? DateTimeOffset.UtcNow, true);
    }
    public void AgregarMensajeFicticio(Mensaje mensaje)
    {
        if (!mensaje.Remitente.EndsWith("@example.invalid", StringComparison.OrdinalIgnoreCase))
            throw new ValidacionException("El simulador sólo admite direcciones ficticias example.invalid.");
        if (mensajes.Any(m => m.Id == mensaje.Id)) return;
        mensajes.Add(mensaje);
    }
}

public sealed class RegistroExpoSimulado : IRegistroExpoRepository
{
    private readonly Dictionary<string, RegistroExpo> registros = new()
    {
        ["demo-001"] = new("demo-001", "Persona Alfa (ficticia)", EstadoRegistro.NoEncontrado, null),
        ["demo-002"] = new("demo-002", "Persona Beta (ficticia)", EstadoRegistro.AcompananteRegistrado, "Titular Delta (ficticio)")
    };
    public RegistroExpo? BuscarPorId(string id) => registros.GetValueOrDefault(id);
}
public sealed class CarpetasSimuladas(string archivo) : ICarpetasService
{
    public IReadOnlyList<Carpeta> ObtenerCarpetas()
    {
        var carpetas = ConfiguracionJson.Leer<Carpeta[]>(archivo);
        if (carpetas.Any(c => !c.Simulada)) throw new ValidacionException("Sólo se admiten carpetas simuladas.");
        return carpetas;
    }
}
