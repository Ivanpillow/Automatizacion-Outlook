using CorreosOutlook.Domain;
using Xunit;

namespace CorreosOutlook.Tests;

public class FechasTests
{
    [Fact]
    public void LimiteConfiguradoEsMartes19Enero2027Mediodia()
    {
        var limite = Fixtures.Config.Evento.Actual.LimiteDocumentacionLocal!.Value;
        Assert.Equal(new DateTime(2027, 1, 19, 12, 0, 0), limite);
        Assert.Equal(DayOfWeek.Tuesday, limite.DayOfWeek);
    }
    [Theory]
    [InlineData(19,11,59,59, PlazoDocumental.AntesDelLimite)]
    [InlineData(19,12,0,0, PlazoDocumental.LimiteFinalizado)]
    [InlineData(19,12,0,1, PlazoDocumental.LimiteFinalizado)]
    [InlineData(25,23,59,59, PlazoDocumental.LimiteFinalizado)]
    [InlineData(26,0,0,0, PlazoDocumental.DuranteEvento)]
    [InlineData(28,23,59,59, PlazoDocumental.DuranteEvento)]
    [InlineData(29,0,0,0, PlazoDocumental.DespuesEvento)]
    public void FronterasTemporales(int dia, int hora, int minuto, int segundo, PlazoDocumental esperado) =>
        Assert.Equal(esperado, new ServicioFechas(Fixtures.Config.Evento).ObtenerPlazo(Fixtures.Fecha(dia,hora,minuto,segundo)));

    [Fact]
    public void UsaZonaDelEventoAunqueElInstanteEsteEnUtc() =>
        Assert.Equal(PlazoDocumental.LimiteFinalizado, new ServicioFechas(Fixtures.Config.Evento)
            .ObtenerPlazo(new DateTimeOffset(2027,1,19,18,0,0,TimeSpan.Zero)));

    [Theory]
    [InlineData(18,"martes 19 de enero de 2027 antes de las 12:00")]
    [InlineData(20,"La recepción de documentos por correo electrónico ha finalizado")]
    [InlineData(26,"La autorización estará sujeta a la revisión presencial")]
    public void TextoCambiaSegunElPlazo(int dia,string texto) =>
        Assert.Contains(texto, Fixtures.Generador().Generar(Fixtures.Solicitud(dia:dia)).Texto);

    [Fact]
    public void DespuesDelEventoInvitaAlSiguienteSinInventarFechas()
    {
        var vista = Fixtures.Generador().Generar(Fixtures.Solicitud("posterior-evento",29));
        Assert.Contains(Fixtures.Config.Evento.Siguiente.Nombre, vista.Texto);
        Assert.Contains("fechas exactas están pendientes", vista.Texto);
        Assert.DoesNotContain("Deberá presentar", vista.Texto);
    }
}
