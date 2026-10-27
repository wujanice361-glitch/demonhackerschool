using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace SQLGameClass1DDetective;

public partial class MenuWindow : Window
{
    private List<string> _hints = new();
    private readonly ObservableCollection<string> _shown = new();

    public bool AllowClose { get; set; }

    /// <summary>使ったヒントの数。</summary>
    public int HintsUsed => _shown.Count;

    /// <summary>「ゲームを最初からやり直す」が押された。</summary>
    public event Action? ResetRequested;

    /// <summary>「導入を見返す」が押された。</summary>
    public event Action? IntroRequested;

    public MenuWindow()
    {
        InitializeComponent();
        WindowHelper.FitToScreen(this);
        HintList.ItemsSource = _shown;
        VolumeSlider.Value = BgmPlayer.Volume;
        UpdateBgmUi();
        UpdateHintUi();
    }

    // ===== ヒント =====
    /// <summary>事件ごとのヒントを設定する（見せるのは1つずつ）。</summary>
    public void SetHints(IEnumerable<string> hints)
    {
        _hints = hints.ToList();
        _shown.Clear();
        UpdateHintUi();
    }

    private void HintButton_Click(object sender, RoutedEventArgs e)
    {
        if (_shown.Count < _hints.Count)
            _shown.Add(_hints[_shown.Count]);
        UpdateHintUi();
    }

    private void UpdateHintUi()
    {
        HintCount.Text = $"{_shown.Count} / {_hints.Count}";
        HintButton.IsEnabled = _shown.Count < _hints.Count;
    }

    // ===== BGM =====
    private void BgmToggle_Click(object sender, RoutedEventArgs e)
    {
        if (BgmPlayer.IsPlaying) BgmPlayer.Pause();
        else BgmPlayer.Play();
        UpdateBgmUi();
    }

    private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        BgmPlayer.Volume = e.NewValue;
        if (VolumeText != null) VolumeText.Text = $"{(int)(e.NewValue * 100)}%";
    }

    private void UpdateBgmUi()
    {
        BgmToggle.Content = BgmPlayer.IsPlaying ? "⏸ 一時停止" : "▶ 再生";
    }

    // ===== 導入・リセット =====
    private void IntroButton_Click(object sender, RoutedEventArgs e) => IntroRequested?.Invoke();

    private void ResetButton_Click(object sender, RoutedEventArgs e) => ResetRequested?.Invoke();

    // ===== 終了 =====
    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        var r = MessageBox.Show(this, "ゲームを終了しますか？", "確認",
                                MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (r == MessageBoxResult.Yes)
        {
            Application.Current.MainWindow?.Close(); // MainWindow.OnClosing で子ウィンドウも閉じられる
        }
        else
        {
            Owner?.Activate();
            Activate();
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!AllowClose)
        {
            e.Cancel = true;
            Owner?.Activate(); // 先にメイン画面を前面にする
            Hide();
        }
        base.OnClosing(e);
    }
}