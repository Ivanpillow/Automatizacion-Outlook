namespace CorreosOutlook.Domain;

public sealed class ServicioFechas(ConfiguracionEvento config)
{
    public DateTime HoraLocal(DateTimeOffset instante) => TimeZoneInfo.ConvertTime(instante,
        TimeZoneInfo.FindSystemTimeZoneById(config.ZonaHoraria)).DateTime;

    public Etapa ObtenerEtapa(DateTimeOffset instante)
    {
        var fecha = DateOnly.FromDateTime(HoraLocal(instante));
        if (config.Actual.Inicio is not { } inicio || config.Actual.Fin is not { } fin)
            throw new InvalidOperationException("El evento actual requiere fechas confirmadas.");
        return fecha < inicio ? Etapa.Previo : fecha > fin ? Etapa.Despues : Etapa.Durante;
    }

    public PlazoDocumental ObtenerPlazo(DateTimeOffset instante) => ObtenerEtapa(instante) switch
    {
        Etapa.Durante => PlazoDocumental.DuranteEvento,
        Etapa.Despues => PlazoDocumental.DespuesEvento,
        _ => HoraLocal(instante) < config.Actual.LimiteDocumentacionLocal!.Value
            ? PlazoDocumental.AntesDelLimite : PlazoDocumental.LimiteFinalizado
    };
}

public sealed class ReglasDocumentales(Politicas politicas)
{
    public EvaluacionDocumental Evaluar(Perfil perfil, Documentacion documentos, DateOnly fecha)
    {
        var avisos = new List<string>();
        var regla = politicas.Documentos;
        var validos = documentos.Comprobantes.Where(c => c.Fecha >= fecha.AddMonths(-regla.AntiguedadMaximaMeses)
            && c.Fecha <= fecha && c.MontoMxn > 0 && c.ANombreDeLaPersona && c.ActividadAfin).ToArray();
        var total = validos.Sum(c => c.MontoMxn);
        if (validos.Length != documentos.Comprobantes.Count)
            avisos.Add("Hay comprobantes fuera de plazo, futuros o que no cumplen titularidad, actividad o monto. Se excluyeron del total válido.");
        if (!documentos.IdentificacionVigenteConFoto)
            avisos.Add("Falta identificación oficial vigente con fotografía.");
        if (perfil == Perfil.Internacional)
        {
            avisos.Add("Política internacional pendiente de confirmación: revisar pasaporte y requisitos manualmente.");
            return new(false, true, total, avisos);
        }
        if (perfil is not (Perfil.Comprador or Perfil.Emprendedor))
        {
            avisos.Add("Este perfil requiere revisión documental manual.");
            return new(false, true, total, avisos);
        }
        Perfil? sugerido = null;
        if (documentos.IdentificacionVigenteConFoto)
        {
            if (documentos.ConstanciaFiscalAfin || documentos.InvitacionPersonalizada
                || (validos.Length > 0 && total >= regla.CompradorMinimoMxn))
                sugerido = Perfil.Comprador;
            else if (validos.Length > 0 && documentos.EvidenciaAdicionalAfinRevisada)
                sugerido = Perfil.Emprendedor;
        }
        if (sugerido == Perfil.Comprador)
            avisos.Add("Corresponde sugerir comprador por CSF afín, invitación personalizada o monto suficiente de comprobantes válidos, aunque se haya solicitado emprendedor.");
        else if (sugerido == Perfil.Emprendedor)
            avisos.Add("Compras inferiores al mínimo de comprador con evidencia adicional afín revisada: se sugiere emprendedor para revisión humana.");
        else if (validos.Length > 0 && total < regla.CompradorMinimoMxn && !documentos.EvidenciaAdicionalAfinRevisada)
            avisos.Add("Las compras bajas por sí solas no bastan: falta evidencia adicional de actividad afín o inicio del negocio.");
        else
            avisos.Add("No se acredita una vía completa con los criterios conocidos. No confirmar registro; requiere revisión humana.");
        if (sugerido.HasValue && sugerido != perfil)
            avisos.Add("Revise el perfil seleccionado; la sugerencia no lo cambia automáticamente.");
        avisos.Add("Esta evaluación no aprueba ni rechaza un registro. La documentación debe revisarse por una persona.");
        return new(sugerido == perfil, true, total, avisos, sugerido);
    }
}

public sealed class ReglasAcompanantes(AcompanantesPolicy politica)
{
    public CupoAcompanantes Evaluar(int registrados, int solicitados)
    {
        long total = (long)registrados + solicitados;
        if (registrados < 0 || solicitados < 0 || total > politica.Maximo)
            return new(false, (int)Math.Clamp(total, 0, int.MaxValue), 0,
                "Cantidad inválida o máximo de acompañantes excedido. Requiere revisión manual; no se puede confirmar.");
        return new(true, (int)total, Math.Max(0, politica.Maximo - (int)total), null);
    }

    public bool CumpleEdadMinima(int edad) => edad >= politica.EdadMinima;
}
