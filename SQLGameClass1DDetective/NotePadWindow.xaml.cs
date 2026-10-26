using System.ComponentModel;
using System.Windows;

namespace SQLGameClass1DDetective;

public partial class NotepadWindow : Window
{
    public bool AllowClose { get; set; }

    public NotepadWindow()
    {
        InitializeComponent();
        WindowHelper.FitToScreen(this);
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        var r = MessageBox.Show(this, "メモをすべて消しますか？", "確認",
                                MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (r == MessageBoxResult.Yes) MemoStore.Instance.Text = "";
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        MemoStore.Instance.Save(); // 隠すときも本当に閉じるときも保存
        if (!AllowClose)
        {
            e.Cancel = true;
            Owner?.Activate();
            Hide();
        }
        base.OnClosing(e);
    }
}