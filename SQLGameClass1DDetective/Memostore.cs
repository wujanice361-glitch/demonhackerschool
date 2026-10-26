using System;
using System.ComponentModel;
using System.IO;

namespace SQLGameClass1DDetective;

/// <summary>メモの本文を1か所で持つ。メイン画面のメモタブと、別ウィンドウのメモ帳が同じ内容を共有する。</summary>
public sealed class MemoStore : INotifyPropertyChanged
{
    // ※ static フィールドの宣言順に注意: MemoPath を Instance より先に書く（コンストラクターで使うため）
    private static readonly string MemoPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SqlMysteryGame", "memo.txt");

    public static MemoStore Instance { get; } = new();

    private string _text = "";

    public event PropertyChangedEventHandler? PropertyChanged;

    private MemoStore()
    {
        try
        {
            if (File.Exists(MemoPath))
                _text = File.ReadAllText(MemoPath);
        }
        catch { /* 読めなくてもゲームは続行 */ }
    }

    public string Text
    {
        get => _text;
        set
        {
            if (_text == value) return;
            _text = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(MemoPath)!);
            File.WriteAllText(MemoPath, _text);
        }
        catch { /* 保存に失敗しても無視 */ }
    }
}