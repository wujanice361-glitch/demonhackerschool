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

        // 「準備中」や導入の画面を閉じてもアプリが終了しないようにする
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

        int lastCleared;
        try
        {
            // DBの作成（事件データは、MainWindow が日ごとに入れる）
            await Task.Run(() => DbInitializer.EnsureReady());
            lastCleared = await Task.Run(() => ProgressStore.GetLastClearedDay());
        }
        catch (Exception ex)
        {
            splash.Close();
            MessageBox.Show(ex.Message, "起動できません", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        splash.Close();

        // 前回の記録（クリア済みの日）があるときだけ、「続きから」か「最初から」かを聞く。
        // 記録がなければ聞かずに、そのまま始める。
        GameProfile? fresh = null;
        if (lastCleared > 0)
        {
            var choice = new StartChoiceWindow(lastCleared);
            choice.ShowDialog();
            if (choice.Restart)
                fresh = GameProfile.CreateDefault();
        }

        if (fresh != null)
        {
            // 最初からやり直す: 導入を最初から見せて、最後まで進んだときだけ、リセットを確定する
            var intro = new IntroWindow(fresh, "ゲームを終了します。（リセットは行われません）");
            if (intro.ShowDialog() != true)
            {
                Shutdown();
                return;
            }

            try
            {
                await GameReset.CommitAsync(fresh);
            }
            catch (Exception ex)
            {
                MessageBox.Show("リセットに失敗しました。\n\n" + ex.Message, "エラー",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }
        }
        else if (!GameProfile.Current.IsIntroDone)
        {
            // 初回（または導入を最後まで終えていない）: 導入を見せる
            var edit = GameProfile.Current.Clone();
            var intro = new IntroWindow(edit, "ゲームを終了します。（次に起動したとき、導入は最初から始まります）");
            if (intro.ShowDialog() != true)
            {
                Shutdown(); // 導入の途中でやめたら、終了する。見たことにはならない
                return;
            }
            GameProfile.Replace(edit);
        }

        var main = new MainWindow();
        MainWindow = main;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        main.Show();
    }
}