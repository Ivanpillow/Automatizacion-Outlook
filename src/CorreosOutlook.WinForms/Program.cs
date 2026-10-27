using CorreosOutlook.Infrastructure;
using System.Text.Json;

namespace CorreosOutlook.WinForms;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        try
        {
            if (args.Length == 4 && args[0] == "--verificar-outlook")
            {
                File.WriteAllText(args[3], "Iniciando verificación de Outlook.");
                var configVerificacion = ConfiguracionJson.CargarBandeja(Path.Combine(AppContext.BaseDirectory, "config", "bandeja.json"));
                File.WriteAllText(args[3], "Configuración de bandeja cargada.");
                var encontrada = OutlookClasico.DescubrirCuentas().Single(c =>
                    string.Equals(c.Correo, args[1], StringComparison.OrdinalIgnoreCase));
                if (!encontrada.Carpetas.Contains(args[2])) return 1;
                File.WriteAllText(args[3], "Cuenta y carpeta descubiertas.");
                var lector = new OutlookClasico(args[1], args[2], configVerificacion);
                File.WriteAllText(args[3], "Cuenta y carpeta conectadas.");
                var total = lector.ObtenerMensajesDisponibles().Count;
                File.WriteAllText(args[3], JsonSerializer.Serialize(new { Cuenta = args[1],
                    Carpeta = args[2], PreregistrosEncontrados = total }));
                return 0;
            }
            var root = AppContext.BaseDirectory;
            var (evento, politicas) = ConfiguracionJson.Cargar(Path.Combine(root, "config"));
            var plantillas = new PlantillasJson(Path.Combine(root, "templates"));
            var bandeja = ConfiguracionJson.CargarBandeja(Path.Combine(root, "config", "bandeja.json"));
            var smoke = args.Contains("--smoke-test");
            var data = smoke ? Path.Combine(Path.GetTempPath(), "ExpoJoyaSmoke", Guid.NewGuid().ToString("N"))
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ExpoJoya", "CorreosOutlook");
            using var form = new MainForm(evento, politicas, plantillas, new HistorialSqlite(Path.Combine(data, "demo.db")),
                new PreregistroSqlite(Path.Combine(data, "demo.db")), bandeja, data);
            if (smoke)
            {
                form.Shown += (_, _) =>
                {
                    try { form.VerificarFlujoDemo(); form.Close(); }
                    catch { Environment.ExitCode = 1; form.Close(); }
                };
            }
            System.Windows.Forms.Application.Run(form);
            return Environment.ExitCode;
        }
        catch (Exception error)
        {
            if (args.Length == 4 && args[0] == "--verificar-outlook")
            {
                File.WriteAllText(args[3], JsonSerializer.Serialize(new { Error = error.GetType().Name, Codigo = error.HResult }));
                return 1;
            }
            if (!args.Contains("--smoke-test")) MessageBox.Show(
                "No se pudo iniciar la aplicación. Revise los archivos config y templates y el permiso de escritura de los datos locales. No se ha conectado Outlook.",
                "Expo Joya · Configuración", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }
}
