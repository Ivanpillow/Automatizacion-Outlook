using CorreosOutlook.Application;
using CorreosOutlook.Domain;
using CorreosOutlook.Infrastructure;
using Xunit;

namespace CorreosOutlook.Tests;

public class BandejaPreregistrosTests
{
    private static ConfiguracionBandeja Config() => ConfiguracionJson.CargarBandeja(Path.Combine(AppContext.BaseDirectory,"config","bandeja.json"));
    private static (BandejaPreregistros Servicio, OutlookSimulado Correo, PreregistroSqlite Repo, string Archivo) Preparar()
    {
        var archivo=Path.Combine(Path.GetTempPath(),"ExpoJoyaTests",Guid.NewGuid()+".db");
        var correo=new OutlookSimulado(Fixtures.Config.Evento.Cuenta);
        correo.AgregarMensajeFicticio(new("prueba-comprador",Fixtures.Config.Evento.Cuenta,"cliente@example.invalid","Persona Ficticia",
            "Pre-registro Comprador",Fixtures.Fecha(18),"Texto ficticio",false));
        var repo=new PreregistroSqlite(archivo);
        var servicio=new BandejaPreregistros(Fixtures.Config.Evento,Config(),repo,correo,Fixtures.Generador(),Fixtures.Plantillas());
        return (servicio,correo,repo,archivo);
    }
    [Theory]
    [InlineData("Pre-registro Comprador",Perfil.Comprador)]
    [InlineData("PRE–REGISTRO comprador: lote",Perfil.Comprador)]
    [InlineData("Pre-registro Emprendedor Expo Joya",Perfil.Emprendedor)]
    [InlineData("Pre-registro Emprendedor Expo Joya - consulta",Perfil.Emprendedor)]
    public void ClasificaAsuntosConfigurados(string asunto,Perfil esperado) =>
        Assert.Equal(esperado,new DetectorPreregistro(Config()).Detectar(asunto)!.Perfil);
    [Theory]
    [InlineData("RE: Pre-registro Comprador")]
    [InlineData("FW: Pre-registro Emprendedor Expo Joya")]
    [InlineData("Consulta Comprador")]
    [InlineData("Pre-registro Compradores")]
    public void NoIncluyeOtrosAsuntos(string asunto) => Assert.Null(new DetectorPreregistro(Config()).Detectar(asunto));
    [Fact]
    public void AcumuladoEsPersistenteYDistingueCadaMensaje()
    {
        var (servicio,correo,repo,archivo)=Preparar();
        try
        {
            Assert.Equal(1,servicio.ActualizarAcumulado());
            Assert.Equal(0,servicio.ActualizarAcumulado());
            correo.AgregarMensajeFicticio(new("prueba-segundo",Fixtures.Config.Evento.Cuenta,"cliente@example.invalid","Otra Persona Ficticia",
                "Pre-registro Comprador",Fixtures.Fecha(18),"Otro texto ficticio",false));
            Assert.Equal(1,servicio.ActualizarAcumulado());
            Assert.Equal(2,new PreregistroSqlite(archivo).Listar(correo.Origen,Fixtures.Config.Evento.Actual.Id).Count);
        }
        finally { File.Delete(archivo); }
    }
    [Fact]
    public void BorradorNoSaleDelAcumuladoYSegundoLoteNoDuplica()
    {
        var (servicio,correo,repo,archivo)=Preparar();
        try
        {
            servicio.ActualizarAcumulado(); var caso=servicio.Listar()[0];
            var revision=servicio.PrepararRevision(caso.Id,Fixtures.Fecha(18));
            Assert.Contains("Persona Ficticia",revision.Vista.Texto);
            Assert.Empty(correo.Borradores);
            servicio.ConfirmarGeneral(revision,true,true,Fixtures.Fecha(18));
            Assert.True(servicio.CrearBorradorConfirmado(caso.Id,Fixtures.Fecha(18)).Creado);
            Assert.False(servicio.CrearBorradorConfirmado(caso.Id,Fixtures.Fecha(18)).Creado);
            Assert.Single(correo.Borradores);
            Assert.Equal(EstadoPreregistro.BorradorCreado,servicio.Listar()[0].Estado);
            Assert.Null(servicio.Listar()[0].Respondido);
            Assert.Equal(EstadoPreregistro.BorradorCreado,new PreregistroSqlite(archivo).Listar(correo.Origen,Fixtures.Config.Evento.Actual.Id)[0].Estado);
        }
        finally { File.Delete(archivo); }
    }
    [Fact]
    public void RespuestaEnviadaVerificadaSaleDePendientesPeroQuedaEnHistorial()
    {
        var (servicio,correo,repo,archivo)=Preparar();
        try
        {
            servicio.ActualizarAcumulado(); var caso=servicio.Listar()[0];
            var revision=servicio.PrepararRevision(caso.Id,Fixtures.Fecha(18));
            servicio.ConfirmarGeneral(revision,true,true,Fixtures.Fecha(18));
            Assert.True(servicio.CrearBorradorConfirmado(caso.Id,Fixtures.Fecha(18)).Creado);
            correo.SimularRespuestaEnviada(caso.MensajeId, caso.Recibido.AddMinutes(1));
            servicio.ActualizarAcumulado();
            Assert.Equal(EstadoPreregistro.Respondido,servicio.Listar()[0].Estado);
            Assert.NotNull(servicio.Listar()[0].EvidenciaEnvioId);
            Assert.Equal(0,servicio.ActualizarAcumulado());
            Assert.Equal(EstadoPreregistro.Respondido,repo.Listar(correo.Origen,Fixtures.Config.Evento.Actual.Id)[0].Estado);
        }
        finally { File.Delete(archivo); }
    }
    [Fact]
    public void CambiarPlantillaODestinatarioInvalidaConfirmacion()
    {
        var (servicio,correo,repo,archivo)=Preparar();
        try
        {
            servicio.ActualizarAcumulado(); var caso=servicio.Listar()[0];
            var revision=servicio.PrepararRevision(caso.Id,Fixtures.Fecha(18));
            servicio.ConfirmarGeneral(revision,true,true,Fixtures.Fecha(18));
            var mensaje=correo.ObtenerMensajesDisponibles().Single(m=>m.Id==caso.MensajeId);
            // Un nuevo origen con igual clave/ID pero texto diferente obliga a revisar de nuevo.
            var otro=new CorreoMutable(correo.Origen,mensaje with { NombreRemitente="Nombre Corregido" });
            var servicioNuevo=new BandejaPreregistros(Fixtures.Config.Evento,Config(),repo,otro,Fixtures.Generador(),Fixtures.Plantillas());
            Assert.False(servicioNuevo.CrearBorradorConfirmado(caso.Id,Fixtures.Fecha(18)).Creado);
            Assert.Equal(EstadoPreregistro.Pendiente,repo.Obtener(caso.Id)!.Estado);
        }
        finally { File.Delete(archivo); }
    }
    [Fact]
    public void RevisionIndividualNoEntraAlLoteNiSePierde()
    {
        var (servicio,correo,repo,archivo)=Preparar();
        try
        {
            servicio.ActualizarAcumulado(); var caso=servicio.Listar()[0];
            servicio.ApartarParaRevision(caso.Id);
            Assert.False(servicio.CrearBorradorConfirmado(caso.Id,Fixtures.Fecha(18)).Creado);
            Assert.Equal(EstadoPreregistro.RevisionIndividual,servicio.Listar()[0].Estado);
        }
        finally { File.Delete(archivo); }
    }
    [Fact]
    public void SinConfirmacionHumanaNoSePreparaLote()
    {
        var (servicio,correo,repo,archivo)=Preparar();
        try
        {
            servicio.ActualizarAcumulado(); var revision=servicio.PrepararRevision(servicio.Listar()[0].Id,Fixtures.Fecha(18));
            Assert.Throws<ValidacionException>(()=>servicio.ConfirmarGeneral(revision,false,true,Fixtures.Fecha(18)));
            Assert.Throws<ValidacionException>(()=>servicio.ConfirmarGeneral(revision,true,false,Fixtures.Fecha(18)));
            Assert.Empty(correo.Borradores);
        }
        finally { File.Delete(archivo); }
    }
    [Fact]
    public void ConcurrenciaNoPermiteReservarDosBorradores()
    {
        var (servicio,correo,repo,archivo)=Preparar();
        try
        {
            servicio.ActualizarAcumulado(); var caso=servicio.Listar()[0];
            var copia=repo.Obtener(caso.Id)!;
            Assert.True(repo.Actualizar(caso with { Estado=EstadoPreregistro.ConfirmadoGeneral },caso.Version));
            Assert.False(repo.Actualizar(copia with { Estado=EstadoPreregistro.ConfirmadoGeneral },copia.Version));
        }
        finally { File.Delete(archivo); }
    }
    [Theory]
    [InlineData("Nombre completo: Nombre de Registro\r\n", "Nombre de Registro")]
    [InlineData("Name: Test Person\n", "Test Person")]
    [InlineData("Sin campo reconocido", "Nombre Remitente")]
    public void SugiereNombreSinDependerSiempreDelRemitente(string cuerpo,string esperado)
    {
        var mensaje=new Mensaje("ficticio",Fixtures.Config.Evento.Cuenta,"cliente@example.invalid","Nombre Remitente",
            "Pre-registro Comprador",Fixtures.Fecha(18),cuerpo,false);
        Assert.Equal(esperado,BandejaPreregistros.SugerirNombre(mensaje));
    }
    [Fact]
    public void NombreCorregidoActualizaRespuestaYNoSeGuardaEnBase()
    {
        var (servicio,correo,repo,archivo)=Preparar();
        try
        {
            servicio.ActualizarAcumulado(); var caso=servicio.Listar()[0];
            var revision=servicio.PrepararRevision(caso.Id,Fixtures.Fecha(18),"Nombre Ficticio Corregido");
            Assert.Contains("Nombre Ficticio Corregido",revision.Vista.Texto);
            servicio.ConfirmarGeneral(revision,true,true,Fixtures.Fecha(18));
            Assert.True(servicio.CrearBorradorConfirmado(caso.Id,Fixtures.Fecha(18)).Creado);
            Assert.DoesNotContain("Nombre Ficticio Corregido",File.ReadAllText(archivo));
        }
        finally { File.Delete(archivo); }
    }
    [Fact]
    public void NombreCorregidoExigeNuevaRevisionSiSeReiniciaAntesDelLote()
    {
        var (servicio,correo,repo,archivo)=Preparar();
        try
        {
            servicio.ActualizarAcumulado(); var caso=servicio.Listar()[0];
            var revision=servicio.PrepararRevision(caso.Id,Fixtures.Fecha(18),"Nombre Ficticio Corregido");
            servicio.ConfirmarGeneral(revision,true,true,Fixtures.Fecha(18));
            var nuevo=new BandejaPreregistros(Fixtures.Config.Evento,Config(),new PreregistroSqlite(archivo),correo,Fixtures.Generador(),Fixtures.Plantillas());
            Assert.False(nuevo.CrearBorradorConfirmado(caso.Id,Fixtures.Fecha(18)).Creado);
            Assert.Equal(EstadoPreregistro.Pendiente,repo.Obtener(caso.Id)!.Estado);
        }
        finally { File.Delete(archivo); }
    }
    [Fact]
    public void BorradorNoLocalizableExigeRevisionSinRecrear()
    {
        var (servicio,correo,repo,archivo)=Preparar();
        try
        {
            servicio.ActualizarAcumulado(); var caso=servicio.Listar()[0];
            var revision=servicio.PrepararRevision(caso.Id,Fixtures.Fecha(18));
            servicio.ConfirmarGeneral(revision,true,true,Fixtures.Fecha(18));
            Assert.True(servicio.CrearBorradorConfirmado(caso.Id,Fixtures.Fecha(18)).Creado);
            var nuevoCorreo=new OutlookSimulado(Fixtures.Config.Evento.Cuenta);
            nuevoCorreo.AgregarMensajeFicticio(correo.ObtenerMensajesDisponibles().Single(m=>m.Id==caso.MensajeId));
            var nuevoServicio=new BandejaPreregistros(Fixtures.Config.Evento,Config(),repo,nuevoCorreo,Fixtures.Generador(),Fixtures.Plantillas());
            nuevoServicio.ActualizarAcumulado();
            Assert.Equal(EstadoPreregistro.ErrorRevision,repo.Obtener(caso.Id)!.Estado);
            Assert.Empty(nuevoCorreo.Borradores);
        }
        finally { File.Delete(archivo); }
    }
    [Fact]
    public void LoteDeOchentaCasosConservaUnoPorMensajeSinDuplicados()
    {
        var (servicio,correo,repo,archivo)=Preparar();
        try
        {
            for (int i=0;i<80;i++)
                correo.AgregarMensajeFicticio(new($"lote-{i}",Fixtures.Config.Evento.Cuenta,$"cliente{i}@example.invalid",
                    $"Persona Ficticia {i}","Pre-registro Comprador",Fixtures.Fecha(18),"Consulta ficticia",false));
            Assert.Equal(81,servicio.ActualizarAcumulado());
            foreach (var caso in servicio.Listar())
            {
                var revision=servicio.PrepararRevision(caso.Id,Fixtures.Fecha(18));
                servicio.ConfirmarGeneral(revision,true,true,Fixtures.Fecha(18));
            }
            foreach (var caso in servicio.Listar())
                Assert.True(servicio.CrearBorradorConfirmado(caso.Id,Fixtures.Fecha(18)).Creado);
            Assert.Equal(81,correo.Borradores.Count);
            Assert.Equal(0,servicio.ActualizarAcumulado());
            Assert.Equal(81,repo.Listar(correo.Origen,Fixtures.Config.Evento.Actual.Id).Count);
            Assert.All(servicio.Listar(),c=>Assert.Equal(EstadoPreregistro.BorradorCreado,c.Estado));
        }
        finally { File.Delete(archivo); }
    }

    private sealed class CorreoMutable(string origen, Mensaje mensaje) : ICorreoBandeja
    {
        public string Origen => origen;
        public IReadOnlyList<Mensaje> ObtenerMensajesDisponibles() => [mensaje];
        public Borrador CrearBorrador(Mensaje m,VistaPrevia v,bool r) => throw new NotSupportedException();
        public Borrador CrearBorradorConClave(Mensaje m,VistaPrevia v,string clave) => throw new NotSupportedException();
        public Borrador? BuscarBorrador(string clave) => null;
        public EvidenciaEnvio? ConsultarRespuestaEnviada(string id) => null;
    }
}
