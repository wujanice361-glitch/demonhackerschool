using System;
using System.IO;
using System.Windows.Media;

namespace SQLGameClass1DDetective;

/// <summary>BGMをループ再生するだけの小さなクラス。</summary>
public static class BgmPlayer
{
    private static readonly MediaPlayer Player = new() { Volume = 0.5 };
    private static bool _loaded;

    public static bool IsPlaying { get; private set; }

    static BgmPlayer()
    {
        Player.MediaEnded += (_, _) =>
        {
            Player.Position = TimeSpan.Zero;
            Player.Play();
        };
    }

    /// <summary>例: BgmPlayer.Load(Path.Combine(AppContext.BaseDirectory, "Assets", "bgm.mp3"));</summary>
    public static void Load(string path)
    {
        if (!File.Exists(path)) return;
        Player.Open(new Uri(path, UriKind.Absolute));
        _loaded = true;
    }

    public static void Play()
    {
        if (!_loaded) return;
        Player.Play();
        IsPlaying = true;
    }

    public static void Pause()
    {
        Player.Pause();
        IsPlaying = false;
    }

    public static double Volume
    {
        get => Player.Volume;
        set => Player.Volume = Math.Clamp(value, 0, 1);
    }
}