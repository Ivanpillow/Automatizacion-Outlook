using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CorreosOutlook.Domain;

public enum EstadoPreregistro { Pendiente, ConfirmadoGeneral, RevisionIndividual, CreandoBorrador, BorradorCreado, ErrorRevision, Respondido, EnvioSolicitado }
public sealed record ReglaPreregistro(string PrefijoAsunto, Perfil Perfil, string PlantillaGeneral, string PlantillaPosterior);
public sealed record ConfiguracionBandeja(int IntervaloActualizacionSegundos, ReglaPreregistro[] Reglas);
public sealed record Preregistro(string Id, string Origen, string EventoId, string MensajeId, DateTimeOffset Recibido,
    Perfil Perfil, EstadoPreregistro Estado = EstadoPreregistro.Pendiente, int Version = 0,
    string? HuellaConfirmada = null, string? OperacionId = null, string? BorradorId = null,
    DateTimeOffset? Respondido = null, string? EvidenciaEnvioId = null, string? CodigoIncidencia = null);
public sealed record EvidenciaEnvio(string MensajeId, string Cuenta, string RespuestaId, DateTimeOffset Fecha, bool Enviado);

public sealed class DetectorPreregistro(ConfiguracionBandeja configuracion)
{
    public ReglaPreregistro? Detectar(string asunto)
    {
        string valor = Normalizar(asunto);
        return configuracion.Reglas.FirstOrDefault(r =>
        {
            var prefijo = Normalizar(r.PrefijoAsunto);
            return valor == prefijo || (valor.StartsWith(prefijo, StringComparison.Ordinal)
                && valor.Length > prefijo.Length && (char.IsWhiteSpace(valor[prefijo.Length]) || ":-([{·".Contains(valor[prefijo.Length])));
        });
    }

    private static string Normalizar(string text)
    {
        var sinAcentos = new string(text.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
        sinAcentos = sinAcentos.Replace('–', '-').Replace('—', '-').Replace('‑', '-');
        sinAcentos = Regex.Replace(sinAcentos, @"\s*-\s*", "-", RegexOptions.None, TimeSpan.FromSeconds(1));
        return Regex.Replace(sinAcentos.Trim().ToUpperInvariant(), @"\s+", " ", RegexOptions.None, TimeSpan.FromSeconds(1));
    }
}
