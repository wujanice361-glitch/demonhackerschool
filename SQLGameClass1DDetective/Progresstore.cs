using System;
using Microsoft.Data.SqlClient;

namespace SQLGameClass1DDetective;

/// <summary>クリア状況の保存と読み込み（進行管理DB。プレイヤーのSQLからは見えない）。</summary>
public static class ProgressStore
{
    /// <summary>クリアを記録する。すでにクリア済みの場合は、より良い記録のときだけ更新する。</summary>
    public static void MarkCleared(int day, int elapsedSeconds, int hintsUsed)
    {
        const string sql = @"
MERGE dbo.Progress AS t
USING (SELECT @day AS DayNo) AS s ON t.DayNo = s.DayNo
WHEN MATCHED AND (t.ElapsedSeconds IS NULL OR @sec < t.ElapsedSeconds) THEN
    UPDATE SET Cleared = 1, ClearedAt = SYSDATETIME(), ElapsedSeconds = @sec, HintsUsed = @hints
WHEN NOT MATCHED THEN
    INSERT (DayNo, Cleared, ClearedAt, ElapsedSeconds, HintsUsed)
    VALUES (@day, 1, SYSDATETIME(), @sec, @hints);";

        using var conn = new SqlConnection(DbConfig.SystemConn);
        conn.Open();
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@day", day);
        cmd.Parameters.AddWithValue("@sec", elapsedSeconds);
        cmd.Parameters.AddWithValue("@hints", hintsUsed);
        cmd.ExecuteNonQuery();
    }

    /// <summary>起動時に始める日。最後にクリアした日の次（全部クリア済みなら最終日）。</summary>
    public static int GetStartDay()
    {
        try
        {
            using var conn = new SqlConnection(DbConfig.SystemConn);
            conn.Open();
            using var cmd = new SqlCommand("SELECT ISNULL(MAX(DayNo), 0) FROM dbo.Progress WHERE Cleared = 1", conn);
            var lastCleared = Convert.ToInt32(cmd.ExecuteScalar());
            return Math.Clamp(lastCleared + 1, 1, CaseCatalog.Count);
        }
        catch
        {
            return 1; // 読めなければ1日目から
        }
    }
}