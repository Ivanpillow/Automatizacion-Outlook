using CorreosOutlook.Domain;

namespace CorreosOutlook.Application;

public sealed record Sugerencia(string Idioma, string Nombre, Perfil Perfil, Etapa Etapa, string PlantillaId, string Explicacion);
public sealed class AnalizadorSimulado(ServicioFechas fechas)
{
    public Sugerencia Analizar(Mensaje mensaje, DateTimeOffset instante)
    {
        string contenido = (mensaje.Asunto + " " + mensaje.Cuerpo).ToLowerInvariant();
        string idioma = contenido.Contains("hello") || contenido.Contains("information") ? "en" : "es";
        var etapa = fechas.ObtenerEtapa(instante);
        var perfil = contenido.Contains("comprador") ? Perfil.Comprador : Perfil.Otro;
        string plantilla = etapa == Etapa.Despues ? (idioma == "en" ? "posterior-en" : "posterior-evento")
            : perfil == Perfil.Comprador && idioma == "es" ? "comprador-requisitos"
            : idioma == "en" ? "informacion-general-en" : "informacion-general";
        return new(idioma, mensaje.NombreRemitente, perfil, etapa, plantilla,
            "Sugerencia por palabras clave y fecha. Nombre tomado del remitente ficticio; revise y corrija los datos.");
    }
}
