using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SQLGameClass1DDetective;

public partial class MainWindow : Window
{
    private const int QueryTabCount = 5;
    private readonly List<QueryPane> _panes = new();

    // 子ウィンドウ（Owner は Window が表示された後でないと設定できないので Loaded で作る）
    private SchemaWindow _schema = null!;
    private NotepadWindow _notepad = null!;
    private MenuWindow _menu = null!;

    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private DateTime _startedAt = DateTime.Now;

    // null = 時間制限なし（経過時間だけ表示）。制限を付けるなら TimeSpan.FromMinutes(15) など。
    private TimeSpan? _timeLimit = null;

    private int _day = 1;
    private int _attempts;
    private TimeSpan _clearedElapsed;
    private bool _schemaTipShown;

    public MainWindow()
    {
        InitializeComponent();
        WindowHelper.FitToScreen(this);

        // クエリのタブを5つ作る（SQL入力と結果が、タブごとに独立している）
        for (int i = 1; i <= QueryTabCount; i++)
        {
            var pane = new QueryPane();
            _panes.Add(pane);
            QueryTabs.Items.Add(new TabItem { Header = $"クエリ {i}", Content = pane });
        }

        _timer.Tick += (_, _) => UpdateTimer();
        _timer.Start();
        UpdateTimer();

        // 吹き出しが他のアプリの前面に残らないようにする
        Deactivated += (_, _) => SchemaTip.IsOpen = false;
        LocationChanged += (_, _) => SchemaTip.IsOpen = false;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _schema = new SchemaWindow { Owner = this };
        _notepad = new NotepadWindow { Owner = this };
        _menu = new MenuWindow { Owner = this };

        // メニューからの「最初からやり直す」「導入を見返す」
        _menu.ResetRequested += async () => { _menu.Hide(); await ResetGameAsync(); };
        _menu.IntroRequested += () => { _menu.Hide(); ReviewIntro(); };

        await LoadDayAsync(ProgressStore.GetStartDay());
    }

    // ===== 日の読み込み（事件データの入れ替え・画面の初期化） =====
    private async Task LoadDayAsync(int day)
    {
        try
        {
            await Task.Run(() => DbInitializer.LoadCase(day));
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "事件データを読み込めません",
                            MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        _day = day;
        _attempts = 0;
        _startedAt = DateTime.Now;
        _timer.Start();
        UpdateTimer();
        SetClearedState(false);

        var info = CaseCatalog.Get(day);
        ShowCaseTexts(info);
        _menu.SetHints(info.Hints.Select(GameProfile.Apply).ToArray());

        AnswerBox.Clear();
        SubmitResult.Text = "";
        foreach (var pane in _panes) pane.Reset();
        QueryTabs.SelectedIndex = 0;

        await _schema.LoadAsync(); // 日が変わるとテーブルも変わるので、スキーマも読み直す

        if (day == 1 && !_schemaTipShown)
        {
            _schemaTipShown = true;
            SchemaTip.IsOpen = true;
        }
    }

    // 出題の文章を画面に出す（{player} はプレイヤーの名前に置き換える）
    private void ShowCaseTexts(CaseInfo info)
    {
        DayText.Text = $"DAY {info.Day}";
        CaseTitle.Text = GameProfile.Apply(info.Title);
        SituationText.Text = GameProfile.Apply(info.Situation);
        OrderText.Text = GameProfile.Apply(info.Order);
    }

    // ===== 子ウィンドウの開閉 =====
    private static void Toggle(Window w)
    {
        // 普通に表示されているときだけ隠す
        if (w.IsVisible && w.WindowState != WindowState.Minimized)
        {
            w.Hide();
            return;
        }

        if (!w.IsVisible) w.Show();
        if (w.WindowState == WindowState.Minimized) w.WindowState = WindowState.Normal; // 元の大きさに戻す
        w.Activate();
    }

    private void SchemaButton_Click(object sender, RoutedEventArgs e)
    {
        SchemaTip.IsOpen = false;
        Toggle(_schema);
    }

    private void SchemaTipClose_Click(object sender, RoutedEventArgs e) => SchemaTip.IsOpen = false;
    private void NotepadButton_Click(object sender, RoutedEventArgs e) => Toggle(_notepad);
    private void MenuButton_Click(object sender, RoutedEventArgs e) => Toggle(_menu);

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        if (e.Cancel) return;

        MemoStore.Instance.Save();

        // 子ウィンドウは通常「閉じる＝隠す」。アプリ終了時だけ本当に閉じられるようにする
        if (_schema != null) _schema.AllowClose = true;
        if (_notepad != null) _notepad.AllowClose = true;
        if (_menu != null) _menu.AllowClose = true;
    }

    // ===== 提出 =====
    private async void SubmitButton_Click(object sender, RoutedEventArgs e)
    {
        var answer = AnswerBox.Text.Trim();
        if (answer.Length == 0)
        {
            SubmitResult.Text = "回答を入力してください。";
            return;
        }

        var r = MessageBox.Show(this, "この内容で提出しますか？\n\n" + answer, "提出の確認",
                                MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (r != MessageBoxResult.Yes) return;

        _attempts++;

        if (!AnswerChecker.IsCorrect(_day, answer))
        {
            SubmitResult.Text = $"教官:「うーん、{GameProfile.Current.DisplayName}、その答えでは説明がつかないな。もう一度、記録を見直してみなさい。」（{_attempts}回目の提出）";
            return;
        }

        await ShowClearAsync();
    }

    private async Task ShowClearAsync()
    {
        _timer.Stop(); // 時間を止める
        _clearedElapsed = DateTime.Now - _startedAt;
        var hints = _menu.HintsUsed;

        try
        {
            await Task.Run(() => ProgressStore.MarkCleared(_day, (int)_clearedElapsed.TotalSeconds, hints));
        }
        catch { /* 保存に失敗しても、ゲームは続ける */ }

        SetClearedState(true);
        SubmitResult.Text = "";
        await ShowClearDialogAsync();
    }

    private async Task ShowClearDialogAsync()
    {
        var info = CaseCatalog.Get(_day);
        var stats = $"経過時間 {(int)_clearedElapsed.TotalMinutes:00}:{_clearedElapsed.Seconds:00}   " +
                    $"提出 {_attempts} 回   ヒント {_menu.HintsUsed} 個";

        var win = new ClearWindow(info, stats, CaseCatalog.HasNext(_day)) { Owner = this };
        win.ShowDialog();

        if (win.Choice == ClearChoice.Next)
            await GoNextAsync();
    }

    // クリア状態の切り替え（提出欄のロックと、ボタンの出し分け）
    private void SetClearedState(bool cleared)
    {
        AnswerBox.IsReadOnly = cleared;
        AnswerBox.Opacity = cleared ? 0.6 : 1.0;
        SubmitButton.Visibility = cleared ? Visibility.Collapsed : Visibility.Visible;
        SubmitHint.Visibility = cleared ? Visibility.Collapsed : Visibility.Visible;
        ClearedPanel.Visibility = cleared ? Visibility.Visible : Visibility.Collapsed;

        var hasNext = CaseCatalog.HasNext(_day);
        NextButton.IsEnabled = hasNext;
        NextButton.Content = hasNext ? "次の問題へ進む ▶" : "（これが最後の問題です）";
    }

    private async Task GoNextAsync()
    {
        if (CaseCatalog.HasNext(_day))
            await LoadDayAsync(_day + 1);
    }

    private async void NextButton_Click(object sender, RoutedEventArgs e) => await GoNextAsync();

    private async void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        var r = MessageBox.Show(this,
            "この問題を最初からやり直しますか？\n（入力したSQLと使ったヒントはリセットされます。クリアの記録は残ります）",
            "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (r == MessageBoxResult.Yes)
            await LoadDayAsync(_day); // 同じ日を読み込み直す＝リセット
    }

    private async void ReviewButton_Click(object sender, RoutedEventArgs e) => await ShowClearDialogAsync();

    // ===== 導入の見返し =====
    private void ReviewIntro()
    {
        var edit = GameProfile.Current.Clone();
        var win = new IntroWindow(edit, "変更は保存されません。") { Owner = this };
        if (win.ShowDialog() == true)
        {
            GameProfile.Replace(edit);          // 名前や自己紹介の編集を保存
            _ = Task.Run(() => DbInitializer.RefreshPlayer()); // 学生名簿の自分の行も更新
            ShowCaseTexts(CaseCatalog.Get(_day)); // 名前が変わったので、出題の文章も更新
        }
    }

    // ===== ゲーム全体のリセット =====
    private async Task ResetGameAsync()
    {
        var r = MessageBox.Show(this,
            "ゲーム全体をリセットして、最初からやり直しますか？\n\n" +
            "消えるもの:\n" +
            "・クリアの記録\n" +
            "・メモの内容\n" +
            "・入力した名前と、編集した自己紹介\n\n" +
            "この操作は取り消せません。",
            "リセットの確認", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
        if (r != MessageBoxResult.Yes) return;

        // 導入をやり直す。最後まで進んだときだけ、リセットを確定する（途中で閉じたら、何も消さない）
        var fresh = GameProfile.CreateDefault();
        var intro = new IntroWindow(fresh, "リセットは行われません。") { Owner = this };
        if (intro.ShowDialog() != true) return;

        try
        {
            await GameReset.CommitAsync(fresh);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "リセットに失敗しました。\n\n" + ex.Message, "エラー",
                            MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        _schemaTipShown = false; // 1日目のチュートリアルの吹き出しを、もう一度出す
        await LoadDayAsync(1);
    }

    // ===== 経過時間 / 残り時間 =====
    private void UpdateTimer()
    {
        var elapsed = DateTime.Now - _startedAt;

        if (_timeLimit is { } limit)
        {
            var rest = limit - elapsed;
            TimerText.Text = rest > TimeSpan.Zero
                ? $"残り {(int)rest.TotalMinutes:00}:{rest.Seconds:00}"
                : "時間切れ";
        }
        else
        {
            TimerText.Text = $"経過 {(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}";
        }
    }
}