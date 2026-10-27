using CorreosOutlook.Application;
using CorreosOutlook.Domain;
using CorreosOutlook.Infrastructure;
using System.Text.Json;

namespace CorreosOutlook.WinForms;

public sealed class BandejaControl : UserControl
{
    private BandejaPreregistros servicio;
    private ICorreoBandeja correo;
    private readonly OutlookSimulado demo;
    private ConfiguracionEvento evento;
    private readonly ConfiguracionBandeja config;
    private readonly Politicas politicas;
    private readonly PlantillasJson plantillas;
    private readonly IPreregistroRepository repository;
    private readonly string archivoConexion;
    private readonly DataGridView tabla = new()
    {
        Dock = DockStyle.Fill, AutoGenerateColumns = true, ReadOnly = true, AllowUserToAddRows = false,
        AllowUserToDeleteRows = false, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false, BackgroundColor = Color.White,
        BorderStyle = BorderStyle.None, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
        GridColor = Color.FromArgb(235, 240, 245), EnableHeadersVisualStyles = false,
        ColumnHeadersHeight = 42, RowTemplate = { Height = 42 }
    };
    private readonly CheckBox verRespondidos = new() { Text = "Incluir respondidos (historial)", AutoSize = true };
    private readonly ComboBox filtroPerfil = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
    private readonly ComboBox filtroEstado = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 190 };
    private readonly Label resumen = new() { AutoSize = true };
    private readonly Label resultado = new() { AutoSize = true, MaximumSize = new Size(980, 0) };
    private readonly Label fuente = new() { AutoSize = true, MaximumSize = new Size(980, 0) };
    private readonly CheckBox crearAlConfirmar = new() { Text = "Crear el borrador al confirmar cada respuesta", AutoSize = true };
    private readonly FlowLayoutPanel acciones = new() { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true,
        Margin = new Padding(0, 8, 0, 8) };
    private readonly Button cancelar = new() { Text = "Detener lote", AutoSize = true, Enabled = false };
    private readonly Button enviarLote = new() { Text = "Crear y enviar respuestas confirmadas", AutoSize = true, Enabled = false };
    private readonly System.Windows.Forms.Timer timer;
    private bool procesando;
    private bool detener;
    private sealed record FilaCaso(string Id, string Tipo, string Estado, string Fecha, string Asunto, string Incidencia);
    private sealed record ConexionGuardada(string Cuenta, string Carpeta);

    public BandejaControl(ConfiguracionEvento evento, ConfiguracionBandeja config, Politicas politicas,
        PlantillasJson plantillas, OutlookSimulado correo, IPreregistroRepository repository, string directorioDatos)
    {
        this.evento = evento; this.config = config; this.correo = correo; demo = correo;
        this.politicas = politicas; this.plantillas = plantillas; this.repository = repository;
        archivoConexion = Path.Combine(directorioDatos, "outlook.local.json");
        servicio = new(evento, config, repository, correo, new GeneradorRespuesta(evento, politicas, new MotorScriban(), plantillas), plantillas);
        Dock = DockStyle.Fill;
        BackColor = VisualStyle.Background;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 8, ColumnCount = 1, Padding = new Padding(16, 12, 16, 12) };
        layout.RowStyles.Add(new(SizeType.AutoSize)); layout.RowStyles.Add(new(SizeType.AutoSize));
        layout.RowStyles.Add(new(SizeType.AutoSize)); layout.RowStyles.Add(new(SizeType.AutoSize));
        layout.RowStyles.Add(new(SizeType.AutoSize)); layout.RowStyles.Add(new(SizeType.Percent, 100));
        layout.RowStyles.Add(new(SizeType.AutoSize)); layout.RowStyles.Add(new(SizeType.AutoSize));
        var guia = new Label { AutoSize = true, ForeColor = VisualStyle.Muted, Margin = new Padding(0, 0, 0, 10),
            Text = "1  Seleccione un correo    →    2  Revise y confirme la respuesta    →    3  Cree el borrador    →    4  Envíelo desde Outlook" };
        layout.Controls.Add(guia);
        layout.Controls.Add(fuente);
        var filtros = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true,
            Margin = new Padding(0, 0, 0, 10) };
        filtros.Controls.Add(new Label { Text = "Categoría", AutoSize = true, Margin = new Padding(0, 7, 7, 0) });
        filtros.Controls.Add(filtroPerfil);
        filtros.Controls.Add(new Label { Text = "Estado", AutoSize = true, Margin = new Padding(16, 7, 7, 0) });
        filtros.Controls.Add(filtroEstado);
        verRespondidos.Margin = new Padding(20, 7, 0, 0);
        filtros.Controls.Add(verRespondidos);
        layout.Controls.Add(resumen); layout.Controls.Add(filtros); layout.Controls.Add(tabla);
        layout.Controls.Add(crearAlConfirmar);
        layout.Controls.Add(acciones); layout.Controls.Add(resultado); Controls.Add(layout);
        Accion("Actualizar", () => Ejecutar(() => Actualizar(true)));
        Accion("Revisar respuesta general", () => Ejecutar(Revisar));
        Accion("Apartar para atención individual", () => Ejecutar(() => { servicio.ApartarParaRevision(IdSeleccionado()); Actualizar(false); }));
        var lote = new Button { Text = "Crear borradores confirmados", AutoSize = true };
        lote.Click += async (_, _) => await CrearLote(); acciones.Controls.Add(lote);
        enviarLote.Click += async (_, _) => await EnviarLote(); acciones.Controls.Add(enviarLote);
        Accion("Simular respuesta enviada", () => Ejecutar(SimularEnvio));
        Accion("Agregar pre-registros ficticios", () => Ejecutar(() => { AgregarEjemplos(false); Actualizar(true); }));
        Accion("Conectar Outlook clásico", () => Ejecutar(ConectarOutlook));
        Accion("Volver a simulación", () => Ejecutar(() => { CambiarFuente(demo, evento with { Cuenta = demo.ObtenerMensajesDisponibles()[0].Cuenta }); Actualizar(true); }));
        cancelar.Click += (_, _) => detener = true; acciones.Controls.Add(cancelar);
        fuente.ForeColor = VisualStyle.Accent;
        fuente.Font = new Font("Segoe UI Semibold", 10);
        fuente.Margin = new Padding(0, 0, 0, 8);
        resumen.ForeColor = VisualStyle.Ink;
        resumen.Font = new Font("Segoe UI Semibold", 10);
        resumen.Margin = new Padding(0, 2, 0, 8);
        resultado.ForeColor = VisualStyle.Muted;
        tabla.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(233, 240, 246);
        tabla.ColumnHeadersDefaultCellStyle.ForeColor = VisualStyle.Ink;
        tabla.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10);
        tabla.DefaultCellStyle.SelectionBackColor = Color.FromArgb(217, 234, 247);
        tabla.DefaultCellStyle.SelectionForeColor = VisualStyle.Ink;
        tabla.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 252, 254);
        VisualStyle.RoundCard(tabla);
        tabla.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || tabla.Columns[e.ColumnIndex].Name != "Estado" || e.Value is not string estado || e.CellStyle is null) return;
            e.CellStyle.ForeColor = estado switch
            {
                nameof(EstadoPreregistro.BorradorCreado) => Color.FromArgb(26, 105, 80),
                nameof(EstadoPreregistro.ConfirmadoGeneral) => VisualStyle.Accent,
                nameof(EstadoPreregistro.Respondido) => VisualStyle.Muted,
                _ => Color.FromArgb(147, 95, 35)
            };
        };
        verRespondidos.CheckedChanged += (_, _) => Actualizar(false);
        filtroPerfil.Items.Add("Todas las categorías");
        filtroPerfil.Items.AddRange(Enum.GetNames<Perfil>());
        filtroEstado.Items.Add("Todos los estados");
        filtroEstado.Items.AddRange(Enum.GetNames<EstadoPreregistro>());
        filtroPerfil.SelectedIndex = 0;
        filtroEstado.SelectedIndex = 0;
        filtroPerfil.SelectedIndexChanged += (_, _) => Actualizar(false);
        filtroEstado.SelectedIndexChanged += (_, _) => Actualizar(false);
        timer = new() { Interval = checked(config.IntervaloActualizacionSegundos * 1000) };
        timer.Tick += (_, _) => { if (!procesando) Ejecutar(() => Actualizar(true)); };
        fuente.Text = "SIMULACIÓN · Mensajes y borradores ficticios. Nada se envía desde aquí.";
        AgregarEjemplos(true);
        if (File.Exists(archivoConexion))
        {
            try
            {
                var guardada = JsonSerializer.Deserialize<ConexionGuardada>(File.ReadAllText(archivoConexion));
                if (guardada is not null) CambiarFuente(new OutlookClasico(guardada.Cuenta, guardada.Carpeta, config),
                    evento with { Cuenta = guardada.Cuenta });
            }
            catch { resultado.Text = "No se pudo reconectar Outlook. Se inició la simulación; use «Conectar Outlook clásico»."; }
        }
        Actualizar(true); timer.Start();
    }
    private void Accion(string texto, Action accion)
    {
        var b = new Button { Text = texto, AutoSize = true, Margin = new Padding(3), Padding = new Padding(4) };
        b.Click += (_, _) => accion(); acciones.Controls.Add(b);
    }
    private void AgregarEjemplos(bool inicial)
    {
        if (!ReferenceEquals(correo, demo) && !inicial)
            throw new ValidacionException("Los mensajes ficticios sólo se agregan en modo simulación.");
        foreach (var regla in config.Reglas)
        {
            var id = inicial ? "demo-preregistro-" + regla.Perfil : "demo-preregistro-" + Guid.NewGuid().ToString("N");
            demo.AgregarMensajeFicticio(new(id, demo.ObtenerMensajesDisponibles()[0].Cuenta, "preregistro@example.invalid", "Persona ficticia " + regla.Perfil,
                regla.PrefijoAsunto, DateTimeOffset.UtcNow, "Mensaje ficticio de pre-registro para comprobar el acumulado. Necesito información para asistir.", false));
        }
    }
    private void CambiarFuente(ICorreoBandeja nueva, ConfiguracionEvento nuevoEvento)
    {
        correo = nueva; evento = nuevoEvento;
        servicio = new(evento, config, repository, correo, new GeneradorRespuesta(evento, politicas, new MotorScriban(), plantillas), plantillas);
        fuente.Text = ReferenceEquals(correo, demo)
            ? "SIMULACIÓN · Los correos y borradores son ficticios; un borrador sigue pendiente hasta el envío."
            : $"OUTLOOK CLÁSICO · Cuenta: {evento.Cuenta}. Revise cada respuesta antes de crear borradores o solicitar su envío.";
        enviarLote.Enabled = correo is IEnvioBandeja;
    }
    private void ConectarOutlook()
    {
        timer.Stop();
        try
        {
            var cuentas = OutlookClasico.DescubrirCuentas();
            if (cuentas.Count == 0) throw new ValidacionException("No hay cuentas de Outlook clásico con Bandeja de entrada disponible.");
            using var dialogo = new Form { Text = "Elegir cuenta y carpeta de Outlook", Size = new Size(610, 300),
                StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false };
            var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(16) };
            var cuentasUi = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 520 };
            var carpetasUi = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 520 };
            cuentasUi.DataSource = cuentas.ToList(); cuentasUi.DisplayMember = nameof(CuentaOutlook.Correo);
            cuentasUi.SelectedIndexChanged += (_, _) =>
            {
                carpetasUi.DataSource = null;
                carpetasUi.DataSource = ((CuentaOutlook)cuentasUi.SelectedItem!).Carpetas.ToList();
            };
            carpetasUi.DataSource = cuentas[0].Carpetas.ToList();
            var confirmar = new Button { Text = "Usar esta cuenta y carpeta", AutoSize = true };
            confirmar.Click += (_, _) => dialogo.DialogResult = DialogResult.OK;
            layout.Controls.Add(new Label { Text = "Cuenta del perfil actual:", AutoSize = true }); layout.Controls.Add(cuentasUi);
            layout.Controls.Add(new Label { Text = "Carpeta debajo de Bandeja de entrada:", AutoSize = true }); layout.Controls.Add(carpetasUi);
            layout.Controls.Add(new Label { Text = "Se usará el perfil de Outlook abierto. No se leen archivos OST/PST directamente.", AutoSize = true });
            layout.Controls.Add(confirmar); dialogo.Controls.Add(layout); VisualStyle.Apply(dialogo);
            if (dialogo.ShowDialog(this) != DialogResult.OK) return;
            var seleccion = (CuentaOutlook)cuentasUi.SelectedItem!;
            var carpeta = (string)carpetasUi.SelectedItem!;
            var real = new OutlookClasico(seleccion.Correo, carpeta, config);
            CambiarFuente(real, evento with { Cuenta = seleccion.Correo });
            File.WriteAllText(archivoConexion, JsonSerializer.Serialize(new ConexionGuardada(seleccion.Correo, carpeta)));
            Actualizar(true);
            resultado.Text = $"Conectado a {seleccion.Correo} · {carpeta}. La selección quedó guardada sólo en este equipo.";
        }
        finally { timer.Start(); }
    }
    private string IdSeleccionado() => tabla.CurrentRow?.DataBoundItem is FilaCaso fila ? fila.Id
        : throw new ValidacionException("Seleccione un caso de la bandeja.");
    private void Actualizar(bool consultar)
    {
        if (consultar) servicio.ActualizarAcumulado();
        var anterior = tabla.CurrentRow?.DataBoundItem as FilaCaso;
        var casos = servicio.Listar();
        resumen.Text = $"Pendientes de envío: {casos.Count(c => c.Estado != EstadoPreregistro.Respondido)}  |  "
            + $"Confirmados para lote: {casos.Count(c => c.Estado == EstadoPreregistro.ConfirmadoGeneral)}  |  "
            + $"Borradores: {casos.Count(c => c.Estado == EstadoPreregistro.BorradorCreado)}  |  "
            + $"Envío solicitado: {casos.Count(c => c.Estado == EstadoPreregistro.EnvioSolicitado)}  |  "
            + $"Respondidos: {casos.Count(c => c.Estado == EstadoPreregistro.Respondido)}";
        tabla.DataSource = casos.Where(c => (verRespondidos.Checked || c.Estado != EstadoPreregistro.Respondido)
                && (filtroPerfil.SelectedIndex <= 0 || c.Perfil.ToString() == (string)filtroPerfil.SelectedItem!)
                && (filtroEstado.SelectedIndex <= 0 || c.Estado.ToString() == (string)filtroEstado.SelectedItem!))
            .Select(c => new FilaCaso(c.Id, c.Perfil.ToString(), c.Estado.ToString(),
                new ServicioFechas(evento).HoraLocal(c.Recibido).ToString("g"), servicio.MensajeDelCaso(c)?.Asunto ?? "Mensaje no disponible",
                c.CodigoIncidencia ?? "")).ToList();
        if (tabla.Columns.Contains("Id")) tabla.Columns["Id"].Visible = false;
        if (anterior is not null)
            foreach (DataGridViewRow row in tabla.Rows)
                if (row.DataBoundItem is FilaCaso f && f.Id == anterior.Id) { tabla.CurrentCell = row.Cells["Asunto"]; break; }
    }
    private void Revisar()
    {
        // Pausar refresco durante la revisión; la confirmación vuelve a comprobar versión, contenido y fecha.
        timer.Stop();
        try
        {
            var revision = servicio.PrepararRevision(IdSeleccionado(), DateTimeOffset.Now);
            using var dialogo = new Form { Text = "Revisar respuesta general · Simulación", Size = new Size(850, 720),
                MinimumSize = new Size(650, 500), StartPosition = FormStartPosition.CenterParent };
            dialogo.Text = "Revisar respuesta general";
            var grid = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 6, ColumnCount = 1, Padding = new Padding(12) };
            grid.RowStyles.Add(new(SizeType.AutoSize)); grid.RowStyles.Add(new(SizeType.AutoSize));
            grid.RowStyles.Add(new(SizeType.Percent, 100));
            grid.RowStyles.Add(new(SizeType.AutoSize)); grid.RowStyles.Add(new(SizeType.AutoSize)); grid.RowStyles.Add(new(SizeType.AutoSize));
            var nombreUi = new TextBox { Text = revision.Nombre, Width = 520 };
            var lectura = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1 };
            lectura.RowStyles.Add(new(SizeType.AutoSize)); lectura.RowStyles.Add(new(SizeType.Percent, 27));
            lectura.RowStyles.Add(new(SizeType.AutoSize)); lectura.RowStyles.Add(new(SizeType.Percent, 73));
            var recibido = new TextBox { ReadOnly = true, Multiline = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical };
            var respuestaHtml = new WebBrowser { Dock = DockStyle.Fill, ScriptErrorsSuppressed = true,
                WebBrowserShortcutsEnabled = false, IsWebBrowserContextMenuEnabled = false };
            respuestaHtml.Navigating += (_, args) =>
            {
                if (args.Url is not null && args.Url.Scheme != "about") args.Cancel = true;
            };
            void MostrarRevision()
            {
                recibido.Text = revision.Mensaje.Asunto + Environment.NewLine + Environment.NewLine + revision.Mensaje.Cuerpo;
                respuestaHtml.DocumentText = revision.Vista.Html;
            }
            lectura.Controls.Add(new Label { Text = "Mensaje recibido", AutoSize = true, Font = new Font("Segoe UI Semibold", 10) });
            lectura.Controls.Add(recibido);
            lectura.Controls.Add(new Label { Text = "Respuesta propuesta · formato final", AutoSize = true, Font = new Font("Segoe UI Semibold", 10) });
            lectura.Controls.Add(respuestaHtml);
            MostrarRevision();
            var caso = new CheckBox { Text = "Verifiqué que corresponde la respuesta general; no requiere folio ni atención individual.", AutoSize = true, MaximumSize = new Size(780, 0) };
            var leido = new CheckBox { Text = "Revisé la respuesta completa para este destinatario.", AutoSize = true };
            var confirmar = new Button { Text = "Confirmar para lote de borradores", AutoSize = true, Enabled = false };
            caso.CheckedChanged += (_, _) => confirmar.Enabled = caso.Checked && leido.Checked && revision.Nombre == nombreUi.Text.Trim();
            leido.CheckedChanged += (_, _) => confirmar.Enabled = caso.Checked && leido.Checked && revision.Nombre == nombreUi.Text.Trim();
            nombreUi.TextChanged += (_, _) =>
            {
                caso.Checked = false; leido.Checked = false; confirmar.Enabled = false;
                try
                {
                    revision = servicio.PrepararRevision(revision.CasoId, DateTimeOffset.Now, nombreUi.Text);
                    MostrarRevision();
                }
                catch (ValidacionException) { recibido.Text = "Revise el nombre para generar de nuevo la respuesta."; respuestaHtml.DocumentText = ""; }
            };
            confirmar.Click += (_, _) => Ejecutar(() =>
            {
                if (revision.Nombre != nombreUi.Text.Trim()) throw new ValidacionException("Actualice y revise el nombre del saludo.");
                servicio.ConfirmarGeneral(revision, caso.Checked, leido.Checked, DateTimeOffset.Now);
                if (crearAlConfirmar.Checked)
                {
                    var resultadoBorrador = servicio.CrearBorradorConfirmado(revision.CasoId, DateTimeOffset.Now);
                    resultado.Text = resultadoBorrador.Mensaje;
                }
                dialogo.DialogResult = DialogResult.OK;
            });
            grid.Controls.Add(new Label { Text = $"Destinatario: {revision.Mensaje.Remitente} · {revision.Mensaje.NombreRemitente}", AutoSize = true });
            var nombreFila = new FlowLayoutPanel { AutoSize = true };
            nombreFila.Controls.Add(new Label { Text = "Nombre para el saludo:", AutoSize = true }); nombreFila.Controls.Add(nombreUi);
            grid.Controls.Add(nombreFila); grid.Controls.Add(lectura); grid.Controls.Add(caso); grid.Controls.Add(leido); grid.Controls.Add(confirmar);
            dialogo.Controls.Add(grid); VisualStyle.Apply(dialogo); dialogo.ShowDialog(this); Actualizar(false);
        }
        finally { timer.Start(); }
    }
    private async Task CrearLote()
    {
        if (procesando) return;
        var ids = servicio.Listar().Where(c => c.Estado == EstadoPreregistro.ConfirmadoGeneral).Select(c => c.Id).ToArray();
        if (ids.Length == 0) { resultado.Text = "No hay casos confirmados. Revise cada respuesta general primero."; return; }
        if (MessageBox.Show(this, $"Se crearán hasta {ids.Length} borradores de respuestas previamente revisadas.\nSeguirán pendientes hasta el envío. No se enviará ningún correo.\n¿Continuar?",
            "Lote de borradores", MessageBoxButtons.OKCancel, MessageBoxIcon.Information) != DialogResult.OK) return;
        procesando = true; detener = false; timer.Stop();
        resultado.Text = "Creando borradores confirmados...";
        foreach (Control control in acciones.Controls) control.Enabled = control == cancelar;
        verRespondidos.Enabled = false;
        int creados = 0, revisiones = 0, procesados = 0;
        try
        {
            foreach (var id in ids)
            {
                if (detener || IsDisposed) break;
                try { if (servicio.CrearBorradorConfirmado(id, DateTimeOffset.Now).Creado) creados++; else revisiones++; }
                catch { revisiones++; }
                procesados++; resultado.Text = $"Procesados {procesados}/{ids.Length}. Borradores: {creados}. Omitidos o por revisar: {revisiones}.";
                await Task.Yield();
            }
        }
        finally
        {
            procesando = false;
            if (!IsDisposed)
            {
                foreach (Control control in acciones.Controls) control.Enabled = control != cancelar && (control != enviarLote || correo is IEnvioBandeja);
                verRespondidos.Enabled = true; Actualizar(false); timer.Start();
                resultado.Text = $"Lote {(detener ? "detenido" : "terminado")}. Borradores: {creados}; omitidos o por revisar: {revisiones}; sin procesar: {ids.Length - procesados}. Ningún correo enviado.";
            }
        }
    }
    private async Task EnviarLote()
    {
        if (procesando || correo is not IEnvioBandeja) return;
        var casos = servicio.Listar().Where(c => c.Estado is EstadoPreregistro.ConfirmadoGeneral or EstadoPreregistro.BorradorCreado).ToArray();
        if (casos.Length == 0) { resultado.Text = "No hay respuestas confirmadas ni borradores pendientes para enviar."; return; }
        var destinatarios = casos.Select(c => servicio.MensajeDelCaso(c)?.Remitente).ToArray();
        if (destinatarios.Any(string.IsNullOrWhiteSpace))
        { resultado.Text = "Falta un mensaje original. Actualice y revise la bandeja antes de enviar."; return; }
        var lista = string.Join(Environment.NewLine, destinatarios.Select((direccion, indice) => $"{indice + 1}. {direccion}"));
        if (MessageBox.Show(this,
            $"Se crearán los borradores que falten y se solicitará a Outlook el envío de {casos.Length} respuestas previamente revisadas:\n\n{lista}\n\nCompruebe cuenta y destinatarios. Esta acción puede enviar correos reales. ¿Continuar?",
            "Confirmar envío de respuestas", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        procesando = true; detener = false; timer.Stop();
        resultado.Text = "Preparando respuestas confirmadas para Outlook...";
        foreach (Control control in acciones.Controls) control.Enabled = control == cancelar;
        verRespondidos.Enabled = false;
        int enviados = 0, omitidos = 0;
        try
        {
            foreach (var caso in casos)
            {
                if (detener || IsDisposed) break;
                try
                {
                    if (caso.Estado == EstadoPreregistro.ConfirmadoGeneral
                        && !servicio.CrearBorradorConfirmado(caso.Id, DateTimeOffset.Now).Creado)
                    { omitidos++; continue; }
                    var envio = servicio.EnviarBorradorConfirmado(caso.Id);
                    if (envio.Enviado) enviados++;
                    else { omitidos++; resultado.Text = envio.Mensaje; }
                }
                catch (Exception)
                {
                    resultado.Text = $"Envío detenido tras {enviados} solicitudes. El resultado del último caso es incierto; compruebe Enviados y Borradores en Outlook antes de continuar.";
                    break;
                }
                await Task.Yield();
            }
        }
        finally
        {
            procesando = false;
            if (!IsDisposed)
            {
                foreach (Control control in acciones.Controls) control.Enabled = control != cancelar && (control != enviarLote || correo is IEnvioBandeja);
                verRespondidos.Enabled = true;
                Actualizar(false); timer.Start();
                if (!resultado.Text.StartsWith("Envío detenido", StringComparison.Ordinal))
                    resultado.Text = $"Lote {(detener ? "detenido" : "terminado")}. Outlook recibió {enviados} solicitudes de envío; {omitidos} casos omitidos. Actualice para comprobarlos en Enviados.";
            }
        }
    }
    private void SimularEnvio()
    {
        if (!ReferenceEquals(correo, demo)) throw new ValidacionException("La simulación de envío está disponible sólo en modo ficticio. Outlook se comprueba en Enviados.");
        var caso = servicio.Listar().Single(c => c.Id == IdSeleccionado());
        if (caso.Estado == EstadoPreregistro.Respondido) throw new ValidacionException("El caso ya está respondido.");
        if (MessageBox.Show(this, "Esto simula que contestó y envió la respuesta fuera de la app.\nEl caso saldrá de pendientes y quedará en el historial de la demo. No se enviará ningún correo.",
            "Simular envío confirmado", MessageBoxButtons.OKCancel, MessageBoxIcon.Information) != DialogResult.OK) return;
        demo.SimularRespuestaEnviada(caso.MensajeId); Actualizar(true);
        resultado.Text = "Respuesta simulada confirmada. El caso permanece en el historial.";
    }
    private void Ejecutar(Action accion)
    {
        try { accion(); }
        catch (ValidacionException ex) { MessageBox.Show(this, ex.Message, "Revisar caso", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        catch { resultado.Text = "No se pudo actualizar la bandeja. Revise los datos locales; no repita una operación de resultado incierto."; }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { detener = true; timer.Stop(); timer.Dispose(); }
        base.Dispose(disposing);
    }
    internal void VerificarFlujoDemo()
    {
        var caso = servicio.Listar().First(c => c.Estado == EstadoPreregistro.Pendiente);
        var revision = servicio.PrepararRevision(caso.Id, DateTimeOffset.Now);
        servicio.ConfirmarGeneral(revision, true, true, DateTimeOffset.Now);
        if (!servicio.CrearBorradorConfirmado(caso.Id, DateTimeOffset.Now).Creado) throw new InvalidOperationException();
        if (servicio.Listar().Single(c => c.Id == caso.Id).Estado != EstadoPreregistro.BorradorCreado) throw new InvalidOperationException();
        demo.SimularRespuestaEnviada(caso.MensajeId); Actualizar(true);
        if (servicio.Listar().Single(c => c.Id == caso.Id).Estado != EstadoPreregistro.Respondido) throw new InvalidOperationException();
    }
}
