using CorreosOutlook.Application;
using CorreosOutlook.Domain;
using CorreosOutlook.Infrastructure;

namespace CorreosOutlook.Tests;

internal static class Fixtures
{
    internal static readonly (ConfiguracionEvento Evento, Politicas Politicas) Config = ConfiguracionJson.Cargar(Path.Combine(AppContext.BaseDirectory, "config"));
    internal static PlantillasJson Plantillas() => new(Path.Combine(AppContext.BaseDirectory, "templates"));
    internal static GeneradorRespuesta Generador() => new(Config.Evento, Config.Politicas, new MotorScriban(), Plantillas());
    internal static DateTimeOffset Fecha(int dia, int hora = 10, int minuto = 0, int segundo = 0) => new(2027, 1, dia, hora, minuto, segundo, TimeSpan.FromHours(-6));
    internal static SolicitudRespuesta Solicitud(string plantilla = "comprador-requisitos", int dia = 18) => new()
    {
        Mensaje = new OutlookSimulado(Config.Evento.Cuenta).ObtenerMensajesDisponibles()[0],
        Plantilla = Plantillas().Catalogo.Single(p => p.Id == plantilla),
        Instante = Fecha(dia), Idioma = "es", Perfil = Perfil.Comprador,
        Etapa = new ServicioFechas(Config.Evento).ObtenerEtapa(Fecha(dia)),
        Nombre = "Persona Prueba (ficticia)"
    };
}
