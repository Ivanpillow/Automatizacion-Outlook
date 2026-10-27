using CorreosOutlook.Domain;
using Xunit;

namespace CorreosOutlook.Tests;

public class PerfilDocumentalTests
{
    [Theory]
    [InlineData(true,false,500,false,Perfil.Comprador)]
    [InlineData(false,true,500,false,Perfil.Comprador)]
    [InlineData(true,false,0,false,Perfil.Comprador)]
    [InlineData(false,true,0,false,Perfil.Comprador)]
    [InlineData(false,false,10000,false,Perfil.Comprador)]
    [InlineData(false,false,10001,true,Perfil.Comprador)]
    [InlineData(false,false,9999,true,Perfil.Emprendedor)]
    [InlineData(false,false,100,true,Perfil.Emprendedor)]
    [InlineData(true,false,100,true,Perfil.Comprador)]
    [InlineData(false,true,100,true,Perfil.Comprador)]
    public void PriorizaLasViasDeCompradorAntesDeEmprendedor(bool fiscal,bool invitacion,decimal monto,bool evidencia,Perfil esperado)
    {
        var fecha=new DateOnly(2027,1,18);
        Comprobante[] notas=monto>0 ? [new(fecha,monto,true,true)] : [];
        var result=new ReglasDocumentales(Fixtures.Config.Politicas).Evaluar(esperado,new(true,fiscal,invitacion,notas,evidencia),fecha);
        Assert.Equal(esperado,result.PerfilSugerido);
        Assert.True(result.CumpleCriteriosConocidos);
        Assert.True(result.RequiereRevision);
    }
    [Theory]
    [InlineData(true,500,false)]
    [InlineData(false,500,true)]
    [InlineData(true,0,false)]
    [InlineData(true,0,true)]
    public void NoConfirmaSinIdentificacionONotasYEvidenciaSuficientes(bool identificacion,decimal monto,bool evidencia)
    {
        var fecha=new DateOnly(2027,1,18);
        Comprobante[] notas=monto>0 ? [new(fecha,monto,true,true)] : [];
        var result=new ReglasDocumentales(Fixtures.Config.Politicas).Evaluar(Perfil.Emprendedor,new(identificacion,false,false,notas,evidencia),fecha);
        Assert.Null(result.PerfilSugerido);
        Assert.False(result.CumpleCriteriosConocidos);
        Assert.True(result.RequiereRevision);
    }
    [Fact]
    public void EvidenciaAdicionalNoVuelveValidosComprobantesAntiguos()
    {
        var fecha=new DateOnly(2027,1,18);
        var result=new ReglasDocumentales(Fixtures.Config.Politicas).Evaluar(Perfil.Emprendedor,
            new(true,false,false,[new(fecha.AddMonths(-7),500,true,true)],true),fecha);
        Assert.Null(result.PerfilSugerido);
        Assert.False(result.CumpleCriteriosConocidos);
    }
}
