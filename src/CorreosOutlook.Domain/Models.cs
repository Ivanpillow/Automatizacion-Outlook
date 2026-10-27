namespace CorreosOutlook.Domain;

public enum Perfil { Otro, Comprador, Emprendedor, Internacional, Acompanante, Expositor }
public enum Etapa { Previo, Durante, Despues }
public enum EstadoRegistro { NoEncontrado, CompradorRegistrado, EmprendedorRegistrado, AcompananteRegistrado, Incompleto, RevisionManual }
public enum NivelAutomatizacion { Manual, Asistido, BorradorAutomatico, EnvioAutomatico }
public enum AccionAcompanante { Continuar, SolicitarComprador, SolicitarEmprendedor, CorregirTitular, RevisionManual }
public enum PlazoDocumental { AntesDelLimite, LimiteFinalizado, DuranteEvento, DespuesEvento }

public sealed record Evento
{
    public required string Id { get; init; }
    public required string Nombre { get; init; }
    public DateOnly? Inicio { get; init; }
    public DateOnly? Fin { get; init; }
    public DateTime? LimiteDocumentacionLocal { get; init; }
    public required string Recinto { get; init; }
    public required string Ciudad { get; init; }
    public required string Estado { get; init; }
}
public sealed record ConfiguracionEvento
{
    public required string Cuenta { get; init; }
    public required string ZonaHoraria { get; init; }
    public required string RegistroCompradorUrl { get; init; }
    public required Evento Actual { get; init; }
    public required Evento Siguiente { get; init; }
}
public sealed record Politicas
{
    public required DocumentosPolicy Documentos { get; init; }
    public required AcompanantesPolicy Acompanantes { get; init; }
    public required InternacionalPolicy Internacional { get; init; }
    public required Dictionary<string, Dictionary<string, string>> Textos { get; init; }
}
public sealed record DocumentosPolicy(bool IdentificacionObligatoria, int AntiguedadMaximaMeses,
    decimal CompradorMinimoMxn, decimal? EmprendedorMinimoMxn)
{
    public string[] EvidenciasAdicionalesEmprendedor { get; init; } = [];
}
public sealed record AcompanantesPolicy(int Maximo, int EdadMinima, string[] IdentificacionesMenores);
public sealed record InternacionalPolicy(decimal FacturaMinimaUsd, string? ModalidadRequisitos,
    string[] Requisitos, bool PoliticaConfirmada);
public sealed record Comprobante(DateOnly Fecha, decimal MontoMxn, bool ANombreDeLaPersona, bool ActividadAfin);
public sealed record Documentacion(bool IdentificacionVigenteConFoto, bool ConstanciaFiscalAfin,
    bool InvitacionPersonalizada, IReadOnlyList<Comprobante> Comprobantes, bool EvidenciaAdicionalAfinRevisada = false);
public sealed record EvaluacionDocumental(bool CumpleCriteriosConocidos, bool RequiereRevision,
    decimal TotalComprobantesValidos, IReadOnlyList<string> Observaciones, Perfil? PerfilSugerido = null);
public sealed record CupoAcompanantes(bool EsValido, int Total, int Disponibles, string? Error);
public sealed record Mensaje(string Id, string Cuenta, string Remitente, string NombreRemitente,
    string Asunto, DateTimeOffset Recibido, string Cuerpo, bool TieneAdjuntos);
public sealed record RegistroExpo(string IdFicticio, string Nombre, EstadoRegistro Estado, string? Titular);
public sealed record Carpeta(string Id, string Nombre, bool Simulada);

public sealed record Plantilla
{
    public required string Id { get; init; }
    public required string Nombre { get; init; }
    public required string Idioma { get; init; }
    public Perfil Perfil { get; init; }
    public required Etapa[] EtapasPermitidas { get; init; }
    public required string Asunto { get; init; }
    public string? ArchivoCuerpo { get; init; }
    public required string[] VariablesObligatorias { get; init; }
    public bool RequiereRevision { get; init; } = true;
    public bool PuedeCrearBorrador { get; init; }
    public bool ElegibleAutoenvioFuturo { get; init; }
    public bool RequiereConsultaBaseDatos { get; init; }
    public bool RepresentaAprobacionORechazo { get; init; }
    public bool Disponible { get; init; }
    public bool PermiteLoteGeneral { get; init; }
    public override string ToString() => $"{Nombre} ({Idioma})";
}
