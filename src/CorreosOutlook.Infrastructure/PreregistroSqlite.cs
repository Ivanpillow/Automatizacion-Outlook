using CorreosOutlook.Application;
using CorreosOutlook.Domain;
using Microsoft.Data.Sqlite;

namespace CorreosOutlook.Infrastructure;

public sealed class PreregistroSqlite : IPreregistroRepository
{
    private readonly string conexion;
    public PreregistroSqlite(string archivo)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(archivo))!);
        conexion = new SqliteConnectionStringBuilder { DataSource = archivo, Pooling = false }.ToString();
        using var db = Abrir();
        using var cmd = db.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS preregistros (
              id TEXT PRIMARY KEY, origen TEXT NOT NULL, evento TEXT NOT NULL, mensaje TEXT NOT NULL,
              recibido TEXT NOT NULL, perfil TEXT NOT NULL, estado TEXT NOT NULL, version INTEGER NOT NULL,
              huella TEXT NULL, operacion TEXT NULL, borrador TEXT NULL, respondido TEXT NULL,
              evidencia_envio TEXT NULL, incidencia TEXT NULL,
              UNIQUE(origen, evento, mensaje));
            CREATE INDEX IF NOT EXISTS ix_preregistros_estado ON preregistros(origen,evento,estado);
            """;
        cmd.ExecuteNonQuery();
    }
    private SqliteConnection Abrir() { var db = new SqliteConnection(conexion); db.Open(); return db; }
    private static void Parametros(SqliteCommand cmd, Preregistro c)
    {
        (string Key, object? Value)[] datos = [("$id", c.Id), ("$origen", c.Origen), ("$evento", c.EventoId),
            ("$mensaje", c.MensajeId), ("$recibido", c.Recibido.ToString("O")), ("$perfil", c.Perfil.ToString()),
            ("$estado", c.Estado.ToString()), ("$version", c.Version), ("$huella", c.HuellaConfirmada),
            ("$operacion", c.OperacionId), ("$borrador", c.BorradorId), ("$respondido", c.Respondido?.ToString("O")),
            ("$evidencia", c.EvidenciaEnvioId), ("$incidencia", c.CodigoIncidencia)];
        foreach (var (key, value) in datos) cmd.Parameters.AddWithValue(key, value ?? DBNull.Value);
    }
    public bool Incorporar(Preregistro caso)
    {
        using var db = Abrir(); using var cmd = db.CreateCommand();
        cmd.CommandText = """
            INSERT INTO preregistros VALUES ($id,$origen,$evento,$mensaje,$recibido,$perfil,$estado,$version,
                $huella,$operacion,$borrador,$respondido,$evidencia,$incidencia)
            ON CONFLICT(origen,evento,mensaje) DO NOTHING;
            """;
        Parametros(cmd, caso); return cmd.ExecuteNonQuery() == 1;
    }
    public bool Actualizar(Preregistro caso, int versionEsperada)
    {
        using var db = Abrir(); using var cmd = db.CreateCommand();
        cmd.CommandText = """
            UPDATE preregistros SET estado=$estado, version=version+1, huella=$huella, operacion=$operacion,
              borrador=$borrador, respondido=$respondido, evidencia_envio=$evidencia, incidencia=$incidencia
            WHERE id=$id AND origen=$origen AND evento=$evento AND version=$esperada;
            """;
        Parametros(cmd, caso); cmd.Parameters.AddWithValue("$esperada", versionEsperada);
        return cmd.ExecuteNonQuery() == 1;
    }
    public IReadOnlyList<Preregistro> Listar(string origen, string eventoId)
    {
        using var db = Abrir(); using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT * FROM preregistros WHERE origen=$origen AND evento=$evento ORDER BY recibido,id";
        cmd.Parameters.AddWithValue("$origen", origen); cmd.Parameters.AddWithValue("$evento", eventoId);
        using var reader = cmd.ExecuteReader(); var rows = new List<Preregistro>();
        while (reader.Read()) rows.Add(Leer(reader)); return rows;
    }
    public Preregistro? Obtener(string id)
    {
        using var db = Abrir(); using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT * FROM preregistros WHERE id=$id"; cmd.Parameters.AddWithValue("$id", id);
        using var reader = cmd.ExecuteReader(); return reader.Read() ? Leer(reader) : null;
    }
    private static Preregistro Leer(SqliteDataReader r)
    {
        string? Texto(int i) => r.IsDBNull(i) ? null : r.GetString(i);
        return new(r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), DateTimeOffset.Parse(r.GetString(4)),
            Enum.Parse<Perfil>(r.GetString(5)), Enum.Parse<EstadoPreregistro>(r.GetString(6)), r.GetInt32(7),
            Texto(8), Texto(9), Texto(10), Texto(11) is { } fecha ? DateTimeOffset.Parse(fecha) : null, Texto(12), Texto(13));
    }
}
