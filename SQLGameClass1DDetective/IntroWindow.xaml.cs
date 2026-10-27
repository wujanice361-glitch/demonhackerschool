using System.ComponentModel;
using System.Windows;

namespace SQLGameClass1DDetective;

/// <summary>
/// ストーリーの導入（学院の紹介 → 目指してほしいこと → 自己紹介）。
/// 自己紹介はプレイヤーの分だけ。最後まで進むと DialogResult = true。
/// 入力内容は、渡した GameProfile に書き込まれる。
/// 途中で閉じようとしたときは、確認を出す（うっかり閉じるのを防ぐ）。
/// </summary>
public partial class IntroWindow : Window
{
    private const int LastPage = 2;

    private readonly GameProfile _profile;
    private readonly string _cancelNote;
    private int _page;
    private bool _completed;

    /// <param name="profile">編集対象のプロフィール</param>
    /// <param name="cancelNote">途中でやめたとき、どうなるか（確認の文章に入る）。例: 「ゲームを終了します。」</param>
    public IntroWindow(GameProfile profile, string cancelNote = "変更は保存されません。")
    {
        InitializeComponent();
        WindowHelper.FitToScreen(this);
        _profile = profile;
        _cancelNote = cancelNote;

        NameBox.Text = profile.PlayerName;
        PlayerIntroBox.Text = profile.IntroTextFor(IntroContent.PlayerKey);

        ShowPage(0);
    }

    private void ShowPage(int page)
    {
        _page = page;

        StoryPage.Visibility = page <= 1 ? Visibility.Visible : Visibility.Collapsed;
        IntroPage.Visibility = page == LastPage ? Visibility.Visible : Visibility.Collapsed;
        PageIndicator.Text = $"{page + 1} / {LastPage + 1}";
        BackButton.Visibility = page == 0 ? Visibility.Collapsed : Visibility.Visible;
        NextButton.Content = page == LastPage ? "この内容で始める" : "次へ ▶";

        switch (page)
        {
            case 0:
                PageTitle.Text = IntroContent.SchoolTitle;
                StoryText.Text = IntroContent.SchoolText;
                break;
            case 1:
                PageTitle.Text = IntroContent.GoalTitle;
                StoryText.Text = IntroContent.GoalText;
                break;
            default:
                PageTitle.Text = "自己紹介";
                NameBox.Focus();
                break;
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (_page > 0) ShowPage(_page - 1);
    }

    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
        if (_page < LastPage)
        {
            ShowPage(_page + 1);
            return;
        }
        Finish();
    }

    private void Finish()
    {
        var name = NameBox.Text.Trim();
        if (name.Length == 0)
        {
            NameError.Text = "名前を入力してください。";
            NameError.Visibility = Visibility.Visible;
            NameBox.Focus();
            return;
        }

        _profile.PlayerName = name;
        _profile.Intros[IntroContent.PlayerKey] = PlayerIntroBox.Text.Trim();
        _profile.IntroCompleted = true;

        _completed = true; // 最後まで進んだ（閉じるときの確認を出さない）
        DialogResult = true;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        if (e.Cancel || _completed) return;

        var r = MessageBox.Show(this,
            $"導入はまだ途中です。ここでやめると、{_cancelNote}\n\nやめますか？",
            "確認", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        if (r != MessageBoxResult.Yes)
            e.Cancel = true; // 閉じるのをやめて、導入を続ける
    }
}