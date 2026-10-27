using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CorreosOutlook.Domain;

namespace CorreosOutlook.Application;

public interface IPreregistroRepository
{
    bool Incorporar(Preregistro caso);
    IReadOnlyList<Preregistro> Listar(string origen, string eventoId);
    Preregistro? Obtener(string id);
    bool Actualizar(Preregistro caso, int versionEsperada);
}
public interface ICorreoBandeja : IOutlookService
{
    string Origen { get; }
    Borrador CrearBorradorConClave(Mensaje mensaje, VistaPrevia vista, string operacionId);
    Borrador? BuscarBorrador(string operacionId);
    EvidenciaEnvio? ConsultarRespuestaEnviada(string mensajeId);
}
public sealed record RevisionGeneral(string CasoId, int Version, Mensaje Mensaje, VistaPrevia Vista, string Huella, string Nombre);
public sealed record ResultadoBorrador(bool Creado, string Mensaje);
public sealed record ResultadoEnvio(bool Enviado, string Mensaje);

public sealed class BandejaPreregistros(ConfiguracionEvento evento, ConfiguracionBandeja configuracion,
    IPreregistroRepository repository, ICorreoBandeja correo, GeneradorRespuesta generador, IPlantillasRepository plantillas)
{
    private readonly Dictionary<string, string> nombresSesion = [];
    public IReadOnlyList<Preregistro> Listar() => repository.Listar(correo.Origen, evento.Actual.Id);
    public Mensaje? MensajeDelCaso(Preregistro caso) => correo.ObtenerMensajesDisponibles()
        .FirstOrDefault(m => m.Id == caso.MensajeId && MismaCuenta(m.Cuenta));
    private bool MismaCuenta(string cuenta) => string.Equals(cuenta, evento.Cuenta, StringComparison.OrdinalIgnoreCase);

    public int ActualizarAcumulado()
    {
        int nuevos = 0;
        var detector = new DetectorPreregistro(configuracion);
        foreach (var mensaje in correo.ObtenerMensajesDisponibles())
        {
            if (!MismaCuenta(mensaje.Cuenta)) continue;
            var regla = detector.Detectar(mensaje.Asunto);
            if (regla is null) continue;
            if (repository.Incorporar(new(Guid.NewGuid().ToString("N"), correo.Origen, evento.Actual.Id,
                mensaje.Id, mensaje.Recibido, regla.Perfil))) nuevos++;
        }
        foreach (var caso in Listar().Where(c => c.Estado != EstadoPreregistro.Respondido))
        {
            var evidencia = correo.ConsultarRespuestaEnviada(caso.MensajeId);
            if (evidencia is { Enviado: true } && evidencia.MensajeId == caso.MensajeId
                && MismaCuenta(evidencia.Cuenta) && !string.IsNullOrWhiteSpace(evidencia.RespuestaId)
                && evidencia.Fecha >= caso.Recibido)
            {
                repository.Actualizar(caso with { Estado = EstadoPreregistro.Respondido, Respondido = evidencia.Fecha,
                    EvidenciaEnvioId = evidencia.RespuestaId, CodigoIncidencia = null }, caso.Version);
                continue;
            }
            if (caso.OperacionId is not null && caso.Estado is EstadoPreregistro.CreandoBorrador or EstadoPreregistro.BorradorCreado)
            {
                var borrador = correo.BuscarBorrador(caso.OperacionId);
                if (borrador is not null && borrador.MensajeId == caso.MensajeId)
                {
                    if (caso.Estado != EstadoPreregistro.BorradorCreado)
                        repository.Actualizar(caso with { Estado = EstadoPreregistro.BorradorCreado, BorradorId = borrador.Id.ToString() }, caso.Version);
                }
                else repository.Actualizar(caso with { Estado = EstadoPreregistro.ErrorRevision,
                    CodigoIncidencia = "BORRADOR_NO_LOCALIZABLE" }, caso.Version);
            }
        }
        return nuevos;
    }

    private Preregistro ObtenerCaso(string id)
    {
        var caso = repository.Obtener(id);
        if (caso is null || caso.Origen != correo.Origen || caso.EventoId != evento.Actual.Id)
            throw new ValidacionException("El caso no pertenece a esta bandeja y evento.");
        return caso;
    }

    public static string SugerirNombre(Mensaje mensaje)
    {
        var coincidencia = Regex.Match(mensaje.Cuerpo, @"(?im)^\s*(?:nombre(?:\s+completo)?|name)\s*:\s*([^\r\n]{2,80})",
            RegexOptions.None, TimeSpan.FromSeconds(1));
        return coincidencia.Success ? coincidencia.Groups[1].Value.Trim() : mensaje.NombreRemitente.Trim();
    }
    public RevisionGeneral PrepararRevision(string id, DateTimeOffset instante, string? nombrePersonalizado = null)
    {
        var caso = ObtenerCaso(id);
        if (caso.Estado is not (EstadoPreregistro.Pendiente or EstadoPreregistro.ConfirmadoGeneral or EstadoPreregistro.RevisionIndividual)
            || caso.OperacionId is not null)
            throw new ValidacionException("El caso ya tiene una operación de borrador o está respondido. Revíselo individualmente.");
        var mensaje = MensajeDelCaso(caso) ?? throw new ValidacionException("El mensaje ya no está disponible. No se creará una respuesta.");
        string nombre = (nombrePersonalizado ?? nombresSesion.GetValueOrDefault(id) ?? SugerirNombre(mensaje)).Trim();
        if (string.IsNullOrWhiteSpace(nombre)) throw new ValidacionException("Revise el nombre que aparecerá en el saludo.");
        var regla = new DetectorPreregistro(configuracion).Detectar(mensaje.Asunto);
        if (regla is null || regla.Perfil != caso.Perfil) throw new ValidacionException("Cambió el tipo de solicitud. Requiere revisión individual.");
        var etapa = new ServicioFechas(evento).ObtenerEtapa(instante);
        var idPlantilla = etapa == Etapa.Despues ? regla.PlantillaPosterior : regla.PlantillaGeneral;
        var plantilla = plantillas.Catalogo.SingleOrDefault(p => p.Id == idPlantilla)
            ?? throw new ValidacionException("No se encontró la plantilla general configurada.");
        if (!plantilla.PermiteLoteGeneral || plantilla.RepresentaAprobacionORechazo || plantilla.RequiereConsultaBaseDatos)
            throw new ValidacionException("Esta plantilla no está habilitada para el lote general.");
        var vista = generador.Generar(new()
        {
            Mensaje = mensaje, Plantilla = plantilla, Instante = instante, Idioma = plantilla.Idioma,
            Perfil = caso.Perfil, Etapa = etapa, EstadoRegistro = EstadoRegistro.NoEncontrado,
            Nombre = nombre
        });
        string contenido = JsonSerializer.Serialize(new[] { mensaje.Id, mensaje.Cuenta, mensaje.Remitente,
            mensaje.Asunto, mensaje.Cuerpo, vista.PlantillaId, vista.Asunto, vista.Html });
        var huella = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(contenido)));
        return new(caso.Id, caso.Version, mensaje, vista, huella, nombre);
    }

    public void ConfirmarGeneral(RevisionGeneral revision, bool casoGeneralVerificado, bool textoRevisado, DateTimeOffset instante)
    {
        if (!casoGeneralVerificado || !textoRevisado) throw new ValidacionException("Confirme el tipo de caso y revise la respuesta completa.");
        var actual = PrepararRevision(revision.CasoId, instante, revision.Nombre);
        if (actual.Version != revision.Version || actual.Huella != revision.Huella)
            throw new ValidacionException("La vista previa cambió. Vuelva a revisarla antes de confirmar.");
        var caso = ObtenerCaso(revision.CasoId);
        if (!repository.Actualizar(caso with { Estado = EstadoPreregistro.ConfirmadoGeneral,
            HuellaConfirmada = revision.Huella, CodigoIncidencia = null }, revision.Version))
            throw new ValidacionException("Otro proceso modificó el caso. Actualice la bandeja.");
        nombresSesion[revision.CasoId] = revision.Nombre;
    }

    public void ApartarParaRevision(string id)
    {
        var caso = ObtenerCaso(id);
        if (caso.Estado == EstadoPreregistro.Respondido || caso.OperacionId is not null)
            throw new ValidacionException("El caso tiene un borrador reservado o está respondido; no se reiniciará automáticamente.");
        if (!repository.Actualizar(caso with { Estado = EstadoPreregistro.RevisionIndividual, HuellaConfirmada = null }, caso.Version))
            throw new ValidacionException("El caso cambió. Actualice la bandeja.");
    }

    public ResultadoBorrador CrearBorradorConfirmado(string id, DateTimeOffset instante)
    {
        var caso = ObtenerCaso(id);
        if (caso.Estado != EstadoPreregistro.ConfirmadoGeneral) return new(false, "Omitido: el caso no está confirmado.");
        var revision = PrepararRevision(id, instante);
        if (revision.Huella != caso.HuellaConfirmada)
        {
            repository.Actualizar(caso with { Estado = EstadoPreregistro.Pendiente, HuellaConfirmada = null,
                CodigoIncidencia = "RESPUESTA_CAMBIO" }, caso.Version);
            return new(false, "La respuesta cambió; debe revisarse de nuevo.");
        }
        var reservado = caso with { Estado = EstadoPreregistro.CreandoBorrador, OperacionId = Guid.NewGuid().ToString("N") };
        if (!repository.Actualizar(reservado, caso.Version)) return new(false, "Omitido: otro proceso está atendiendo el caso.");
        reservado = reservado with { Version = caso.Version + 1 };
        try
        {
            // Se vuelve a comprobar el envío antes de crear, incluso si llegó después de la última actualización de pantalla.
            var envio = correo.ConsultarRespuestaEnviada(caso.MensajeId);
            if (envio is { Enviado: true } && envio.MensajeId == caso.MensajeId && MismaCuenta(envio.Cuenta)
                && !string.IsNullOrWhiteSpace(envio.RespuestaId) && envio.Fecha >= caso.Recibido)
            {
                repository.Actualizar(reservado with { Estado = EstadoPreregistro.Respondido,
                    Respondido = envio.Fecha, EvidenciaEnvioId = envio.RespuestaId }, reservado.Version);
                return new(false, "Omitido: ya se confirmó una respuesta enviada.");
            }
            var borrador = correo.CrearBorradorConClave(revision.Mensaje, revision.Vista, reservado.OperacionId!);
            if (borrador.MensajeId != caso.MensajeId) throw new InvalidOperationException();
            if (!repository.Actualizar(reservado with { Estado = EstadoPreregistro.BorradorCreado,
                BorradorId = borrador.Id.ToString() }, reservado.Version))
                return new(false, "Borrador creado; actualizar la bandeja para conciliar su estado. No repetir.");
            return new(true, "Borrador creado. Sigue pendiente de envío.");
        }
        catch
        {
            // Mantener la reserva: una excepción no prueba que el proveedor no haya creado el borrador.
            return new(false, "Resultado incierto. Actualice y revise el caso; no se reintentará automáticamente.");
        }
    }

    public ResultadoEnvio EnviarBorradorConfirmado(string id)
    {
        var caso = ObtenerCaso(id);
        if (caso.Estado != EstadoPreregistro.BorradorCreado || caso.OperacionId is null)
            return new(false, "Omitido: el caso no tiene un borrador confirmado pendiente.");
        if (correo is not IEnvioBandeja envio)
            return new(false, "El envío directo requiere Outlook clásico conectado.");
        var previo = correo.ConsultarRespuestaEnviada(caso.MensajeId);
        if (previo is { Enviado: true })
        {
            ActualizarAcumulado();
            return new(false, "Omitido: Outlook ya registra una respuesta enviada.");
        }
        var borrador = correo.BuscarBorrador(caso.OperacionId);
        if (borrador is null || borrador.MensajeId != caso.MensajeId)
            return new(false, "No se encontró el borrador esperado. Actualice y revise el caso.");
        envio.ValidarBorrador(caso.MensajeId, caso.OperacionId);
        if (!repository.Actualizar(caso with { Estado = EstadoPreregistro.EnvioSolicitado }, caso.Version))
            return new(false, "El caso cambió. Actualice la bandeja antes de enviar.");
        // No se reintenta una llamada Send con resultado incierto. La conciliación se hace en Enviados.
        envio.EnviarBorrador(caso.MensajeId, caso.OperacionId);
        return new(true, "Envío solicitado a Outlook. Confirme el estado en Enviados.");
    }
}

public interface IEnvioBandeja
{
    void ValidarBorrador(string mensajeId, string operacionId);
    void EnviarBorrador(string mensajeId, string operacionId);
}
