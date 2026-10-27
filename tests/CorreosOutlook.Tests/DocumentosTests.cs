using CorreosOutlook.Domain;
using Xunit;

namespace CorreosOutlook.Tests;

public class DocumentosTests
{
    private readonly ReglasDocumentales reglas = new(Fixtures.Config.Politicas);
    private static readonly DateOnly Hoy = new(2027,1,19);
    private static Comprobante Nota(decimal monto, DateOnly? fecha = null, bool titular = true, bool afin = true) => new(fecha ?? Hoy, monto, titular, afin);
    private EvaluacionDocumental Evaluar(Documentacion d, Perfil p = Perfil.Comprador) => reglas.Evaluar(p,d,Hoy);

    [Fact]
    public void IdentificacionEsObligatoriaInclusoConTodasLasAlternativas() =>
        Assert.False(Evaluar(new(false,true,true,[Nota(10000)])).CumpleCriteriosConocidos);
    [Theory]
    [InlineData(true,false,false)]
    [InlineData(false,true,false)]
    [InlineData(false,false,true)]
    public void BastaUnaAlternativa(bool fiscal,bool invitacion,bool notas)
    {
        var r = Evaluar(new(true,fiscal,invitacion,notas ? [Nota(10000)] : []));
        Assert.True(r.CumpleCriteriosConocidos); Assert.True(r.RequiereRevision);
    }
    [Fact]
    public void IdentificacionSolaNoBasta() => Assert.False(Evaluar(new(true,false,false,[])).CumpleCriteriosConocidos);
    [Theory]
    [InlineData(9999.99,false)]
    [InlineData(10000,true)]
    [InlineData(15000,true)]
    public void MontoMinimoComprador(decimal monto,bool cumple) => Assert.Equal(cumple,Evaluar(new(true,false,false,[Nota(monto)])).CumpleCriteriosConocidos);
    [Fact]
    public void SumaVariosComprobantesValidos() => Assert.True(Evaluar(new(true,false,false,[Nota(4000),Nota(6000)])).CumpleCriteriosConocidos);
    [Theory]
    [InlineData(2026,7,18,false)]
    [InlineData(2026,7,19,true)]
    [InlineData(2026,7,20,true)]
    [InlineData(2027,1,20,false)]
    public void AntiguedadMaximaSeisMesesYNoFuturo(int year,int month,int day,bool cumple) =>
        Assert.Equal(cumple,Evaluar(new(true,false,false,[Nota(10000,new(year,month,day))])).CumpleCriteriosConocidos);
    [Fact]
    public void NoSumaComprobantesAntiguos()
    {
        var r = Evaluar(new(true,false,false,[Nota(9000),Nota(2000,Hoy.AddMonths(-6).AddDays(-1))]));
        Assert.Equal(9000,r.TotalComprobantesValidos); Assert.False(r.CumpleCriteriosConocidos);
    }
    [Theory]
    [InlineData(false,true)]
    [InlineData(true,false)]
    public void TitularidadYActividadSonNecesarias(bool titular,bool afin) =>
        Assert.False(Evaluar(new(true,false,false,[Nota(10000,titular:titular,afin:afin)])).CumpleCriteriosConocidos);
    [Fact]
    public void NotaAntiguaNoInvalidaOtraAlternativaValida()
    {
        var r=Evaluar(new(true,true,false,[Nota(10000,Hoy.AddYears(-1))]));
        Assert.True(r.CumpleCriteriosConocidos); Assert.Equal(0,r.TotalComprobantesValidos);
    }
    [Fact]
    public void ComprasAltasSugierenCompradorAunqueSeSeleccioneEmprendedor()
    {
        var r=Evaluar(new(true,false,false,[Nota(100000)]),Perfil.Emprendedor);
        Assert.False(r.CumpleCriteriosConocidos); Assert.True(r.RequiereRevision);
        Assert.Equal(Perfil.Comprador,r.PerfilSugerido);
    }
    [Fact]
    public void InternacionalNoAsumeAlternativasLocales()
    {
        var r=Evaluar(new(true,true,true,[Nota(100000)]),Perfil.Internacional);
        Assert.False(r.CumpleCriteriosConocidos); Assert.True(r.RequiereRevision);
    }
}
