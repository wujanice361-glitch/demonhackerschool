using System;
using System.ComponentModel;
using System.IO;
using System.Windows;

namespace SQLGameClass1DDetective;

public partial class NotepadWindow : Window
{
    private static readonly string MemoPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SqlMysteryGame", "memo.txt");

    public bool AllowClose { get; set; }

    public NotepadWindow()
    {
        InitializeComponent();
        try
        {
            if (File.Exists(MemoPath))
                MemoBox.Text = File.ReadAllText(MemoPath);
        }
        catch { /* 読めなくてもゲームは続行 */ }
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(MemoPath)!);
            File.WriteAllText(MemoPath, MemoBox.Text);
        }
        catch { /* 保存に失敗しても無視 */ }
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        var r = MessageBox.Show(this, "メモをすべて消しますか？", "確認",
                                MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (r == MessageBoxResult.Yes) MemoBox.Clear();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        Save(); // 隠すときも本当に閉じるときも保存
        if (!AllowClose)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnClosing(e);
    }
}