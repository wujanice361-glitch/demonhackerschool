using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SQLGameClass1DDetective;

/// <summary>サブクエスト1つ分のカード。伏せられている間は「???」と表示する。</summary>
public partial class SubQuestCard : UserControl
{
    public SubQuestInfo Info { get; }
    public bool IsRevealed { get; private set; }
    public bool IsDone { get; private set; }

    /// <summary>答えが入力されて「答える」が押された（判定は、受け取った側で行う）。</summary>
    public event Action<SubQuestCard, string>? AnswerSubmitted;

    public SubQuestCard(SubQuestInfo info)
    {
        InitializeComponent();
        Info = info;
        FormatText.Text = AnswerChecker.FormatHint(info.Kind);
        Refresh(info.RevealTable == null);
    }

    /// <summary>伏せていたカードを現す（すでに現れていれば何もしない）。</summary>
    public void Reveal()
    {
        if (IsRevealed) return;
        Refresh(true);
        StatusText.Text = "NEW";
    }

    public void MarkDone()
    {
        IsDone = true;
        StatusText.Text = "✔ 達成";
        CardBorder.BorderBrush = (Brush)FindResource("AccentBrush");
        AnswerArea.Visibility = Visibility.Collapsed;
    }

    public void ShowResult(string message) => ResultText.Text = message;

    private void Refresh(bool revealed)
    {
        IsRevealed = revealed;

        if (!revealed)
        {
            HeaderText.Text = $"{Info.No}. ???";
            PromptText.Text = "まだ何も見えていない。";
            RewardText.Visibility = Visibility.Collapsed;
            AnswerArea.Visibility = Visibility.Collapsed;
            return;
        }

        HeaderText.Text = string.IsNullOrEmpty(Info.Giver)
            ? $"{Info.No}. {Info.Label}"
            : $"{Info.No}. {Info.Label} ・ {Info.Giver}";
        PromptText.Text = GameProfile.Apply(Info.Prompt);

        if (string.IsNullOrEmpty(Info.RewardNote))
        {
            RewardText.Visibility = Visibility.Collapsed;
        }
        else
        {
            RewardText.Text = "報酬: " + Info.RewardNote;
            RewardText.Visibility = Visibility.Visible;
        }

        AnswerArea.Visibility = IsDone ? Visibility.Collapsed : Visibility.Visible;
    }

    private void Submit()
    {
        var text = AnswerBox.Text.Trim();
        if (text.Length == 0)
        {
            ShowResult("答えを入力してください。");
            return;
        }
        AnswerSubmitted?.Invoke(this, text);
    }

    private void AnswerButton_Click(object sender, RoutedEventArgs e) => Submit();

    private void AnswerBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            Submit();
        }
    }
}