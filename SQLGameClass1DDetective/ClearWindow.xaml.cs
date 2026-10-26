using System.Windows;

namespace SQLGameClass1DDetective;

public enum ClearChoice { Stay, Next }

public partial class ClearWindow : Window
{
    /// <summary>プレイヤーの選択。×で閉じた場合は Stay（その場に残る）。</summary>
    public ClearChoice Choice { get; private set; } = ClearChoice.Stay;

    public ClearWindow(CaseInfo info, string stats, bool hasNext)
    {
        InitializeComponent();

        TitleText.Text = $"DAY {info.Day}  {info.Title}";
        ExplanationText.Text = info.Explanation;
        StatsText.Text = stats;

        if (!hasNext)
        {
            NextButton.Content = "（これが最後の事件です）";
            NextButton.IsEnabled = false;
        }
    }

    private void StayButton_Click(object sender, RoutedEventArgs e)
    {
        Choice = ClearChoice.Stay;
        DialogResult = true;
    }

    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
        Choice = ClearChoice.Next;
        DialogResult = true;
    }
}