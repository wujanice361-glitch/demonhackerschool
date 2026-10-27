using System.Windows;

namespace SQLGameClass1DDetective;

/// <summary>起動時に、前回の記録があるとき「続きから」か「最初から」かを選ばせる。</summary>
public partial class StartChoiceWindow : Window
{
    /// <summary>「最初からやり直す」が確定したか。×で閉じたときは false（＝続きから）。</summary>
    public bool Restart { get; private set; }

    public StartChoiceWindow(int lastClearedDay)
    {
        InitializeComponent();
        WindowHelper.FitToScreen(this);

        var name = GameProfile.Current.PlayerName;
        var who = string.IsNullOrWhiteSpace(name) ? "" : $"「{name}」の";

        var status = lastClearedDay > 0
            ? $"DAY {lastClearedDay} までクリア済みです。"
            : "まだクリアした日はありません。DAY 1 から始めます。";

        MessageText.Text =
            $"{who}記録があります。\n{status}\n\n" +
            "続きから始めますか？\n" +
            "最初からやり直すと、クリアの記録・メモ・名前と自己紹介が消えて、導入からやり直します。";
    }

    private void ContinueButton_Click(object sender, RoutedEventArgs e)
    {
        Restart = false;
        DialogResult = true;
    }

    private void RestartButton_Click(object sender, RoutedEventArgs e)
    {
        var r = MessageBox.Show(this,
            "本当に最初からやり直しますか？\n\n" +
            "消えるもの:\n" +
            "・クリアの記録\n" +
            "・メモの内容\n" +
            "・入力した名前と、編集した自己紹介\n\n" +
            "このあと導入をもう一度見て、最後まで進むとリセットが確定します。",
            "リセットの確認", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
        if (r != MessageBoxResult.Yes) return;

        Restart = true;
        DialogResult = true;
    }
}