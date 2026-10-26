using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SQLGameClass1DDetective;

/// <summary>SQL入力と結果表示の1セット。タブの数だけ並べて使う。</summary>
public partial class QueryPane : UserControl
{
    public QueryPane()
    {
        InitializeComponent();
    }

    /// <summary>入力と結果を空に戻す（日の切り替えや、やり直しのとき）。</summary>
    public void Reset()
    {
        SqlBox.Clear();
        ResultGrid.ItemsSource = null;
        ErrorPanel.Visibility = Visibility.Collapsed;
        ResultGrid.Visibility = Visibility.Visible;
        StatusText.Text = "SQLを入力して実行してください。";
    }

    private void RunButton_Click(object sender, RoutedEventArgs e) => _ = RunQueryAsync();

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        SqlBox.Clear();
        SqlBox.Focus();
    }

    private void SqlBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        bool ctrlEnter = e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control;
        if (ctrlEnter || e.Key == Key.F5)
        {
            e.Handled = true;
            _ = RunQueryAsync();
        }
    }

    private async Task RunQueryAsync()
    {
        var sql = SqlBox.Text;
        if (string.IsNullOrWhiteSpace(sql))
        {
            StatusText.Text = "SQLを入力してください。";
            return;
        }

        RunButton.IsEnabled = false;
        StatusText.Text = "実行中...";
        var sw = Stopwatch.StartNew();

        try
        {
            var result = await Task.Run(() => QueryRunner.Execute(sql));

            if (result.Table is { } table)
            {
                ErrorPanel.Visibility = Visibility.Collapsed;
                ResultGrid.Visibility = Visibility.Visible;
                ResultGrid.ItemsSource = table.DefaultView;

                var note = table.Rows.Count >= QueryRunner.MaxRows
                    ? $"  ※先頭{QueryRunner.MaxRows}行のみ表示" : "";
                StatusText.Text = $"{table.Rows.Count} 行  ({sw.ElapsedMilliseconds} ms){note}";
            }
            else
            {
                ResultGrid.ItemsSource = null;
                ResultGrid.Visibility = Visibility.Collapsed;
                ErrorPanel.Visibility = Visibility.Visible;
                ErrorTitle.Text = result.ErrorTitle;
                ErrorDetail.Text = result.ErrorDetail;
                StatusText.Text = "エラー";
            }
        }
        finally
        {
            RunButton.IsEnabled = true;
        }
    }

    // 列名の「_」はWPFでアクセスキー扱いされて消えるので、2つにして表示させる
    private void ResultGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        if (e.Column.Header is string h)
            e.Column.Header = h.Replace("_", "__");
    }
}