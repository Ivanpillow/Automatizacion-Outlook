using CorreosOutlook.Domain;

namespace CorreosOutlook.Application;

public interface IOutlookService
{
    IReadOnlyList<Mensaje> ObtenerMensajesDisponibles();
    Borrador CrearBorrador(Mensaje mensaje, VistaPrevia vista, bool revisionConfirmada);
}
public interface ICarpetasService { IReadOnlyList<Carpeta> ObtenerCarpetas(); }
public interface IRegistroExpoRepository { RegistroExpo? BuscarPorId(string id); }
public interface ITemplateEngine { string Render(string texto, IReadOnlyDictionary<string, object?> variables); }
public interface IPlantillasRepository
{
    IReadOnlyList<Plantilla> Catalogo { get; }
    string LeerCuerpo(Plantilla plantilla);
}
public interface IHistorialRepository { void Registrar(Operacion operacion); long Contar(); }

public sealed record Operacion(Guid Id, string? OutlookId, string EventoId, DateTimeOffset FechaRecibida,
    DateTimeOffset? FechaRespondida, string Idioma, Perfil Perfil, string TipoSolicitud, string PlantillaId,
    string Estado, string Resultado, string? CarpetaDestino, bool IntervencionManual,
    int CantidadAcompanantes, bool PreRegistroCompleto);
public sealed record Borrador(Guid Id, string MensajeId, string Asunto, string Html, bool Simulado)
{
    public string? OutlookEntryId { get; init; }
}
public sealed record VistaPrevia(string MensajeId, string PlantillaId, string Asunto, string Texto, string Html,
    bool PuedeCrearBorrador, bool RequiereRevision);
public sealed record SolicitudRespuesta
{
    public required Mensaje Mensaje { get; init; }
    public required Plantilla Plantilla { get; init; }
    public required DateTimeOffset Instante { get; init; }
    public required string Idioma { get; init; }
    public Perfil Perfil { get; init; }
    public Etapa Etapa { get; init; }
    public EstadoRegistro EstadoRegistro { get; init; }
    public string Nombre { get; init; } = "";
    public string Titular { get; init; } = "";
    public string Folio { get; init; } = "";
    public string Observaciones { get; init; } = "";
    public string RespuestaManual { get; init; } = "";
    public AccionAcompanante AccionAcompanante { get; init; }
    public int AcompanantesRegistrados { get; init; }
    public int AcompanantesSolicitados { get; init; }
    public string[] NombresAcompanantes { get; init; } = [];
    public bool DatosConfirmadosManualmente { get; init; }
}
public sealed class ValidacionException(string mensaje) : Exception(mensaje);
