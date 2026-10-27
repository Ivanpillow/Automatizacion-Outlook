using System.Globalization;
using System.Net;
using CorreosOutlook.Domain;

namespace CorreosOutlook.Application;

public sealed class GeneradorRespuesta(ConfiguracionEvento config, Politicas politicas,
    ITemplateEngine motor, IPlantillasRepository plantillas)
{
    public VistaPrevia Generar(SolicitudRespuesta solicitud)
    {
        var p = solicitud.Plantilla;
        if (!p.Disponible) throw new ValidacionException("La plantilla todavía está pendiente de redacción.");
        if (!p.EtapasPermitidas.Contains(solicitud.Etapa))
            throw new ValidacionException("La plantilla no corresponde a la etapa seleccionada.");
        if (p.Idioma != solicitud.Idioma) throw new ValidacionException("El idioma y la plantilla deben coincidir.");
        if (p.Perfil != Perfil.Otro && p.Perfil != solicitud.Perfil)
            throw new ValidacionException("El perfil y la plantilla deben coincidir.");
        var fechas = new ServicioFechas(config);
        if (solicitud.Etapa != fechas.ObtenerEtapa(solicitud.Instante))
            throw new ValidacionException("La etapa no coincide con la fecha de evaluación. Corrija la fecha o la etapa.");
        if (!string.Equals(solicitud.Mensaje.Cuenta, config.Cuenta, StringComparison.OrdinalIgnoreCase))
            throw new ValidacionException("El mensaje no pertenece a la cuenta configurada.");
        var cupo = new ReglasAcompanantes(politicas.Acompanantes).Evaluar(
            solicitud.AcompanantesRegistrados, solicitud.AcompanantesSolicitados);
        if (!cupo.EsValido) throw new ValidacionException(cupo.Error!);
        if (p.Id == "confirmacion-acompanantes")
        {
            if (!solicitud.DatosConfirmadosManualmente || cupo.Total == 0
                || solicitud.NombresAcompanantes.Length != cupo.Total
                || solicitud.NombresAcompanantes.Any(string.IsNullOrWhiteSpace)
                || solicitud.NombresAcompanantes.Distinct(StringComparer.OrdinalIgnoreCase).Count() != cupo.Total)
                throw new ValidacionException("Confirme manualmente los datos e indique un nombre distinto por cada acompañante del total.");
        }
        if (p.Id == "acompanante-encontrado" && solicitud.EstadoRegistro != EstadoRegistro.AcompananteRegistrado)
            throw new ValidacionException("Este caso requiere el estado Acompañante registrado.");

        var cultura = CultureInfo.GetCultureInfo(solicitud.Idioma == "es" ? "es-MX" : "en-US");
        string Fecha(DateOnly? fecha) => fecha?.ToString(solicitud.Idioma == "es"
            ? "dddd d 'de' MMMM 'de' yyyy" : "dddd, MMMM d, yyyy", cultura) ?? "";
        var textos = politicas.Textos[solicitud.Idioma];
        var v = new Dictionary<string, object?>
        {
            ["nombre"] = solicitud.Nombre.Trim(), ["titular"] = solicitud.Titular.Trim(),
            ["folio"] = solicitud.Folio.Trim(), ["observaciones"] = solicitud.Observaciones.Trim(),
            ["respuesta_manual"] = solicitud.RespuestaManual.Trim(),
            ["evento"] = config.Actual.Nombre, ["inicio"] = Fecha(config.Actual.Inicio), ["fin"] = Fecha(config.Actual.Fin),
            ["recinto"] = config.Actual.Recinto, ["ciudad"] = config.Actual.Ciudad, ["estado"] = config.Actual.Estado,
            ["correo"] = config.Cuenta, ["registro_url"] = config.RegistroCompradorUrl,
            ["fecha_limite"] = Fecha(DateOnly.FromDateTime(config.Actual.LimiteDocumentacionLocal!.Value)),
            ["hora_limite"] = config.Actual.LimiteDocumentacionLocal.Value.ToString("HH:mm", cultura),
            ["siguiente_evento"] = config.Siguiente.Nombre, ["siguiente_recinto"] = config.Siguiente.Recinto,
            ["siguiente_ciudad"] = config.Siguiente.Ciudad,
            ["siguientes_fechas"] = config.Siguiente.Inicio.HasValue && config.Siguiente.Fin.HasValue
                ? $"{Fecha(config.Siguiente.Inicio)} – {Fecha(config.Siguiente.Fin)}" : textos["fechas_pendientes"],
            ["antiguedad_meses"] = politicas.Documentos.AntiguedadMaximaMeses,
            ["comprador_minimo"] = politicas.Documentos.CompradorMinimoMxn.ToString("C0", cultura),
            ["maximo_acompanantes"] = politicas.Acompanantes.Maximo, ["edad_minima"] = politicas.Acompanantes.EdadMinima,
            ["identificaciones_menores"] = string.Join(", ", politicas.Acompanantes.IdentificacionesMenores),
            ["disponibles"] = cupo.Disponibles, ["total_acompanantes"] = cupo.Total,
            ["nombres_acompanantes"] = string.Join(", ", solicitud.NombresAcompanantes),
            ["accion"] = solicitud.AccionAcompanante.ToString()
        };
        v["texto_documentacion"] = motor.Render(textos[fechas.ObtenerPlazo(solicitud.Instante).ToString()], v);
        v["requisitos_comprador"] = motor.Render(textos["requisitos_comprador"], v);
        v["requisitos_emprendedor"] = motor.Render(textos["requisitos_emprendedor"], v);
        v["acompanantes_reglas"] = motor.Render(textos["acompanantes_reglas"], v);
        v["texto_cupo"] = motor.Render(textos[cupo.Disponibles == 0 ? "cupo_lleno" : cupo.Disponibles == 1 ? "cupo_uno" : "cupo_varios"], v);
        var faltantes = p.VariablesObligatorias.Where(x => !v.TryGetValue(x, out var value)
            || value is null || (value is string s && string.IsNullOrWhiteSpace(s))).ToArray();
        if (faltantes.Length > 0) throw new ValidacionException("Faltan variables obligatorias: " + string.Join(", ", faltantes));
        var asunto = motor.Render(p.Asunto, v).Trim();
        var texto = motor.Render(plantillas.LeerCuerpo(p), v).Trim();
        if (string.IsNullOrWhiteSpace(texto) || string.IsNullOrWhiteSpace(asunto))
            throw new ValidacionException("La respuesta no puede estar vacía.");
        var parrafos = texto.Replace("\r\n", "\n").Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(parrafo => "<p style=\"margin:0 0 18px;line-height:1.65\">"
                + WebUtility.HtmlEncode(parrafo.Trim()).Replace("\n", "<br>") + "</p>");
        var html = "<html><body style=\"margin:0;padding:24px;background:#f6f8fb\">"
            + "<div style=\"max-width:680px;margin:auto;padding:32px;background:#ffffff;"
            + "border:1px solid #e1e9f0;border-top:5px solid #266290;border-radius:12px;"
            + "font:16px Arial,sans-serif;color:#223041\">"
            + string.Concat(parrafos) + "</div></body></html>";
        return new(solicitud.Mensaje.Id, p.Id, asunto, texto, html, p.PuedeCrearBorrador, true);
    }
}
