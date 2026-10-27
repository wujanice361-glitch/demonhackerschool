using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SQLGameClass1DDetective;

/// <summary>プレイヤーの名前と、編集した自己紹介を持つ。ファイルに保存される。</summary>
public sealed class GameProfile
{
    // ※ static フィールドの宣言順に注意: FilePath と JsonOptions を Current より先に書く（Load で使うため）
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SqlMysteryGame", "profile.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // 日本語をそのまま保存する
    };

    /// <summary>いま使っているプロフィール。</summary>
    public static GameProfile Current { get; private set; } = Load();

    public string PlayerName { get; set; } = "";
    public bool IntroCompleted { get; set; }

    /// <summary>自己紹介の文章（キー → 本文）。編集していない人はここに入らず、初期の文章が使われる。</summary>
    public Dictionary<string, string> Intros { get; set; } = new();

    /// <summary>画面に出す名前（未入力のときは「見習い」）。</summary>
    [JsonIgnore]
    public string DisplayName => string.IsNullOrWhiteSpace(PlayerName) ? "見習い" : PlayerName;

    /// <summary>導入を最後まで終えているか（名前が入っていることも条件にする）。</summary>
    [JsonIgnore]
    public bool IsIntroDone => IntroCompleted && !string.IsNullOrWhiteSpace(PlayerName);

    public string IntroTextFor(string key) =>
        Intros.TryGetValue(key, out var text) ? text : IntroContent.DefaultFor(key);

    /// <summary>まっさらなプロフィール（ゲーム全体のリセット用）。</summary>
    public static GameProfile CreateDefault() => new();

    /// <summary>編集用のコピー（導入画面で編集し、完了したときだけ Replace で反映する）。</summary>
    public GameProfile Clone() => new()
    {
        PlayerName = PlayerName,
        IntroCompleted = IntroCompleted,
        Intros = new Dictionary<string, string>(Intros),
    };

    public static void Replace(GameProfile profile)
    {
        Current = profile;
        profile.Save();
    }

    /// <summary>文章の中の {player} を、プレイヤーの名前に置き換える。</summary>
    public static string Apply(string text) => text.Replace("{player}", Current.DisplayName);

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch { /* 保存に失敗しても、ゲームは続ける */ }
    }

    private static GameProfile Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<GameProfile>(File.ReadAllText(FilePath)) ?? new();
        }
        catch { /* 壊れていたら、まっさらから始める */ }
        return new();
    }
}