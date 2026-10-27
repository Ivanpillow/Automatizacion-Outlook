using CorreosOutlook.Application;
using CorreosOutlook.Domain;
using CorreosOutlook.Infrastructure;

namespace CorreosOutlook.WinForms;

public sealed class MainForm : Form
{
    private readonly ConfiguracionEvento evento;
    private readonly Politicas politicas;
    private readonly PlantillasJson plantillas;
    private readonly IHistorialRepository historial;
    private readonly OutlookSimulado outlook;
    private readonly RegistroExpoSimulado registros = new();
    private readonly GeneradorRespuesta generador;
    private readonly ServicioFechas fechas;
    private readonly ComboBox mensajes = Combo();
    private readonly TextBox cuerpo = Texto(true, true);
    private readonly Label datosMensaje = new() { AutoSize = true, MaximumSize = new Size(900, 0) };
    private readonly ComboBox idioma = Combo();
    private readonly ComboBox perfil = EnumCombo<Perfil>();
    private readonly ComboBox etapa = EnumCombo<Etapa>();
    private readonly ComboBox estado = EnumCombo<EstadoRegistro>();
    private readonly ComboBox tipo = Combo();
    private readonly ComboBox plantilla = Combo();
    private readonly ComboBox accion = EnumCombo<AccionAcompanante>();
    private readonly TextBox nombre = Texto();
    private readonly TextBox titular = Texto();
    private readonly TextBox folio = Texto();
    private readonly TextBox nombres = Texto();
    private readonly TextBox observaciones = Texto();
    private readonly TextBox manual = Texto(true);
    private readonly NumericUpDown registrados = Numero();
    private readonly NumericUpDown solicitados = Numero();
    private readonly Label cupo = new() { AutoSize = true };
    private readonly DateTimePicker fecha = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm:ss", Width = 240 };
    private readonly CheckBox datosConfirmados = new() { Text = "Confirmé manualmente nombres y cantidades (simulación)", AutoSize = true };
    private readonly CheckBox revisado = new() { Text = "Revisé esta vista previa y autorizo el borrador simulado", AutoSize = true };
    private readonly TextBox preview = Texto(true, true);
    private readonly Label status = new() { AutoSize = true, Text = "Demostración lista. Seleccione un mensaje ficticio." };
    private readonly Label sugerenciaTexto = new() { AutoSize = true, MaximumSize = new Size(900, 0) };
    private readonly TabControl tabs = new() { Dock = DockStyle.Fill };
    private readonly Button crear = new() { Text = "Crear borrador simulado", AutoSize = true, Enabled = false };
    private VistaPrevia? vista;
    private Sugerencia? sugerencia;
    private bool cargando;
    private readonly BandejaControl bandeja;

    public MainForm(ConfiguracionEvento evento, Politicas politicas, PlantillasJson plantillas, IHistorialRepository historial,
        IPreregistroRepository preregistros, ConfiguracionBandeja configBandeja, string directorioDatos)
    {
        this.evento = evento; this.politicas = politicas; this.plantillas = plantillas; this.historial = historial;
        outlook = new(evento.Cuenta); fechas = new(evento);
        generador = new(evento, politicas, new MotorScriban(), plantillas);
        Text = "Expo Joya · Asistente de correo";
        Size = new Size(1240, 840); MinimumSize = new Size(960, 680); StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10); AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = VisualStyle.Background;
        var baseLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(22, 16, 22, 12) };
        baseLayout.RowStyles.Add(new(SizeType.AutoSize)); baseLayout.RowStyles.Add(new(SizeType.Percent, 100)); baseLayout.RowStyles.Add(new(SizeType.AutoSize));
        var encabezado = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Margin = new Padding(0, 0, 0, 12) };
        encabezado.Controls.Add(new Label { Text = "Expo Joya  /  Bandeja de entrada", AutoSize = true,
            Font = new Font("Segoe UI Semibold", 19), ForeColor = VisualStyle.Ink });
        encabezado.Controls.Add(new Label { Text = "Clasifique los mensajes, revise cada respuesta y conecte Outlook clásico para enviar los borradores confirmados.",
            AutoSize = true, ForeColor = VisualStyle.Muted, Margin = new Padding(2, 3, 0, 0) });
        baseLayout.Controls.Add(encabezado);
        status.ForeColor = VisualStyle.Muted;
        status.Padding = new Padding(3, 9, 0, 0);
        baseLayout.Controls.Add(tabs); baseLayout.Controls.Add(status); Controls.Add(baseLayout);
        idioma.Items.AddRange(["es", "en"]); idioma.SelectedIndex = 0;
        plantilla.DataSource = plantillas.Catalogo.Where(p => p.Disponible).ToArray();
        tipo.DataSource = plantillas.Catalogo.Take(27).ToArray(); tipo.DisplayMember = nameof(Plantilla.Nombre);
        fecha.Value = fechas.HoraLocal(DateTimeOffset.Now);
        bandeja = new(evento, configBandeja, politicas, plantillas, outlook, preregistros, directorioDatos);
        var tabBandeja = new TabPage("Bandeja de entrada") { BackColor = VisualStyle.Background };
        tabBandeja.Controls.Add(bandeja); tabs.TabPages.Add(tabBandeja);
        ConstruirCorreo(); ConstruirRespuesta(); ConstruirReglas();
        tabs.TabPages[1].Text = "Correo individual · demo";
        tabs.TabPages[2].Text = "Respuesta · demo";
        tabs.TabPages[3].Text = "Comprobar reglas";
        tabs.SelectedTab = tabBandeja;
        mensajes.DisplayMember = nameof(Mensaje.Asunto); mensajes.DataSource = outlook.ObtenerMensajesDisponibles();
        mensajes.SelectedIndexChanged += (_, _) => CargarMensaje();
        foreach (var control in new Control[] { idioma, perfil, etapa, estado, tipo, plantilla, accion, nombre, titular, folio,
            nombres, observaciones, manual, registrados, solicitados, datosConfirmados })
        {
            if (control is ComboBox c) c.SelectedIndexChanged += (_, _) => Invalidar();
            else if (control is NumericUpDown n) n.ValueChanged += (_, _) => Invalidar();
            else if (control is CheckBox b) b.CheckedChanged += (_, _) => Invalidar();
            else control.TextChanged += (_, _) => Invalidar();
        }
        plantilla.SelectedIndexChanged += (_, _) =>
        {
            if (plantilla.SelectedItem is Plantilla p)
            {
                idioma.SelectedItem = p.Idioma;
                if (p.Perfil != Perfil.Otro) perfil.SelectedItem = p.Perfil;
                tipo.SelectedItem = plantillas.Catalogo.FirstOrDefault(x => x.Id == p.Id) ?? tipo.SelectedItem;
            }
        };
        fecha.ValueChanged += (_, _) => { etapa.SelectedItem = fechas.ObtenerEtapa(Instante()); Invalidar(); };
        revisado.CheckedChanged += (_, _) => crear.Enabled = revisado.Checked && vista is not null;
        crear.Click += (_, _) => Ejecutar(CrearBorrador);
        CargarMensaje();
        VisualStyle.Apply(this);
    }

    private static ComboBox Combo() => new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 400, DropDownWidth = 520 };
    private static ComboBox EnumCombo<T>() where T : struct, Enum { var c = Combo(); c.DataSource = Enum.GetValues<T>(); return c; }
    private static TextBox Texto(bool multi = false, bool soloLectura = false) => new()
    { Multiline = multi, ReadOnly = soloLectura, ScrollBars = multi ? ScrollBars.Vertical : ScrollBars.None, Width = 620, Height = multi ? 130 : 30 };
    private static NumericUpDown Numero() => new() { Minimum = 0, Maximum = 100, Width = 100 };
    private static Button Boton(string text, Action click)
    { var b = new Button { Text = text, AutoSize = true, Padding = new Padding(6) }; b.Click += (_, _) => click(); return b; }
    private TableLayoutPanel Pagina(string nombre)
    {
        var page = new TabPage(nombre) { AutoScroll = true, Padding = new Padding(16), BackColor = VisualStyle.Background };
        var grid = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(20), BackColor = VisualStyle.Surface };
        VisualStyle.RoundCard(grid);
        grid.ColumnStyles.Add(new(SizeType.Absolute, 200)); grid.ColumnStyles.Add(new(SizeType.Percent, 100));
        page.Controls.Add(grid); tabs.TabPages.Add(page); return grid;
    }
    private static void Fila(TableLayoutPanel grid, string label, Control control)
    {
        int row = grid.RowCount++; grid.RowStyles.Add(new(SizeType.AutoSize));
        grid.Controls.Add(new Label { Text = label, AutoSize = true, MaximumSize = new Size(185, 0), Margin = new Padding(0, 8, 10, 8) }, 0, row);
        control.Margin = new Padding(0, 4, 0, 4); grid.Controls.Add(control, 1, row);
    }
    private void ConstruirCorreo()
    {
        var g = Pagina("1 · Correo y clasificación");
        Fila(g, "Correo ficticio", mensajes); Fila(g, "Datos del mensaje", datosMensaje); Fila(g, "Contenido (texto)", cuerpo);
        var acciones = new FlowLayoutPanel { AutoSize = true };
        acciones.Controls.Add(Boton("Analizar correo", () => Ejecutar(Analizar)));
        acciones.Controls.Add(Boton("Usar sugerencia", () => Ejecutar(AplicarSugerencia)));
        Fila(g, "Análisis local", acciones); Fila(g, "Sugerencia", sugerenciaTexto);
        Fila(g, "Fecha de evaluación\n(hora del evento)", fecha);
        Fila(g, "Idioma", idioma); Fila(g, "Perfil solicitado", perfil); Fila(g, "Tipo de solicitud", tipo);
        Fila(g, "Etapa", etapa); Fila(g, "Estado en registros\n(selección manual)", estado);
        Fila(g, "Siguiente paso", Boton("Preparar respuesta →", () => tabs.SelectedIndex = 2));
    }
    private void ConstruirRespuesta()
    {
        var g = Pagina("2 · Respuesta y borrador");
        Fila(g, "Plantilla", plantilla); Fila(g, "Nombre", nombre); Fila(g, "Persona titular", titular);
        Fila(g, "Acción si es acompañante", accion); Fila(g, "Folio (manual; no se asigna)", folio);
        Fila(g, "Ya registrados", registrados); Fila(g, "Adicionales solicitados", solicitados); Fila(g, "Cupo", cupo);
        Fila(g, "Nombres del total\n(separar con ;)", nombres); Fila(g, "Confirmación de datos", datosConfirmados);
        Fila(g, "Observaciones\n(no se guardan)", observaciones); Fila(g, "Texto personalizado", manual);
        var acciones = new FlowLayoutPanel { AutoSize = true };
        acciones.Controls.Add(Boton("Vista previa", () => Ejecutar(GenerarVista)));
        acciones.Controls.Add(Boton("Marcar pendiente", () => { Invalidar(); status.Text = "Pendiente de atención (sólo en esta sesión)."; }));
        acciones.Controls.Add(Boton("Requiere revisión", () => { estado.SelectedItem = EstadoRegistro.RevisionManual; status.Text = "Caso marcado para revisión en esta sesión."; }));
        acciones.Controls.Add(Boton("Cancelar", () => { Invalidar(); status.Text = "Vista previa cancelada."; }));
        Fila(g, "Acciones", acciones); preview.Height = 260; Fila(g, "Vista previa", preview);
        Fila(g, "Revisión humana", revisado); Fila(g, "Borrador en memoria", crear);
        Fila(g, "Outlook", new Label { AutoSize = true, Text = "Editar en Outlook no está disponible en esta fase." });
    }
    private void ConstruirReglas()
    {
        var g = Pagina("3 · Comprobar reglas");
        var id = new CheckBox { Text = "Identificación vigente con fotografía", AutoSize = true };
        var fiscal = new CheckBox { Text = "Constancia fiscal con actividad afín", AutoSize = true };
        var invitacion = new CheckBox { Text = "Invitación personalizada de expositor", AutoSize = true };
        var nota = new CheckBox { Text = "Incluir un comprobante ficticio", AutoSize = true };
        var fechaNota = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = fecha.Value };
        var monto = new NumericUpDown { Maximum = 10000000, DecimalPlaces = 2, Width = 220, ThousandsSeparator = true };
        var aNombre = new CheckBox { Text = "A nombre de la persona", AutoSize = true };
        var afin = new CheckBox { Text = "Compra de joyería o actividad afín", AutoSize = true };
        var evidencia = new CheckBox { Text = "Revisé evidencia adicional afín o de inicio del negocio", AutoSize = true };
        var salida = Texto(true, true); salida.Height = 200;
        Fila(g, "Uso", new Label { Text = "Evaluación orientativa. No abre adjuntos, aprueba ni rechaza registros.\nUsa el perfil y la fecha de la primera pestaña.", AutoSize = true });
        Fila(g, "Obligatorio", id); Fila(g, "Alternativa fiscal", fiscal); Fila(g, "Alternativa invitación", invitacion);
        Fila(g, "Alternativa notas", nota); Fila(g, "Fecha del comprobante", fechaNota); Fila(g, "Monto MXN", monto);
        Fila(g, "Titularidad", aNombre); Fila(g, "Actividad", afin);
        Fila(g, "Evidencia adicional", evidencia);
        Fila(g, "Ejemplos de evidencia", new Label { AutoSize = true, MaximumSize = new Size(620, 0),
            Text = string.Join(", ", politicas.Documentos.EvidenciasAdicionalesEmprendedor) + ". No se abre ni valida automáticamente ningún enlace." });
        Fila(g, "Evaluación", Boton("Comprobar criterios", () => Ejecutar(() =>
        {
            Comprobante[] comprobantes = nota.Checked ? [new(DateOnly.FromDateTime(fechaNota.Value), monto.Value, aNombre.Checked, afin.Checked)] : [];
            var result = new ReglasDocumentales(politicas).Evaluar((Perfil)perfil.SelectedItem!,
                new(id.Checked, fiscal.Checked, invitacion.Checked, comprobantes, evidencia.Checked), DateOnly.FromDateTime(fecha.Value));
            salida.Text = $"Perfil sugerido: {result.PerfilSugerido?.ToString() ?? "Sin determinar"}" + Environment.NewLine
                + (result.CumpleCriteriosConocidos ? "Cumple criterios conocidos del perfil seleccionado; pendiente de revisión humana." : "Revise el perfil seleccionado o la evidencia; no confirmar registro.")
                + Environment.NewLine + $"Total válido: {result.TotalComprobantesValidos:C2} MXN" + Environment.NewLine + string.Join(Environment.NewLine, result.Observaciones);
        })));
        Fila(g, "Resultado", salida);
    }
    private Mensaje MensajeActual => (Mensaje)mensajes.SelectedItem!;
    private DateTimeOffset Instante()
    {
        var local = DateTime.SpecifyKind(fecha.Value, DateTimeKind.Unspecified);
        return new(local, TimeZoneInfo.FindSystemTimeZoneById(evento.ZonaHoraria).GetUtcOffset(local));
    }
    private void CargarMensaje()
    {
        if (mensajes.SelectedItem is not Mensaje mensaje) return;
        cargando = true;
        cuerpo.Text = mensaje.Cuerpo;
        datosMensaje.Text = $"{mensaje.NombreRemitente} <{mensaje.Remitente}>\nCuenta: {mensaje.Cuenta}\nRecibido: {fechas.HoraLocal(mensaje.Recibido):g} · Adjuntos: {(mensaje.TieneAdjuntos ? "Sí (no se abren)" : "No")}";
        var registro = registros.BuscarPorId(mensaje.Id);
        nombre.Text = mensaje.NombreRemitente; titular.Text = registro?.Titular ?? ""; folio.Clear(); nombres.Clear();
        observaciones.Clear(); manual.Clear(); registrados.Value = 0; solicitados.Value = 0; datosConfirmados.Checked = false;
        estado.SelectedItem = registro?.Estado ?? EstadoRegistro.NoEncontrado;
        perfil.SelectedItem = Perfil.Otro; idioma.SelectedItem = "es"; accion.SelectedItem = AccionAcompanante.Continuar;
        etapa.SelectedItem = fechas.ObtenerEtapa(Instante()); sugerencia = null; sugerenciaTexto.Text = "Sin análisis. Los datos de registro mostrados son ficticios.";
        SeleccionarPlantilla("informacion-general"); cargando = false; Invalidar();
    }
    private void Analizar()
    {
        sugerencia = new AnalizadorSimulado(fechas).Analizar(MensajeActual, Instante());
        sugerenciaTexto.Text = $"Idioma: {sugerencia.Idioma} · Perfil: {sugerencia.Perfil} · Etapa: {sugerencia.Etapa}\n{sugerencia.Explicacion}";
    }
    private void AplicarSugerencia()
    {
        if (sugerencia is null) throw new ValidacionException("Primero analice el correo.");
        idioma.SelectedItem = sugerencia.Idioma; perfil.SelectedItem = sugerencia.Perfil;
        etapa.SelectedItem = sugerencia.Etapa; nombre.Text = sugerencia.Nombre; SeleccionarPlantilla(sugerencia.PlantillaId);
        status.Text = "Sugerencia aplicada. Puede corregir cualquier campo.";
    }
    private void SeleccionarPlantilla(string id) => plantilla.SelectedItem = plantillas.Catalogo.Single(p => p.Id == id);
    private SolicitudRespuesta Solicitud() => new()
    {
        Mensaje = MensajeActual, Plantilla = (Plantilla)plantilla.SelectedItem!, Instante = Instante(),
        Idioma = (string)idioma.SelectedItem!, Perfil = (Perfil)perfil.SelectedItem!, Etapa = (Etapa)etapa.SelectedItem!,
        EstadoRegistro = (EstadoRegistro)estado.SelectedItem!, Nombre = nombre.Text, Titular = titular.Text, Folio = folio.Text,
        Observaciones = observaciones.Text, RespuestaManual = manual.Text, AccionAcompanante = (AccionAcompanante)accion.SelectedItem!,
        AcompanantesRegistrados = (int)registrados.Value, AcompanantesSolicitados = (int)solicitados.Value,
        NombresAcompanantes = nombres.Text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        DatosConfirmadosManualmente = datosConfirmados.Checked
    };
    private void Invalidar()
    {
        if (cargando) return;
        vista = null; revisado.Checked = false; crear.Enabled = false; preview.Clear();
        var result = new ReglasAcompanantes(politicas.Acompanantes).Evaluar((int)registrados.Value, (int)solicitados.Value);
        cupo.Text = result.EsValido ? $"Total: {result.Total} · Lugares disponibles: {result.Disponibles}" : result.Error;
    }
    private void GenerarVista()
    {
        Invalidar(); vista = generador.Generar(Solicitud());
        preview.Text = "ASUNTO: " + vista.Asunto + Environment.NewLine + Environment.NewLine + vista.Texto;
        status.Text = "Vista previa lista. Revísela antes de crear el borrador simulado.";
    }
    private void CrearBorrador()
    {
        if (vista is null) throw new ValidacionException("Genere una vista previa actualizada.");
        var solicitud = Solicitud();
        outlook.CrearBorrador(MensajeActual, vista, revisado.Checked);
        Invalidar();
        try
        {
            historial.Registrar(new(Guid.NewGuid(), null, evento.Actual.Id, MensajeActual.Recibido, null,
                solicitud.Idioma, solicitud.Perfil, ((Plantilla)tipo.SelectedItem!).Id, solicitud.Plantilla.Id,
                "BorradorSimulado", "PendienteRevision", null, true,
                solicitud.AcompanantesRegistrados + solicitud.AcompanantesSolicitados, false));
            status.Text = $"Borrador simulado creado en memoria. Total en esta sesión: {outlook.Borradores.Count}. No se envió ningún correo.";
        }
        catch { status.Text = "Borrador simulado creado, pero no se pudo guardar el historial. No vuelva a crearlo; revise permisos o espacio disponible."; }
    }
    private void Ejecutar(Action action)
    {
        try { action(); }
        catch (ValidacionException e) { MessageBox.Show(this, e.Message, "Revisar datos", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        catch { MessageBox.Show(this, "No se pudo completar la operación. Revise configuración y archivos locales. Outlook no ha sido modificado.", "Operación no completada", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }
    internal void VerificarFlujoDemo()
    {
        fecha.Value = evento.Actual.LimiteDocumentacionLocal!.Value.AddDays(-1);
        Analizar(); AplicarSugerencia(); GenerarVista();
        if (vista is null || !vista.Texto.Contains(evento.Actual.Nombre)) throw new InvalidOperationException();
        revisado.Checked = true; CrearBorrador();
        if (outlook.Borradores.Count != 1 || historial.Contar() != 1) throw new InvalidOperationException();
        GenerarVista(); revisado.Checked = true; nombre.Text = "Persona modificada (ficticia)";
        if (vista is not null || crear.Enabled || revisado.Checked) throw new InvalidOperationException();
        bandeja.VerificarFlujoDemo();
    }
}
