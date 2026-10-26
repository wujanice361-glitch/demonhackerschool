using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SQLGameClass1DDetective;

public record ColumnInfo(string Name, string DataType);
public record TableInfo(string Name, List<ColumnInfo> Columns);

public partial class SchemaWindow : Window
{
    public bool AllowClose { get; set; }

    public SchemaWindow()
    {
        InitializeComponent();
    }

    /// <summary>読み取り専用ユーザーで INFORMATION_SCHEMA から一覧を取得して表示する。</summary>
    public async Task LoadAsync()
    {
        try
        {
            const string sql =
                "SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE " +
                "FROM INFORMATION_SCHEMA.COLUMNS " +
                "ORDER BY TABLE_NAME, ORDINAL_POSITION";

            var dt = await Task.Run(() => QueryRunner.Run(sql));

            var tables = dt.Rows.Cast<DataRow>()
                .GroupBy(r => (string)r["TABLE_NAME"])
                .Select(g => new TableInfo(
                    g.Key,
                    g.Select(r => new ColumnInfo((string)r["COLUMN_NAME"], (string)r["DATA_TYPE"])).ToList()))
                .ToList();

            SchemaTree.ItemsSource = tables;
            StatusText.Text = $"{tables.Count} テーブル";
        }
        catch (Exception ex)
        {
            StatusText.Text = "スキーマの取得に失敗しました: " + ex.Message;
        }
    }

    // 「×」では閉じずに隠す（アプリ終了時だけ AllowClose = true になる）
    protected override void OnClosing(CancelEventArgs e)
    {
        if (!AllowClose)
        {
            e.Cancel = true;
            Owner?.Activate();   // 先にメイン画面を前面にする
            Hide();
        }
        base.OnClosing(e);
    }
}