using System.Text.Json;
using System.Text.Json.Serialization;
using CorreosOutlook.Application;
using CorreosOutlook.Domain;

namespace CorreosOutlook.Infrastructure;

public static class ConfiguracionJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };
    public static T Leer<T>(string path) => JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options)
        ?? throw new ValidacionException("El archivo de configuración está vacío.");

    public static (ConfiguracionEvento Evento, Politicas Politicas) Cargar(string directorio)
    {
        var evento = Leer<ConfiguracionEvento>(Path.Combine(directorio, "evento.json"));
        var politicas = Leer<Politicas>(Path.Combine(directorio, "politicas.json"));
        Validar(evento, politicas);
        return (evento, politicas);
    }

    public static ConfiguracionBandeja CargarBandeja(string archivo)
    {
        var config = Leer<ConfiguracionBandeja>(archivo);
        if (config.IntervaloActualizacionSegundos < 5 || config.IntervaloActualizacionSegundos > 3600
            || config.Reglas.Length == 0 || config.Reglas.Any(r => string.IsNullOrWhiteSpace(r.PrefijoAsunto)
                || string.IsNullOrWhiteSpace(r.PlantillaGeneral) || string.IsNullOrWhiteSpace(r.PlantillaPosterior)
                || r.Perfil is not (Perfil.Comprador or Perfil.Emprendedor)))
            throw new ValidacionException("Revise los prefijos, plantillas y frecuencia de la bandeja.");
        return config;
    }

    public static void Validar(ConfiguracionEvento e, Politicas p)
    {
        if (!System.Net.Mail.MailAddress.TryCreate(e.Cuenta, out _)
            || !Uri.TryCreate(e.RegistroCompradorUrl, UriKind.Absolute, out var url) || url.Scheme != "https"
            || e.Actual.Inicio is null || e.Actual.Fin is null || e.Actual.Inicio > e.Actual.Fin
            || e.Actual.LimiteDocumentacionLocal is null
            || e.Actual.LimiteDocumentacionLocal.Value.Kind != DateTimeKind.Unspecified
            || DateOnly.FromDateTime(e.Actual.LimiteDocumentacionLocal.Value) >= e.Actual.Inicio)
            throw new ValidacionException("Revise cuenta, URL HTTPS, fechas y límite local del evento.");
        _ = TimeZoneInfo.FindSystemTimeZoneById(e.ZonaHoraria);
        foreach (var evento in new[] { e.Actual, e.Siguiente })
        {
            if (new[] { evento.Id, evento.Nombre, evento.Recinto, evento.Ciudad, evento.Estado }.Any(string.IsNullOrWhiteSpace)
                || evento.Inicio.HasValue != evento.Fin.HasValue || evento.Inicio > evento.Fin)
                throw new ValidacionException("Complete los datos del evento; las fechas pendientes deben quedar ambas vacías.");
        }
        if (!p.Documentos.IdentificacionObligatoria || p.Documentos.AntiguedadMaximaMeses <= 0
            || p.Documentos.CompradorMinimoMxn <= 0 || p.Documentos.EmprendedorMinimoMxn is not null
            || p.Documentos.EvidenciasAdicionalesEmprendedor.Length == 0
            || p.Acompanantes.Maximo <= 0 || p.Acompanantes.EdadMinima < 0
            || p.Acompanantes.IdentificacionesMenores.Length == 0 || p.Internacional.FacturaMinimaUsd <= 0)
            throw new ValidacionException("Políticas inválidas. La identificación es obligatoria; no se define un mínimo monetario independiente para emprendedores.");
        // Confirmar una política pendiente exige implementar y probar su interpretación, no sólo cambiar un booleano.
        if (p.Internacional.PoliticaConfirmada || p.Internacional.ModalidadRequisitos is not null)
            throw new ValidacionException("La política internacional confirmada requiere una fase posterior de implementación.");
        string[] claves = ["AntesDelLimite", "LimiteFinalizado", "DuranteEvento", "DespuesEvento", "fechas_pendientes",
            "requisitos_comprador", "requisitos_emprendedor", "acompanantes_reglas", "cupo_uno", "cupo_lleno", "cupo_varios"];
        foreach (var idioma in new[] { "es", "en" })
            if (!p.Textos.TryGetValue(idioma, out var textos) || claves.Any(k => !textos.TryGetValue(k, out var t) || string.IsNullOrWhiteSpace(t)))
                throw new ValidacionException("Faltan textos comunes de políticas en español o inglés.");
    }
}

public sealed class PlantillasJson : IPlantillasRepository
{
    private readonly string directorio;
    public IReadOnlyList<Plantilla> Catalogo { get; }
    public PlantillasJson(string directorio)
    {
        this.directorio = Path.GetFullPath(directorio);
        Catalogo = ConfiguracionJson.Leer<Plantilla[]>(Path.Combine(directorio, "catalogo.json"));
        if (Catalogo.Select(p => p.Id).Distinct().Count() != Catalogo.Count)
            throw new ValidacionException("El catálogo tiene identificadores duplicados.");
        foreach (var p in Catalogo.Where(p => p.Disponible))
        {
            if (p.Idioma is not ("es" or "en") || p.EtapasPermitidas.Length == 0
                || string.IsNullOrWhiteSpace(p.Asunto) || string.IsNullOrWhiteSpace(p.Nombre))
                throw new ValidacionException("Metadatos de plantilla incompletos.");
            _ = LeerCuerpo(p);
        }
    }
    public string LeerCuerpo(Plantilla plantilla)
    {
        if (string.IsNullOrWhiteSpace(plantilla.ArchivoCuerpo)) throw new ValidacionException("Plantilla sin archivo de cuerpo.");
        var archivo = Path.GetFullPath(Path.Combine(directorio, plantilla.ArchivoCuerpo));
        if (!archivo.StartsWith(directorio + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new ValidacionException("El cuerpo debe permanecer en la carpeta de plantillas.");
        return File.ReadAllText(archivo);
    }
}
