using CorreosOutlook.Application;
using CorreosOutlook.Domain;
using CorreosOutlook.Infrastructure;
using Microsoft.Data.Sqlite;
using Xunit;

namespace CorreosOutlook.Tests;

public class IntegracionTests
{
    [Fact]
    public void VariableObligatoriaVaciaBloqueaVista() => Assert.Throws<ValidacionException>(()=>
        Fixtures.Generador().Generar(Fixtures.Solicitud() with { Nombre=" " }));
    [Fact]
    public void VariableDesconocidaBloqueaRender() => Assert.Throws<ValidacionException>(()=>
        new MotorScriban().Render("Hola {{ inexistente }}",new Dictionary<string,object?>()));
    [Fact]
    public void SintaxisIncorrectaBloqueaRender() => Assert.Throws<ValidacionException>(()=>
        new MotorScriban().Render("{{ if nombre }}",new Dictionary<string,object?>()));
    [Fact]
    public void HtmlSeEscapaYVariablesNoSeReevaluan()
    {
        var vista=Fixtures.Generador().Generar(Fixtures.Solicitud() with { Nombre="<script>alert(1)</script>{{ secreto }}" });
        Assert.DoesNotContain("<script>",vista.Html); Assert.Contains("&lt;script&gt;",vista.Html);
        Assert.Contains("{{ secreto }}",vista.Texto);
    }
    [Fact]
    public void ConfiguracionRechazaIdentificacionOpcional() => Assert.Throws<ValidacionException>(()=>
        ConfiguracionJson.Validar(Fixtures.Config.Evento,Fixtures.Config.Politicas with {
            Documentos=Fixtures.Config.Politicas.Documentos with { IdentificacionObligatoria=false } }));
    [Fact]
    public void CatalogoContieneLosCasosYSoloHabilitaArchivosDisponibles()
    {
        var repo=Fixtures.Plantillas(); Assert.Equal(29,repo.Catalogo.Count);
        Assert.All(repo.Catalogo.Where(p=>p.Disponible),p=>Assert.NotEmpty(repo.LeerCuerpo(p)));
        Assert.All(repo.Catalogo.Where(p=>p.RepresentaAprobacionORechazo),p=>Assert.False(p.Disponible));
    }
    [Fact]
    public void TodasLasPlantillasDisponiblesGeneranConDatosValidos()
    {
        foreach(var p in Fixtures.Plantillas().Catalogo.Where(p=>p.Disponible))
        {
            int dia=p.EtapasPermitidas.Contains(Etapa.Previo)?18:29;
            var s=Fixtures.Solicitud(p.Id,dia) with { Idioma=p.Idioma, Perfil=p.Perfil, EstadoRegistro=EstadoRegistro.AcompananteRegistrado,
                Titular="Titular Ficticio", AcompanantesSolicitados=1, NombresAcompanantes=["Acompañante Ficticio"],
                DatosConfirmadosManualmente=true, RespuestaManual="Respuesta ficticia para revisión." };
            Assert.NotEmpty(Fixtures.Generador().Generar(s).Texto);
        }
    }
    [Fact]
    public void CambioDeConfiguracionSeReflejaSinEditarPlantilla()
    {
        var config=Fixtures.Config.Evento with { Actual=Fixtures.Config.Evento.Actual with { Nombre="Evento de prueba",Recinto="Recinto ficticio" } };
        var g=new GeneradorRespuesta(config,Fixtures.Config.Politicas,new MotorScriban(),Fixtures.Plantillas());
        var v=g.Generar(Fixtures.Solicitud("informacion-general"));
        Assert.Contains("Evento de prueba",v.Texto); Assert.Contains("Recinto ficticio",v.Texto);
    }
    [Fact]
    public void RechazaEtapaIncoherente() => Assert.Throws<ValidacionException>(()=>
        Fixtures.Generador().Generar(Fixtures.Solicitud() with { Etapa=Etapa.Durante }));
    [Fact]
    public void RechazaIdiomaIncoherente() => Assert.Throws<ValidacionException>(()=>
        Fixtures.Generador().Generar(Fixtures.Solicitud() with { Idioma="en" }));
    [Fact]
    public void BorradorRequiereRevisionYCuentaCorrecta()
    {
        var outlook=new OutlookSimulado(Fixtures.Config.Evento.Cuenta); var mensaje=outlook.ObtenerMensajesDisponibles()[0];
        var vista=Fixtures.Generador().Generar(Fixtures.Solicitud() with { Mensaje=mensaje });
        Assert.Throws<ValidacionException>(()=>outlook.CrearBorrador(mensaje,vista,false));
        Assert.Throws<ValidacionException>(()=>outlook.CrearBorrador(mensaje with { Cuenta="otra@example.invalid" },vista,true));
        Assert.Throws<ValidacionException>(()=>outlook.CrearBorrador(mensaje,vista with { MensajeId="otro" },true));
        Assert.Empty(outlook.Borradores);
        Assert.True(outlook.CrearBorrador(mensaje,vista,true).Simulado); Assert.Single(outlook.Borradores);
    }
    [Fact]
    public void RepositoriosSonSimulados()
    {
        Assert.Equal(EstadoRegistro.AcompananteRegistrado,new RegistroExpoSimulado().BuscarPorId("demo-002")!.Estado);
        Assert.Null(new RegistroExpoSimulado().BuscarPorId("no-existe"));
        Assert.All(new CarpetasSimuladas(Path.Combine(AppContext.BaseDirectory,"config","carpetas.example.json")).ObtenerCarpetas(),c=>Assert.True(c.Simulada));
    }
    [Fact]
    public void HistorialPersisteMetadatosSinMarcarComoRespondido()
    {
        var path=Path.Combine(Path.GetTempPath(),"ExpoJoyaTests",Guid.NewGuid()+".db");
        try
        {
            var repo=new HistorialSqlite(path);
            repo.Registrar(new(Guid.NewGuid(),null,"evento-ficticio",Fixtures.Fecha(18),null,"es",Perfil.Comprador,
                "consulta","informacion-general","BorradorSimulado","PendienteRevision",null,true,0,false));
            Assert.Equal(1,new HistorialSqlite(path).Contar());
            using var connection=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=path,Pooling=false}.ToString()); connection.Open();
            using var command=connection.CreateCommand(); command.CommandText="SELECT fecha_respondida FROM operaciones";
            Assert.Equal(DBNull.Value,command.ExecuteScalar());
            command.CommandText="PRAGMA table_info(operaciones)";
            using var reader=command.ExecuteReader(); var columnas=new List<string>();
            while(reader.Read()) columnas.Add(reader.GetString(1));
            Assert.DoesNotContain("cuerpo",columnas); Assert.DoesNotContain("nombre",columnas); Assert.DoesNotContain("correo",columnas);
        }
        finally { if(File.Exists(path)) File.Delete(path); }
    }
}
