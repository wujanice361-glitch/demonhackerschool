using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

    // ===== いまの事件の状態 =====
    private int _day = 1;
    private CaseInfo _info = null!;
    private int _questionIndex;          // いま何問目か（0始まり）
    private int _attempts;               // 提出した回数（全設問の合計）
    private int _hintsUsed;              // 使ったヒントの数
    private bool _hintShown;             // いまの設問のヒントを開いたか
    private bool _isCleared;
    private TimeSpan _clearedElapsed;
    private bool _schemaTipShown;

    private readonly ObservableCollection<string> _answered = new();
    private readonly List<SubQuestCard> _cards = new();
    private readonly HashSet<int> _doneQuests = new();

    public MainWindow()
    {
        InitializeComponent();
        WindowHelper.FitToScreen(this);

        AnsweredList.ItemsSource = _answered;

        // クエリのタブを5つ作る（SQL入力と結果が、タブごとに独立している）
        for (int i = 1; i <= QueryTabCount; i++)
        {
            var pane = new QueryPane();
            pane.QuerySucceeded += OnQuerySucceeded;
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
        _info = CaseCatalog.Get(day);
        _attempts = 0;
        _hintsUsed = 0;
        _questionIndex = 0;
        _isCleared = false;
        _startedAt = DateTime.Now;
        _timer.Start();
        UpdateTimer();

        ShowCaseTexts(_info);

        _answered.Clear();
        _doneQuests.Clear();
        BuildSubQuestCards();
        SetClearedState(false);
        ShowQuestion();

        foreach (var pane in _panes) pane.Reset();
        QueryTabs.SelectedIndex = 0;

        await _schema.LoadAsync(); // 日が変わるとテーブルも変わるので、スキーマも読み直す

        if (day == 1 && !_schemaTipShown)
        {
            _schemaTipShown = true;
            ShowSchemaTip("まずここをチェック！",
                "ここから、テーブルの構造（どんな表があって、どんな列があるか）を見られます。捜査の前に確認しましょう。");
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

    private void ShowSchemaTip(string title, string body)
    {
        SchemaTipTitle.Text = title;
        SchemaTipBody.Text = body;
        SchemaTip.IsOpen = true;
    }

    // ===== 設問 =====
    private void ShowQuestion()
    {
        var q = _info.Questions[_questionIndex];

        QuestionNumber.Text = $"設問 {_questionIndex + 1} / {_info.Questions.Length}";
        QuestionText.Text = GameProfile.Apply(q.Prompt);
        AnswerFormat.Text = AnswerChecker.FormatHint(q.Kind);

        AnswerBox.Clear();
        SubmitResult.Text = "";
        HintText.Text = "";
        HintText.Visibility = Visibility.Collapsed;
        HintButton.IsEnabled = true;
        _hintShown = false;
    }

    private void HintButton_Click(object sender, RoutedEventArgs e)
    {
        var q = _info.Questions[_questionIndex];

        if (!_hintShown)
        {
            _hintShown = true;
            _hintsUsed++;
        }
        HintText.Text = "ヒント: " + GameProfile.Apply(q.Hint);
        HintText.Visibility = Visibility.Visible;
        HintButton.IsEnabled = false;
    }

    private void AnswerBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            SubmitButton_Click(sender, e);
        }
    }

    private async void SubmitButton_Click(object sender, RoutedEventArgs e)
    {
        var answer = AnswerBox.Text.Trim();
        if (answer.Length == 0)
        {
            SubmitResult.Text = "回答を入力してください。";
            return;
        }

        _attempts++;
        var q = _info.Questions[_questionIndex];

        if (!AnswerChecker.IsCorrect(q.Accepted, answer, q.Kind))
        {
            SubmitResult.Text =
                $"教官:「うーん、{GameProfile.Current.DisplayName}、その答えでは説明がつかないな。" +
                $"もう一度、記録を見直してみなさい。」（{_attempts}回目の提出）";
            return;
        }

        _answered.Add($"✔ 設問 {_questionIndex + 1}: {answer}");
        _questionIndex++;

        if (_questionIndex < _info.Questions.Length)
        {
            ShowQuestion();
            SubmitResult.Text = "教官:「…そうだ。次の設問だ。」";
            return;
        }

        await ShowClearAsync();
    }

    // ===== サブクエスト =====
    private void BuildSubQuestCards()
    {
        SubQuestList.Children.Clear();
        _cards.Clear();

        foreach (var sq in _info.SubQuests)
        {
            var card = new SubQuestCard(sq);
            card.AnswerSubmitted += OnSubQuestAnswered;
            _cards.Add(card);
            SubQuestList.Children.Add(card);
        }
        UpdateSubQuestHeader();
    }

    private void UpdateSubQuestHeader()
    {
        SubQuestTab.Header = $"サブクエスト {_doneQuests.Count}/{_info.SubQuests.Length}";
    }

    // SQLが成功したとき、伏せているサブクエストを現す条件（指定のテーブルを検索した）に当てはまるか調べる
    private void OnQuerySucceeded(string sql)
    {
        foreach (var card in _cards)
        {
            if (card.IsRevealed || card.Info.RevealTable is null) continue;

            if (sql.Contains(card.Info.RevealTable, StringComparison.OrdinalIgnoreCase))
                card.Reveal();
        }
    }

    private async void OnSubQuestAnswered(SubQuestCard card, string answer)
    {
        if (card.IsDone) return;

        var sq = card.Info;
        if (!AnswerChecker.IsCorrect(sq.Accepted, answer, sq.Kind))
        {
            card.ShowResult("…違うようだ。");
            return;
        }

        card.MarkDone();
        _doneQuests.Add(sq.No);
        UpdateSubQuestHeader();
        card.ShowResult("");

        // 報酬: テーブルの解放
        if (sq.UnlockScript != null)
        {
            try
            {
                var script = sq.UnlockScript;
                await Task.Run(() => DbInitializer.RunUnlock(script));
                await _schema.LoadAsync();

                var message = sq.UnlockMessage ?? "新しいテーブルが解放されました。";
                card.ShowResult(message);
                ShowSchemaTip("テーブルが解放されました", message);
            }
            catch (Exception ex)
            {
                card.ShowResult("解放に失敗しました: " + ex.Message);
            }
        }

        // 事件解決のあとに達成した分も、記録に反映する
        if (_isCleared) SaveClearRecord();
    }

    // ===== クリア =====
    private async Task ShowClearAsync()
    {
        _timer.Stop(); // 時間を止める
        _clearedElapsed = DateTime.Now - _startedAt;
        _isCleared = true;

        await Task.Run(SaveClearRecord);

        SetClearedState(true);
        SubmitResult.Text = "";
        await ShowClearDialogAsync();
    }

    private void SaveClearRecord()
    {
        try
        {
            ProgressStore.MarkCleared(_day, (int)_clearedElapsed.TotalSeconds, _hintsUsed, _doneQuests.Count);
        }
        catch { /* 保存に失敗しても、ゲームは続ける */ }
    }

    private async Task ShowClearDialogAsync()
    {
        var stats =
            $"経過時間 {(int)_clearedElapsed.TotalMinutes:00}:{_clearedElapsed.Seconds:00}   " +
            $"提出 {_attempts} 回   ヒント {_hintsUsed} 個   " +
            $"サブクエスト {_doneQuests.Count}/{_info.SubQuests.Length}";

        var extra = CaseCatalog.BuildSubQuestComment(_info, _doneQuests);

        var win = new ClearWindow(_info, stats, CaseCatalog.HasNext(_day), extra) { Owner = this };
        win.ShowDialog();

        if (win.Choice == ClearChoice.Next)
            await GoNextAsync();
    }

    // クリア状態の切り替え（設問欄と、クリア後のボタンの出し分け）
    private void SetClearedState(bool cleared)
    {
        QuestionPanel.Visibility = cleared ? Visibility.Collapsed : Visibility.Visible;
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
            "この問題を最初からやり直しますか？\n（入力したSQL、解いた設問、サブクエストの達成はリセットされます。クリアの記録は残ります）",
            "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (r == MessageBoxResult.Yes)
            await LoadDayAsync(_day); // 同じ日を読み込み直す＝リセット
    }

    private async void ReviewButton_Click(object sender, RoutedEventArgs e) => await ShowClearDialogAsync();

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

    // ===== 導入の見返し =====
    private void ReviewIntro()
    {
        var edit = GameProfile.Current.Clone();
        var win = new IntroWindow(edit, "変更は保存されません。") { Owner = this };
        if (win.ShowDialog() == true)
        {
            GameProfile.Replace(edit);          // 名前や自己紹介の編集を保存
            _ = Task.Run(() => DbInitializer.RefreshPlayer()); // 学生名簿の自分の名前も更新
            ShowCaseTexts(_info);               // 名前が変わったので、出題の文章も更新
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