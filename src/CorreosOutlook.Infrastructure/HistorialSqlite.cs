using CorreosOutlook.Application;
using Microsoft.Data.Sqlite;

namespace CorreosOutlook.Infrastructure;

public sealed class HistorialSqlite : IHistorialRepository
{
    private readonly string connectionString;
    public HistorialSqlite(string archivo)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(archivo))!);
        connectionString = new SqliteConnectionStringBuilder { DataSource = archivo, Pooling = false }.ToString();
        using var connection = Abrir();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS operaciones (
                id TEXT PRIMARY KEY, outlook_id TEXT NULL, evento TEXT NOT NULL,
                fecha_recibida TEXT NOT NULL, fecha_respondida TEXT NULL,
                idioma TEXT NOT NULL, perfil TEXT NOT NULL, tipo_solicitud TEXT NOT NULL,
                plantilla TEXT NOT NULL, estado TEXT NOT NULL, resultado TEXT NOT NULL,
                carpeta_destino TEXT NULL, intervencion_manual INTEGER NOT NULL,
                acompanantes INTEGER NOT NULL CHECK(acompanantes >= 0), preregistro_completo INTEGER NOT NULL);
            PRAGMA user_version = 1;
            """;
        command.ExecuteNonQuery();
    }
    private SqliteConnection Abrir()
    {
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        return connection;
    }
    public void Registrar(Operacion o)
    {
        using var connection = Abrir();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO operaciones VALUES ($id,$outlook,$evento,$recibida,$respondida,$idioma,$perfil,
                $tipo,$plantilla,$estado,$resultado,$carpeta,$manual,$acompanantes,$completo)
            """;
        (string Key, object? Value)[] values = [("$id", o.Id.ToString()), ("$outlook", o.OutlookId), ("$evento", o.EventoId),
            ("$recibida", o.FechaRecibida.ToString("O")), ("$respondida", o.FechaRespondida?.ToString("O")),
            ("$idioma", o.Idioma), ("$perfil", o.Perfil.ToString()), ("$tipo", o.TipoSolicitud),
            ("$plantilla", o.PlantillaId), ("$estado", o.Estado), ("$resultado", o.Resultado),
            ("$carpeta", o.CarpetaDestino), ("$manual", o.IntervencionManual),
            ("$acompanantes", o.CantidadAcompanantes), ("$completo", o.PreRegistroCompleto)];
        foreach (var (key, value) in values) command.Parameters.AddWithValue(key, value ?? DBNull.Value);
        command.ExecuteNonQuery();
    }
    public long Contar()
    {
        using var connection = Abrir();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM operaciones";
        return (long)command.ExecuteScalar()!;
    }
}
