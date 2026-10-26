using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace SQLGameClass1DDetective;

/// <summary>接続文字列をここにまとめる。</summary>
public static class DbConfig
{
    public const string GameDb = "SchoolCase";          // プレイヤーが調べるDB（事件データ）
    public const string SystemDb = "SchoolCaseAnswer";  // 進行状況など（プレイヤーのSQLからは見えない）

    private const string Server = "(localdb)\\MSSQLLocalDB";

    public static string Master => Build("master");
    public static string GameAdminConn => Build(GameDb);              // 事件データの投入用（管理者権限）
    public static string SystemConn => Build(SystemDb);               // 正解判定・進行保存用
    public static string PlayerConn => Build(GameDb, pooling: false); // プレイヤーのSQL実行用（実行時に権限を絞る）

    private static string Build(string db, bool pooling = true) =>
        $"Server={Server};Database={db};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30;Pooling={pooling};";
}

/// <summary>起動時にDBを自動で用意する。利用者は何も設定しなくてよい。</summary>
public static class DbInitializer
{
    // 事件データを入れ替える前に、既存テーブルを全部消すSQL
    private const string DropAllTablesSql = @"
DECLARE @sql nvarchar(max) = N'';
SELECT @sql += N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name)
             + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';'
FROM sys.foreign_keys fk JOIN sys.tables t ON fk.parent_object_id = t.object_id;
SELECT @sql += N'DROP TABLE ' + QUOTENAME(SCHEMA_NAME(schema_id)) + N'.' + QUOTENAME(name) + N';'
FROM sys.tables;
EXEC sp_executesql @sql;";

    /// <summary>DB・読み取り専用ユーザー・進行管理テーブルを（なければ）作る。何度実行しても安全。</summary>
    public static void EnsureReady()
    {
        SqlConnection conn;
        try
        {
            conn = new SqlConnection(DbConfig.Master);
            conn.Open();
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException(
                "SQL Server LocalDB に接続できませんでした。\n\n" +
                "Visual Studio Installer を開き、「個別のコンポーネント」から\n" +
                "「SQL Server Express LocalDB」を追加して、もう一度実行してください。\n\n" +
                "（詳細: " + ex.Message + "）", ex);
        }

        using (conn)
        {
            RunScript(conn, ReadResource("Db.00_setup.sql"));
        }
    }

    /// <summary>指定した日の事件データに入れ替える（前の日のデータは消える＝先の日のネタバレも見えない）。</summary>
    public static void LoadCase(int day)
    {
        using var conn = new SqlConnection(DbConfig.GameAdminConn);
        conn.Open();

        using (var drop = new SqlCommand(DropAllTablesSql, conn) { CommandTimeout = 60 })
            drop.ExecuteNonQuery();

        RunScript(conn, ReadResource($"Cases.Day{day:00}.sql"));
    }

    // ===== 内部処理 =====

    /// <summary>「GO」で区切られたスクリプトを1バッチずつ実行する（SqlClientはGOを理解しないため）。</summary>
    private static void RunScript(SqlConnection conn, string script)
    {
        var batches = Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        foreach (var batch in batches)
        {
            if (string.IsNullOrWhiteSpace(batch)) continue;
            using var cmd = new SqlCommand(batch, conn) { CommandTimeout = 60 };
            cmd.ExecuteNonQuery();
        }
    }

    /// <summary>埋め込みリソースのSQLを読む（プロジェクト名が変わっても動くよう、末尾一致で探す）。</summary>
    private static string ReadResource(string suffix)
    {
        var asm = Assembly.GetExecutingAssembly();
        var name = asm.GetManifestResourceNames()
                      .FirstOrDefault(n => n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                   ?? throw new FileNotFoundException(
                       "埋め込みSQLが見つかりません: " + suffix +
                       "\n（.sql ファイルのビルドアクションが「埋め込みリソース」か確認してください）");

        using var stream = asm.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}