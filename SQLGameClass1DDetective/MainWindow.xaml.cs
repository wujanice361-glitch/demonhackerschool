using SQLGameClass1DDetective;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace SQLGameClass1DDetective;

public partial class MainWindow : Window
{
    private const int MaxRows = 1000; // QueryRunner の MaxRows と合わせる

    // 子ウィンドウ（Owner は Window が表示された後でないと設定できないので Loaded で作る）
    private SchemaWindow _schema = null!;
    private NotepadWindow _notepad = null!;
    private MenuWindow _menu = null!;

    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };

    // null = 時間制限なし（経過時間だけ表示）。制限を付けるなら TimeSpan.FromMinutes(15) など。
    private TimeSpan? _timeLimit = null;

    private int _day = 1;
    private DateTime _startedAt = DateTime.Now;
    private int _attempts;

    //クリア後用のフィールド
    private bool _cleared;
    private TimeSpan _clearedElapsed;

    public MainWindow()
    {
        InitializeComponent();
        _timer.Tick += (_, _) => UpdateTimer();
        _timer.Start();
        UpdateTimer();
        Deactivated += (_, _) => SchemaTip.IsOpen = false;
        LocationChanged += (_, _) => SchemaTip.IsOpen = false;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _schema = new SchemaWindow { Owner = this };
        _notepad = new NotepadWindow { Owner = this };
        _menu = new MenuWindow { Owner = this };

        await LoadDayAsync(ProgressStore.GetStartDay());
    }

    private async Task LoadDayAsync(int day)
    {
        StatusText.Text = "事件ファイルを準備しています...";
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
        SetClearedState(false);
        _attempts = 0;
        _startedAt = DateTime.Now;
        _timer.Start();
        UpdateTimer();

        var info = CaseCatalog.Get(day);
        DayText.Text = $"DAY {day}";
        CaseTitle.Text = info.Title;
        SituationText.Text = info.Situation;
        ObjectiveText.Text = info.Objective;
        _menu.SetHints(info.Hints);

        AnswerBox.Clear();
        SubmitResult.Text = "";
        SqlBox.Clear();
        ResultGrid.ItemsSource = null;
        ErrorPanel.Visibility = Visibility.Collapsed;
        ResultGrid.Visibility = Visibility.Visible;
        StatusText.Text = "SQLを入力して実行してください。";

        await _schema.LoadAsync(); // 日が変わるとテーブルも変わるので、スキーマも読み直す

        //初日のみスキーマのボタンの説明
        if (day == 1 && !_schemaTipShown)
        {
            _schemaTipShown = true;
            SchemaTip.IsOpen = true;
        }
    }

    /// <summary>事件ごとの内容をここで流し込む（仮の文章）。</summary>
    private void LoadCase()
    {
        DayText.Text = "DAY 1";
        CaseTitle.Text = "（仮）事件タイトル";
        SituationText.Text =
            "（仮）ここに事件の状況説明を書きます。\n" +
            "先生が残した記録から、何が起きたのかを調べてください。";
        ObjectiveText.Text =
            "（仮）ここに課題の目的を書きます。\n" +
            "例: 事件が起きた時間帯に、教室にいた人物を特定する。";

        _menu.SetHints(new[]
        {
            "（仮）ヒント1: まずは入退室の記録を見てみよう。",
            "（仮）ヒント2: 時間帯で絞り込むと候補が減るよ。",
            "（仮）ヒント3: 別のテーブルと突き合わせてみよう。",
        });
    }

    //スキーマボタンの紹介
    private bool _schemaTipShown;

    private void SchemaTipClose_Click(object sender, RoutedEventArgs e) => SchemaTip.IsOpen = false;

    private void SchemaButton_Click(object sender, RoutedEventArgs e)
    {
        SchemaTip.IsOpen = false;
        Toggle(_schema);
    }

    // ===== 子ウィンドウの開閉 =====
    private static void Toggle(Window w)
    {
        if (w.IsVisible) { w.Hide(); }
        else { w.Show(); w.Activate(); }
    }

    private void NotepadButton_Click(object sender, RoutedEventArgs e) => Toggle(_notepad);
    private void MenuButton_Click(object sender, RoutedEventArgs e) => Toggle(_menu);

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        if (e.Cancel) return;

        // 子ウィンドウは通常「閉じる＝隠す」。アプリ終了時だけ本当に閉じられるようにする
        if (_schema != null) _schema.AllowClose = true;
        if (_notepad != null) _notepad.AllowClose = true;
        if (_menu != null) _menu.AllowClose = true;
    }

    // ===== SQL実行 =====
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

                var note = table.Rows.Count >= MaxRows ? $"  ※先頭{MaxRows}行のみ表示" : "";
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
            SubmitResult.Text = $"先生:「うーん、その答えでは説明がつかないな。もう一度、記録を見直してみなさい。」（{_attempts}回目の提出）";
            return;
        }

        await ShowClearAsync();
    }

    private async Task ShowClearAsync()
    {
        _timer.Stop();
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

    // クリア状態の切り替え（提出欄のロックと、ボタンの出し分け）
    private void SetClearedState(bool cleared)
    {
        _cleared = cleared;
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
}