using CorreosOutlook.Application;
using CorreosOutlook.Domain;
using Xunit;

namespace CorreosOutlook.Tests;

public class AcompanantesTests
{
    [Theory]
    [InlineData(0,0,2,true)]
    [InlineData(1,0,1,true)]
    [InlineData(2,0,0,true)]
    [InlineData(0,1,1,true)]
    [InlineData(0,2,0,true)]
    [InlineData(1,1,0,true)]
    [InlineData(2,1,0,false)]
    [InlineData(0,3,0,false)]
    [InlineData(3,0,0,false)]
    [InlineData(-1,0,0,false)]
    [InlineData(1,-1,0,false)]
    [InlineData(2147483647,2147483647,0,false)]
    public void CupoNoEsNegativoNiPermiteExcedentes(int registrados,int nuevos,int libres,bool valido)
    {
        var r=new ReglasAcompanantes(Fixtures.Config.Politicas.Acompanantes).Evaluar(registrados,nuevos);
        Assert.Equal(libres,r.Disponibles); Assert.Equal(valido,r.EsValido);
    }
    [Theory]
    [InlineData(11,false)]
    [InlineData(12,true)]
    [InlineData(20,true)]
    public void EdadMinima(int edad,bool cumple) => Assert.Equal(cumple,new ReglasAcompanantes(Fixtures.Config.Politicas.Acompanantes).CumpleEdadMinima(edad));
    [Theory]
    [InlineData(1,"Todavía cuenta con un lugar")]
    [InlineData(2,"se han ocupado los 2 lugares")]
    public void ConfirmacionCalculaTexto(int cantidad,string esperado)
    {
        var s=Fixtures.Solicitud("confirmacion-acompanantes") with { AcompanantesSolicitados=cantidad,
            NombresAcompanantes=Enumerable.Range(1,cantidad).Select(i=>$"Acompañante ficticio {i}").ToArray(), DatosConfirmadosManualmente=true };
        Assert.Contains(esperado,Fixtures.Generador().Generar(s).Texto);
    }
    [Theory]
    [InlineData(3,true)]
    [InlineData(1,false)]
    [InlineData(2,true)]
    public void BloqueaExcesoFaltaDeConfirmacionONombres(int total,bool confirmado)
    {
        var s=Fixtures.Solicitud("confirmacion-acompanantes") with { AcompanantesSolicitados=total,
            NombresAcompanantes=["Persona Ficticia"], DatosConfirmadosManualmente=confirmado };
        Assert.Throws<ValidacionException>(()=>Fixtures.Generador().Generar(s));
    }
    [Theory]
    [InlineData(AccionAcompanante.Continuar,"no necesita hacer un nuevo registro")]
    [InlineData(AccionAcompanante.SolicitarComprador,"Para solicitar el cambio a comprador")]
    [InlineData(AccionAcompanante.SolicitarEmprendedor,"evidencia adicional de actividad afín")]
    [InlineData(AccionAcompanante.CorregirTitular,"confirme los datos")]
    [InlineData(AccionAcompanante.RevisionManual,"requiere revisión manual")]
    public void AcompananteEncontradoRespetaAccionSinInferirGenero(AccionAcompanante accion,string esperado)
    {
        var s=Fixtures.Solicitud("acompanante-encontrado") with { EstadoRegistro=EstadoRegistro.AcompananteRegistrado,
            Titular="Titular Ficticio", AccionAcompanante=accion };
        var texto=Fixtures.Generador().Generar(s).Texto;
        Assert.Contains(esperado,texto); Assert.Contains("con la persona titular",texto);
    }
}
