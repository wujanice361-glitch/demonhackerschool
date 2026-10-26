using SQLGameClass1DDetective;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SQLGameClass1DDetective;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 「準備中」ウィンドウを閉じてもアプリが終了しないようにする
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var splash = new Window
        {
            Title = "準備中",
            Width = 400,
            Height = 130,
            ResizeMode = ResizeMode.NoResize,
            WindowStyle = WindowStyle.ToolWindow,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Background = (Brush)FindResource("BgBrush"),
            Content = new TextBlock
            {
                Text = "データベースを準備しています...\n（初回は少し時間がかかります）",
                Foreground = (Brush)FindResource("TextBrush"),
                FontSize = 15,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            }
        };
        splash.Show();

        try
        {
            // DB作成と事件データの投入（UIが固まらないよう別スレッドで）
            await Task.Run(() =>
            {
                DbInitializer.EnsureReady();
            });
        }
        catch (Exception ex)
        {
            splash.Close();
            MessageBox.Show(ex.Message, "起動できません", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        var main = new MainWindow();
        MainWindow = main;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        main.Show();
        splash.Close();
    }
}