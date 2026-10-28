using System;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace SQLGameClass1DDetective; // ← 自分のプロジェクトの名前空間に合わせる

/// <summary>実行結果。成功なら Table、失敗なら ErrorTitle / ErrorDetail が入る。</summary>
public record QueryResult(DataTable? Table, string? ErrorTitle, string? ErrorDetail);

/// <summary>SELECT以外など、実行前に弾いた入力。</summary>
public class QueryRejectedException : Exception
{
    public QueryRejectedException(string message) : base(message) { }
}

/// <summary>プレイヤーが入力したSQLを、権限を絞った状態で実行する。</summary>
public static class QueryRunner
{
    public const int MaxRows = 1000;

    /// <summary>
    /// 画面用。例外を外に投げず、エラーは QueryResult に入れて返す。
    /// （Task.Run の中で例外が外に出ると、Visual Studio のデバッグ中にコード画面で止まってしまうため）
    /// </summary>
    public static QueryResult Execute(string sql)
    {
        try
        {
            return new QueryResult(Run(sql), null, null);
        }
        catch (QueryRejectedException ex)
        {
            return new QueryResult(null, "無効な構文です", ex.Message);
        }
        catch (SqlException ex)
        {
            return new QueryResult(null, Classify(ex), ex.Message);
        }
        catch (Exception ex)
        {
            return new QueryResult(null, "実行できませんでした", ex.Message);
        }
    }

    /// <summary>失敗すると例外を投げる版（スキーマ取得など、内部用）。</summary>
    public static DataTable Run(string sql)
    {
        if (!IsAllowed(sql, out var error))
            throw new QueryRejectedException(error);

        using var conn = new SqlConnection(DbConfig.PlayerConn);
        conn.Open();

        // 読み取り専用ユーザーとして実行する。NO REVERT なので REVERT で元の権限には戻れない。
        using (var init = new SqlCommand("EXECUTE AS USER = 'player_reader' WITH NO REVERT;", conn))
            init.ExecuteNonQuery();

        // 日本語の文字列に N を付け忘れても、正しく比較されるようにする（表示している入力欄の中身は、変えない）
        using var cmd = new SqlCommand(AddUnicodePrefix(sql), conn) { CommandTimeout = 5 };
        using var reader = cmd.ExecuteReader();

        var table = new DataTable();
        for (int i = 0; i < reader.FieldCount; i++)
        {
            // 列名が空・重複していても落ちないようにする（JOINで同じ名前の列が出るため）
            var name = reader.GetName(i);
            if (string.IsNullOrEmpty(name)) name = $"列{i + 1}";
            var baseName = name;
            int n = 2;
            while (table.Columns.Contains(name)) name = $"{baseName}_{n++}";
            table.Columns.Add(name, typeof(object));
        }

        int count = 0;
        while (count < MaxRows && reader.Read())
        {
            var values = new object[reader.FieldCount];
            reader.GetValues(values);
            table.Rows.Add(values);
            count++;
        }
        return table;
    }

    /// <summary>
    /// 文字列リテラル 'abc' を、Unicode の N'abc' に直す（すでに N が付いているものは、そのまま）。
    /// プレイヤーが N を付け忘れても、日本語がデータベースの文字の設定に左右されず、正しく比較されるようにする。
    /// コメント（-- と /* */）の中、[角括弧] や "二重引用符" で囲んだ名前は、触らない。
    /// </summary>
    internal static string AddUnicodePrefix(string sql)
    {
        var sb = new StringBuilder(sql.Length + 8);
        int i = 0;
        int n = sql.Length;

        while (i < n)
        {
            char c = sql[i];

            // 行コメント
            if (c == '-' && i + 1 < n && sql[i + 1] == '-')
            {
                int end = sql.IndexOf('\n', i);
                if (end < 0) end = n;
                sb.Append(sql, i, end - i);
                i = end;
                continue;
            }

            // ブロックコメント
            if (c == '/' && i + 1 < n && sql[i + 1] == '*')
            {
                int end = sql.IndexOf("*/", i + 2, StringComparison.Ordinal);
                end = end < 0 ? n : end + 2;
                sb.Append(sql, i, end - i);
                i = end;
                continue;
            }

            // [角括弧] や "二重引用符" で囲んだ名前
            if (c == '[' || c == '"')
            {
                char close = c == '[' ? ']' : '"';
                int end = sql.IndexOf(close, i + 1);
                end = end < 0 ? n : end + 1;
                sb.Append(sql, i, end - i);
                i = end;
                continue;
            }

            // 文字列リテラル
            if (c == '\'')
            {
                bool standaloneN = false;
                bool prevIsIdentifier = false;
                if (sb.Length > 0)
                {
                    char p = sb[sb.Length - 1];
                    prevIsIdentifier = IsIdentifierChar(p);
                    // 直前が、単独の N（単語の一部ではない N）なら、すでに Unicode リテラル
                    standaloneN = (p == 'N' || p == 'n')
                                  && (sb.Length == 1 || !IsIdentifierChar(sb[sb.Length - 2]));
                }

                if (!standaloneN)
                {
                    if (prevIsIdentifier) sb.Append(' '); // WHEN'x' のように、直前の単語とくっついているとき
                    sb.Append('N');
                }

                // リテラルの終わりまで、そのまま写す（'' は、引用符1つぶんの文字）
                sb.Append('\'');
                i++;
                while (i < n)
                {
                    if (sql[i] == '\'')
                    {
                        if (i + 1 < n && sql[i + 1] == '\'')
                        {
                            sb.Append("\'\'");
                            i += 2;
                            continue;
                        }
                        sb.Append('\'');
                        i++;
                        break;
                    }
                    sb.Append(sql[i]);
                    i++;
                }
                continue;
            }

            sb.Append(c);
            i++;
        }
        return sb.ToString();
    }

    private static bool IsIdentifierChar(char ch) =>
        char.IsLetterOrDigit(ch) || ch == '_' || ch == '@' || ch == '#' || ch == '$';

    /// <summary>SQL Serverのエラー番号から、分かりやすい見出しを決める。</summary>
    private static string Classify(SqlException ex) => ex.Number switch
    {
        102 or 105 or 156 or 170 => "無効な構文です",
        207 => "列名が見つかりません",
        208 => "テーブル名が見つかりません",
        229 or 230 or 262 or 916 => "このデータは参照できません",
        8120 or 8121 => "GROUP BY の指定が足りません",
        -2 => "時間がかかりすぎたため中断しました",
        _ => "SQLエラー",
    };

    private static bool IsAllowed(string sql, out string error)
    {
        error = "";
        var s = sql.Trim().TrimEnd(';').Trim();

        if (!Regex.IsMatch(s, @"^(SELECT|WITH)\b", RegexOptions.IgnoreCase))
        { error = "SELECT 文で始めてください。（例: SELECT * FROM People）"; return false; }

        if (s.Contains(';'))
        { error = "複数の文は実行できません。1つの SELECT 文だけを書いてください。"; return false; }

        return true;
    }
}